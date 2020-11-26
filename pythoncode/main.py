# This is a sample Python script.

# Press Mayús+F10 to execute it or replace it with your code.
# Press Double Shift to search everywhere for classes, files, tool windows, actions, and settings.


import numpy as np
from typing import Dict
import curlsac
import matplotlib.pyplot as plt
import os
import time
import argparse
import torch
from copy import deepcopy
from utils import ReplayBuffer
import utils
from logger import Logger
from list_utils import zeros_initializer
from mlagents_envs.environment import UnityEnvironment

from curlsac import CurlSacAgent

from yaml_operations import load_yaml


def main():

    #1 - First take the config of the yml file for the algorithm
    #2 - Open the Unity environment and initiate the neural networks
    #3 - Initialize unity (agents)
    #4 - Run simulation

    args = load_yaml("config.yaml")
    args_env = args["unity_wrapper"]
    args_train = args["curl_sac"]
    args_buffer = args["buffer"]

    env = init_unity_env(args_env)

    # make directory
    ts = time.gmtime()
    ts = time.strftime("%m-%d", ts)
    env_name = args["environment"]["domain_name"] + '-' + args["environment"]["task_name"]
    exp_name = env_name + '-' + ts + '-im' + str(args["environment"]["image_size_pre"]) +'-b'  \
    + str(args["environment"]["batch_size"]) + '-s' + str(args["environment"]["seed"])  + '-' + args["environment"]["encoder_type"]
    args["environment"]["work_dir"] = args["environment"]["work_dir"] + '/'  + exp_name

    utils.make_dir(args["environment"]["work_dir"])
    #video_dir = utils.make_dir(os.path.join(args["environment"]["work_dir"], 'video'))
    model_dir = utils.make_dir(os.path.join(args["environment"]["work_dir"], 'model'))
    buffer_dir = utils.make_dir(os.path.join(args["environment"]["work_dir"], 'buffer'))

    behavior_spec = env.behavior_specs
    behavior_name_left = list(env.behavior_specs)[0]
    spec = env.behavior_specs[behavior_name_left]
    action_size = spec.action_size
    action_shape = spec.action_shape
    print(spec.observation_shapes, action_size, action_shape,"aquiii bro", spec.discrete_action_branches)

    action_shape = (action_shape,1)
    obs_shape = (3 * args["environment"]["frame_stack"], args["environment"]["image_size_post"], args["environment"]["image_size_post"])
    pre_aug_obs_shape = (3 * args["environment"]["frame_stack"], args["environment"]["image_size_pre"], args["environment"]["image_size_pre"])
    #device = torch.device('cuda' if torch.cuda.is_available() else 'cpu')

    print(env.EnvSpec)
    print(len(env.EnvSpec), obs_shape)
    print(env.group_agents,env.fixed_group_names,env.group_names)
    print(env.group_num)
    vis = env.reset()
    print("list:", len(vis), "0-Zero", len(vis[0]), vis[0], "1- Primer", type(vis[1]), len(vis[1]), vis[1][0][:,0].shape, vis[1][0][0].shape)#, vis[1][1].shape, vis[1][0][0].shape, vis[1][1][0].shape, "2-Segon", vis[2], "3-Tercer", vis[3], "4-Cuart", vis[4])
    #Give [A, C, H, W, C] visual( Agent1(Camera1(H, W, C), camera2(H, W, C)), Agent2(Camera1(H, W, C), camera2(H, W, C))

    #print("Reset:", vis[0], vis[0].shape, "next")
    #print(env.)
    for i, brain_name in enumerate(env.group_names):
        print(i, brain_name)
    print("--------------------------")
    for i, fgn in enumerate(env.fixed_group_names):
        print(i, "+",fgn)
    ObsRewDone = zip(*env.reset())
    """for i, (v, vs, r, d, info) in enumerate(ObsRewDone):
        print(v)
        print(vs)
        print(r)
        print(d)
        print(info)
        print("done")"""

    agents = initialize_model_buffer_each_agent(args, env)

    L = Logger(args["environment"]["work_dir"], use_tb=args["environment"]["save_tb"])

    episode, episode_reward, done = 0, 0, True
    start_time = time.time()

    for step in range(args["train"]["num_train_steps"]):
        # evaluate agent periodically

        if step % args["train"]["eval_freq"] == 0:
            L.log('eval/episode', episode, step)
            evaluate(env, agents[0][0], args["train"]["num_eval_episodes"], L, step, args)
            if args["train"]["save_model"]:
                agents[0][0].save_curl(model_dir, step)
            if args["train"]["save_buffer"]:
                agents[0][1].save(buffer_dir)

        if done:
            if step > 0:
                if step % args["train"]["log_interval"] == 0:
                    L.log('train/duration', time.time() - start_time, step)
                    L.dump(step)
                start_time = time.time()
            if step % args["train"]["log_interval"] == 0:
                L.log('train/episode_reward', episode_reward, step)

            _, obs, _, _, _ = env.reset()
            obs = np.transpose(obs[0][0][0], (2, 0, 1))
            done = False
            episode_reward = 0
            episode_step = 0
            episode += 1
            if step % args["train"]["log_interval"] == 0:
                L.log('train/episode', episode, step)

        # sample action for data collection
        if step < args["train"]["init_steps"]:
            action = env.random_action() #action_space.sample()
            actions = {f'{brain_name}': action[i] for i, brain_name in enumerate(env.group_names)}
            print("Action random: ", action, len(action))
        else:
            with utils.eval_mode(agents[0][0]):
                action = agents[0][0].sample_action(obs)
                actions = {f'{brain_name}': action for i, brain_name in enumerate(env.group_names)}

        # run training update
        if step >= args["train"]["init_steps"]:
            num_updates = 1
            for _ in range(num_updates):
                agents[0][0].update(agents[0][1], L, step)

        print("Noves actions: ", actions, type(actions))
        _, next_obs, reward, done, _ = env.step(actions)
        next_obs = np.transpose(next_obs[0][0][0], (2, 0, 1))
        # allow infinit bootstrap
        _max_episode_steps = 200
        done_bool = 0 if episode_step + 1 == _max_episode_steps else float(
            done[0]
        )
        episode_reward += reward[0][0]
        print("Final: ", reward )
        agents[0][1].add(obs, action[0], reward[0], next_obs, done_bool)

        obs = next_obs
        episode_step += 1

    env.close()




