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


subprocess.Popen([os.getcwd() + "/132_runYgoPro.py -c"], 
			 shell=True, stdin=None, stdout=None,
			 stderr=None, close_fds=True)
				 
AI1 = 'Random'
deck1 = 'AI_Random.ydk'
#subprocess.run([os.getcwd() + "/12_makeDeck.py", "AI_Random.ydk"],shell=True)
p1 = subprocess.Popen([os.getcwd() + "/133_runAi.py",AI1,'master','1'],
					  shell=True,stdout=subprocess.PIPE, 
					  stderr = subprocess.PIPE,
					  universal_newlines=True)
output, stderr = p1.communicate()	
			  
print(format(output))
 		  
src_dir=os.getcwd()+"/windbot_master/bin/Debug/Decks/"+ deck1
dst_dir=os.getcwd()+"/KoishiPro_Sakura/deck/"+ deck1
shutil.copy(src_dir,dst_dir)