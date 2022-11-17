import datetime
import glob
import sys, string, os, time, keyboard
import subprocess
import shutil
import sqlite3
import math
import sqlite3

AIName1 = 'bot1'
AIName2 = 'bot2'	

#The Deck name and location	
AI1Deck = 'Random'
AI2Deck = 'Random2'
AIMaster = 'Master'

deck1 = 'AI_Random.ydk'
deck2 = 'AI_Random2.ydk'

totalGames = 1
rolloutCount = 1
isFirst = True
IsTraining = True
winThresh = 50
pastWinLim = 20

reset = False

def isrespondingPID(PID):
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
  global reset, isTraining, repeatFor, matches, gamesToPlay

  reset = len(sys.argv)>1 and ("--reset" in sys.argv or "-r" in sys.argv)
  isTraining = str(not (len(sys.argv)>1 and ("--training" in sys.argv or "-t" in sys.argv)))

  if "--repeat" in sys.argv:
    repeatFor = int(sys.argv[sys.argv.index("--repeat")+1])
  if "--matches" in sys.argv:
    matches = int(sys.argv[sys.argv.index("--matches")+1])
  if "--games" in sys.argv:
    gamesToPlay = int(sys.argv[sys.argv.index("--games")+1])

def setup():
  if reset:
    resetDB()
    subprocess.run([os.getcwd() + "/ShuffleDeck.py", "AI_Random.ydk"],shell=True)
    src_dir=os.getcwd() + '/WindBot-Ignite-master/bin/Debug/Decks/' + "AI_Random.ydk"
    dst_dir=os.getcwd() + '/WindBot-Ignite-master/bin/Debug/Decks/' + "AI_Random2.ydk"
    shutil.copy(src_dir,dst_dir)

  resetYgoPro()

def main_game_runner():
  start = time.time()

  #subprocess.Popen - does not wait to finish
  #subprocess.run - waits to finish

  g = subprocess.Popen([os.getcwd() + "/edopro_bin/ygopro.exe", "-c"])

  while(g.poll() == None and not isrespondingPID(g.pid)):
    time.sleep(1)

  check = 0
  
  time.sleep(3)
  
  print("	runningAi1")

  p1 = subprocess.Popen([os.getcwd() + "/run_ai.py",
              "--deck "+AI1Deck,
              "--name "+AIName1,
              "--hand 1",
              "--total "+totalGames,
              "--rollout "+rolloutCount,
              "--first "+isFirst,
              "--training "+isTraining,
              "--winthresh "+winThresh,
              "--pastwinslimit "+pastWinLim
              ],
              shell=True,stdout=subprocess.PIPE, 
              stderr = subprocess.PIPE,
              universal_newlines=True)
  time.sleep(1)
  print("	runningAi2")
  p2 = subprocess.Popen([os.getcwd() + "/run_ai.py",
              "--deck "+AI2Deck,
              "--name "+AIName2,
              "--hand 2",
              "--total "+totalGames,
              "--rollout "+rolloutCount,
              "--first "+(not isFirst),
              "--training "+isTraining,
              "--winthresh "+winThresh,
              "--pastwinslimit "+pastWinLim
              ],
              shell=True)
  
  if (p1.poll() == None or p2.poll() == None):
    time.sleep(1)
  
  if (not (p1.poll() == None or p2.poll() == None)) and check == 0:
    print("	WARNING! ai is not running")
    check = 1
    
  timer = 0
  timeout = 30 * 60 # Length of run
  
  #make sure the game does not run longer than needed
  #ends the ygopro program as soon as the ais are done. Ais play faster than what you see.
  
  while ((timer < timeout and (p1.poll() == None and p2.poll() == None))):
    time.sleep(1)
    timer += 1
        
  print("	Game took "+str(timer)+" seconds.")
    
  os.system("	TASKKILL /F /IM ygopro.exe")
  
  output, stderr = p1.communicate()
  #print("	"+output)

  end = time.time()

  print("Time Past:" + str(datetime.timedelta(seconds=int(end - start))))
  print("Average Game Time:"+str(datetime.timedelta(seconds=int((end)/(gamesToPlay)))))

def main():
  parseArg()
  setup()
  main_game_runner()

if __name__ == "__main__":
  main()
