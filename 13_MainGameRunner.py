import sys, string, os, time
import subprocess
import shutil
import sqlite3

def writeDict(d, filename, sep):
	with open(filename, "w") as f:
		for i in d.keys():			  
			f.write(i + sep + str(d[i])+"\n")
		f.close()
			
def readDict(filename, sep):
	with open(filename, "r") as f:
		d = {}
		for line in f:
			if len(line) > 6:
				values = line.split(sep)
				d[values[0]] = int(values[1])
		f.close()
		return(d)
	
def hostGame(): 
	print("	hosting game")
	#time.sleep(2.5)
	
	#print("click lan button")
	#subprocess.run([os.getcwd() + "/131_ClickImage.py","LanBut.png"],shell=True)
	#time.sleep(0.3)
	
	#print("click host button")
	#subprocess.run([os.getcwd() + "/131_ClickImage.py","HostBut.png"],shell=True)
	#time.sleep(1)
	
	#subprocess.run([os.getcwd() + "/131_ClickImage.py","dontCheckBut.png"],
	#				shell=True)
	#time.sleep(0.1)
	#subprocess.run([os.getcwd() + "/131_ClickImage.py","okBut.png"],shell=True)
	time.sleep(3)
	
	subprocess.run([os.getcwd() + "/131_ClickImage.py","spectateBut.png"],
				   shell=True)
				   
	time.sleep(0.1)
	
	subprocess.run([os.getcwd() + "/131_ClickImage.py","spectateBut.png"],
			   shell=True)
def GetCardQuantity(deck):
	dict = {}
	deckFile = open(os.getcwd() 
				+"/windbot_master/bin/Debug/Decks/"+ deck ,"r")
	for card in deckFile:
		cardId = card.strip()
		if len(cardId)>3:
			if cardId[0] !='#' and cardId[0] != '!':
				if cardId not in dict:
					dict[cardId] = 1
				else:
					dict[cardId] = dict[cardId] + 1
	deckFile.close()
	
	return dict

def GetGameLog(deck):
	file = []
	deckFile = open(os.getcwd()+'/windbot_master/bin/Debug/'+ deck + ".txt" ,"r")
	for l in deckFile:
		if len(l)>3:
			file.append(l.split("]")[1].strip())
	deckFile.close()
	
	return file
	
def UpdateDatabase(deck, deckQuant, deckOther, deckQuantOther,result,name):
	# Takes in two list and a dictonary
	# deck = the deck you are saving, list of ids
	# deckQuant= the quantity of all the cards in your deck
	# deckOther = the other deck
	# deckQuantOther=the quantity of all the cards in the other deck
	# this takes a long time, need to optimize
	conn = sqlite3.connect(os.getcwd() +'/cardData.cdb')
	c = conn.cursor()
	#record the relationship in the deck
	for card in deckQuant:
		for related in deckQuant:
			if (related not in deckQuantOther or deckQuant[related] != deckQuantOther[related]) and (card not in deckQuantOther or deckQuant[card] != deckQuantOther[card]):
				c.execute('SELECT wins,gamesPlayed, games FROM cardRelated where id = (?) and relatedid = (?) and idQuant = (?) and relatedQuant = (?) and inprogress = (?)', 
					(int(card),int(related),deckQuant[card],deckQuant[related],name))
				list = c.fetchone()
				if list != None:
					wins = int(list[0])
					gamesPlayed = int(list[1])
					games = int(list[2]) + 1
					if card in deck: # Run only if played
						gamesPlayed += 1
						wins += result
					value = (wins, gamesPlayed ,games, int(card),int(related),deckQuant[card],deckQuant[related],name)
					c.execute('UPDATE cardRelated SET wins = (?), gamesPlayed = (?), games = (?) WHERE id = (?) and relatedid = (?) and idQuant = (?) and relatedQuant = (?) and inprogress = (?)', 
					value)
				else:
					value = (card,related,deckQuant[card],deckQuant[related],result,1,1,name)
					c.execute('INSERT INTO cardRelated VALUES (?,?,?,?,?,?,?,?)', value)				
	conn.commit()
	
	# Record the relationship against the other deck
	i = 0
	j = 0
	for card in deck:
		j=0
		for related in deckOther:
			if related != card:
				if related not in deck:
					if card not in deckOther:
						if card not in deck[i+1:]:
							if related not in deckOther[j+1:]:
								c.execute('SELECT wins,games FROM cardCounter where id = (?) and otherid = (?) and inprogress = (?)', (int(card),int(related),name))
								list = c.fetchone()
								if list != None:
									wins = int(list[0]) + result
									games = int(list[1]) + 1
									value = (wins ,games,int(card),int(related),name)
									c.execute('UPDATE cardCounter SET wins = (?), games = (?) WHERE id = (?) and otherid = (?) and inprogress = (?)', value)
								else:
									value = (card,related,result,1, name)
									c.execute('INSERT INTO cardCounter VALUES (?,?,?,?,?)', value)
			j +=1
		i +=1
	conn.commit()
	c.close()
	
