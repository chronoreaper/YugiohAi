import sys, os
import random
from random import randrange
from collections import OrderedDict
import sqlite3


def readDict(filename, sep):
    with open(filename, "r") as f:
        d = {}
        for line in f:
            if len(line) > 6:
                values = line.split(sep)
                d[values[0]] = int(values[1])
        f.close()
        return(d)

cardList = open("cardData.txt","r")
conn = sqlite3.connect(os.getcwd() +'/cardData.cdb')
c = conn.cursor()

card_weight = {}
card_list = []
deck_list_main = []
deck_list_extr = []
deck_list_side = []

#adds all cards in the cardList file
for l in cardList:
    name = l[ :l.find(':')]
    card_list.append(name)
    
card_weight = readDict('cardData.txt',':')

card_weight_sorted = sorted(card_weight, key=card_weight.get, reverse=True)

deck_list_main = []

deckSize = 40
topCards = 0
topCardsRange = max(topCards, 40)

#adds random card to main 

c.execute('SELECT id,name from cardList ORDER BY percentage DESC LIMIT (?)', (topCards,))
for row in c.fetchall():
    deck_list_main.append(str(row[0])) 

#i = 0
#while (i < topCards):
    #card = card_weight_sorted[randrange(topCardsRange)]
    #if deck_list_main.count(card)<1:#3:
        #deck_list_main.append(card) 
        #i += 1
c.execute('SELECT id,name from cardList ORDER BY RANDOM()')
lst = c.fetchall()
count = 0
index = 0
while (count < deckSize - topCards):
    card = lst[index][0]#card_list[randrange(len(card_list))]
    index += 1
    if deck_list_main.count(card)<1:#3:
        deck_list_main.append(str(card)) 
        count += 1

#f = open(os.getcwd() + '/windbot_master/bin/Debug/Decks/AI_Random.ydk' ,"w+")
f = open(os.getcwd() + '/windbot_master/bin/Debug/Decks/'+ sys.argv[1] ,"w+")    

f.write("#created by deck_maker_ai\n")

f.write("#main\n")
cardcount=0
#print("----")
for i in deck_list_main:
    #print(i)
    f.write(i +'\n')
f.write("#extra\n")
f.write("!side\n")
#for i in deck_list_side:
#    f.write(i +'\n')    

f.close()