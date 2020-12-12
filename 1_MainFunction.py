import sys, os, subprocess, time, glob
import datetime
import argparse

def Log(string):
    file = open("log.txt","a")
    print(string)
    file.write(string+"\n")
    file.close()
	
generation = 0
	
start = time.time()
error = 0 
warning = 0
#Erase Previous Log
file = open("log.txt","w")
file.write("")
file.close()

#Erase FLList or there will be errors
file = open(os.getcwd() + "/KoishiPro_Sakura/lflist.conf","w")
file.write("")
file.close()

if len(sys.argv)>1 and ("-reset" in sys.argv or "-r" in sys.argv):
	print("Setting up cards in database")
	#gets all the cards from the database
	subprocess.run([os.getcwd() + "/11_SqlReader.py"],shell=True)

print("deleting old deck files from ygopro")
files = glob.glob(os.getcwd() +"/KoishiPro_Sakura/deck/*")
for f in files:
    os.remove(f)
	
print("deleting old replays from ygopro")
files = glob.glob(os.getcwd() +"/KoishiPro_Sakura/replay/*")
for f in files:
    os.remove(f)

startNoSetup = time.time()
print("done set up")
#makes the two random decks
repeatFor = 3
gamesToPlay = 1
count = 1
if "--repeat" in sys.argv:
	repeatFor = int(sys.argv[sys.argv.index("--repeat")+1])
	
if "--games" in sys.argv:
	gamesToPlay = int(sys.argv[sys.argv.index("--games")+1])
	
print("Running " + str(repeatFor) + " times X " + str(gamesToPlay) + " games")
while ((count <= repeatFor) and (error == 0) and (warning < 3)):
    file = open("log.txt","a")
    print("Game:"+str(count))
    file.write("Game:"+str(count)+"\n")
    print("making decks")
    
    #subprocess.run([os.getcwd() + "/12_makeDeck.py", "AI_Random.ydk"],shell=True)
    #subprocess.run([os.getcwd() + "/12_makeDeck.py", "AI_Random2.ydk"],shell=True)
    
    #Runs the game
    
    print("running game")
    
    subprocess.run([os.getcwd() + "/13_MainGameRunner.py",str(generation),str(count),str(gamesToPlay)],shell=True)
                   #stdout=subprocess.PIPE)
    
    #output = p.stdout.read().decode("utf-8") 
    #print(output)
    
    #if output.find("!ERROR!"):
        #error = 1
    #if output.find("WARNING!"):
        #warning += 1
    
    count+=1
    file.close()

file = open("log.txt","a")
end = time.time()

Log("Time Past:" + str(datetime.timedelta(seconds=int(end - start))))
Log("Time Past Excluding Setup:" + str(datetime.timedelta(seconds=int(end - startNoSetup))))
Log("Average Game Time:"+str(datetime.timedelta(seconds=int((end - startNoSetup)/(count-1)))))
if error == 1:
    print("there were errors")
if warning >= 3:
    print("too many warnings")
file.close()