AIName1 = 'bot1'
AIName2 = 'bot2'	

#The Deck name and location	
AI1Deck = 'Random'
AI2Deck = 'Random2'
deck1 = 'AI_Random.ydk'
deck2 = 'AI_Random2.ydk'

winWeight = 0
gameCount = 0

generation = sys.argv[1]
subGen = sys.argv[2]

result = 0
win1 = 0
win2 = 0
	
#how many games to play with this deck
while gameCount < 1:  
	win1 = 0
	win2 = 0
	
	print("	running game " + str(gameCount))
	#subprocess.Popen - does not wait to finish
	#subprocess.run - waits to finish
	subprocess.Popen([os.getcwd() + "/132_runYgoPro.py"], 
				 shell=True, stdin=None, stdout=None,
				 stderr=None, close_fds=True)
	check = 0
	
	hostGame()
	
	time.sleep(0.2)
	
	print("	runningAi1")
	p1 = subprocess.Popen([os.getcwd() + "/133_runAi.py",AI1Deck,AIName1,'1'],
						  shell=True,stdout=subprocess.PIPE, 
						  stderr = subprocess.PIPE,
						  universal_newlines=True)
	time.sleep(1)
	print("	runningAi2")
	p2 = subprocess.Popen([os.getcwd() + "/133_runAi.py",AI2Deck,AIName2,'0'],
						  shell=True)
	
	if (p1.poll() == None or p2.poll() == None):
		time.sleep(1)
	
	time.sleep(0.5)
	
	print("	click start")
	subprocess.run([os.getcwd() + "/131_ClickImage.py","startBut.png"],shell=True)
	
	if (not (p1.poll() == None or p2.poll() == None)) and check == 0:
		print("	WARNING! ai is not running")
		check = 1
	  
	count = 0
	globalTimeOut = 60*5
	
	#make sure the game does not run longer than needed
	#ends the ygopro program as soon as the ais are done. Ais play faster than what you see.
	#
	while globalTimeOut > 0 and ((count < 300 and (p1.poll() == None or p2.poll() == None)) or (p1.poll() == None and p2.poll() != None) or (p1.poll() != None and p2.poll() == None)):
		time.sleep(1)
		count += 1
		globalTimeOut -=1
		
	# the game probably never started
	if globalTimeOut <= 0 or (count>= 300 and (p1.poll() == None and p2.poll() == None)):
		print("	Game too long to finish")
		
	print("	Game took "+str(count)+" seconds.")
		
	os.system("	TASKKILL /F /IM ygopro.exe")	   
	
	output, stderr = p1.communicate()
	print("	"+output)
	if format(output).find('win') >= 0:
		win1 += 1
	elif format(output).find('lose') >= 0:
		win2 += 1  
	  
	gameCount += 1
	
	print("	Saving deck to database")
	# Save to database
	deckList = GetGameLog(AIName1)
	deckListOther = GetGameLog(AIName2)

	deckQuant = GetCardQuantity(deck1)	
	deckQuantOther = GetCardQuantity(deck2)

	time.sleep(1)
	
	print("	Saving Deck 1 Results")
	UpdateDatabase(deckList,deckQuant,deckListOther,deckQuantOther, win1, AIName1)

	print("	Saving Deck 2 Results")
	UpdateDatabase(deckListOther,deckQuantOther,deckList,deckQuant, win2, AIName2)
	
# Copy decks
newDeckname = str(generation) + "_"+ str(subGen) + "_"+ str(win1)+ deck1 
src_dir=os.getcwd()+"/windbot_master/bin/Debug/Decks/"+ deck1
dst_dir=os.getcwd()+"/KoishiPro_Sakura/deck/"+ newDeckname
shutil.copy(src_dir,dst_dir)

newDeckname = str(generation) + "_"+ str(subGen) + "_"+ str(win2)+ deck2
src_dir=os.getcwd()+"/windbot_master/bin/Debug/Decks/"+ deck2
dst_dir=os.getcwd()+"/KoishiPro_Sakura/deck/"+ newDeckname
shutil.copy(src_dir,dst_dir)