"""
    #Number of times that the goal if succed
    for n in range(num_train_steps):
        #number of steps maximum that have the agent to get the goal
        for steps in range(max_steps):
            behavior_names, behavior_spec = env.behavior_specs
            print(behavior_spec, behavior_names[1])

            behavior_name_left = list(env.behavior_specs)[0]
            behavior_name_right = list(env.behavior_specs)[1]

            spec =env.behavior_specs[behavior_name_left]

            # Examine the number of observations per Agent
            print("Number of observations : ", len(spec.observation_shapes))

            # Is there a visual observation ?
            # Visual observation have 3 dimensions: Height, Width and number of channels
            vis_obs = any(len(shape) == 3 for shape in spec.observation_shapes)
            print("Is there a visual observation ?", vis_obs)

            print(f"There are {spec.action_size} action(s)")

            # For discrete actions only : How many different options does each action has ?
            if spec.is_action_discrete():
                for action, branch_size in enumerate(spec.discrete_action_branches):
                    print(f"Action number {action} has {branch_size} different options")

            decision_steps_left, terminal_steps_left = env.get_steps(behavior_name_left)
            decision_steps_right, terminal_steps_right = env.get_steps(behavior_name_right)
            print(decision_steps_left.reward, decision_steps_right.reward)


            if (steps % 4) == 0:
                image = get_image(decision_steps_left[0].obs[0], 1)
                image = get_image(decision_steps_right[0].obs[0], 1)
            else:
                image = concatenate_image(image, get_image(decision_steps_left[0].obs[0], 1))

            if image.shape[0] == 4:
                batch_images = add_batch_dimension(image, batch_size)
                print(batch_images.shape)

                actions = []
                actions.append(np.random.uniform(0, 6, 1))
                env.set_actions(behavior_name_left, np.array(actions))
                env.set_actions(behavior_name_right, np.array(actions))

            env.step()


        batch_observation.append(decision_steps_left[0].obs[0])
        print(len(batch_observation))
        if len(batch_observation) == 4:
            print( len(batch_observation))
            observation = decision_steps_left[0].obs[0]
            prova = np.transpose(observation, (2, 0, 1))
            image_observation = observation[:,:,0]
            print(observation[:,:,0].shape, type(batch_observation[0]), prova.shape)
            #temp = decision_steps_left[0].obs[0]
            plt.imshow(observation[:,:,0])
            plt.show()
            if np.array_equal(batch_observation[0],batch_observation[3]) :
                print("equal")
            batch_observation.clear()
            actions = []
            actions.append(np.random.uniform(0,6,1))
            print(len(actions))
            env.set_actions(behavior_name_left, np.array(actions))
            env.set_actions(behavior_name_right, np.array(actions))
        #print(type(decision_steps_left[0].obs[0]))
            env.step()
    for observation in decision_steps_left[0].obs:
        print(observation.shape)
        if len(observation.shape) == 3 :
            print("uno")
    #print(get_behavior_spec)"""

    #print(decision_steps_left.agent_id[0], decision_steps_right.agent_id[0])
    #print(terminal_steps_left.agent_id, terminal_steps_right.agent_id)
    #if():
    #    env

    #h, w = env.behavior_specs.obserbation_shapes[1:]
    #print(decision_steps_left.obs, h, w)

    #curl(decision_steps_left)

    #env.step()

    #print(len(decision_steps_left), len(terminal_steps_left), behavior_name_left)
    #print(len(decision_steps_right), len(terminal_steps_right), behavior_name_right)
    #tracked_agent = decision_steps.agent_id[0]

    #print(list(decision_steps), list(terminal_steps), tracked_agent)


