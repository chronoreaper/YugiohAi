import sys, os
import random
from random import randrange
from collections import OrderedDict


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

deckSize = 20
topCards = 15
topCardsRange = max(topCards, 40)

#adds random card to main 
i = 0
while (i < topCards):
    card = card_weight_sorted[randrange(topCardsRange)]
    if deck_list_main.count(card)<3:
        deck_list_main.append(card) 
        i += 1
i = 0
while (i < deckSize - topCards):
    card = card_list[randrange(len(card_list))]
    if deck_list_main.count(card)<3:
        deck_list_main.append(card) 
        i += 1

#f = open(os.getcwd() + '/windbot_master/bin/Debug/Decks/AI_Random.ydk' ,"w+")
f = open(os.getcwd() + '/windbot_master/bin/Debug/Decks/'+ sys.argv[1] ,"w+")    

f.write("#created by deck_maker_ai\n")

f.write("#main\n")
cardcount=0

for i in deck_list_main:
    f.write(i +'\n')
f.write("#extra\n")
f.write("!side\n")
#for i in deck_list_side:
#    f.write(i +'\n')    

f.close()