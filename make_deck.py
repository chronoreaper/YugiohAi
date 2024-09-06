import sys, os
import random
from random import randrange
from collections import OrderedDict
import sqlite3

import datetime
import glob
import math
import os
import shutil
import sqlite3
import string
import subprocess
import sys
import time
import random
import typing
import csv
import numpy as np
import pickle
import itertools
from sklearn.model_selection import train_test_split
from sklearn.metrics import accuracy_score

import torch
import numpy as np
from typing import Dict, List, Tuple
import torch.optim
from torch.utils.data import Dataset, DataLoader
import torch.nn as nn
import torch.nn.functional as F
from torch.utils.data.dataloader import default_collate

from sys import platform
from pathlib import Path

dtype = torch.float
device = torch.device('cuda' if torch.cuda.is_available() else 'cpu')

"""
For Basic Nth order value getting
https://elvishjerricco.github.io/2015/09/24/automatically-generating-magic-decks.html
"""

CardInfoId = 0

class CardInfo():
	id:int
	card_id:int
	card_name:str
	quant:int
	location:int
	
	def __init__(self, card_id, card_name, quant, location, id = None):
		global CardInfoId
		if id is None:
			self.id = CardInfoId
			CardInfoId += 3 # id is for each quant
		else:
			self.id = id
		self.card_id = card_id
		self.card_name = card_name
		self.quant = quant
		self.location = location


# Torch Classes

class Data(Dataset):
  def __init__(self, X_train, y_train):
    # need to convert float64 to float32 else 
    # will get the following error
    # RuntimeError: expected scalar type Double but found Float
    self.X = torch.from_numpy(X_train.astype(np.float32))
    # need to convert float64 to Long else 
    # will get the following error
    # RuntimeError: expected scalar type Long but found Float
    self.y = torch.from_numpy(y_train).type(torch.LongTensor)
    self.len = self.X.shape[0]
  
  def __getitem__(self, index):
    return self.X[index], self.y[index]
  
  def __len__(self):
    return self.len
	
class Network(nn.Module):
  def __init__(self, input_dim, output_dim):
    super(Network, self).__init__()
    hidden_layers = (input_dim + output_dim) * 2

    self.layer1 = nn.Linear(input_dim, hidden_layers)
    self.layer2 = nn.Linear(hidden_layers, hidden_layers)
    self.output = nn.Linear(hidden_layers, output_dim)
    self.single = nn.Linear(input_dim, output_dim)
    #print(self.layer1.weight)
    #self.output.bias = nn.Parameter(bias)
    self.dropout1 = nn.Dropout(0.5)
    self.dropout2 = nn.Dropout(0.2)
    self.act1 = nn.Tanh()
    self.act2 = nn.ReLU()
    self.act0 = nn.Sigmoid()

  def forward(self, x):
    #x = self.single(x)
    x = self.layer1(x)
    x = self.dropout1(x)
    x = self.act2(x)

    # x = self.layer2(x)
    # x = self.act2(x)

    # x = self.layer2(x)
    # x = self.act2(x)

    x = self.output(x)
    #x = self.act0(x)
    return x
	
card_pool:Dict[int,CardInfo] = {}
game_id_to_win = {}

class CardPairings():
	card_ids:List[Tuple[str,int]]
	card_names:List[Tuple[str,int]]

	won_game_id:List[int]
	used_games_id:List[int]
	games_in_id:List[int]
	
	def __init__(self, card_ids, card_names) -> None:
		self.card_ids = card_ids
		self.card_ids.sort()
		self.card_names = card_names
		self.won_game_id = []
		self.used_games_id = []
		self.games_in_id = []
	
	def get_win_rate(self)->float:
		if (len(self.used_games_id) == 0):
			return 0
		return float(len(self.won_game_id))/float(len(self.games_in_id))
	
	def get_usage_rate(self)->float:
		if (len(self.used_games_id) == 0):
			return 0
		return float(len(self.used_games_id))/float(len(self.games_in_id))
	
	def get_win_usage_rate(self)->float:
		return self.get_win_played_games_rate() * self.get_usage_rate()
	
	def get_win_played_games_rate(self) -> float:
		if (len(self.used_games_id) == 0):
			return 0
		return float(len(set(self.won_game_id) & set(self.used_games_id))) / float(len(self.used_games_id))

	def name_string(self) -> str:
		s = ""
		for name in self.card_names:
			s += f"({name[0][:20]},{name[1]}),"

		return s

	def __repr__(self) -> np.str:
		#string = f"{str(self.card_names):150}"
		string = f"{self.name_string():100}"
		string += "Win Played:"
		string += f"{round(self.get_win_played_games_rate(), 2):<4} "
		# string += "Win Rate:"
		# string += f"{round(self.get_win_rate(), 2):<4} "
		# string += "Usage Rate:"
		# string += f"{round(self.get_usage_rate(), 2):<4} "
		string += "WinUsage Rate:"
		string += f"{round(self.get_win_usage_rate(), 2):<4}"
		return string

