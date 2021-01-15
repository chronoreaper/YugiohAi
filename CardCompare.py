import sqlite3, os
from difflib import SequenceMatcher

def similar(a, b):
    '''
    Takes two string a, b
    returns a double from 0 - 1 that is how similar they are
    '''
    return 1 - SequenceMatcher(None, a, b).ratio()

Name = "Luster Dragon"
text = ""
attack = 0

conn = sqlite3.connect(os.getcwd() +'/windbot_master/bin/Debug/cards.cdb')

c = conn.cursor()


for row in c.execute('SELECT texts.name, texts.desc, datas.type,datas.atk From texts Inner JOIN datas ON texts.id = datas.id where texts.name = "'+ Name +'"'):
    text = row[1]
    Name = row[0]
    attack = int(row[3])
    if row[2] == 17:
       text = ""
    print(Name + ":" + text)
    print("_____")

related = {}
relatedText = {}

for row in c.execute('SELECT texts.name, texts.desc, datas.type,datas.atk From texts Inner JOIN datas ON texts.id = datas.id where texts.name != "'+ Name +'"'):
	cardName = row[0]
	cardText = row[1]
	cardAttack = int(row[3])
	if row[2] == 17:
		cardText = ""
	related[cardName] = similar(text,cardText) + (abs(attack - cardAttack)/5000)
	relatedText[cardName] = cardText

related = {k: v for k, v in sorted(related.items(), key=lambda item: item[1], reverse=False)}

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