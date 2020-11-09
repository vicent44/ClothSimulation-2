# This is a sample Python script.

# Press Mayús+F10 to execute it or replace it with your code.
# Press Double Shift to search everywhere for classes, files, tool windows, actions, and settings.


import numpy as np
import matplotlib.pyplot as plt
import os
from mlagents_envs.environment import UnityEnvironment
# This is a non-blocking call that only loads the environment.
env = UnityEnvironment(file_name=None)
# Start interacting with the environment.
env.reset()
env.reset()
#print(env.behavior_spec.observation_shapes)

batch_observation =[]

episodes = 500 #Number of games (number of times that succed the goal)
max_steps = 1000 #Maximum steps for each episode

im = np.random.randint(0, 255, (16, 16))
print(im.shape, type(im))

for episode in range(episodes):

    for steps in range(max_steps):
        behavior_names = env.behavior_specs
        #for behavior_name in behavior_names:
        #    print(behavior_name)
        #    decision_steps, terminal_steps = env.get_steps(behavior_name)

        #print(decision_steps.action_mask)
        #print(decision_steps.obs)
        #print(decision_steps.agent_id[0])

        behavior_name_left = list(env.behavior_specs)[0]
        behavior_name_right = list(env.behavior_specs)[1]

        decision_steps_left, terminal_steps_left = env.get_steps(behavior_name_left)
        decision_steps_right, terminal_steps_right = env.get_steps(behavior_name_right)
        print( decision_steps_left.reward, decision_steps_right.reward)
        #batch_observation_temp = np.array([decision_steps_left[0].obs[0]])

        batch_observation.append(decision_steps_left[0].obs[0])
        print(len(batch_observation))
        if len(batch_observation) == 4:
            print( len(batch_observation))
            print(decision_steps_left[0].obs[0].shape, type(batch_observation[0]))
            temp = decision_steps_left[0].obs[0]
            plt.imshow(temp.reshape(84,84))
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
    """for observation in decision_steps_left[0].obs:
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



def print_hi(name):
    # Use a breakpoint in the code line below to debug your script.
    print(f'Hi, {name}')  # Press Ctrl+F8 to toggle the breakpoint.


# Press the green button in the gutter to run the script.
if __name__ == '__main__':
    print_hi('PyCharm')
