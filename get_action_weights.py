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

action_data = {}
reward_data = {}
compare_count = 0

def load_data():
  global action_data, compare_count, reward_data
  datacount = len(glob.glob("./data/action*"))
  print("Counted " + str(datacount) + " data.")
  action_data = {}
  reward_data = {}

  for i in range(1, datacount + 1):
    if (os.path.exists("./data/action"+str(i))):
      file = open("./data/action"+str(i), 'rb')
      action_data[i] = pickle.load(file)
      file.close()
    else:
      print("Path does not exist for data " + str(i))

  for i in range(1, datacount + 1):
    if (os.path.exists("./data/reward"+str(i))):
      file = open("./data/reward"+str(i), 'rb')
      reward_data[i] = pickle.load(file)
      file.close()
    else:
      print("Path does not exist for data " + str(i))

  # print("Loaded " + str(len(action_data)) + " data.")

  conn = sqlite3.connect(os.getcwd() +'/cardData.cdb')
  c = conn.cursor()
  c.execute('SELECT max(rowid) FROM L_CompareTo')
  compare_count = c.fetchone()[0]
  conn.close()

  # print("Compare Count:" + str(compare_count))

def get_predictions(data: typing.List[int], actions: typing.List[int]):
  global action_data, compare_count, reward_data
  compare = []
  for i in range(1, compare_count + 1):
    if (i not in data) :
      compare.append(0)
    else:
      compare.append(1)

  #print("compare")
  #print(len(compare))
  #print(action_data.keys())
  result = {}
  for action in actions:
    if int(action) in action_data:
      result[action] = str(action_data[int(action)].predict_proba([compare])[0][1])
      # print("Action" + str(action) + ":" + result[action])
      #print("Actions "  + str(action) + ":" +str(action_data[int(action)].predict_proba([compare])[0][0]))
      if int(action) in reward_data:
        # Reward print
        reward1 = [1,0]
        reward2 = [0,1]
        reward1.extend(compare)
        reward2.extend(compare)
        print("Actions "  + str(action))
        print("Reward no:" + str(reward_data[int(action)].predict([reward1])[0]))
        print("Reward yes:" + str(reward_data[int(action)].predict([reward2])[0]))

    else:
      #print(str(action) + " was not in action data")
      result[action] = str(-1)

  return result

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
        predictions = get_predictions(data, actions)

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