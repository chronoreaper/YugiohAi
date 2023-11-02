import gym
from gym import spaces
import numpy as np

class YugiohEnv(gym.Env):
    def __init__(self) -> None:
        super(YugiohEnv, self).__init__()