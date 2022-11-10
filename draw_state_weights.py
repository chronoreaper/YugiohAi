from argparse import Action
import math
import os
import sqlite3
from pyvis.network import Network
import networkx as nx


conn = sqlite3.connect(os.getcwd() +'/cardData.cdb')
c = conn.cursor()

c.execute('SELECT SUM(Visited) FROM WeightTree')
total = c.fetchone()[0]
print("Total search made=" + str(total))

nx_graph = nx.DiGraph()
const = 0.5

c.execute('SELECT * FROM WeightTree ORDER BY CardId, Action, Verify')
records = c.fetchall()
NodeIds = {}
NodeIdCount = 0

for record in records:
    cardid = record[0]
    action = record[1]
    verify = record[2]
    reward = record[3]
    visited = max(0.0001, record[4])

    if visited < 2:
      continue

    activation = reward + const * math.sqrt((math.log(total + 1) + 1) / visited)
    activation = round( min(activation, 25) * 100 )/ 100

    nodeKey = cardid + action
    if nodeKey not in NodeIds:
      NodeIds[nodeKey] = NodeIdCount
      nx_graph.add_node(NodeIdCount, label=f'{action} {cardid}', title=f'{action} {cardid}', group=1)
      NodeIdCount += 1

    parentid = NodeIds[nodeKey]

    rowid = NodeIdCount
    NodeIdCount += 1

    nx_graph.add_edge(parentid, rowid, label=f'{verify}', weight=activation, group=2)
    
    print(f"{activation}| {cardid} | {action} | {verify}") 

c.close()

nt = Network('1000px', '1000px', directed=True)
# populates the nodes and edges data structures
nt.from_nx(nx_graph)
nt.show_buttons(filter_=['physics'])
nt.show('netx.html')