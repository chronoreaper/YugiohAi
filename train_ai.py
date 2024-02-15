import datetime
import glob
import math
import os
import shutil
import sqlite3
import string
import subprocess
import sys
from threading import Thread
import time
import random
import typing
from sys import platform
from pathlib import Path
import psutil

# import tensorflow as tf
# from keras.models import Sequential
# from keras.layers import Dense, Flatten
# from keras.models import load_model

import multiprocessing
import read_game_data as read_game_data
import get_action_weights as get_action_weights

import torch


#The Deck name and location	
AI1Deck = 'Random1'
AI2Deck = 'Random2'
AIMaster = 'Master'

deck1 = 'AI_Random1.ydk'
deck2 = 'AI_Random2.ydk'

totalGames = 25
generations = 10
cycles = 1
parallelGames = 1

rolloutCount = 1
isFirst = True
isTraining = True
winThresh = 60
pastWinLim = 5
idNumber = 1

reset = False

def isrespondingPID(PID):
  #if platform == "linux" or platform == "linux2":
  return True

  #https://stackoverflow.com/questions/16580285/how-to-tell-if-process-is-responding-in-python-on-windows
  os.system('tasklist /FI "PID eq %d" /FI "STATUS eq running" > tmp.txt' % PID)
  tmp = open('tmp.txt', 'r')
  a = tmp.readlines()
  tmp.close()
  try:
    if int(a[-1].split()[1]) == PID:
      return True
    else:
      return False
  except:
    return False

def deleteData():

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

def resetDB():

  deleteData()
  dbfile = './cardData.cdb'
  con = sqlite3.connect(dbfile)
  cur = con.cursor()
  sql_delete_query = """DELETE from L_ActionList"""
  cur.execute(sql_delete_query)
  sql_delete_query = """DELETE from L_CompareTo"""
  cur.execute(sql_delete_query)
  sql_delete_query = """DELETE from L_PlayRecord"""
  cur.execute(sql_delete_query)
  sql_delete_query = """DELETE from L_FieldState"""
  cur.execute(sql_delete_query)
  sql_delete_query = """DELETE from L_ActionState"""
  cur.execute(sql_delete_query)
  sql_delete_query = """DELETE from L_Weights"""
  cur.execute(sql_delete_query)
  sql_delete_query = """DELETE from L_GameResult"""
  cur.execute(sql_delete_query)
  con.commit()
  con.close()

def resetMCTS():
  dbfile = './cardData.cdb'
  con = sqlite3.connect(dbfile)
  cur = con.cursor()
  sql_delete_query = """DELETE from MCST"""
  cur.execute(sql_delete_query)
  con.commit()
  con.close()

def softResetDB():
  dbfile = './cardData.cdb'
  con = sqlite3.connect(dbfile)
  cur = con.cursor()
  sql_delete_query = """DELETE from L_ActionList"""
  cur.execute(sql_delete_query)
  sql_delete_query = """DELETE from L_CompareTo"""
  cur.execute(sql_delete_query)
  sql_delete_query = """DELETE from L_PlayRecord"""
  cur.execute(sql_delete_query)
  sql_delete_query = """DELETE from L_FieldState"""
  cur.execute(sql_delete_query)
  sql_delete_query = """DELETE from L_ActionState"""
  cur.execute(sql_delete_query)
  sql_delete_query = """DELETE from L_Weights"""
  cur.execute(sql_delete_query)
  sql_delete_query = """DELETE from L_GameResult"""
  cur.execute(sql_delete_query)
  con.commit()
  con.close()

def resetYgoPro():
  print("deleting old deck files from ygopro")
  files = glob.glob(os.getcwd() +"/ProjectIgnis/deck/*")
  for f in files:
      os.remove(f)
    
  print("deleting old replays from ygopro")
  files = glob.glob(os.getcwd() +"/ProjectIgnis/replay/*")
  for f in files:
      os.remove(f)

