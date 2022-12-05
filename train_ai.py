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
from sys import platform
from pathlib import Path

AIName1 = 'bot1'
AIName2 = 'bot2'	

#The Deck name and location	
AI1Deck = 'Random1'
AI2Deck = 'Random2'
AIMaster = 'Master'

deck1 = 'AI_Random1.ydk'
deck2 = 'AI_Random2.ydk'

totalGames = 200
rolloutCount = 1
isFirst = True
isTraining = True
winThresh = 50
pastWinLim = 5

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

def resetDB():
  dbfile = './cardData.cdb'
  con = sqlite3.connect(dbfile)
  cur = con.cursor()
  sql_delete_query = """DELETE from MCST"""
  cur.execute(sql_delete_query)
  sql_delete_query = """DELETE from WeightTree"""
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

  print(isTraining)

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
          WinsThreshold = 50,
          PastWinsLimit = 20
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
                        "TotalGames="+str(TotalGames), 
                        "RolloutCount="+str(RolloutCount), 
                        "IsFirst="+str(IsFirst), 
                        "WinsThreshold="+str(WinsThreshold), 
                        "PastWinsLimit="+str(PastWinsLimit)])
    
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
  global AI2Deck, AIName2, isTraining

  if reset:
    resetDB()
    shuffle_deck(deck1)
    src_dir=os.getcwd() + '/WindBot-Ignite-master/bin/Debug/Decks/' + deck1
    dst_dir=os.getcwd() + '/WindBot-Ignite-master/bin/Debug/Decks/' + deck2
    shutil.copy(src_dir,dst_dir)

  resetYgoPro()

  if not isTraining:
    AI2Deck = AIMaster
    AIName2 = AIMaster

def main_game_runner():
  start = time.time()

  #subprocess.Popen - does not wait to finish
  #subprocess.run - waits to finish

  file_path = os.getcwd() + "/edopro_bin/ygopro.exe"

  if platform == "linux" or platform == "linux2":
    file_path = str(Path(__file__).resolve().parent.parent) + "/ProjectIgnisLinux/ygopro"
 
  
  g = subprocess.Popen([file_path])


  while(g.poll() == None and not isrespondingPID(g.pid)):
    time.sleep(1)

  time.sleep(3)
  
  print("	runningAi1")

  p1 = runAi(AI1Deck, AIName1, 2, totalGames, rolloutCount, isFirst, isTraining, winThresh, pastWinLim)
  # p1 = subprocess.Popen(["python " + os.getcwd() + "/run_ai.py",
  #             "--deck",AI1Deck,
  #             "--name",AIName1,
  #             "--hand", str(1),
  #             "--total",str(totalGames),
  #             "--rollout",str(rolloutCount),
  #             "--first",str(isFirst),
  #             "--training",str(isTraining),
  #             "--winthresh",str(winThresh),
  #             "--pastwinslimit",str(pastWinLim)
  #             ],
  #             shell=True,stdout=subprocess.PIPE, 
  #             stderr = subprocess.PIPE,
  #             universal_newlines=True)
  time.sleep(1)
  print("	runningAi2")
  p2 = runAi(AI2Deck, AIName2, 3, totalGames, rolloutCount, not isFirst, False, winThresh, pastWinLim)
  # p2 = subprocess.Popen(["python " + os.getcwd() + "/run_ai.py",
  #             "--deck",AI2Deck,
  #             "--name",AIName2,
  #             "--hand", str(2),
  #             "--total",str(totalGames),
  #             "--rollout",str(rolloutCount),
  #             "--first",str(not isFirst),
  #             "--training",str(isTraining),
  #             "--winthresh",str(winThresh),
  #             "--pastwinslimit",str(pastWinLim)
  #             ],
  #             shell=True)
  
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
  
  while (p1.poll() == None and p2.poll() == None):
    continue
     
     
  if platform == "linux" or platform == "linux2":
    os.system("kill -9 " + str(g.pid))
  else:
    os.system("	TASKKILL /F /IM ygopro.exe")
  
  end = time.time()

  print("Time Past:" + str(datetime.timedelta(seconds=int(end - start))))
  print("Average Game Time:"+str(datetime.timedelta(seconds=int((end - start)/(totalGames)))))

def main():
  parseArg()
  setup()
  main_game_runner()

if __name__ == "__main__":
  main()
