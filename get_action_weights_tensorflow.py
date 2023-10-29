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

import tensorflow as tf
from keras.models import Sequential
from keras.layers import Dense, Flatten
from keras.models import load_model
from keras import optimizers

action_data = None
compare_count = 0
action_count = 0

def load_data():
  global action_data, compare_count, action_count

  if os.path.exists("data.h5"):
    action_data = load_model('data.h5')

  conn = sqlite3.connect(os.getcwd() +'/cardData.cdb')
  c = conn.cursor()
  c.execute('SELECT max(rowid) FROM L_CompareTo')
  compare_count = c.fetchone()[0]
  c.execute('SELECT max(rowid) FROM L_ActionList')
  action_count = c.fetchone()[0]
  conn.close()

  print("Compare Count:" + str(compare_count))
  print("Action Count:" + str(action_count))

def get_predictions(data: typing.List[int], actions: typing.List[int], other: typing.List[int]):
  global action_data, compare_count
  if (action_data == None):
    return []
  
  input_length = action_count + compare_count + 2

  input_list = [0] * input_length

  for id in data:
    index = int(id) - 1 + action_count
    if (index < len(input_list) and index >= 0):
      input_list[index] = 1

  for i in range(len(other)):
    index = i + action_count + compare_count
    if (index < len(input_list) and index >= 0):
      input_list[index] = int(other[i])

  for id in actions:
    index = int(id) - 1
    if (index < len(input_list) and index >= 0):
      input_list[index] = 1

  result = action_data.predict([input_list], batch_size=1)
  print("Estimate:" + str(np.argmax(result)) + " " + str(len(result[0])))
  ind = np.argpartition(result, -4)[0][-4:]
  index = ind[np.argsort(result[0][ind])]
  print("Top k:")
  for i in index:
    print(str(i) + ":" + str(result[0][i]))
  return result[0].tolist()

# def run_command_line():
#   while True:
#     data = input("Enter input data\n").split(' ')
#     actions = input("Enter Actions\n").split(' ')
#     predictions = get_predictions(data, actions)

#     for action in predictions:
#       print("Action" + str(action) + ":" +str(predictions[action]))

class handler(BaseHTTPRequestHandler):
    def do_POST(self):
        content_len = int(self.headers.get('Content-Length'))
        get_body = self.rfile.read(content_len).decode()
        raw_data = json.loads(get_body)

        # print(raw_data["data"])
        # print(raw_data["actions"])

        data = raw_data["data"].split(' ')
        actions = raw_data["actions"].split(' ')
        other = raw_data["other"].split(' ')
        predictions = get_predictions(data, actions, other)

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
  load_data()
  #run_command_line()
  run_server()