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
from sys import platform
from pathlib import Path

#The Deck name and location	
AI1Deck = 'Random1'
AI2Deck = 'Random2'
AIMaster = 'Master'

deck1 = 'AI_Random1.ydk'
deck2 = 'AI_Random2.ydk'

totalGames = 3
species = 10
connections = 10
generations = 1

totalRealGames = 10
rolloutCount = 1
isFirst = True
isTraining = True
winThresh = 60
pastWinLim = 5
idNumber = 1

reset = False

def isrespondingPID(PID):
  #if platform == "linux" or platform == "linux2":
  return True

  #https://stackoverflow.com/questions/16580285/how-to-tell-if-process-is-responding-in-python-on-windows
  os.system('tasklist /FI "PID eq %d" /FI "STATUS eq running" > tmp.txt' % PID)
  tmp = open('tmp.txt', 'r')
  a = tmp.readlines()
  tmp.close()
  try:
    if int(a[-1].split()[1]) == PID:
      return True
    else:
      return False
  except:
    return False

def resetDB():
  dbfile = './cardData.cdb'
  con = sqlite3.connect(dbfile)
  cur = con.cursor()
  sql_delete_query = """DELETE from Connections"""
  cur.execute(sql_delete_query)
  sql_delete_query = """DELETE from InnovationNumber"""
  cur.execute(sql_delete_query)
  sql_delete_query = """DELETE from NodeName"""
  cur.execute(sql_delete_query)
  sql_delete_query = """DELETE from SpeciesRecord"""
  cur.execute(sql_delete_query)
  con.commit()
  con.close()

def resetYgoPro():
  print("deleting old deck files from ygopro")
  files = glob.glob(os.getcwd() +"/ProjectIgnis/deck/*")
  for f in files:
      os.remove(f)
    
  print("deleting old replays from ygopro")
  files = glob.glob(os.getcwd() +"/ProjectIgnis/replay/*")
  for f in files:
      os.remove(f)
  
def parseArg():
  global reset, isTraining, repeatFor, matches, totalGames, isFirst

  reset = len(sys.argv)>1 and ("--reset" in sys.argv or "-r" in sys.argv)
  isTraining = len(sys.argv)>1 and ("--training" in sys.argv or "-t" in sys.argv)
  isFirst = len(sys.argv)>1 and ("--first" in sys.argv or "-f" in sys.argv)

  print(isTraining)

  if "--repeat" in sys.argv:
    repeatFor = int(sys.argv[sys.argv.index("--repeat")+1])
  if "--matches" in sys.argv:
    matches = int(sys.argv[sys.argv.index("--matches")+1])
  if "--games" in sys.argv:
    totalGames = int(sys.argv[sys.argv.index("--games")+1])

def runAi(Deck = "Random1", 
          Name = "Random1",
          Hand = 0,
          TotalGames = 1,
          RolloutCount = 0,
          IsFirst = True,
          IsTraining = True,
          ShouldUpdate = True,
          WinsThreshold = 50,
          PastWinsLimit = 20,
          Id = 0
          ):
  currentdir = os.getcwd()
  os.chdir(os.getcwd()+'/WindBot-Ignite-master/bin/Debug')

  file_name = "WindBot.exe"

  if platform == "linux" or platform == "linux2":
    file_name = os.getcwd() + "/WindBot.exe"

  p = subprocess.Popen([file_name,"Deck="+Deck,
                        "Name="+str(Name),
                        "Hand="+str(Hand),
                        "IsTraining="+str(IsTraining), 
                        "ShouldUpdate="+str(ShouldUpdate), 
                        "TotalGames="+str(TotalGames), 
                        "RolloutCount="+str(RolloutCount), 
                        "IsFirst="+str(IsFirst), 
                        "WinsThreshold="+str(WinsThreshold), 
                        "PastWinsLimit="+str(PastWinsLimit),
                        "Id="+str(Id)
                        ])
    
  os.chdir(currentdir)
  
  return p

def shuffle_deck(deck_name):
  filePath = os.getcwd() + '/WindBot-Ignite-master/bin/Debug/Decks/'+ deck_name 

  f = open(filePath,"r")
  main = []
  extra = []
  side = []
  part = 0
  for line in f.readlines():

    if "#extra" in line:
      part = 1
      continue
    elif "!side" in line:
      part = 2
      continue
    elif "#main" in line:
      part = 0
      continue
    elif "#" in line:
      continue

    if part == 0:
      main.append(line.strip())
    elif part == 1:
      extra.append(line.strip())
    else:
      side.append(line.strip())
    random.shuffle(main)
    random.shuffle(extra)
    random.shuffle(side)

  f.close()

  f = open(filePath, "w")    
  f.write("#created by deck_maker_ai\n")

  f.write("#main\n")
  for i in main:
    f.write(i +'\n')
  f.write("#extra\n")
  for i in extra:
    f.write(i +'\n')
  f.write("!side\n")
  for i in side:
    f.write(i +'\n')    

  f.close()

