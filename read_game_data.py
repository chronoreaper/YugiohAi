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

TrainData = not (len(sys.argv)>1 and ("--s" in sys.argv or "-s" in sys.argv))
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
  def __init__(self, id, gameId, turnId, actionId, c1h, c1f, c2h, c2f, p1h, p1f, p2h, p2f, result) -> None:
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
    self.result = result

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

  c.execute('SELECT rowid, GameId, TurnId, ActionId, CurP1Hand, CurP1Field, CurP2Hand, CurP2Field, PostP1Hand, PostP1Field, PostP2Hand, PostP2Field, Result FROM L_PlayRecord')
  records = c.fetchall()
  for record in records:
    play_record[record[0]] = PlayRecord(record[0], record[1], record[2], record[3], record[4], record[5], record[6], record[7], record[8], record[9], record[10], record[11], record[12])


  input_length = len(action_list) + len(compare_to) + 2
  output_length = len(action_list)
  print("length")
  print(input_length)
  print(output_length)
  if input_length + output_length == 0:
    return

  print("to check length:" + str(len(compare_to)))
  print("action length:" + str(len(action_list)))

  # Game History
  if __name__ == "__main__" and ShowData:
    clf = None

    if os.path.exists("./data/action"):
      file = open("./data/action", 'rb')
      clf = pickle.load(file)
      file.close()


    keys = list(play_record.keys())
    random.shuffle(keys)
    for id in keys:
    
      record = play_record[id]

      if not TrainData:
        print("result:" + str(record.result))
        input_list = [0] * input_length
        output_list = -1

        for state in field_state[id]:
          input_list[state.compareId + len(action_list) - 1] = 1

        input_list[len(action_list) + len(compare_to)] = record.actionId
        # input_list[len(action_list) + len(compare_to) + 1] = record.turnId

        for state in action_state[id]:
          input_list[state.actionId - 1] = 1
          if (state.performed == 'True'):
            output_list = state.actionId

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

        # field = [
        #   int(record.curP1Hand),
        #   int(record.curP1Field),
        #   int(record.curP2Hand),
        #   int(record.curP2Field),
        #   int(record.postP1Hand),
        #   int(record.postP1Field),
        #   int(record.postP2Hand),
        #   int(record.postP2Field)
        # ]
      
        print("--------Field State--------")

        stateField = field_state[id]
        for j in stateField: # To Update
          compare = compare_to[j.compareId]
          print("  " + str(compare))

        print("--------Possible Actions--------")

        stateAction = action_state[id]
        for j in stateAction:
          action = action_list[j.actionId]
          print("  (" + str(j.actionId) + ")" + str(j.performed) + "| " + str(action))
        
        if clf:
          # print("Estimated Value:" + str((clf.predict_proba([input_list]))))
          print("result:" + str(clf.predict([input_list])))
          
          result = clf.predict([input_list])
          print("Estimate:" + str(np.argmax(result)))
          ind = np.argpartition(result, -4)[0][-4:]
          index = ind[np.argsort(result[0][ind])]
          print("Top k:")
          for i in index:
            print(str(i) + ":" + str(result[0][i]))

          print("Expected answer:" + str(output_list))
          pass

        if len(stateAction) <= 1:
          continue

        value = -1
        leave = False

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

        if (leave):
          break

  # Generate training data
  data = []
  answer = []
  for history_id in play_record:

    if (play_record[history_id].result != 1):
      continue

    input_list = [0] * input_length
    output_list = [0] * (output_length + 1)
    next_phase = False
    
    for state in field_state[id]:
      input_list[state.compareId + len(action_list) - 1] = 1
    
    posssible_actions = action_state[history_id]
    for state in posssible_actions:
      input_list[state.actionId - 1] = 1
      if (state.performed == 'True'):
        output_list[ state.actionId] = 1#= state.actionId #

        if (action_list[state.actionId].name == ""):
          next_phase = True
    
    input_list[len(action_list) + len(compare_to)] = play_record[history_id].actionId
    #input_list[len(action_list) + len(compare_to) + 1] = play_record[history_id].turnId

    if next_phase and False:
      continue

    data.append(input_list)
    answer.append(output_list)

  if (TrainData):
    # Solve data
    print("solve data")
    # Create an answer for each action
    if(len(data) < 10):
      x_train = x_test = data
      y_train = y_test = answer
    else:
      x_train, x_test, y_train, y_test = train_test_split(data, answer, test_size=0.5)

    print("Samples=" + str(len(data)))
    if len(data) == 0:
      return
    # Action classifier
    
    clf:MLPClassifier= None
    if os.path.exists('./data/action') and len(x_train) > 0:
      file = open("./data/action", 'rb')
      clf = pickle.load(file)
      if len(x_train[0]) == clf.n_features_in_ and clf.classes_.all() in y_train and clf.classes_.__len__() >= y_train.__len__():     
        clf.partial_fit(x_train, y_train)
      else:
        clf = None
    if clf == None:
      clf = MLPClassifier(activation='relu', solver='adam', max_iter=1000, hidden_layer_sizes=(input_length + output_length, input_length + output_length)).fit(x_train, y_train)
      print("New Network")
    
    print(clf.score(x_test, y_test))

    if not os.path.exists("./data"):
      os.mkdir("./data")
    file = open('./data/action', 'wb')
    pickle.dump(clf, file)
    file.close()
      
  conn.commit()
  conn.close()

if __name__ == "__main__":
  deleteData()
  read_data()