def parseArg():
  global reset, isTraining, repeatFor, matches, totalGames, isFirst

  reset = len(sys.argv)>1 and ("--reset" in sys.argv or "-r" in sys.argv)
  isTraining = len(sys.argv)>1 and ("--training" in sys.argv or "-t" in sys.argv)
  isFirst = len(sys.argv)>1 and ("--first" in sys.argv or "-f" in sys.argv)

  if "--repeat" in sys.argv:
    repeatFor = int(sys.argv[sys.argv.index("--repeat")+1])
  if "--matches" in sys.argv:
    matches = int(sys.argv[sys.argv.index("--matches")+1])
  if "--games" in sys.argv:
    totalGames = int(sys.argv[sys.argv.index("--games")+1])

def runAi(Deck = "Random1", 
          Name = "Random1",
          Hand = 0,
          TotalGames = 1,
          RolloutCount = 0,
          IsFirst = True,
          IsTraining = True,
          ShouldUpdate = True,
          WinsThreshold = 50,
          PastWinsLimit = 20,
          Id = 0,
          Port = 7911,
          IsMCTS= False
          ):
  currentdir = os.getcwd()
  os.chdir(os.getcwd()+'/WindBot-Ignite-master/bin/Debug')

  file_name = "WindBot.exe"

  if platform == "linux" or platform == "linux2":
    file_name = os.getcwd() + "/WindBot.exe"

  p = subprocess.Popen([file_name,"Deck="+Deck,
                        "Name="+str(Name),
                        "Hand="+str(Hand),
                        "IsTraining="+str(IsTraining), 
                        "ShouldUpdate="+str(ShouldUpdate), 
                        "TotalGames="+str(TotalGames), 
                        "RolloutCount="+str(RolloutCount), 
                        "IsFirst="+str(IsFirst), 
                        "WinsThreshold="+str(WinsThreshold), 
                        "PastWinsLimit="+str(PastWinsLimit),
                        "Id="+str(Id),
                        "Port="+str(Port),
                        "IsMCTS="+str(IsMCTS)
                        ],
                        stdout=subprocess.DEVNULL
                        )
    
  os.chdir(currentdir)
  
  return p

def shuffle_deck(deck_name):
  filePath = os.getcwd() + '/WindBot-Ignite-master/bin/Debug/Decks/'+ deck_name 

  f = open(filePath,"r")
  main = []
  extra = []
  side = []
  part = 0
  for line in f.readlines():

    if "#extra" in line:
      part = 1
      continue
    elif "!side" in line:
      part = 2
      continue
    elif "#main" in line:
      part = 0
      continue
    elif "#" in line:
      continue

    if part == 0:
      main.append(line.strip())
    elif part == 1:
      extra.append(line.strip())
    else:
      side.append(line.strip())
    random.shuffle(main)
    random.shuffle(extra)
    random.shuffle(side)

  f.close()

  f = open(filePath, "w")    
  f.write("#created by deck_maker_ai\n")

  f.write("#main\n")
  for i in main:
    f.write(i +'\n')
  f.write("#extra\n")
  for i in extra:
    f.write(i +'\n')
  f.write("!side\n")
  for i in side:
    f.write(i +'\n')    

  f.close()

def setup():
  global AI2Deck, AI2Deck, isTraining, totalGames

  if reset:
    if isTraining:
      resetDB()
    shuffle_deck(deck1)
    shuffle_deck(deck2)
    # src_dir=os.getcwd() + '/WindBot-Ignite-master/bin/Debug/Decks/' + deck1
    # dst_dir=os.getcwd() + '/WindBot-Ignite-master/bin/Debug/Decks/' + deck2
    # shutil.copy(src_dir,dst_dir)

  resetYgoPro()

  if not isTraining:
    AI2Deck = AIMaster

