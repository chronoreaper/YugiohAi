import sys, string, os, time
import subprocess
import shutil
import sqlite3

###
#Makes the best deck that the ai can make
#Run after you hosted a game
###
AI1 = 'Random'
#subprocess.run([os.getcwd() + "/12_makeDeck.py", "AI_Random.ydk"],shell=True)
p1 = subprocess.Popen([os.getcwd() + "/133_runAi.py",AI1,'bot1','1'],
					  shell=True,stdout=subprocess.PIPE, 
					  stderr = subprocess.PIPE,
					  universal_newlines=True)