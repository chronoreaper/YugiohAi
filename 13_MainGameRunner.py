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
    print("hosting game")
    #time.sleep(2.5)
    
    #print("click lan button")
    #subprocess.run([os.getcwd() + "/131_ClickImage.py","LanBut.png"],shell=True)
    #time.sleep(0.3)
    
    #print("click host button")
    #subprocess.run([os.getcwd() + "/131_ClickImage.py","HostBut.png"],shell=True)
    #time.sleep(1)
    
    #subprocess.run([os.getcwd() + "/131_ClickImage.py","dontCheckBut.png"],
    #               shell=True)
    #time.sleep(0.1)
    #subprocess.run([os.getcwd() + "/131_ClickImage.py","okBut.png"],shell=True)
    time.sleep(3)
    
    subprocess.run([os.getcwd() + "/131_ClickImage.py","spectateBut.png"],
                   shell=True)
AI1 = 'Random'
AI2 = 'Random2'
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
    print("running game " + str(gameCount))
    #subprocess.Popen - does not wait to finish
    #subprocess.run - waits to finish
    subprocess.Popen([os.getcwd() + "/132_runYgoPro.py"], 
                 shell=True, stdin=None, stdout=None,
                 stderr=None, close_fds=True)
    check = 0
    
    hostGame()
    
    time.sleep(0.2)
	
    p1 = subprocess.Popen([os.getcwd() + "/133_runAi.py",AI1,'bot1','1'],
                          shell=True,stdout=subprocess.PIPE, 
                          stderr = subprocess.PIPE,
                          universal_newlines=True)
    time.sleep(0.2)
    p2 = subprocess.Popen([os.getcwd() + "/133_runAi.py",AI2,'bot2','0'],
                          shell=True)
    
    if (p1.poll() == None or p2.poll() == None):
        time.sleep(2)
	
    time.sleep(0.5)
    
    print("click start")
    subprocess.run([os.getcwd() + "/131_ClickImage.py","startBut.png"],shell=True)
    
    if (not (p1.poll() == None or p2.poll() == None)) and check == 0:
        print("WARNING! ai is not running")
        check = 1
      
    count = 0
    
    #make sure the game does not run longer than 50 sec
	#ends the ygopro program as soon as the ais are done. Ais play faster than what you see.
    while count < 50 and (p1.poll() == None or p2.poll() == None):
        time.sleep(1)
        count += 1
      
    os.system("TASKKILL /F /IM ygopro.exe")    
    
    output, stderr = p1.communicate()
    print(output)
    if format(output).find('win') >= 0:
        win1 += 1
    elif format(output).find('lose') >= 0:
        win2 += 1  
      
    #if result != 0:  
    #    winWeight += result 
    gameCount += 1

# Save the deck list

conn = sqlite3.connect(os.getcwd() +'/cardData.cdb')
c = conn.cursor()

newDeckname = str(generation) + "_"+ str(subGen) + "_"+ str(result)+ deck1 
src_dir=os.getcwd()+"/windbot_master/bin/Debug/Decks/"+ deck1
dst_dir=os.getcwd()+"/KoishiPro_Sakura/deck/"+ newDeckname
shutil.copy(src_dir,dst_dir)

newDeckname = str(generation) + "_"+ str(subGen) + "_"+ str(result * -1)+ deck2
src_dir=os.getcwd()+"/windbot_master/bin/Debug/Decks/"+ deck2
dst_dir=os.getcwd()+"/KoishiPro_Sakura/deck/"+ newDeckname
shutil.copy(src_dir,dst_dir)

card_list = {}
card_list = readDict(os.getcwd() +"/cardData.txt",':')
cardListSize = len(card_list)

deckList = open(os.getcwd() 
                +"/windbot_master/bin/Debug/Decks/"+ deck1 ,"r") 

deckListOther = open(os.getcwd() 
                +"/windbot_master/bin/Debug/Decks/"+ deck2 ,"r")

#if abs(winWeight) < gameCount/2:
#    winWeight = winWeight - gameCount
win1 = win1/gameCount
win2 = win2/gameCount
for l in deckList:
    if len(l)>3:
        if l[0] !='#' and l[0] != '!':
            cardId = l.strip()
			if cardId not in deckListOther:
				c.execute('SELECT win,games,percentage FROM cardList where id = (?)', (int(cardId),))
				list = c.fetchone()
				wins = int(list[0])
				wins += win1
				games = int(list[1]) + gameCount
				percentage = wins/games
				c.execute('UPDATE cardList SET win = (?), games = (?), percentage = (?) WHERE id = (?)', (wins, games, percentage, int(cardId)))
				conn.commit()
	    
for l in deckListOther:
    if len(l)>3:
        if l[0] !='#' and l[0] != '!':
		    cardId = l.strip()
			if cardId not in deckList:
				c.execute('SELECT win,games,percentage FROM cardList where id = (?)', (int(cardId)),))
				list = c.fetchone()
				wins = int(list[0])
				wins += win2
				games = int(list[1]) + gameCount
				percentage = wins/games
				c.execute('UPDATE cardList SET win = (?), games = (?), percentage = (?) WHERE id = (?)', (wins, games, percentage, int(cardId)))
				conn.commit()	    
            
deckList.close()
deckListOther.close()
c.close()