def setup():
  global AI2Deck, AI2Deck, isTraining, totalGames

  if reset:
    if isTraining:
      resetDB()
    #shuffle_deck(deck1)
    src_dir=os.getcwd() + '/WindBot-Ignite-master/bin/Debug/Decks/' + deck1
    dst_dir=os.getcwd() + '/WindBot-Ignite-master/bin/Debug/Decks/' + deck2
    shutil.copy(src_dir,dst_dir)

  resetYgoPro()

  if not isTraining:
    AI2Deck = AIMaster
    totalGames = totalRealGames

def main_game_runner(isTraining, totalGames, Id1, Id2):
  start = time.time()

  #subprocess.Popen - does not wait to finish
  #subprocess.run - waits to finish

  file_path = os.getcwd() + "/edopro_bin/ygopro.exe"

  if platform == "linux" or platform == "linux2":
    file_path = str(Path(__file__).resolve().parent.parent) + "/ProjectIgnisLinux/ygopro"
 
  
  g = subprocess.Popen([file_path])


  while(g.poll() == None and not isrespondingPID(g.pid)):
    time.sleep(1)

  time.sleep(5)
  
  print("	runningAi1 " + str(Id1))

  p1 = runAi( Deck = AI1Deck, 
              Name = AI1Deck,
              Hand = 2,
              TotalGames = totalGames,
              RolloutCount = rolloutCount,
              IsFirst = isFirst,
              IsTraining = isTraining,
              ShouldUpdate= isTraining,
              WinsThreshold = winThresh,
              PastWinsLimit = pastWinLim,
              Id = Id1
            )
  time.sleep(1)
  print("	runningAi2 "+ str(Id2))
  p2 = runAi(Deck = AI2Deck, 
              Name = AI2Deck,
              Hand = 3,
              TotalGames = totalGames,
              RolloutCount = rolloutCount,
              IsFirst = (not isFirst),
              IsTraining = isTraining,
              ShouldUpdate= False,
              WinsThreshold = winThresh,
              PastWinsLimit = pastWinLim,
              Id = Id2
            )
  
  if (p1.poll() == None or p2.poll() == None):
    time.sleep(1)
  
  if (not (p1.poll() == None or p2.poll() == None)):
    print("	WARNING! ai is not running")

  timer = 0
  timeout = 30 * 60 # Length of run
  
  # print(p1.pid)
  # print(p2.pid)
  #make sure the game does not run longer than needed
  #ends the ygopro program as soon as the ais are done. Ais play faster than what you see.
  
  while (p1.poll() == None and p2.poll() == None):
    continue
     
     
  if platform == "linux" or platform == "linux2":
    os.system("kill -9 " + str(g.pid))
  else:
    os.system("	TASKKILL /F /IM ygopro.exe")
  
  end = time.time()

  print("Time Past:" + str(datetime.timedelta(seconds=int(end - start))))
  print("Average Game Time:"+str(datetime.timedelta(seconds=int((end - start)/(totalGames)))))

class Node():
  def __init__(self, id, name):
    self.id = id
    self.name = name

class Connection():
  def __init__(self, innovationNumber, enabled, weight, speciesId, wins, games):
    self.inNumb = innovationNumber
    self.enabled = enabled
    self.weight = weight
    self.speciesId = speciesId
    self.wins = wins
    self.games = games

class Species():
  def __init__(self, id, connections: typing.Dict[int, Connection], nodes: typing.List[Node]) -> None:
    self.connections = connections
    self.id = id
    self.nodes = nodes

def makeSpecies(number):
  global connections
  in_nodes = []
  out_nodes = []
  nodes = []

  species = []

  conn = sqlite3.connect(os.getcwd() +'/cardData.cdb')
  c = conn.cursor()

  c.execute('SELECT Id, Name, Type FROM NodeName')
  records = c.fetchall()
  conn.close()

  for record in records:
      Id = record[0]
      Name = record[1]
      Type = record[2]

      if Type == 1: #and Name == "Default":
        in_nodes.append(Node(Id, Name))
      elif Type == -1:
        out_nodes.append(Node(Id, Name))
      nodes.append(Node(Id, Name))

  if (len(in_nodes) == 0 or len(out_nodes) == 0):
    print("no input/output nodes")
    return
  
  currentSpecies = GetCurrentSpecies()
  new_species_ammount = number
  if (len(currentSpecies) > 0):
    new_species_ammount = 2
  species = MakeNewSpecies(new_species_ammount, in_nodes, out_nodes, nodes)
  for speciesId in range(len(species), number):
    new_species = combine(random.choice(list(currentSpecies.values())), random.choice(list(currentSpecies.values())))
    mutate(new_species, in_nodes, out_nodes, nodes, 5)
    new_species.id = speciesId
    species.append(new_species)
  
  saveSpeciesToDatabase(species)