card_ranking:Dict[int,List[CardPairings]] = {}


def GetCardPool():
	print("Get Card pool db")
	# Get from db
	dbfile = './cardData.cdb'
	con = sqlite3.connect(dbfile)
	cur = con.cursor()
	cur.execute('SELECT distinct CardId,CardName,DeckLocation from GameStats')
	records = cur.fetchall()
	for record in records:
		id = record[0]
		name = record[1]
		location = record[2]
		card_pool[id] = CardInfo(id, name, 3, location)
	con.close()

	file_path = os.getcwd() + "/edopro_bin/deck/card_pool.ydk"
	
	print("Get Card pool file")
	# Get from file after
	location = 0
	f = open(file_path,"r")
	for line in f.readlines():
		if "#main" in line:
			location = 0
			continue
		elif "#extra" in line:
			location = 1
			continue
		elif "!side" in line:
			location = 2
			continue
		elif '#' in line:
			continue

		id = line.strip("\n")
		if id not in card_pool:
			card_pool[id] = CardInfo(id, "", 3, location)

	f.close()



	ForbiddenLimitedUpdate()


# Updates the Card Pool based on Limited list
# Assumes that the card pool contains 3 of each card
def ForbiddenLimitedUpdate():
	print("FL List update")
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

		card_id = line.split(' ')[0]
		if card_id in card_pool:
			# if limited == 0:
			# 	card_pool.pop(card_id)
			# else:
			card_pool[card_id].quant = limited

	f.close()

####### Get the nth order stats of each card with the relation to each other
def UpdateCardStats():
	print("update card stats")
	card_ranking[0] = []

	# Get card total played, games won
	dbfile = './cardData.cdb'
	con = sqlite3.connect(dbfile)
	cur = con.cursor()
	cur.execute('SELECT * from GameStats')
	records = cur.fetchall()
	for record in records:
		game_id = record[0]
		card_name = record[1]
		card_id = record[2]
		played = record[3]
		quant = record[4]

		if _combanationInCardRanking(0, [(card_id, quant)]) is None:
			parings = CardPairings([(card_id, quant)],[(card_name, quant)])
			card_ranking[0].append(parings)

		parings = _combanationInCardRanking(0, [(card_id, quant)])
		parings.games_in_id.append(game_id)
		if played == 'True':
			parings.used_games_id.append(game_id)
		if game_id_to_win[game_id] == 0:
			parings.won_game_id.append(game_id)


	con.close()

	print("making initial card rankings")
	card_ranking[0].sort(key=_sortWinUsageRate, reverse=True)
	PruneBadStats(0, 0.2, 0.1)
	for i in range(1,3):
		print(f"making {i}th order card rankings")
		_makeNthOrderParings(i)
		card_ranking[i].sort(key=_sortWinPlayedGames, reverse=True)
		PruneBadStats(i, 0.5, 0.2)

	if __name__ == "__main__":
		for j in card_ranking:
			for i in card_ranking[j]:
				print(i)

def PruneBadStats(order, winratemin = 0, winusagemin = 0):
	# for order in card_ranking:
	card_ranking[order] = [x for x in card_ranking[order] if x.get_win_rate() > winratemin and x.get_win_usage_rate() > winusagemin]
	card_ranking[order] = card_ranking[order][:100]

def _makeNthOrderParings(order:int):
	card_ranking[order] = []

	for i in range(len(card_ranking[order - 1])):
		for j in range(len(card_ranking[0])):
			base:CardPairings = card_ranking[0][j]
			compare:CardPairings = card_ranking[order - 1][i]

			combo:Tuple[str,int] = compare.card_ids.copy()
			combo.extend(base.card_ids)
			combo.sort()

			if len(set(base.card_ids) & set(compare.card_ids)) > 0: # No duplicate ids
				continue

			if _combanationInCardRanking(order, combo) is None:
				card_names_combo = compare.card_names.copy()
				card_names_combo.extend(base.card_names)
				parings = CardPairings(combo,card_names_combo)
				parings.games_in_id = (set(base.games_in_id) & set(compare.games_in_id))
				parings.won_game_id = (set(base.won_game_id) & set(compare.won_game_id))
				parings.used_games_id = (set(base.used_games_id) & set(compare.used_games_id))
				
				
				card_ranking[order].append(parings)
			else:
				continue # Duplicate entry

