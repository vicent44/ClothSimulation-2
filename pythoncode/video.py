import imageio
import os
import numpy as np


class VideoRecorder(object):
    def __init__(self, dir_name, obs_shape,height=256, width=256, camera_id=0, fps=30, capacity=2500):
        self.dir_name = dir_name
        self.height = height
        self.width = width
        self.camera_id = camera_id
        self.fps = fps
        self.frames = []
        self.capacity = capacity
        obs_dtype = np.float32 if len(obs_shape) == 1 else np.uint8
        self.obses = np.empty((capacity, *obs_shape), dtype=obs_dtype)


    def init(self, enabled=True):
        self.frames = []
        self.enabled = self.dir_name is not None and enabled
        self.idx = 0

    def record(self, obs):
        if self.enabled:
            """try:
                frame = env.render(
                    mode='rgb_array',
                    height=self.height,
                    width=self.width,
                    camera_id=self.camera_id
                )
            except:
                frame = env.render(
                    mode='rgb_array',
                )"""

            #self.frames.append(env)
            #self.obses.
            np.copyto(self.obses[self.idx], obs)
            self.idx = (self.idx + 1) % self.capacity

    def save(self, file_name):
        if self.enabled:
            print(self.obses.shape)
            path = os.path.join(self.dir_name, file_name)
            obs = np.transpose(self.obses, (0, 2, 3, 1))
            imageio.mimsave(path, obs[:,:,:,0:3], fps=self.fps)