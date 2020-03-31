import sqlite3, os

conn = sqlite3.connect(os.getcwd() +'/windbot_master/bin/Debug/cards.cdb')
conn2 = sqlite3.connect(os.getcwd() +'/cardData.cdb')

file = open("cardData.txt","w")

c = conn.cursor()
c2 = conn2.cursor()

c2.execute('DELETE FROM cardList')

cardList = {}

c.execute('SELECT texts.id, texts.name From texts '+ 
                     'Inner JOIN datas ON texts.id = datas.id '+
                     'WHERE ot=3 AND level=4 AND type=17 AND def < 2000')
					 
for row in c.fetchall():
    print("	Adding to DB:" + str(row))
    value = (row[0], row[1],0,0,0)
    c2.execute('INSERT INTO cardList VALUES (?,?,?,?,?)', value)
    conn2.commit()
    
    cardList[row[0]] = 0

for key in cardList:
    file.write(str(key) + ":" + str(cardList[key])+ "\n")
    
file.close()
c.close()
c2.close()