def saveSpeciesToDatabase(species: typing.List[Species]) -> None:
  conn = sqlite3.connect(os.getcwd() +'/cardData.cdb')
  c = conn.cursor()

  for specie in species:
    speciesId = specie.id
    for key in specie.connections:
      connection = specie.connections[key]
      c.execute('INSERT INTO Connections values (?,?,?,?,?,?)', (connection.inNumb, connection.enabled, connection.weight, speciesId, connection.games, connection.wins))
  
  conn.commit()
  conn.close()

def get_innovation_number(nodeid1, nodeid2):
  conn = sqlite3.connect(os.getcwd() +'/cardData.cdb')
  c = conn.cursor()

  c.execute('SELECT rowid FROM InnovationNumber where Input = ? AND Output = ?', (nodeid1.id, nodeid2.id))
  record = c.fetchone()
  if record is not None:
    id = record[0]
    conn.close()
    return id

  c = conn.cursor()
  c.execute('INSERT INTO InnovationNumber values (?,?)', (nodeid1.id, nodeid2.id))
  conn.commit()

  id = c.lastrowid
  conn.close()
  return id

def prune_results():
  success = []
  fail = []

  conn = sqlite3.connect(os.getcwd() +'/cardData.cdb')
  c = conn.cursor()

  # Select the top few species
  c.execute('SELECT Id, Games, Wins FROM SpeciesRecord')
  records = c.fetchall()
  for record in records:
      id = record[0]
      games = record[1]
      wins = record[2]

      if (wins < games - 2):
        fail.append(id)
      else:
        success.append(id)
  
  c.executemany("DELETE FROM SpeciesRecord WHERE Id = ?", list((i,) for i in fail))
  #c.execute("UPDATE SpeciesRecord SET Games = 0, Wins = 0")
  
  # Prune the connection tree of the weakest species

  c.executemany("DELETE FROM Connections WHERE SpeciesId = ?",list((i,) for i in fail))

  # Prune the connection tree of un activated weights

  #c.execute("DELETE FROM Connections WHERE Games = 0")
  conn.commit()
  conn.close()

  return success

def GetCurrentSpecies():
  species = {}
  conn = sqlite3.connect(os.getcwd() +'/cardData.cdb')
  c = conn.cursor()

  # Select the top few species
  c.execute('SELECT InnovationId, Enabled, Weight, SpeciesId, Wins, Games FROM Connections')
  records = c.fetchall()
  for record in records:
      inNum = record[0]
      enable = record[1]
      weight = record[2]
      speciesId = record[3]
      wins = record[4]
      games = record[5]
      edge = Connection(inNum, enable, weight, species, wins, games)

      c.execute('SELECT Input, Output FROM InnovationNumber where rowid = ?', (inNum,))
      record = c.fetchone()
      nodes = []
      if record is not None:
        nodes.append(Node(record[0],""))
        nodes.append(Node(record[1],""))
  
      if (speciesId not in species):
        species[speciesId] = Species(speciesId, {}, [])
      species[speciesId].connections[inNum] = edge
      species[speciesId].nodes.extend(nodes)

  conn.close()

  return species

def MakeNewSpecies(number, in_nodes, out_nodes, nodes):
  global connections
  species = []

  conn = sqlite3.connect(os.getcwd() +'/cardData.cdb')
  c = conn.cursor()
  
  for speciesId in range(number):
    print("Making Species " + str(speciesId))
    edges = {}
    nodes = []

    for _ in range(connections):
      # Select 2 random nodes and join them
      node1 = random.choice(in_nodes)
      node2 = random.choice(out_nodes)

      id = get_innovation_number(node1, node2)
      c.execute('SELECT rowid FROM Connections where InnovationId = ? AND SpeciesId = ?', (id, speciesId))
      record = c.fetchone()
      if record is not None:
        continue
      
      weight = random.uniform(0, 1)
      edges[id] = Connection(id, True, weight, speciesId, 0, 0)
      nodes.append(node1)
      nodes.append(node2)

    species.append(Species(speciesId, edges, nodes))

  conn.close()

  return species