def init_unity_env(env_args):
    from unity_wrapper import (UnityWrapper, UnityReturnWrapper,
                                InfoWrapper, ActionWrapper, StackVisualWrapper)
    env_kargs = deepcopy(env_args)
    env = UnityWrapper(env_kargs)
    print('Unity UnityWrapper success.')

    env = InfoWrapper(env, env_kargs)
    print('Unity InfoWrapper success.')

    if env_args['frame_stack'] > 1:
        env = StackVisualWrapper(env, stack_nums=env_kargs['frame_stack'])
        print('Unity StackVisualWrapper success.')
    else:
        env = UnityReturnWrapper(env)
        print('Unity UnityReturnWrapper success.')

    env = ActionWrapper(env)
    print('Unity ActionWrapper success.')

    return env

def initialize_model_buffer_each_agent(args, env):
    models = []

    for i, fgn in enumerate(env.fixed_group_names):
        _bargs, _targs, _aargs = map(deepcopy, [args["buffer"], args["train"], args["curl_sac"]])

        behavior_spec = env.behavior_specs
        behavior_name_left = list(env.behavior_specs)[0]
        spec = env.behavior_specs[behavior_name_left]
        action_shape = (spec.action_shape,2)

        obs_shape = (3 * args["environment"]["frame_stack"], args["environment"]["image_size_post"],
                     args["environment"]["image_size_post"])
        pre_aug_obs_shape = (3 * args["environment"]["frame_stack"], args["environment"]["image_size_pre"],
                             args["environment"]["image_size_pre"])

        replay_buffer = ReplayBuffer(
            obs_shape=pre_aug_obs_shape,
            action_shape=action_shape,
            capacity=args["environment"]["buffer_size"],
            batch_size=args["environment"]["batch_size"],
            device='cpu',
            image_size=args["environment"]["image_size_post"]
        )

        model = make_agent(obs_shape, action_shape, _aargs, "cpu")
        model_buffer =[model, replay_buffer]
        models.append(model_buffer)

    return models



