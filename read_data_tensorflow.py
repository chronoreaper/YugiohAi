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

import tensorflow as tf
from keras.models import Sequential
from keras.layers import Dense, Dropout
from keras.models import load_model
from keras import optimizers

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
    os.remove("data.h5")

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


  input_length = len(action_list) + len(compare_to) + 2 # Turn id, action id
  output_length = len(action_list)
  print("length")
  print("input"+str(input_length))
  print("outpuy"+str(output_length))
  print("records" +str(len(play_record)))

  if (len(action_list) + len(compare_to)) == 0:
    return
  if (len(play_record) <= 0):
    return

  print("to check length:" + str(len(compare_to)))
  print("action length:" + str(len(action_list)))

  # Game History
  if __name__ == "__main__" and ShowData:
    model: Sequential = None
    critic: Sequential = None

    if os.path.exists("data.h5"):
      model = load_model('data.h5')
    # if os.path.exists("critic.h5"):
    #   critic = load_model('critic.h5')

    keys = list(play_record.keys())
    random.shuffle(keys)
    for id in keys:
    
      record = play_record[id]
    
      if not TrainData:
        print("RESULT:" + str(record.result))
        input_list = [0] * input_length
        output_list = -1

        for state in field_state[id]:
          input_list[state.compareId - 1 + len(action_list)] = 1
        
        input_list[len(action_list) + len(compare_to)] = record.actionId
        input_list[len(action_list) + len(compare_to) + 1] = record.turnId

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
        
        if model:
          result = model.predict([input_list], batch_size=1)
          print("Estimate:" + str(np.argmax(result)))
          #ind = np.argpartition(result, -4)[0][-4:]
          ind = np.argpartition(result, -4)[0][-4:]
          index = ind[np.argsort(result[0][ind])]
          print("Top k:")
          for i in index:
            print(str(i) + ":" + str(result[0][i]))

          print("Expected answer:" + str(output_list))
          
        if critic:
          result = critic.predict([input_list], batch_size=1)
          print("Critic:" + str(result))

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
        print("")
        if (leave):
          break

  # Generate training data
  data = []
  answer = []
  critic_answer = []
  for history_id in play_record:

    reward = 1
    critic_reward = 1
    punishment = 0

    if (play_record[history_id].result != 1):
      continue
      if (play_record[history_id].result == -1):
         reward = 0
         critic_reward = -1
         punishment = 0
      else:
        continue
    


    input_list = [0] * input_length
    output_list = [0] * (output_length + 1)
    next_phase = False
    
    # All field states at the end
    for state in field_state[id]:
      input_list[state.compareId - 1 + len(action_list)] = 1
    
    posssible_actions = action_state[history_id]
    # All possible actions as input
    for state in posssible_actions:
      input_list[state.actionId - 1] = 1
      # Mark which action you performed
      if (state.performed == 'True'):
        output_list[state.actionId] = reward

        if (action_list[state.actionId].name == ""):
          next_phase = True
      
      else:
        output_list[state.actionId] = punishment
    
    input_list[len(action_list) + len(compare_to)] = play_record[history_id].actionId
    input_list[len(action_list) + len(compare_to) + 1] = play_record[history_id].turnId

    # if next_phase:
    #   continue

    data.append(input_list)
    answer.append(output_list)
    critic_answer.append(critic_reward)

  if (TrainData) and len(data) > 0:
    # Solve data
    print("solve data")
    print("data"+str(len(data)))

    if(len(data) < 10):
      x_train = x_test = data
      y_train = y_test = answer
      xc_train = xc_test = data
      yc_train = yc_test = critic_answer
    else:
      x_train, x_test, y_train, y_test = train_test_split(data, answer, test_size=0.1)
      xc_train, xc_test, yc_train, yc_test = train_test_split(data, critic_answer, test_size=0.1)
    
    model = Sequential()
    model.add(Dense(units=200, activation='relu', input_dim=input_length, kernel_initializer='random_uniform', bias_initializer='zeros'))
    model.add(Dropout(0.2))
    # model.add(Dense(units=500, activation='relu', kernel_initializer='random_uniform', bias_initializer='zeros'))
    # model.add(Dense(units=500, activation='relu', kernel_initializer='random_uniform', bias_initializer='zeros'))
    # model.add(Dense(units=250, activation='relu', kernel_initializer='random_uniform', bias_initializer='zeros'))
    model.add(Dense(units=100, activation='relu', kernel_initializer='random_uniform', bias_initializer='zeros'))
    model.add(Dropout(0.2))
    model.add(Dense(output_length + 1, kernel_initializer='random_uniform', bias_initializer='zeros'))
    model.compile(optimizer='adam', loss='mean_squared_error', metrics=['accuracy'])

    model.fit(np.asarray(x_train), np.asarray(y_train), epochs=30, batch_size=min(len(y_train),10), verbose=1)
    model.evaluate(x_test, y_test, batch_size= len(y_test))

    model.save('data.h5')
    del model
    model = load_model('data.h5')

    # Critic 
    # model = Sequential()
    # model.add(Dense(units=100, activation='relu', input_dim=input_length, kernel_initializer='random_uniform', bias_initializer='zeros'))
    # model.add(Dense(units=50, activation='relu', kernel_initializer='random_uniform', bias_initializer='zeros'))
    # model.add(Dense(1, kernel_initializer='random_uniform', bias_initializer='zeros'))
    # model.compile(optimizer='adam', loss='mean_squared_error', metrics=['accuracy'])

    # model.fit(np.asarray(xc_train), np.asarray(yc_train), epochs=20, batch_size=min(len(yc_train),120), verbose=1)
    # model.evaluate(xc_test, yc_test, batch_size= len(yc_test))

    # model.save('critic.h5')
    # del model
    # model = load_model('critic.h5')      

  conn.commit()
  conn.close()

if __name__ == "__main__":
  #deleteData()
  read_data()