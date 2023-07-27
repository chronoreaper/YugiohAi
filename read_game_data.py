import datetime
import glob
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

from scipy.linalg import lstsq
from sklearn.model_selection import train_test_split
from sklearn.linear_model import LinearRegression
from sklearn.ensemble import GradientBoostingRegressor
from sklearn.ensemble import RandomForestRegressor
from sklearn.neural_network import MLPClassifier, MLPRegressor

from sys import platform
from pathlib import Path

TrainData = False
ShowData = True

class Action:
  def __init__(self, id, name, action) -> None:
    self.id = id
    self.name = name
    self.action = action

  def __str__(self) -> str:
    return str(self.name + " " + self.action)

class ActionState:
  def __init__(self, id, actionId, historyId, performed: bool) -> None:
    self.id = id
    self.actionId = actionId
    self.historyId = historyId
    self.performed = performed

class CompareTo:
  def __init__(self, id, location, compare, value) -> None:
    self.id = id
    self.location = location
    self.compare = compare
    self.value = value

  def __str__(self) -> str:
    return str(self.location + " " + self.compare + " " + self.value)

class FieldState:
  def __init__(self, id, compareId, historyId) -> None:
    self.id = id
    self.compareId = compareId
    self.historyId = historyId

class PlayRecord:
  def __init__(self, id, gameId, turnId, actionId, c1h, c1f, c2h, c2f, p1h, p1f, p2h, p2f) -> None:
    self.id = id
    self.gameId = gameId
    self.turnId = turnId
    self.actionId = actionId
    self.curP1Hand = c1h
    self.curP1Field = c1f
    self.curP2Hand = c2h
    self.curP2Field = c2f
    self.postP1Hand = p1h
    self.postP1Field = p1f
    self.postP2Hand = p2h
    self.postP2Field = p2f

def deleteData():
  global TrainData, ShowData
  if (TrainData):
    folder = './data'
    for filename in os.listdir(folder):
        file_path = os.path.join(folder, filename)
        try:
            if os.path.isfile(file_path) or os.path.islink(file_path):
                os.unlink(file_path)
            elif os.path.isdir(file_path):
                shutil.rmtree(file_path)
        except Exception as e:
            print('Failed to delete %s. Reason: %s' % (file_path, e))

