import sys, os
import random
from random import randrange
from collections import OrderedDict
import sqlite3

def AddToDeck(cards, limit, deck_main ):
	count = 0
	index = 0
	while (count < limit and len(cards) > 0):
		#index = random.randint(0,len(cards)-1)
		card = cards[index][0]
		quant = 3#cards[index][1]
		if deck_main.count(str(card))==0:#if not in list
			# make sure you dont exceed the limit
			if count + quant > limit:
				quant = limit - count
			#print("	Adding "+str(card)+ " x"+str(quant))
			for x in range(quant):
				deck_main.append(str(card)) 
				count += 1
		del cards[index]#Remove it from the list
	return deck_main

def AddToDeckRandom(cards, limit, deck_main ):
	count = 0
	while (count < limit and len(cards) > 0):
		index = random.randint(0,len(cards)-1)
		card = cards[index][0]
		quant = random.randint(0,2)+1
		
		if deck_main.count(str(card))==0:#if not in list
			# make sure you dont exceed the limit
			if count + quant > limit:
				quant = limit - count
			#print("	Adding "+str(card)+ " x"+str(quant))
			for x in range(quant):
				deck_main.append(str(card)) 
				count += 1
			del cards[index]#Remove it from the list
	return deck_main

conn = sqlite3.connect(os.getcwd() +'/cardData.cdb')
c = conn.cursor()

#select t.id, t1.name, t.relatedQuant, wins, games, wins*1.0/games as percentage, wins*wins*1.0/games as #weight
#from cardRelated t
#join cardList t1 on t1.id = t.id 
#join cardList t2 on t2.id = t.relatedid
#where t.id = t.relatedid
#group by t1.name
#order by weight desc

# these contain strings
deck_main = []
deck_extr = []
deck_side = []

deckSize = 40
topCards = 30
topCardsRange = min(topCards, 40)

#adds random card to main 

'''c.execute('select id, idQuant, sum(wins)/sum(games) as weight '+
	'from cardRelated '+
	'where id = relatedid '+
	'group by id '+
	'order by weight desc '+
	'LIMIT (?)', (topCards,))
cards = c.fetchall()'''

cards = []
sql = "SELECT id, sum(activation) as a, sum(games), sum(activation)/ sum(games) as value from playCard GROUP BY id order by a desc"
c.execute(sql)
records = c.fetchall()

avg = "select avg(a) from (" + sql + ")"
c.execute(avg)
avg = c.fetchone()[0]
if (avg != None):
	avg = float(avg)
else:
	avg = 1
if avg == 0:
	avg = 1

for record in records:
	name = record[0]
	weight = record[1]
	c.execute("SELECT id from cardList where name = (?)", (name,))
	cardId = c.fetchone()
	if cardId == None:
		continue
	cardId = cardId[0]
	cards.append((cardId, name, weight / avg))
	

deck_main = AddToDeck(cards,topCardsRange, deck_main)

c.execute('SELECT id from cardList ORDER BY RANDOM()')
cards = c.fetchall()
deck_main = AddToDeckRandom(cards,deckSize - len(deck_main), deck_main)

#f = open(os.getcwd() + '/windbot_master/bin/Debug/Decks/AI_Random.ydk' ,"w+")
f = open(os.getcwd() + '/windbot_master/bin/Debug/Decks/'+ sys.argv[1] ,"w+")    

f.write("#created by deck_maker_ai\n")

f.write("#main\n")
cardcount=0
#print("----")
for i in deck_main:
    #print(i)
    f.write(i +'\n')
f.write("#extra\n")
f.write("!side\n")
#for i in deck_side:
#    f.write(i +'\n')    

f.close()