import sqlite3, os
from difflib import SequenceMatcher

def similar(a, b):
    return SequenceMatcher(None, a, b).ratio()

conn = sqlite3.connect(os.getcwd() +'/windbot_master/bin/Debug/cards.cdb')

c = conn.cursor()
text = ""

for row in c.execute('SELECT name, desc FROM texts where name="Monster Reborn"'):
    print(row)
    text = row[1]

related = {}
relatedText = {}

for row in c.execute('SELECT name, desc FROM texts where name!="Monster Reborn"'):
    related[row[0]] = similar(text,row[1])
    relatedText[row[0]] = row[1]

related = {k: v for k, v in sorted(related.items(), key=lambda item: item[1], reverse=True)}

i = 10
for key in related:
    if i > 0:
        i -= 1
        print(key + ":" + str(related[key]) + ":" + relatedText[key])
        print("_____")

# card_select = c.execute('SELECT desc FROM texts where id=10000')
# cardList =  {}# id to text
# card_similar = {} # id to similarity

# i = 0;
# for row in c.execute('SELECT id, desc FROM texts'):
#     cardList[row[0]] = row[1]
#     i +=1

# for key in cardList:
#     textdistance.hamming.normalized_similarity(card_select, cardList[key])


# card_weight_sorted = sorted(card_similar, key=card_similar.get, reverse=True)

# i = 0
# for key in card_weight_sorted:
#     if i < 10:
#         i +=1
#         print(str(key) +" " + str(card_similar[key]))