def _combanationInCardRanking(order:int, combo:List[Tuple[str,int]]) -> CardPairings:
	combo.sort()

	if order not in card_ranking:
		return None

	for parings in card_ranking[order]:
		#if len(set(parings.card_ids) & set(combo)) == len(parings.card_ids):
		if parings.card_ids == combo:
			return parings

	return None


def _getNextBestCard(cards:List[str]) -> List[int]:
	related:List[CardPairings] = []
	order = len(cards)
	if order not in card_ranking:
		return []
	
	for parings in card_ranking[order]:
		paring_ids = [x[0] for x in parings.card_ids]
		if len(set(cards) & set(paring_ids)) >= len(cards):
			to_add = CardPairings(list(set(paring_ids).difference(set(cards))), [])
			to_add.games_in_id = parings.games_in_id
			to_add.used_games_id = parings.used_games_id
			to_add.won_game_id = parings.won_game_id
			related.append(to_add)
	related.sort(key=_sortWinRate, reverse=True)

	ids:List[int] = []
	for i in related:
		ids.extend(i.card_ids)
	return ids

######

#### Use Pytorch to make decks

def UpdateCardStatsPyTorch(random_cards = 1):
	print("update card stats pytorch")
	global game_id_to_win
	#{gameId:(cardId,quant)}
	game_result:Dict[int,List[int]] = {}


	dbfile = './cardData.cdb'
	con = sqlite3.connect(dbfile)
	cur = con.cursor()
	cur.execute('SELECT * from GameStats')
	records = cur.fetchall()
	for record in records:
		game_id = record[0]
		card_name = record[1]
		card_id = record[2]
		played = record[3]
		quant = record[4]

		if not played:
			continue

		if game_id not in game_result:
			game_result[game_id] = [-1] * CardInfoId

		card_index = card_pool[card_id].id + quant - 1
		if game_id_to_win[game_id] == 0: # if the game was won
			game_result[game_id][card_index] = 1
		else:
			game_result[game_id][card_index] = 0

	con.close()
	clf = TrainTorch(game_result)
	cards_added = []
	for y in range(10):
		print(f"---Making Deck {y}---")
		deck_raw, cards_added, deck_main_count,	deck_extra_count,	deck_side_count = AddRandomCards([], [], 0, 0, 0, 40, 15, random_cards)
		deck_raw, cards_added, deck_main_count,	deck_extra_count,	deck_side_count = GenerateDeckPytorch(clf, deck_raw, cards_added, deck_main_count,	deck_extra_count,	deck_side_count, 40)
		ExportDeck(deck_raw, y)

def TrainTorch(game_result:Dict[int,List[int]]):
	print("Train torch")
	y_train = np.array(list(game_result.values()))
	x_train = y_train.copy()
	x_train[x_train == 0] = 1
	x_train[x_train == -1] = 0
	y_train[y_train == -1] = 0

	traindata = Data(x_train, y_train)
	batch_size = min(40, len(y_train))#len(y_train)
	trainloader = DataLoader(traindata, batch_size=batch_size, shuffle=True, collate_fn=lambda x: tuple(x_.to(device) for x_ in default_collate(x)))
	clf = Network(CardInfoId, CardInfoId)
	clf.to(device)

	criterion = nn.BCEWithLogitsLoss().cuda()
	#criterion = nn.CrossEntropyLoss().cuda()
	optimizer = torch.optim.Adam(clf.parameters(), lr=0.01)

	# criterion = nn.MSELoss() 
	# optimizer = torch.optim.SGD(clf.parameters(), lr=0.01)

	epochs = 10
	for epoch in range(epochs):
		y_true = []
		y_pred = []
		running_loss = 0.0
		for i, data in enumerate(trainloader):
			inputs, labels = data
			inputs, labels = inputs.to(device), labels.to(device)
			
			clf.train()
			optimizer.zero_grad()
			
			# forward propagation
			outputs = clf(inputs)
			#outputs = torch.sigmoid(outputs)
			#outputs = torch.softmax(outputs, 1)

			# mask = (labels.cpu() != -1).to(device)
			# outputs2 = outputs.masked_select(mask)
			# labels2 = labels.masked_select(mask)

			loss = criterion(outputs, labels.float())
			#loss = criterion(outputs, torch.sigmoid(labels.float()))
			#loss = criterion(outputs, labels.unsqueeze(1).float())

			# set optimizer to zero grad to remove previous epoch gradients
			#optimizer.zero_grad()
			# backward propagation
			loss.backward()
			# optimize
			optimizer.step()

			running_loss += loss.item()

			#PREDICTIONS 
			clf.eval()
			with torch.no_grad():
				pred = np.round(outputs.cpu().detach().numpy())
				labels = np.round(labels.cpu().detach().numpy())         
				# y_pred = pred.tolist()
				# y_true = labels.tolist()
				y_pred.extend(pred.tolist())
				y_true.extend(labels.tolist())
			
		y_pred = [np.argmax(i) for i in y_pred]
		y_true = [np.argmax(i) for i in y_true]
		print(f"[{epoch + 1}, {i + 1:5d}]Accuracy on training set is " + str(accuracy_score(np.array(y_true),np.array(y_pred))))
		print(f'[{epoch + 1}, {i + 1:5d}] loss: {running_loss / (i + 1):.5f}')
	return clf

