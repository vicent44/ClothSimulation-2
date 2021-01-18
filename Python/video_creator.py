import os
import torch
import numpy as np
import imageio
from yaml_operations import load_yaml

def main():

    args = load_yaml("config.yaml")
    capacity_buffer = args["environment"]["buffer_size"]

    obses_new, next_obses_new, actions_new, rewards_new, not_done_new = load("data/buffer", capacity_buffer)  

    save("video-train.mp4",obses_new)


def load(save_dir, capacity_buff):
    capacity = capacity_buff
    obs_shape = (12, 84, 84)
    action_shape = (6,)
    obs_dtype = np.float32 if len(obs_shape) == 1 else np.uint8
    obses = np.empty((capacity, *obs_shape), dtype=obs_dtype)
    next_obses = np.empty((capacity, *obs_shape), dtype=obs_dtype)
    actions = np.empty((capacity, *action_shape), dtype=np.float32)
    rewards = np.empty((capacity, 1), dtype=np.float32)
    not_dones = np.empty((capacity, 1), dtype=np.float32)

    idx = 0
    last_save = 0
    
    chunks = os.listdir(save_dir)
    chucks = sorted(chunks, key=lambda x: int(x.split('_')[0]))
    for chunk in chucks:
        start, end = [int(x) for x in chunk.split('.')[0].split('_')]
        print(start, end)
        path = os.path.join(save_dir, chunk)
        payload = torch.load(path)
        assert idx == start
        obses[start:end] = payload[0]
        next_obses[start:end] = payload[1]
        actions[start:end] = payload[2]
        rewards[start:end] = payload[3]
        not_dones[start:end] = payload[4]
        idx = end
        print(start, end)
        
    return obses, next_obses, actions, rewards, not_dones

def save(file_name, obses):
    enabled = True
    if enabled:
        path = os.path.join("data/", file_name)
        imageio.mimsave(path, obses[:,:,:,0:3], fps=30)


if __name__ == "__main__":
    main()

