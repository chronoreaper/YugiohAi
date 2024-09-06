import os
import sqlite3
import random, os, sys
from typing import List


card_pool = []

class Deck:
  main = []
  extra = []
  side = []


def generate_deck(deck_name):
    input_path = os.getcwd() + '/deck_gen' + deck_name
    output_path = os.getcwd() + '/decks' + deck_name

    core, variants = _get_data(input_path)
    final_deck:Deck = _create_deck(core, variants)
    f = open(output_path, "w")    
    f.write("#created by deck_maker_ai\n")
    f.write("#main\n")
    for i in final_deck.main:
        f.write(i +'\n')
    f.write("#extra\n")
    for i in final_deck.extra:
        f.write(i +'\n')
    f.write("!side\n")
    for i in final_deck.side:
        f.write(i +'\n')    

    f.close()

def _get_data(file_path):
    core = Deck()
    variants = []
    to_add = Deck()
    f = open(file_path,"r")
    part = 0
    mode = 0
    for line in f.readlines():

        if "#extra" in line:
            part = 1
            continue
        elif "!side" in line:
            part = 2
            continue
        elif "#main" in line:
            part = 0
            if (mode == 1):
                variants.append(to_add)
                to_add = Deck()
            continue
        elif "#variant" in line:
            mode = 1
            continue
        elif "#" in line:
            continue
        
        if (mode == 0):
            if part == 0:
                core.main.append(line.strip())
            elif part == 1:
                core.extra.append(line.strip())
            else:
                core.side.append(line.strip())
        else:
            if part == 0:
                to_add.main.append(line.strip())
            elif part == 1:
                to_add.extra.append(line.strip())
            else:
                to_add.side.append(line.strip())

    if (mode == 1):
        variants.append(to_add)
    f.close()

    return core,variants

def _create_deck(core:Deck, variant:List[Deck], main_limit = 40) -> Deck:
    random.shuffle(variant)

    while(len(variant) > 0):
        cur:Deck = variant.pop(0)

        core_all:List[int] = core.main.copy()
        core_all.extend(core.extra)
        core_all.extend(core.side)

        cur_all:List[int] = cur.main.copy()
        cur_all.extend(cur.extra)
        cur_all.extend(cur.side)

        if intersect(core_all, cur_all):
            continue
        if (len(core.extra) + len(cur.extra) > 15):
            continue
        if (len(core.side) + len(cur.side) > 15):
            continue


        core.main.extend(cur.main)
        core.extra.extend(cur.extra)
        core.side.extend(cur.side)

        if (len(core.main) > main_limit):
            break
    return core


def GetCardPool():
	dbfile = './cardData.cdb'
	con = sqlite3.connect(dbfile)
	cur = con.cursor()
	cur.execute('SELECT distinct CardId from GameStats')
	result = cur.fetchone()

	ForbiddenLimitedUpdate()

# Updates the Card Pool based on Limited list
# Assumes that the card pool contains 3 of each card
def ForbiddenLimitedUpdate():
	file_path = os.getcwd() + "/edopro_bin/repositories/lflists/0TCG.lflist.conf"
	
	limited = -1

	f = open(file_path,"r")
	for line in f.readlines():

		if "#Forbidden" in line:
			limited = 0
			continue
		elif "#Limited" in line:
			limited = 1
			continue
		elif "#Semi-limited" in line:
			limited = 2
			continue
		elif "#" == line[0] or "!" == line[0]:
			continue

		card = line.split(' ')[0]
		if card in card_pool:
			for _ in range(3 - limited):
				card_pool.remove(card)

	f.close()
     

def intersect(lst1, lst2):
    return len(list(set(lst1) & set(lst2))) > 0

if __name__ == "__main__":
    generate_deck(sys.argv[1])