def GenerateDeckPytorch(clf:Network, deck_raw:List[CardInfo], cards_added:List[int], deck_main_count,	deck_extra_count,	deck_side_count, deckSize = 40):
	print("Generate deck pytorch")
	clf.eval()

	while deck_main_count < deckSize or deck_extra_count < 15:
		
		# Convert deck into input
		input = [0] * CardInfoId
		for c in deck_raw:
			input[c.id + c.quant - 1] = 1
		
		# Get result
		output = torch.sigmoid(clf(torch.from_numpy(np.array(input)).to(device).float())).cpu().data.numpy() #.cpu().detach().numpy()
		#output = clf(torch.from_numpy(np.array(input)).to(device).float()).cpu().data.numpy()

		index = sorted(range(len(output)), key=lambda k: output[k])
		index = index[::-1]
		

		card_list = list(card_pool.values())
		
		# loop through all options and choose the best one that fits
		i = 0
		while i < len(index):
			card_selected:CardInfo = None
			id = math.floor(float(index[i])/3.0)*3
			quant = (index[i] % 3) + 1
			for card_info in card_list:
				if card_info.id == id:
					card_selected = card_info
					break
			i += 1

			if card_selected == None:
				continue

			# TODO add random ratios adjustment

			# If you already have added this card, skip
			if card_selected.card_id in cards_added:
				continue
			
			
			# Check if you can add into given location and Keep track of how may cards added
			if card_selected.location == 0:
				if deck_main_count >= deckSize:
					continue

				deck_main_count += quant

			elif card_selected.location == 1:
				if quant + deck_extra_count > 15:
					continue

				deck_extra_count += quant

			elif card_selected.location == 2:
				if quant + deck_side_count > 15:
					continue

				deck_side_count += quant

			# Add card
			deck_raw.append(CardInfo(
														card_id=card_selected.card_id, 
														card_name=card_selected.card_name, 
														quant=quant,
														location=card_selected.location,
														id=card_selected.id))
			cards_added.append(card_selected.card_id)



			print(f"{round(output[index[i-1]], 2):<4} [{deck_main_count}][{deck_extra_count}][{deck_side_count}] ({quant}): {card_selected.card_name} ")
			break
		if i >= len(index):
			print("ERROR Reached the end of the card pool list without adding any more")
			break

	return deck_raw, cards_added, deck_main_count,	deck_extra_count,	deck_side_count
#######

def AddRandomCards(deck_raw:List[CardInfo], cards_added:List[int], deck_main_count,	deck_extra_count,	deck_side_count, deckSize = 40, extraSize = 0, cardCount = 0):
	print(f"Add random cards")
	newly_added_cards:List[int] = []
	card_pool_copy = card_pool.copy()
	
	cardAdded = 0
	while ((deck_main_count < deckSize or deck_extra_count < extraSize) and (cardAdded < cardCount or cardCount <= 0)):
		card_id = random.choice(list(card_pool_copy.keys()))
		if cardAdded == 0 and len(card_ranking[0]) > 0:
			card_id = random.choice(card_ranking[0][:math.ceil(len(card_ranking[0])/2)]).card_ids[0][0]
			print(card_id)
		elif cardCount > 0 and len(deck_raw) > 0:
			next_best:List[int] = _getNextBestCard([deck_raw[-1].card_id])
			next_best = next_best[:math.ceil(len(next_best)/2)]
			random.shuffle(next_best)
			for i in next_best:
				if i in card_pool_copy:
					card_id = i
					break

		if card_id in card_pool_copy:
			card:CardInfo = card_pool_copy[card_id]
			card_pool_copy.pop(card_id)
		
		if card_id in cards_added:
			print("ERROR, CARD already added")
			continue



		if card.quant == 0:
			continue

		if card.location == 0:

			if deck_main_count >= deckSize:
				continue

			quant = min(randrange(card.quant) + 1, deckSize - deck_main_count)
			if cardCount > 0:
				quant = randrange(card.quant) + 1
			deck_main_count += quant

		elif card.location == 1:

			if deck_extra_count >= extraSize:
				continue

			quant = min(randrange(card.quant) + 1, extraSize - deck_extra_count)
			if cardCount > 0:
				quant = randrange(card.quant) + 1
			deck_extra_count += quant

		elif card.location == 2:
			continue
			#deck_side_count += quant
		



		deck_raw.append(CardInfo(
													card_id=card.card_id, 
													card_name=card.card_name, 
													quant=quant,
													location=card.location,
													id=card.id
													)
		)
		newly_added_cards.append(card.card_id)
		cards_added.append(card.card_id)

		print(f"added [{deck_main_count}][{deck_extra_count}][{deck_side_count}] ({quant}): {card.card_id} ")
		cardAdded += 1

	return deck_raw, newly_added_cards, deck_main_count,	deck_extra_count,	deck_side_count
	

