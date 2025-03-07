from mlagents_envs.environment import UnityEnvironment
from mlagents_envs.side_channel.engine_configuration_channel import EngineConfigurationChannel
from mlagents_envs.side_channel.environment_parameters_channel import EnvironmentParametersChannel

import numpy as np
from copy import deepcopy
import os
import cv2
from collections import namedtuple
from yaml_operations import load_yaml
from collections import deque
import itertools
from LazyFrames import LazyFrames



def int2action_index(x, action_dim_list):
    """
    input: [0,1,2,3,4,5,6,7,8,9,10,11], [3, 2, 2]
    output:
        [[0 0 0]
        [0 0 1]
        [0 1 0]
        [0 1 1]
        [1 0 0]
        [1 0 1]
        [1 1 0]
        [1 1 1]
        [2 0 0]
        [2 0 1]
        [2 1 0]
        [2 1 1]]
    """
    x = np.array(x)
    index_list = list(itertools.product(*[list(range(l)) for l in action_dim_list]))
    return np.asarray(index_list)[x]

SingleAgentEnvArgs = namedtuple('SingleAgentEnvArgs',
                                [
                                    's_dim',
                                    'visual_sources',
                                    'visual_resolutions',
                                    'a_dim',
                                    'is_continuous',
                                    'n_agents'
                                ])

MultiAgentEnvArgs = namedtuple('MultiAgentEnvArgs',
                               SingleAgentEnvArgs._fields + ('group_controls',))


class UnityWrapper(object):

    def __init__(self, env_args):
        self.engine_configuration_channel = EngineConfigurationChannel()
        if env_args['train_mode']:
            self.engine_configuration_channel.set_configuration_parameters(time_scale=env_args['train_time_scale'])
        else:
            self.engine_configuration_channel.set_configuration_parameters(width=env_args['width'],
                                                                           height=env_args['height'],
                                                                           quality_level=env_args['quality_level'],
                                                                           time_scale=env_args['inference_time_scale'],
                                                                           frame_stack=env_args['frame_stack'])
        self.float_properties_channel = EnvironmentParametersChannel()
        if env_args['file_path'] is None:
            self._env = UnityEnvironment(base_port=5004,
                                         seed=env_args['env_seed'],
                                         side_channels=[self.engine_configuration_channel, self.float_properties_channel])
        else:
            #unity_env_dict = load_yaml(os.path.dirname(__file__) + '/../../unity_env_dict.yaml')
            self._env = UnityEnvironment(file_name=env_args['file_path'],
                                         base_port=env_args['port'],
                                         no_graphics=not env_args['render'],
                                         seed=env_args['env_seed']#,
                                         #side_channels=[self.engine_configuration_channel, self.float_properties_channel]#,
                                         #additional_args=[
                                         #    '--scene', str(unity_env_dict.get(env_args.get('env_name', 'Roller'), 'None')),
                                         #    '--n_agents', str(env_args.get('env_num', 1))
            #])
                                         )
        self.reset_config = env_args['reset_config']

    def reset(self, **kwargs):
        reset_config = kwargs.get('reset_config', None) or self.reset_config
        for k, v in reset_config.items():
            self.float_properties_channel.set_float_parameter(k, v)
        self._env.reset()

    def __getattr__(self, name):
        if name.startswith('_'):
            raise AttributeError("attempted to get missing private attribute '{}'".format(name))
        return getattr(self._env, name)


class BasicWrapper:
    def __init__(self, env: UnityWrapper):
        self._env = env
        self._env.reset()

    def __getattr__(self, name):
        if name.startswith('_'):
            raise AttributeError("attempted to get missing private attribute '{}'".format(name))
        return getattr(self._env, name)