def make_agent(obs_shape, action_shape, args, device):
    if args["agent"] == 'curl_sac':
        return CurlSacAgent(
            obs_shape=obs_shape,
            action_shape=action_shape,
            device=device,
            hidden_dim=args["hidden_dim"],
            discount=args["discount"],
            init_temperature=args["init_temperature"],
            alpha_lr=args["alpha_lr"],
            alpha_beta=args["alpha_beta"],
            actor_lr=args["actor_lr"],
            actor_beta=args["actor_beta"],
            actor_log_std_min=args["actor_log_std_min"],
            actor_log_std_max=args["actor_log_std_max"],
            actor_update_freq=args["actor_update_freq"],
            critic_lr=args["critic_lr"],
            critic_beta=args["critic_beta"],
            critic_tau=args["critic_tau"],
            critic_target_update_freq=args["critic_target_update_freq"],
            encoder_type=args["encoder_type"],
            encoder_feature_dim=args["encoder_feature_dim"],
            encoder_lr=args["encoder_lr"],
            encoder_tau=args["encoder_tau"],
            num_layers=args["num_layers"],
            num_filters=args["num_filters"],
            log_interval=args["log_interval"],
            detach_encoder=args["detach_encoder"],
            curl_latent_dim=args["curl_latent_dim"]

        )
    else:
        assert 'agent is not supported: %s' % args.agent


def evaluate(env, agent, num_episodes, L, step, args):
    all_ep_rewards = []

    def run_eval_loop(sample_stochastically=False):
        start_time = time.time()
        prefix = 'stochastic_' if sample_stochastically else ''
        for i in range(num_episodes):
            _, obs, _, _, _ = env.reset()
            #video.init(enabled=(i == 0))
            done = False
            episode_reward = 0
            while not done:
                # center crop image
                if True: #args.encoder_type == 'pixel':
                    prev = np.transpose(obs[0][0][0], (2, 0, 1))
                    obs = utils.center_crop_image(prev, args["train"]["image_size_post"])
                    #print(obs, type(obs), obs.shape, agent, type(agent))
                with utils.eval_mode(agent):
                    print(utils.eval_mode(agent), type(utils.eval_mode(agent)))
                    if sample_stochastically:
                        action = agent.sample_action(obs)
                    else:
                        action = agent.select_action(obs)


                print(action, type(action), action.shape)
                print("Arg max: ", np.argmax(action))
                #action.shape = [1, 7]
                #print(action.shape, action[1])
                actions = {f'{brain_name}': action for i, brain_name in enumerate(env.group_names)}
                print(actions, type(actions))
                print(actions.keys())
                """ObsRewDone = zip(*env.step(actions))
                print(ObsRewDone)

                for i, (vect, visual, rew, don, inf) in enumerate(ObsRewDone):
                    obs = visual
                    reward = rew
                    done = done

                print(obs.shape, reward, done)"""
                #env.set_actions(env.group_names[0], )
                _, obs, reward, done, _ = env.step(actions)
                #video.record(env)
                print("Reward: ", reward)
                episode_reward += reward[0][0]

            #video.save('%d.mp4' % step)
            L.log('eval/' + prefix + 'episode_reward', episode_reward, step)
            all_ep_rewards.append(episode_reward)

        L.log('eval/' + prefix + 'eval_time', time.time() - start_time, step)
        mean_ep_reward = np.mean(all_ep_rewards)
        best_ep_reward = np.max(all_ep_rewards)
        L.log('eval/' + prefix + 'mean_episode_reward', mean_ep_reward, step)
        L.log('eval/' + prefix + 'best_episode_reward', best_ep_reward, step)

    run_eval_loop(sample_stochastically=False)
    L.dump(step)

if __name__ == "__main__":
    main()