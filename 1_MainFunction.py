import sys, os, subprocess, time, glob
import datetime
import argparse
import sqlite3

def Log(string):
    file = open("log.txt","a")
    print(string)
    file.write(string+"\n")
    file.close()
	
generation = 0
	
winCount =[]
loseCount = []

start = time.time()
error = 0 
warning = 0
#Erase Previous Log
file = open("log.txt","w")
file.write("")
file.close()

#TODO Copy config file


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
matches = 1
gamesToPlay = 1
count = 1
if "--repeat" in sys.argv:
	repeatFor = int(sys.argv[sys.argv.index("--repeat")+1])
	
if "--matches" in sys.argv:
	matches = int(sys.argv[sys.argv.index("--matches")+1])
	
if "--games" in sys.argv:
	gamesToPlay = int(sys.argv[sys.argv.index("--games")+1])
	
print("Running " + str(repeatFor) + " times X "+ str(matches) + " matches X " + str(gamesToPlay) + " games")

while (count <= repeatFor):
	print("Running:"+str(count))
	matchNum = 1
	#subprocess.run([os.getcwd() + "/12_makeDeck.py", "AI_Random.ydk"],shell=True)
	#subprocess.run([os.getcwd() + "/12_makeDeck.py", "AI_Random2.ydk"],shell=True)
	while ((matchNum <= matches) and (error == 0) and (warning < 3)):
		file = open("log.txt","a")
		print("Match:"+str(matchNum))
		file.write("Match:"+str(matchNum)+"\n")
		print("making decks")


		subprocess.run([os.getcwd() + "/ShuffleDeck.py", "AI_Random.ydk"],shell=True)
		subprocess.run([os.getcwd() + "/ShuffleDeck.py", "AI_Random2.ydk"],shell=True)
		#Runs the game

		print("running game")

		#subprocess.run([os.getcwd() + "/13_MainGameRunner.py",str(generation),str(count),str(gamesToPlay)],shell=True)
		p = subprocess.Popen([os.getcwd() + "/13_MainGameRunner.py",str(count),str(matchNum),str(gamesToPlay)],
								shell=True,stdout=subprocess.PIPE,stderr=None,
								universal_newlines=True)

		output = p.stdout.read()
		#print(output)
		winCount.append(output.count('[win]'))
		loseCount.append(output.count('[lose]'))

		# print stats
		wins = 0
		losses = 0
		for i in range(matchNum - 1 ,-1,-1):
			wins += winCount[i]
			losses += loseCount[i]

		print(f"{output.count('[win]')} / {output.count('[win]') + output.count('[lose]')} this round")
		percentage = (wins/(losses + wins)) * 100
		print(f"{percentage}% {wins}/{wins + losses} Total Win rate")
		#if '[mlerr]' in output:
		err = output.split('[mlerr]')[1]
		print(f"{err} Squared error")
		
		if matchNum > 10:
			wins = 0
			losses = 0
			for i in range(matchNum - 1 ,matchNum - 11,-1):
				wins += winCount[i]
				losses += loseCount[i]
				
			percentage = (wins/(losses + wins)) * 100
			print(f"{percentage}% {wins}/{wins + losses} Total Win rate last 10 cycles")
			#if percentage > 65:
			#	exit(0)
		#if output.find("!ERROR!"):
			#error = 1
		#if output.find("WARNING!"):
			#warning += 1

		matchNum += 1
		file.close()
	#conn = sqlite3.connect(os.getcwd() +'/cardData.cdb')
	#c = conn.cursor()
	#c.execute('UPDATE playCard SET inprogress = (?) WHERE inprogress = \"master\"', ("master"+str(count),))
	#conn.commit()
	count += 1

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