from difflib import SequenceMatcher

def similar(a, b):
    return SequenceMatcher(None, a, b).ratio()

# import sqlite3, os

# import textdistance

# conn = sqlite3.connect(os.getcwd() +'/windbot_master/bin/Debug/cards.cdb')

# c = conn.cursor()

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