def main_game_runner(isTraining, totalGames, Id1, Id2, Deck1, Deck2, port):
  start = time.time()

  #subprocess.Popen - does not wait to finish
  #subprocess.run - waits to finish

  file_path = os.getcwd() + "/edopro_bin/ygoprodll.exe"
  if platform == "linux" or platform == "linux2":
    file_path = str(Path(__file__).resolve().parent.parent) + "/ProjectIgnisLinux/ygopro"
 
  g = subprocess.Popen([file_path, "-p", str(port)], stdout=subprocess.DEVNULL)


  while(g.poll() == None and not isrespondingPID(g.pid)):
    time.sleep(1)

  time.sleep(8)
  
  print("	runningAi1 " + str(Id1) + ":" + Deck1)

  p1 = runAi( Deck = Deck1, 
              Name = Id1,
              Hand = 2,
              TotalGames = totalGames,
              RolloutCount = rolloutCount,
              IsFirst = (not isFirst),
              IsTraining = isTraining,
              ShouldUpdate= isTraining,
              WinsThreshold = winThresh,
              PastWinsLimit = pastWinLim,
              Id = Id1,
              Port = port,
              IsMCTS=isTraining
            )
  time.sleep(1)
  print("	runningAi2 "+ str(Id2) + ":" + Deck2)
  p2 = runAi(Deck = Deck2, 
              Name = Id2,
              Hand = 3,
              TotalGames = totalGames,
              RolloutCount = rolloutCount,
              IsFirst = (isFirst),
              IsTraining = isTraining,
              ShouldUpdate= False,
              WinsThreshold = winThresh,
              PastWinsLimit = pastWinLim,
              Id = Id2,
              Port = port,
              IsMCTS=False
            )
  
  #psutil.Process(g.pid).nice(psutil.BELOW_NORMAL_PRIORITY_CLASS)
  #psutil.Process(p1.pid).nice(psutil.BELOW_NORMAL_PRIORITY_CLASS)
  #psutil.Process(p2.pid).nice(psutil.BELOW_NORMAL_PRIORITY_CLASS)

  if (p1.poll() == None or p2.poll() == None):
    time.sleep(1)
  
  if (not (p1.poll() == None or p2.poll() == None)):
    print("	WARNING! ai is not running")

  timer = 0
  timeout = 30 * 60 # Length of run
  
  # print(p1.pid)
  # print(p2.pid)
  #make sure the game does not run longer than needed
  #ends the ygopro program as soon as the ais are done. Ais play faster than what you see.
  
  while (p1.poll() == None or p2.poll() == None):
    continue
     
  if platform == "linux" or platform == "linux2":
    os.system("kill -9 " + str(g.pid))
  else:
    os.system("	TASKKILL /F /IM ygoprodll.exe")
  
  end = time.time()

  print("Time Past:" + str(datetime.timedelta(seconds=int(end - start))))
  print("Average Game Time:"+str(datetime.timedelta(seconds=int((end - start)/(totalGames)))))

def main():
  global reset, totalGames, generations, cycles
  parseArg()
  setup()

  # if not reset:
  #   read_game_data.createBetterDataset()
  #   softResetDB()

  for g in range(generations):
    for c in range(cycles):
      read_game_data.read_data()
      get_action_weights.fetchDatabaseData()
      get_action_weights.load_data()
      resetMCTS()

      shuffle_deck(deck1)
      shuffle_deck(deck2)

      #resetDB()
      proc = multiprocessing.Process(target=get_action_weights.run_server, args=())
      proc.start()

      jobs = []
      pairs = []
      bots = list(range(parallelGames * 2))
      if (isTraining and AI2Deck != AIMaster) and False:
        while bots:
          r1 = bots.pop(random.randrange(0, len(bots)))
          r2 = bots.pop(random.randrange(0, len(bots)))
          pairs.append((r1, r2))
      else:
        while bots:
          r1 = bots.pop(0)
          r2 = bots.pop(0)
          pairs.append((r1, r2))

      for i in range(len(pairs)):
        print("generation " + str(g) + " cycle: " + str(c) +" running game " + str(i) + ":" + str(pairs[i][0]) + "vs" + str(pairs[i][1]) + ": Total games " + str(totalGames))
        port = 7910 + i
        p = multiprocessing.Process(target=main_game_runner, args=(True, totalGames, str(pairs[i][0]), str(pairs[i][1]), AI1Deck, AI2Deck, port))
        #psutil.Process(p.pid).nice(psutil.BELOW_NORMAL_PRIORITY_CLASS)
        jobs.append(p)
        p.start()
      
      for job in jobs:
        job.join()
      
      proc.terminate()  # sends a SIGTERM
      print("done cycle")
    if g <= generations - 1:
      limit = parallelGames * cycles

      read_game_data.read_data()
      read_game_data.createBetterDataset()
      #softResetDB()
  read_game_data.read_data()

if __name__ == "__main__":
  torch.multiprocessing.set_start_method('spawn')
  main()
