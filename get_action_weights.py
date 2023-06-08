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

datacount = len(glob.glob("./data/*"))
print("Counted " + str(datacount) + " data.")
action_data = []

for i in range(1, datacount + 1):
  file = open("./data/action"+str(i), 'rb')
  action_data.append(pickle.load(file))
  file.close()

print("Loaded " + str(len(action_data)) + " data.")

conn = sqlite3.connect(os.getcwd() +'/cardData.cdb')
c = conn.cursor()
c.execute('SELECT max(rowid) FROM L_CompareTo')
compare_count = c.fetchone()[0]
conn.close()

print("Compare Count:" + str(compare_count))

def get_predictions(data: typing.List[int], actions: typing.List[int]):
    compare = []
    for i in range(1, compare_count + 1):
      if (i not in data) :
        compare.append(0)
      else:
        compare.append(1)

    result = {}

    for action in actions:
      if int(action) - 1 < len(action_data):
        result[action] = str(action_data[int(action) - 1].predict([compare])[0])
        print("Action" + str(action) + ":" + result[action])

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

        print(raw_data["data"])
        print(raw_data["actions"])

        data = raw_data["data"].split(' ')
        actions = raw_data["actions"].split(' ')
        predictions = get_predictions(data, actions)

        self.send_response(200)
        self.send_header('Content-type','text/html')
        self.end_headers()

        print(predictions)
        message = json.dumps(predictions)
        self.wfile.write(bytes(message, "utf8"))

def run_server():


  with HTTPServer(('', 8000), handler) as server:
      server.serve_forever()

#run_command_line()
run_server()