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
    args_buffer = args["environment"]

    env = init_unity_env(args_env)

    # make directory
    ts = time.gmtime()
    ts = time.strftime("%m-%d", ts)
    env_name = args["environment"]["domain_name"] + '-' + args["environment"]["task_name"]
    exp_name = env_name + '-' + ts + '-im' + str(args["environment"]["image_size_pre"]) +'-b'  \
    + str(args["unity_wrapper"]["batch_size"]) + '-s' + str(args["environment"]["seed"])  + '-' + args["environment"]["encoder_type"]
    args["environment"]["work_dir"] = args["environment"]["work_dir"] + '/' + 'data'#+ exp_name

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


    print(env.EnvSpec)
    print(env.group_agents,env.fixed_group_names,env.group_names)
    #print(env.group_num) #Number of brains
    vis = env.reset()
    print("list:", len(vis), "0-Zero", len(vis[0]), vis[0], "1- Primer", type(vis[1]), len(vis[1]), vis[1][0][:,0].shape, vis[1][0][0].shape)#, vis[1][1].shape, vis[1][0][0].shape, vis[1][1][0].shape, "2-Segon", vis[2], "3-Tercer", vis[3], "4-Cuart", vis[4])
    #Give [A, C, H, W, C] visual( Agent1(Camera1(H, W, C), camera2(H, W, C)), Agent2(Camera1(H, W, C), camera2(H, W, C))


    for i, brain_name in enumerate(env.group_names):
        print(i, brain_name)
    print("--------------------------")
    for i, fgn in enumerate(env.fixed_group_names):
        print(i, "+",fgn)

    device = torch.device('cuda' if torch.cuda.is_available() else 'cpu')
    agents = initialize_model_buffer_each_agent(args, env, device)
    print("Agents: ", type(agents), len(agents), agents)

    L = Logger(args["environment"]["work_dir"], use_tb=args["environment"]["save_tb"])

    episode, episode_reward, done, done_bool = 0, 0, True, True
    start_time = time.time()
    step_train = 0

    for step in range(args["train"]["train_steps"]):

        # evaluate agent periodically
        if(step%100 == 0):
            print("Step: ", step)

        if step % args["train"]["eval_freq"] == 0:
            L.log('eval/episode', episode, step)
            evaluate(env, agents[0], args["train"]["num_eval_episodes"], L, step, args)
            if args["train"]["save_model"]:
                agents[0].save_curl(model_dir, step)
            if args["train"]["save_buffer"]:
                agents[1].save(buffer_dir)
            start_time = time.time()

        #if(step%(args["train"]["num_train_steps"]) == 0):
        #    done = True
        if (done_bool):
            print("Dentro :", step)
            if step > 0:
                if step % args["train"]["log_interval"] == 0:
                    L.log('train/duration', time.time() - start_time, step)
                    L.dump(step)
                start_time = time.time()

            if step % args["train"]["log_interval"] == 0:
                L.log('train/episode_reward', episode_reward, step)
                #print("Hi-1")

            _, obs, _, _, _ = env.reset()
            obs = np.transpose(obs[0][0][0], (2, 0, 1))
            done = False
            done_bool = False
            episode_reward = 0
            episode_step = 0
            episode += 1
            if step % args["train"]["log_interval"] == 0:
                L.log('train/episode', episode, step)

        # sample action for data collection
        if step < args["train"]["init_steps"]:
            action = env.random_action() #action_space.sample()
            actions = {f'{brain_name}': action[i] for i, brain_name in enumerate(env.group_names)}

        else:
            with utils.eval_mode(agents[0]):
                action = agents[0].sample_action(obs)
                actions = {f'{brain_name}': action for i, brain_name in enumerate(env.group_names)}

        # run training update
        if step >= args["train"]["init_steps"]:
            num_updates = 1
            for _ in range(num_updates):
                agents[0].update(agents[1], L, step)

        _, next_obs, reward, done, _ = env.step(actions)
        next_obs = np.transpose(next_obs[0][0][0], (2, 0, 1))
        reward = reward[0][0]
        done = done[0][0].item()
        # allow infinit bootstrap
        _max_episode_steps = args["train"]["num_train_steps"]
        done_bool = 1 if episode_step + 1 == _max_episode_steps else float(
            done
        )
        #print(type(done_bool))
        if(done_bool == 1):
            print(done, done_bool)
        episode_reward += reward
        agents[1].add(obs, action, reward, next_obs, done_bool)
        #print(type(obs), type(action), type(reward), type(next_obs), type(done_bool))

        obs = next_obs
        episode_step += 1
        step_train += 1

    env.close()


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

