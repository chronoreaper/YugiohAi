import datetime
import glob
import json
import math
import os
import shutil
import sqlite3
import string
import subprocess
import sys
import time
import random
import typing
import csv
from typing import Any
import numpy as np
import pickle
import glob

from http.server import BaseHTTPRequestHandler, HTTPServer
from scipy.linalg import lstsq
from sklearn.model_selection import train_test_split
from sklearn.linear_model import LinearRegression
from sklearn.ensemble import GradientBoostingRegressor
from sklearn.ensemble import RandomForestRegressor
from sklearn.neural_network import MLPClassifier

# import tensorflow as tf
# from keras.models import Sequential
# from keras.layers import Dense, Flatten
# from keras.models import load_model
# from keras import optimizers

import torch
import torch.nn as nn

from read_game_data import fetchDatabaseData, getTorchData, getBetterPrediction, getTorchPrediction, read_data

action_data = None
compare_count = 0
action_count = 0

#Torch settings
dtype = torch.float
device = torch.device('cuda' if torch.cuda.is_available() else 'cpu')
# print('Using device:', device)
# print()


def load_data():
  global action_data, compare_count, action_count

  action_data = getTorchData()

  conn = sqlite3.connect(os.getcwd() +'/cardData.cdb')
  c = conn.cursor()
  c.execute('SELECT max(rowid) FROM L_CompareTo')
  compare_count = c.fetchone()[0]
  c.execute('SELECT max(rowid) FROM L_ActionList')
  action_count = c.fetchone()[0]
  conn.close()

  # print("Compare Count:" + str(compare_count))

def get_predictions(data: typing.List[int], actions: typing.List[int], name: string):
  global action_data, compare_count
  if (action_data == None):
    return []

  key_name = name
  if key_name not in action_data.keys():
     return []
  
  input_length = 1 + compare_count# + action_count 

  input_list = [0] * input_length

  for id in data:
    index = int(id)
    if (index < len(input_list) and index >= 0):
      input_list[index] = 1

  # for id in actions:
  #   index = int(id) + 1 + compare_count
  #   if (index < len(input_list) and index >= 0):
  #     input_list[index] = 1
    
  final_result = []
  final_result = getTorchPrediction(action_data, [input_list])

  text = key_name + ":"
  ind = np.argpartition(final_result[key_name], -4)[-4:]
  index = ind[np.argsort(final_result[key_name][ind])]
  index = index[::-1]
  for i in index:
    text += "[" + str(i) + "]" + ":" + str(final_result[key_name][i]) + ","

  final_result = final_result[key_name].tolist()
  print(text)
  
  # better = getBetterPrediction(final_result, 0)[0]
  # final_result.clear()
  # final_result = [0] * len(better[0])
  # for b in better:
  #   final_result[b[0]] = b[1]
  
  return final_result

def run_command_line():
  while True:
    data = input("Enter input data\n").split(' ')
    actions = input("Enter Actions\n").split(' ')
    predictions = get_predictions(data, actions)

    for action in predictions:
      print("Action" + str(action) + ":" +str(predictions[action]))

class handler(BaseHTTPRequestHandler):
    def do_POST(self):
        content_len = int(self.headers.get('Content-Length'))
        get_body = self.rfile.read(content_len).decode()
        raw_data = json.loads(get_body)

        # print(raw_data["data"])
        # print(raw_data["actions"])

        data = raw_data["data"].split(' ')
        actions = raw_data["actions"].split(' ')
        name = raw_data["name"].split(' ')[0]
        predictions = get_predictions(data, actions, name)

        self.send_response(200)
        self.send_header('Content-type','text/html')
        self.end_headers()

        # print(predictions)
        message = json.dumps(predictions)
        self.wfile.write(bytes(message, "utf8"))

    def log_message(self, format: str, *args: Any) -> None:
      return
      return super().log_message(format, *args)

def run_server():
  with HTTPServer(('', 8000), handler) as server:
      server.serve_forever()

if __name__ == "__main__":
  global input_length
  input_length = 1
  torch.multiprocessing.set_start_method('spawn')
  fetchDatabaseData()
  load_data()
  #run_command_line()
  run_server()