class InfoWrapper(BasicWrapper):
    def __init__(self, env, env_args):
        super().__init__(env)
        self._env.step()    # NOTE: 在一些图像输入的场景，如果初始化时不执行这条指令，那么将不能获取正确的场景智能体数量
        self.resize = env_args['resize']

        self.group_names = list(self._env.behavior_specs.keys())  # 所有脑的名字列表
        self.fixed_group_names = list(map(lambda x: x.replace('?', '_'), self.group_names))
        self.group_specs = [self._env.behavior_specs[g] for g in self.group_names]  # 所有脑的信息
        self.vector_idxs = [[i for i, g in enumerate(spec.observation_shapes) if len(g) == 1] for spec in self.group_specs]   # 得到所有脑 观测值为向量的下标
        self.vector_dims = [[g[0] for g in spec.observation_shapes if len(g) == 1] for spec in self.group_specs]  # 得到所有脑 观测值为向量的维度
        self.visual_idxs = [[i for i, g in enumerate(spec.observation_shapes) if len(g) == 3] for spec in self.group_specs]   # 得到所有脑 观测值为图像的下标
        self.group_num = len(self.group_names)
        #print(self.vector_dims, self.visual_idxs, self.vector_idxs, "spec", self.group_specs, self.group_num)
        self.visual_sources = [len(v) for v in self.visual_idxs]
        self.visual_resolutions = []
        stack_visual_nums = env_args['frame_stack'] if env_args['frame_stack'] > 1 else 1
        for spec in self.group_specs:
            for g in spec.observation_shapes:
                if len(g) == 3:
                    self.visual_resolutions.append(
                        list(self.resize) + [list(g)[-1] * stack_visual_nums])
                    break
            else:
                self.visual_resolutions.append([])

        self.s_dim = [sum(v) for v in self.vector_dims]
        self.a_dim = [int(np.asarray(spec.action_shape).prod()) for spec in self.group_specs]
        self.discrete_action_dim_list = [spec.action_shape for spec in self.group_specs]
        self.a_size = [spec.action_size for spec in self.group_specs]
        self.is_continuous = [spec.is_action_continuous() for spec in self.group_specs]
        #print(self.discrete_action_dim_list[0])
        self.group_agents = self.get_real_agent_numbers()  # 得到每个环境控制几个智能体
        if all('#' in name for name in self.group_names):
            # use for multi-agents
            print("Multi-Agent-EachAgent")
            self.group_controls = list(map(lambda x: int(x.split('#')[0]), self.group_names))
            self.env_copys = self.group_agents[0] // self.group_controls[0]
            self.EnvSpec = MultiAgentEnvArgs(
                s_dim=self.s_dim,
                a_dim=self.a_dim,
                visual_sources=self.visual_sources,
                visual_resolutions=self.visual_resolutions,
                is_continuous=self.is_continuous,
                n_agents=self.group_agents,
                group_controls=self.group_controls
            )
        else:
            print("Single-Agent-EachAgent")
            self.EnvSpec = [
                SingleAgentEnvArgs(
                    s_dim=self.s_dim[i],
                    a_dim=self.a_dim[i],
                    visual_sources=self.visual_sources[i],
                    visual_resolutions=self.visual_resolutions[i],
                    is_continuous=self.is_continuous[i],
                    n_agents=self.group_agents[i]
                ) for i in range(self.group_num)]

    def random_action(self):
        '''
        choose random action for each group and each agent.
        continuous: [-1, 1]
        discrete: [0-max, 0-max, ...] i.e. action dim = [2, 3] => action range from [0, 0] to [1, 2].
        '''
        actions = []
        for i in range(self.group_num):
            if self.is_continuous[i]:
                actions.append(np.random.random((self.group_agents[i], self.a_dim[i])) * 2 - 1)  # [-1, 1]
            else:
                actions.append(np.random.randint(self.a_dim[i], size=(self.group_agents[i],), dtype=np.int32))
        return actions

    def get_real_agent_numbers(self):
        group_agents = [0] * self.group_num
        for _ in range(10):  # 10 step
            for i, gn in enumerate(self.group_names):
                d, t = self._env.get_steps(gn)
                group_agents[i] = max(group_agents[i], len(d.agent_id))
                group_spec = self.group_specs[i]
                if group_spec.is_action_continuous():
                    action = np.random.randn(len(d), group_spec.action_size)
                else:
                    branch_size = group_spec.discrete_action_branches
                    action = np.column_stack([
                        np.random.randint(0, branch_size[j], size=(len(d)))
                        for j in range(len(branch_size))
                    ])
                self._env.set_actions(gn, action)
            self._env.step()
        return group_agents