def initialize_model_buffer_each_agent(args, env, device):
    #models = []

    for i, fgn in enumerate(env.fixed_group_names):
        _bargs, _targs, _aargs = map(deepcopy, [args["environment"], args["train"], args["curl_sac"]])

        behavior_spec = env.behavior_specs
        behavior_name_left = list(env.behavior_specs)[0]
        spec = env.behavior_specs[behavior_name_left]
        action_shape = (spec.action_shape,)

        obs_shape = (3 * args["unity_wrapper"]["frame_stack"], args["environment"]["image_size_post"],
                     args["environment"]["image_size_post"])
        pre_aug_obs_shape = (3 * args["unity_wrapper"]["frame_stack"], args["environment"]["image_size_pre"],
                             args["environment"]["image_size_pre"])

        replay_buffer = ReplayBuffer(
            obs_shape=pre_aug_obs_shape,
            action_shape=action_shape,
            capacity=args["environment"]["buffer_size"],
            batch_size=args["unity_wrapper"]["batch_size"],
            device=device,
            image_size=args["environment"]["image_size_post"]
        )

        model = make_agent(obs_shape, action_shape, _aargs, device)
        model_buffer =[model, replay_buffer]
        #models.append(model_buffer)

    return model_buffer


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
        assert 'agent is not supported: %s' % args["curl_sac"]["agent"]


def evaluate(env, agent, num_episodes, L, step, args):
    all_ep_rewards = []

    def run_eval_loop(sample_stochastically=False):
        start_time = time.time()
        prefix = 'stochastic_' if sample_stochastically else ''
        for i in range(num_episodes):
            _, obs, _, _, _ = env.reset()

            obs = np.transpose(obs[0][0][0], (2, 0, 1))
            #video.init(enabled=(i == 0))
            done = False
            done_bool = False
            episode_reward = 0
            steps = 0
            while(not done_bool):#(not done) and (steps <= args["train"]["num_train_steps"])):
                # center crop image
                #print(steps)
                if(steps%100==0):
                    print("Eval: ", steps)

                if args["curl_sac"]["encoder_type"] == 'pixel':
                    obs = utils.center_crop_image(obs, args["train"]["image_size_post"])
                with utils.eval_mode(agent):
                    if sample_stochastically:
                        action = agent.sample_action(obs)
                    else:
                        action = agent.select_action(obs)

                actions = {f'{brain_name}': action for i, brain_name in enumerate(env.group_names)}

                _, obs, reward, done, _ = env.step(actions)
                obs = np.transpose(obs[0][0][0], (2, 0, 1))
                reward = reward[0][0]
                done = done[0][0].item()
                #video.record(env)
                _max_episode_steps = args["train"]["num_train_steps"]
                done_bool = 1 if steps + 1 == _max_episode_steps else float(
                    done
                )

                episode_reward += reward
                steps +=1

            #video.save('%d.mp4' % step)
            L.log('eval/' + prefix + 'episode_reward', episode_reward, step)
            all_ep_rewards.append(episode_reward)

        L.log('eval/' + prefix + 'eval_time', time.time() - start_time, step)
        mean_ep_reward = np.mean(all_ep_rewards)
        best_ep_reward = np.max(all_ep_rewards)
        L.log('eval/' + prefix + 'mean_episode_reward', mean_ep_reward, step)
        L.log('eval/' + prefix + 'best_episode_reward', best_ep_reward, step)
        #print("DOne 2: ", done, type(done))

    run_eval_loop(sample_stochastically=False)
    L.dump(step)

if __name__ == "__main__":
    main()