def read_data():
  global TrainData, ShowData
  print("Reading data")
  action_list: typing.Dict[int, Action] = {}
  action_state: typing.Dict[int, typing.List[ActionState]] = {}
  compare_to: typing.Dict[int, CompareTo] = {}
  field_state: typing.Dict[int, typing.List[FieldState]] = {}
  play_record: typing.Dict[int, PlayRecord] = {}

  conn = sqlite3.connect(os.getcwd() +'/cardData.cdb')
  c = conn.cursor()

  #c.execute('SELECT rowid, Name, Action FROM L_ActionList where Output = ?', (node_id,))

  c.execute('SELECT rowid, Name, Action FROM L_ActionList')
  records = c.fetchall()
  for record in records:
    action_list[record[0]] = Action(record[0], record[1], record[2])

  c.execute('SELECT rowid, ActionId, HistoryId, Performed FROM L_ActionState')
  records = c.fetchall()
  for record in records:
    id = record[2]
    if (id not in action_state):
      action_state[id] = []
    action_state[id].append(ActionState(record[0], record[1], record[2], record[3]))

  c.execute('SELECT rowid, Location, Compare, Value FROM L_CompareTo')
  records = c.fetchall()
  for record in records:
    compare_to[record[0]] = CompareTo(record[0], record[1], record[2], record[3])

  c.execute('SELECT rowid, CompareId, HistoryId FROM L_FieldState')
  records = c.fetchall()
  for record in records:
    id = record[2]
    if (id not in field_state):
      field_state[id] = []
    field_state[id].append(FieldState(record[0], record[1], record[2]))

  c.execute('SELECT rowid, GameId, TurnId, ActionId, CurP1Hand, CurP1Field, CurP2Hand, CurP2Field, PostP1Hand, PostP1Field, PostP2Hand, PostP2Field FROM L_PlayRecord')
  records = c.fetchall()
  for record in records:
    play_record[record[0]] = PlayRecord(record[0], record[1], record[2], record[3], record[4], record[5], record[6], record[7], record[8], record[9], record[10], record[11])

  # Game History
  if __name__ == "__main__" and ShowData:
    stats_input = []
    stats_output = []

    clf = None

    if os.path.exists("./data/stats"):
      file = open("./data/stats", 'rb')
      clf = pickle.load(file)
      file.close()


    keys = list(play_record.keys())
    random.shuffle(keys)
    for id in keys:
      record = play_record[id]
      print("Game:" + str(record.gameId) + " Turn:" + str(record.turnId) + " Action:" + str(record.actionId))
      print("")
      print("--------Stats--------")
      print("curP1Hand:" + str(record.curP1Hand))
      print("curP1Field:" + str(record.curP1Field))
      print("curP2Hand:" + str(record.curP2Hand))
      print("curP2Field:" + str(record.curP2Field))
      print("postP1Hand:" + str(record.postP1Hand))
      print("postP1Field:" + str(record.postP1Field))
      print("postP2Hand:" + str(record.postP2Hand))
      print("postP2Field:" + str(record.postP2Field))

      field = [
        int(record.curP1Hand),
        int(record.curP1Field),
        int(record.curP2Hand),
        int(record.curP2Field),
        int(record.postP1Hand),
        int(record.postP1Field),
        int(record.postP2Hand),
        int(record.postP2Field)
      ]

      print("--------Field State--------")

      stateField = field_state[id]
      for j in stateField: # To Update
        compare = compare_to[j.compareId]
        print("  " + str(compare))

      print("--------Possible Actions--------")
      stateAction = action_state[id]
      for j in stateAction:
        action = action_list[j.actionId]
        print("  " + str(j.performed) + "| " + str(action))
      
      if clf:
        print("Estimated Value:" + str(clf.predict_proba([field])[0]))
        print("Estimate:" + str(clf.predict([field])[0]))

      if len(stateAction) <= 1:
        continue

      value = -1
      leave = False

      if not TrainData:
        while value != '0' and value != '1':
          value = input("good (1) or bad (0)")
          try:
            if (len(value) == 0):
              leave = True
              break
            elif (int(value) != 0 and int(value) != 1):
              value = -1
          except:
            value = -1
            print("Input error, try again")
      elif (clf and clf.predict([field])[0] == 0) or len(stateAction) <= 1: # If action is deemed bad, remove it?
          if len(stateAction) == 2: # OTher choice is better
            for j in stateAction:
              j.performed = not j.performed
          else: #idk what to do, remove it?
            action_state.pop(id)
            field_state.pop(id)

      if (leave):
        break

      stats_input.append(field)
      stats_output.append(int(value))
    
    if not TrainData:
      #x_train = x_test = stats_input
      #y_train = y_test = stats_output
      x_train, x_test, y_train, y_test = train_test_split(stats_input, stats_output, test_size=0.4)

      print("Samples=" + str(len(stats_input)))

      if (clf == None):
        clf = MLPClassifier(activation='relu', solver='adam', max_iter=1000,
                    hidden_layer_sizes=(16, 16)).fit(x_test, y_test)
      else:
        clf.partial_fit(x_train, y_train)
      print(clf.score(x_test, y_test))

      if not os.path.exists("./data"):
        os.mkdir("./data")
      file = open('./data/stats', 'wb')
      pickle.dump(clf, file)
      file.close()


  # Generate training data
  #         | compare1 | compare2 | .....
  # action1 |   0/1    |    0/1   | .....
  # action1 |   0/1    |    0/1   | .....
  # action2 |   0/1    |    0/1   | .....
  print("Compiling data")
  # { actionId:{ ActionState: [ compareId ]} }
  
  training_data: typing.Dict[int, typing.Dict[ActionState, typing.List[int]]] = {}

  for id in action_state:
    for action in action_state[id]:
      if (action.historyId in field_state):
        states = field_state[action.historyId]
        for state in states:
          if (state.historyId == action.historyId):
            if (action.actionId not in training_data):
              training_data[action.actionId] = {}
            if (action not in training_data[action.actionId]):
              training_data[action.actionId][action] = []
            training_data[action.actionId][action].append(state.compareId)

  if (TrainData):
    print("append data to csv")
    # Create data
    with open('data.csv', 'w', newline='') as file:
      writer = csv.writer(file)
      compare_len = len(compare_to)
      print("Compare length " + str(compare_len))

      compare_list = []
      compare_list.append("ACTION")
      compare_list.append("ACTIVATED")
      for i in range(1, compare_len + 1):
        if (i not in compare_to):
          print("ERROR " + str(i) + " Not in compare to list")
        compare_list.append(str(compare_to[i]))

      writer.writerow(compare_list)

      for actionId in training_data:
        for action in training_data[actionId]:
          row = []
          compareId = training_data[actionId][action]
          row.append(str(action_list[actionId]))  
          row.append(str(action.performed))
          for i in range(1, compare_len + 1):
            if (i in compareId):
              row.append(1)  
            else:
              row.append(0)  
          
          writer.writerow(row)

    # Solve data
    print("solve data")
    # Create an answer for each action

    with open('weights.csv', 'w', newline='') as file:
      writer = csv.writer(file)

      compare_list = []
      compare_list.append("ACTION")
      for i in range(1, compare_len + 1):
        if (i not in compare_to):
          print("ERROR " + str(i) + " Not in compare to list")
        compare_list.append(str(compare_to[i]))
      writer.writerow(compare_list)

      for actionId in training_data:
        answer = []
        data = []
        for action in training_data[actionId]:
          answer.append(int(action.performed == 'True'))

          row = []
          compare_len = len(compare_to)
          for i in range(1, compare_len + 1):
            if (i in training_data[actionId][action]):
              row.append(1)
            else:
              row.append(0)

          data.append(row)

        x_train = x_test = data
        y_train = y_test = answer
        #x_train, x_test, y_train, y_test = train_test_split(data, answer, test_size=0.4)

        print(str(actionId) + ": Samples=" + str(len(training_data[actionId])))

        clf = MLPClassifier(activation='relu', solver='adam', max_iter=1000,
                    hidden_layer_sizes=(compare_len * 2, compare_len * 2)).fit(x_train, y_train)
        print(clf.score(x_test, y_test))


        if not os.path.exists("./data"):
          os.mkdir("./data")
        file = open('./data/action'+str(actionId), 'wb')
        pickle.dump(clf, file)
        file.close()

        M = np.array(data)
        y = np.array(answer)

        p, res, rnk, s = lstsq(M, y)

        to_insert = [str(action_list[actionId])]
        to_insert.extend(p.tolist())
        writer.writerow(to_insert)
      
  conn.commit()
  conn.close()

  return training_data

if __name__ == "__main__":
  deleteData()
  training_data = read_data()