class UnityReturnWrapper(BasicWrapper):
    def __init__(self, env):
        super().__init__(env)

    def reset(self, **kwargs):
        self._env.reset(**kwargs)
        return self.get_obs()

    def step(self, actions):
        for k, v in actions.items():
            self._env.set_actions(k, v)
        self._env.step()
        return self.get_obs()

    def get_obs(self):
        '''
        解析环境反馈的信息，将反馈信息分为四部分：向量、图像、奖励、done信号
        '''
        vector = []
        visual = []
        reward = []
        done = []
        info = []
        for i, gn in enumerate(self.group_names):
            vec, vis, r, d, ifo = self.coordinate_information(i, gn)
            vector.append(vec)
            visual.append(vis)
            reward.append(r)
            done.append(d)
            info.append(ifo)
        return (vector, visual, reward, done, info)

    def coordinate_information(self, i, gn):
        '''
        TODO: Annotation
        '''
        n = self.group_agents[i]
        d, t = self._env.get_steps(gn)
        ps = [t]

        if len(d) != 0 and len(d) != n:
            raise ValueError(f'agents number error. Expected 0 or {n}, received {len(d)}')

        while len(d) != n:
            self._env.step()
            d, t = self._env.get_steps(gn)
            ps.append(t)

        obs, reward = d.obs, d.reward
        done = np.full(n, False)
        info = dict(max_step=np.full(n, False), real_done=np.full(n, False))

        for t in ps:    # TODO: 有待优化
            if len(t) != 0:
                info['max_step'][t.agent_id] = t.interrupted
                info['real_done'][t.agent_id[~t.interrupted]] = True  # 去掉因为max_step而done的，只记录因为失败/成功而done的
                reward[t.agent_id] = t.reward
                done[t.agent_id] = True
                for _obs, _tobs in zip(obs, t.obs):
                    _obs[t.agent_id] = _tobs

        return (self.deal_vector(n, [obs[vi] for vi in self.vector_idxs[i]]),
                self.deal_visual(n, [obs[vi] for vi in self.visual_idxs[i]]),
                np.asarray(reward),
                np.asarray(done),
                info)

    def deal_vector(self, n, vecs):
        '''
        把向量观测信息 按每个智能体 拼接起来
        '''
        if len(vecs):
            return np.hstack(vecs)
        else:
            return np.array([]).reshape(n, -1)

    def deal_visual(self, n, viss):
        '''
        viss : [camera1, camera2, camera3, ...]
        把图像观测信息 按每个智能体 组合起来
        '''
        ss = []
        for j in range(n):
            s = []
            for v in viss:
                s.append(self.resize_image(v[j]))
            ss.append(np.array(s))  # [agent1(camera1, camera2, camera3, ...), ...]
        return np.array(ss, dtype=np.uint8)  # [B, N, (H, W, C)]

    def resize_image(self, image):
        image = cv2.resize(image, tuple(self.resize), interpolation=cv2.INTER_AREA).reshape(list(self.resize) + [-1])
        image *= 255
        return image


class ActionWrapper(BasicWrapper):

    def __init__(self, env):
        super().__init__(env)

    def step(self, actions):
        actions = deepcopy(actions)
        for i, k in enumerate(actions.keys()):
            if self.is_continuous[i]:
                pass
            else:
                actions[k] = int2action_index(actions[k], self.discrete_action_dim_list[i])
        return self._env.step(actions)


class StackVisualWrapper(UnityReturnWrapper):

    def __init__(self, env, stack_nums=4):
        super().__init__(env)
        self._stack_nums = stack_nums
        self._stack_deque = {gn: deque([], maxlen=self._stack_nums) for gn in self.group_names}

    def reset(self, **kwargs):
        self._env.reset(**kwargs)
        return self.get_reset_obs()

    def get_reset_obs(self):
        '''
        解析环境反馈的信息，将反馈信息分为四部分：向量、图像、奖励、done信号
        '''
        vector = []
        visual = []
        reward = []
        done = []
        info = []
        for i, gn in enumerate(self.group_names):
            vec, vis, r, d, ifo = self.coordinate_reset_information(i, gn)
            vector.append(vec)
            visual.append(vis)
            reward.append(r)
            done.append(d)
            info.append(ifo)
        return (vector, visual, reward, done, info)

    def coordinate_reset_information(self, i, gn):
        vector, visual, reward, done, info = super().coordinate_information(i, gn)
        for _ in range(self._stack_nums):
            self._stack_deque[gn].append(visual)
        return (vector, np.concatenate(self._stack_deque[gn], axis=-1), reward, done, info)

    def coordinate_information(self, i, gn):
        vector, visual, reward, done, info = super().coordinate_information(i, gn)
        self._stack_deque[gn].append(visual)
        return (vector, np.concatenate(self._stack_deque[gn], axis=-1), reward, done, info)