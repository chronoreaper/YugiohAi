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
from sklearn.neural_network import MLPClassifier

from sys import platform
from pathlib import Path

class Action:
  def __init__(self, id, name, action) -> None:
    self.id = id
    self.name = name
    self.action = action

  def __str__(self) -> str:
    return str(self.name + " " + self.action)

class ActionState:
  def __init__(self, id, actionId, historyId, performed) -> None:
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
  def __init__(self, id, gameId, turnId, actionId) -> None:
    self.id = id
    self.gameId = gameId
    self.turnId = turnId
    self.actionId = actionId


action_list: typing.Dict[int, Action] = {}
action_state: typing.Dict[int, ActionState] = {}
compare_to: typing.Dict[int, CompareTo] = {}
field_state: typing.Dict[int, FieldState] = {}
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
  action_state[record[0]] = ActionState(record[0], record[1], record[2], record[3])

c.execute('SELECT rowid, Location, Compare, Value FROM L_CompareTo')
records = c.fetchall()
for record in records:
  compare_to[record[0]] = CompareTo(record[0], record[1], record[2], record[3])

c.execute('SELECT rowid, CompareId, HistoryId FROM L_FieldState')
records = c.fetchall()
for record in records:
  field_state[record[0]] = FieldState(record[0], record[1], record[2])

c.execute('SELECT rowid, GameId, TurnId, ActionId FROM L_PlayRecord')
records = c.fetchall()
for record in records:
  play_record[record[0]] = PlayRecord(record[0], record[1], record[2], record[3])


# Game History
def game_history():
  if (len(play_record) < 500):
    for id in play_record:
      record = play_record[id]
      print("Game:" + str(record.gameId) + " Turn:" + str(record.turnId) + " Action:" + str(record.actionId))
      print("")
      print("--------Field State--------")
      for j in field_state:
        state = field_state[j]
        if (state.historyId == record.id):
          compare = compare_to[state.compareId]
          print("  " + str(compare))

      print("--------Possible Actions--------")
      for j in action_state:
        state = action_state[j]
        if (state.historyId == record.id):
          action = action_list[state.actionId]
          print("  " + str(state.performed) + "| " + str(action))

#game_history()

# Generate training data
#         | compare1 | compare2 | .....
# action1 |   0/1    |    0/1   | .....
# action1 |   0/1    |    0/1   | .....
# action2 |   0/1    |    0/1   | .....

# { actionId:{ ActionState: [ compareId ]} }
training_data: typing.Dict[int, typing.Dict[ActionState, typing.List[int]]] = {}

for id in action_state:
  action = action_state[id]
  for j in field_state:
    state = field_state[j]
    if (state.historyId == action.historyId):
      if (action.actionId not in training_data):
        training_data[action.actionId] = {}
      if (action not in training_data[action.actionId]):
        training_data[action.actionId][action] = []
      training_data[action.actionId][action].append(state.compareId)
        

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

# Create an answer for each action

sql_delete_query = """DELETE from L_Weights"""
c.execute(sql_delete_query)
conn.commit()

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

    # model = LinearRegression().fit(x_train, y_train)
    
    # print(model.score(x_train, y_train))
    # print(model.score(x_test, y_test))


    # model = GradientBoostingRegressor(random_state=0).fit(x_train, y_train)
    # print(model.score(x_train, y_train))
    # print(model.score(x_test, y_test))
    # #print(model.get_params())


    # model = RandomForestRegressor(random_state=0).fit(x_train, y_train)
    # print(model.score(x_train, y_train))
    # print(model.score(x_test, y_test))

    clf = MLPClassifier(solver='lbfgs', alpha=1e-5,
                    hidden_layer_sizes=(15, 12), random_state=0).fit(x_train, y_train)
    
    print(clf.score(x_test, y_test))

    clf = MLPClassifier(activation='relu', solver='adam', max_iter=500,
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

    for compareId in range(len(p)):
      c.execute('INSERT INTO L_Weights values (?,?,?)', (actionId, compareId, p[compareId]))
    
conn.commit()
conn.close()