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
c2.execute('DELETE FROM cardCounter')
c2.execute('DELETE FROM playCard')

cardList = {}
# for the other deck
#'WHERE texts.id = 84754430 '+
#'or texts.id = 10000000 '+
#'or texts.id = 51945556 '+
#'or texts.id = 14778250 '+
#'or texts.id = 31786629 '+
#'or texts.id = 7572887 '+
#'or texts.id = 84290642 '+
#'or texts.id = 25259669 '+
#'or texts.id = 74131780 '+
#'or texts.id = 16226786 '+
#'or texts.id = 91133740 '+
#'or texts.id = 53620899 '+
#'or texts.id = 8131171 '+
#'or texts.id = 12538374 '+

c.execute('SELECT texts.id, texts.name From texts '+ 
                     'Inner JOIN datas ON texts.id = datas.id '+
					 
					# 'WHERE texts.id = 47226949 '+
					# 'or texts.id = 35052053 '+
					# 'or texts.id = 81823360 '+
					# 'or texts.id = 84754430 '+
					# 'or texts.id = 91731841 '+
					# 'or texts.id = 92125819 '+
					# 'or texts.id = 11321183 '+
					# 'or texts.id = 14778250 '+
					# 'or texts.id = 7572887 '+
					# 'or texts.id = 12580477 '+
					# 'or texts.id = 19523799 '+
					# 'or texts.id = 47325505 '+
					# 'or texts.id = 55144522 '+
					# 'or texts.id = 79571449 '+
					
                     'where ot=3 and level=4 and type=33 and race = 1 and attribute = 1 ' +
					 'or texts.name like "pot of greed" '+
					 'or texts.name like "raigeki" ' +
					 'or texts.name like "sparks" '+
					 'or texts.name like "red medicine" '+
					 'or texts.name like "ookazi" '+
					 'or texts.name like "oops!" ' +
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


