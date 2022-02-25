import sys, string, os, time
import subprocess
import shutil, glob
import sqlite3

###
#Makes the best deck that the ai can make
#Run after you hosted a game
###

#print("deleting old deck files from ygopro")
#files = glob.glob(os.getcwd() +"/KoishiPro_Sakura/deck/*")
#for f in files:
#    os.remove(f)


g = subprocess.Popen([os.getcwd() + "/ProjectIgnis/ygopro.exe", "-c"])

AIName1 = 'bot1'		 
AI1Deck = 'Random'
deck1 = 'AI_Random.ydk'
#subprocess.run([os.getcwd() + "/12_makeDeck.py", "AI_Random.ydk"],shell=True)
time.sleep(3)			  
p1 = subprocess.Popen([os.getcwd() + "/133_runAi.py",AI1Deck,AIName1,'1','1',"false"],
					  shell=True,stdout=subprocess.PIPE, 
					  stderr = subprocess.PIPE,
					  universal_newlines=True)
output, stderr = p1.communicate()	
			  
print(format(output))
 		  
src_dir=os.getcwd()+"/windbot_master/bin/Debug/Decks/"+ deck1
dst_dir=os.getcwd()+"/KoishiPro_Sakura/deck/"+ deck1
shutil.copy(src_dir,dst_dir)