def combine(a: Species, b: Species) -> Species:

  edgesA = a.connections
  edgesB = b.connections

  childEdge = {}

  for id in edgesA:
    if id not in edgesB:
      childEdge[id] = (Connection(id, edgesA[id].enabled, edgesA[id].weight, edgesA[id].speciesId, edgesA[id].wins, edgesA[id].games))
    else:
      edgeA = edgesA[id]
      edgeB = edgesB[id]
      choice = 0

      if (edgeA.games == 0 and edgeB.games == 0):
        choice = random.randint(0, 1)
      elif edgeA.games == 0:
        choice = 1
      elif edgeB.games == 0:
        choice = 0
      else:
        if (edgeA.wins/edgeA.games > edgeB.wins/edgeB.games):
          choice = 0
        else:
          choice = 1
      if choice == 0:
        childEdge[id] = (Connection(id, edgeA.enabled, edgeA.weight, edgeA.speciesId, edgeA.wins, edgeA.games))
      else:
        childEdge[id] = (Connection(id, edgeB.enabled, edgeB.weight, edgeB.speciesId, edgeB.wins, edgeB.games))

  for id in edgesB:
    if id not in edgesA:
      childEdge[id] = (Connection(id, edgesB[id].enabled, edgesB[id].weight, edgesB[id].speciesId, edgesB[id].wins, edgesB[id].games))
  
  a.nodes.extend(b.nodes)

  return Species(0, childEdge, a.nodes)

def mutate(a: Species, in_nodes, out_nodes, nodes, amount):
  global connections
  conn = sqlite3.connect(os.getcwd() +'/cardData.cdb')
  c = conn.cursor()

  for _ in range(amount):
    
    choice = 0

    r = random.uniform(0, 1)
    if (r < 0.1):
      choice = 1
    elif( r < 0.3):
      choice = 2
    elif ( r < 0.6):
      choice = 3
    
    if (choice == 0):
      # Add Node
      input_node = []

      for node in a.nodes:
        if node not in out_nodes:
          input_node.append(node)
      node1 = random.choice(list(input_node))

      # Find all node ids that are previous to node1
      before = []
      tocheck = [node1.id]
      while len(tocheck) > 0:
        node_id = tocheck.pop(0)
        before.append(node_id)
        c.execute('SELECT Input FROM InnovationNumber where Output = ?', (node_id,))
        records = c.fetchall()
        for input in records:
          if (input[0] not in before and input[0] not in tocheck):
            tocheck.append(input[0])

      # Make sure you don't add duplicate connections
      for key in a.connections:
        edge = a.connections[key]
        c.execute('SELECT Input, Output FROM InnovationNumber where rowid = ?', (edge.inNumb,))
        record = c.fetchone()
        if (record is not None):
          if record[0] == node1.id:
            before.append(record[1])

      output_node = []
      for node in a.nodes:
        if node.id not in before:
          output_node.append(node)
      
      for node in out_nodes:
        if node.id not in before:
          output_node.append(node)
      
      if (len(output_node) > 0):
        node2 = random.choice(list(output_node))
        id = get_innovation_number(node1, node2)
        weight = random.uniform(0, 1)
        a.connections[id] = (Connection(id, True, weight, a.id, 0, 0))
      else:
        print("No more connections for id " + str(a.id))

    elif choice == 1:
      # Add connection
      node1 = random.choice(list(a.nodes))
      node2 = random.choice(list(a.nodes))

      id = get_innovation_number(node1, node2)
      if (id in a.connections):
        continue
          
      weight = random.uniform(0, 1)
      a.connections[id] = (Connection(id, True, weight, a.id, 0, 0))
    elif choice == 2:
      # flip Connection
      key = random.choice(list(a.connections))
      a.connections[key].enabled = not a.connections[key].enabled
    elif choice == 3:
      # change weight
      weight = random.uniform(0, 1)
      if (random.uniform(0, 1) < 0.1):
        weight = - weight
      key = random.choice(list(a.connections))
      a.connections[key].weight = weight
  conn.close()

def main():
  global reset, totalGames, species, generations
  parseArg()
  setup()

  if (reset):
    print("Generating input output")
    main_game_runner(True, 5, 0, 0)
  for g in range(generations):
    print("making species for generation " + str(g))
    makeSpecies(species)
    species_list = list(range(species))
    while len(species_list) > 4:
      print("Remaining species: " + str(len(species_list)))
      for i in range(0, len(species_list), 2):
        if i + 1 >= len(species_list):
          continue
        print("running game " + str(i))
        main_game_runner(False, totalGames, species_list[i], species_list[i + 1])
      species_list = prune_results()

if __name__ == "__main__":
  main()
