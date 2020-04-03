import sqlite3, os

#select t.id, t1.name,t.[relatedquantity], wins, games, wins*1.0/games as percentage
#from cardRelated t
#join cardList t1 on t1.id = t.id 
#join cardList t2 on t2.id = t.relatedid
#where t.id = t.relatedid
#order by wins desc, percentage desc

conn = sqlite3.connect(os.getcwd() +'/windbot_master/bin/Debug/cards.cdb')
conn2 = sqlite3.connect(os.getcwd() +'/cardData.cdb')

c = conn.cursor()
c2 = conn2.cursor()

print("	Resetting DB Values")
c2.execute('DELETE FROM cardList')
c2.execute('DELETE FROM cardRelated')

cardList = {}

c.execute('SELECT texts.id, texts.name From texts '+ 
                     'Inner JOIN datas ON texts.id = datas.id '+
                     'WHERE ot=3 AND level=4 AND type=17 AND def < 2000 AND atk >= 1800 ' +
					 'or texts.name like "pot of greed" '+
					 'or texts.name like "raigeki" ' +
					 'or texts.name like "sparks" '+
					 'or texts.name like "red medicine" '+
					 'or texts.name like "ookazi" '+
					 'or texts.name like "fossil dig" '+
					 'GROUP by texts.name')
count = 0				 
for row in c.fetchall():
    print("	Adding to DB:" + str(row))
    value = (row[0], row[1])
    c2.execute('INSERT INTO cardList VALUES (?,?)', value)
    
    cardList[row[0]] = 0
    count += 1
	
conn2.commit()

print("	"+str(count)+" Cards in card pool") 
c.close()
c2.close()