def LoadInitialDeck():
	deck_raw:List[CardInfo] = []
	cards_added:List[int] = []
	deck_main_count = 0	
	deck_extra_count = 0
	deck_side_count = 0

	file_path = os.getcwd() + "/edopro_bin/deck/card_pool.ydk"
	
	print("Get Card pool file")
	# Get from file after
	location = 0
	f = open(file_path,"r")
	for line in f.readlines():
		if "#main" in line:
			location = 0
			continue
		elif "#extra" in line:
			location = 1
			continue
		elif "!side" in line:
			location = 2
			continue
		elif '#' in line:
			continue

		id = line
		if id not in card_pool:
			card_pool[id.strip("\n")] = CardInfo(id.strip("\n"), "", 3, location)

	f.close()
	return deck_raw, cards_added, deck_main_count,	deck_extra_count,	deck_side_count

def ExportDeck(deck_raw:List[CardInfo], deck_index):

	#f = open(os.getcwd() + '/WinBot-Ignite-master/bin/Debug/Decks/AI_Random.ydk' ,"w+")
	f = open(os.getcwd() + f'/edopro_bin/deck/AI_Combined{deck_index}.ydk' ,"w+")
	#f = open(os.getcwd() + '/windbot_master/bin/Debug/Decks/'+ sys.argv[1] ,"w+")    

	f.write("#created by deck_maker_ai\n")

	f.write("#main\n")

	deck_main:List[str] = []
	deck_extr:List[str] = []
	deck_side:List[str] = []

	for i in deck_raw:
		info:CardInfo = i

		for _ in range(info.quant):
			if info.location == 0:
				deck_main.append(info.card_id)
			elif info.location == 1:
				deck_extr.append(info.card_id)
			elif info.location == 2:
				deck_side.append(info.card_id)

	#print("----")
	for i in deck_main:
			f.write(i +'\n')
	f.write("#extra\n")
	for i in deck_extr:
		f.write(i +'\n')
	f.write("!side\n")
	for i in deck_side:
		f.write(i +'\n') 

	f.close()


def _sortWinRate(e:CardPairings):
	return e.get_win_rate()

def _sortWinUsageRate(e:CardPairings):
	return e.get_win_usage_rate()

def _sortWinPlayedGames(e:CardPairings):
	return e.get_win_played_games_rate()


def GetGameResults():
	print("Get Game Results")
	dbfile = './cardData.cdb'
	con = sqlite3.connect(dbfile)
	cur = con.cursor()
	cur.execute('SELECT rowid, result from GameTable')
	records = cur.fetchall()
	for record in records:
		game_id_to_win[record[0]] = record[1]
	con.close()

def MakeDeckRandom():
	CardInfoId = 0
	card_pool:Dict[int,CardInfo] = {}
	game_id_to_win = {}
	card_ranking:Dict[int,List[CardPairings]] = {}
	GetGameResults()
	GetCardPool()
	UpdateCardStats()
	for y in range(10):
		print(f"---Making Deck random {y}---")
		deck_raw, cards_added, deck_main_count,	deck_extra_count,	deck_side_count = AddRandomCards([], [], 0, 0, 0, 40, 15, 0)
		ExportDeck(deck_raw, y)

def MakeDeckPytorch(random_cards = 1):
	CardInfoId = 0
	card_pool:Dict[int,CardInfo] = {}
	game_id_to_win = {}
	card_ranking:Dict[int,List[CardPairings]] = {}
	GetGameResults()
	GetCardPool()
	UpdateCardStats()
	UpdateCardStatsPyTorch(random_cards)


if __name__ == "__main__":
	#MakeDeckRandom()
	MakeDeckPytorch(1)