from argparse import Action
import math
import os
import sqlite3
from pyvis.network import Network
import networkx as nx

conn = sqlite3.connect(os.getcwd() +'/cardData.cdb')
c = conn.cursor()

class Node():
  def __init__(self, id, name):
    self.id = id
    self.name = name

class Connection():
    def __init__(self, id, input, output):
      self.id = id
      self.input = input
      self.output = output

nx_graph = nx.DiGraph()

node_list = {}
innovation_list = {}
connection_list = []

c.execute('SELECT Id, Name FROM NodeName')
records = c.fetchall()
for record in records:
  rowid = record[0]
  name = record[1]
  node_list[rowid] = Node(rowid, name)
  nx_graph.add_node(rowid, label=f'{name}')

c.execute('SELECT rowid, Input, Output FROM InnovationNumber')
records = c.fetchall()
for record in records:
  rowid = record[0]
  input = record[1]
  output = record[2]
  innovation_list[rowid] = Connection(rowid, input, output)

c.execute('SELECT InnovationId, Enabled, SpeciesId, Wins, Games, Weight output FROM Connections')
records = c.fetchall()
for record in records:
  innoId = record[0]
  enabled = record[1]
  speciesId = record[2]
  wins = record[3]
  games = record[4]
  weight = record[5]
  innovation = innovation_list[innoId]
  input = node_list[innovation.input]
  output = node_list[innovation.output]
  connection_list.append(Connection(innoId, input, output))
  if speciesId == 2:
    nx_graph.add_edge(input.id, output.id, weight=weight*10, label=f'{speciesId}', group=speciesId)

c.close()

nt = Network('1000px', '1000px', directed=True)
# populates the nodes and edges data structures
nt.from_nx(nx_graph)
nt.show_buttons(filter_=['physics'])
nt.show('nx.html')