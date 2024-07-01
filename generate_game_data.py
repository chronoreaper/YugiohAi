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
from export_database import export_database
import read_game_data as read_game_data
import get_action_weights as get_action_weights

import torch


#The Deck name and location	
AI1Deck = 'SnakeEyes'
AI2Deck =  'SnakeEyes'

deck1 = os.getcwd() +'/decks/AI_SnakeEyes.ydk'
deck2 = os.getcwd() +'/decks/AI_SnakeEyes.ydk'

reset = False
totalGames = 3
parallelGames = 1

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

def resetDB():
  dbfile = './cardData.cdb'
  con = sqlite3.connect(dbfile)
  cur = con.cursor()
  sql_delete_query = """DELETE from GameStats"""
  cur.execute(sql_delete_query)
  sql_delete_query = """DELETE from GameTable"""
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

def getPool():
  dbfile = './cardData.cdb'
  con = sqlite3.connect(dbfile)
  cur = con.cursor()
  cur.execute('SELECT MAX(Pool) FROM GameTable')
  result = cur.fetchone()
  con.close()

  result = result[0]
  if result == None:
    result = 0
  return result

def parseArg():
  global totalGames, reset

  reset = len(sys.argv)>1 and ("--reset" in sys.argv or "-r" in sys.argv)
  if "--games" in sys.argv:
    totalGames = int(sys.argv[sys.argv.index("--games")+1])

def runAi(Deck = "Random1",
          DeckFile = "",
          Name = "Random1",
          Hand = 0,
          TotalGames = 1,
          IsFirst = True,
          Id = 0,
          Port = 7911
          ):
  currentdir = os.getcwd()
  os.chdir(os.getcwd()+'/WindBot-Ignite-master/bin/Debug')

  file_name = "WindBot.exe"

  if platform == "linux" or platform == "linux2":
    file_name = os.getcwd() + "/WindBot.exe"

  p = subprocess.Popen([file_name,"Deck="+Deck,
                        "DeckFile="+DeckFile,
                        "Name="+str(Name),
                        "Hand="+str(Hand),
                        "TotalGames="+str(TotalGames), 
                        "IsFirst="+str(IsFirst), 
                        "Id="+str(Id),
                        "Port="+str(Port),
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


def main_game_runner(pool, totalGames, Id1, Id2, Deck1, Deck2, DeckFile1, DeckFile2, port):
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
              DeckFile = DeckFile1,
              Name = Id1,
              TotalGames = totalGames,
              Id = pool,
              Port = port,
            )
  time.sleep(1)
  print("	runningAi2 "+ str(Id2) + ":" + Deck2)
  p2 = runAi(Deck = Deck2, 
              DeckFile = DeckFile2,
              Name = Id2,
              TotalGames = totalGames,
              Id = pool,
              Port = port,
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
  global totalGames
  parseArg()

  if reset:
    resetDB()
    resetYgoPro()
  
  pool = getPool()

  proc = multiprocessing.Process(target=get_action_weights.run_server, args=())
  proc.start()

  jobs = []
  pairs = []

  print("running game " + str(0) + ":" + str(0) + "vs" + str(1) + ": Total games " + str(totalGames))
  port = 7911 + 0
  p = multiprocessing.Process(target=main_game_runner, args=(pool, totalGames, str(0), str(1), AI1Deck, AI2Deck, deck1, deck2, port))
  #psutil.Process(p.pid).nice(psutil.BELOW_NORMAL_PRIORITY_CLASS)
  jobs.append(p)
  p.start()

  for job in jobs:
    job.join()
  
  proc.terminate()  # sends a SIGTERM
  print("done cycle")
  export_database()

if __name__ == "__main__":
  torch.multiprocessing.set_start_method('spawn')
  main()
