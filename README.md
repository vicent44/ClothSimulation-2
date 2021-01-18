# Robotic folding with CURL in simulation

The goal of this thesis is cloth folding using visual observations as an input for a DRL algorithm. The thesis wants to check whether the robot can learn to recognize a piece of cloth and fold it accordingly to the visual observations that it receives as an input. We want to experiment with the [CURL](https://arxiv.org/abs/2004.04136) algorithm which is the first RL algorithm that accelerates visual recognition using Contrastive Representation Learning ([CRL](https://ieeexplore.ieee.org/document/9226466)). CURL coupple a RL algorithm with a CRL where CRL learns a representation from images by extracting the features using [Contrastive Learning](https://ieeexplore.ieee.org/abstract/document/1640964) and gives that information to the RL algorithm.

As a simulation engine, [Unity](https://unity.com) has been chosen because it has a package called [Machine Learning Agents](https://github.com/Unity-Technologies/ml-agents) (ML-Agents) that enables games and simulations to serve as environments for training intelligent agents. Those agents can be trained using reinforcement learning, imitation learning, neuroevolution, or other machine learning methods through a Python API.


### Thesis

The thesis is divided in three parts:
* Cloth simulation
* CURL implementation
* Experimental setup: Robot simulation to manipulate the cloth simulation through CURL algorithm

This image shows the schema of the thesis, how the different parts are interconnected.


![alt text](https://github.com/vicent44/RobotAgents/blob/master/Thesis/overview.png?raw=true)

## Authors

* **Vicent Roig Server** - vicent44@gmail.com

