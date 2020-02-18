import sqlite3, os

conn = sqlite3.connect(os.getcwd() +'/windbot_master/bin/Debug/cards.cdb')
file = open("cardData.txt","w")

c = conn.cursor()

cardList = {}

for row in c.execute('SELECT id FROM datas where type=17 AND ot=3 AND level=4'):
    cardList[row[0]] = 0

for key in cardList:
    file.write(str(key) + ":" + str(cardList[key])+ "\n")
    
file.close()
