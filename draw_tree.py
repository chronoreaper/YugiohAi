from argparse import Action
import math
import os
import sqlite3
from pyvis.network import Network
import networkx as nx


conn = sqlite3.connect(os.getcwd() +'/cardData.cdb')
c = conn.cursor()

c.execute('SELECT SUM(Visited) FROM MCST WHERE CardId != \"Result\" AND ParentId = 0')
total = c.fetchone()[0]
print("Total search made=" + str(total))

nx_graph = nx.DiGraph()
const = 0.5

c.execute('SELECT rowid, * FROM MCST')
records = c.fetchall()
group_count = 0
for record in records:
    rowid = record[0]
    parentid = record[1]
    cardid = record[2]
    action = record[3]
    reward = record[4]
    visited = max(0.0001, record[5])

    activation = reward + const * math.sqrt((math.log(total + 1) + 1) / visited)
    activation = min(activation * 2, 50)
    nx_graph.add_node(25, label=f'{action} {cardid}', title=f'{action} {cardid}', group=group_count)
    nx_graph.add_edge(parentid, rowid, label=f'{action} {cardid}', weight=activation, group=group_count)

    if (action == "GoToEndPhase"):
      group_count += 1

# Get Best average moves?
c.execute("Select cardId,Action, SUM(Reward) as r,SUM(Visited) as v from MCST group by cardid, action order by cardid, action")
records = c.fetchall()
for record in records:
    cardid = record[0]
    action = record[1]
    reward = record[2]
    visited =record[3]
    #const = 0

    activation = round((reward + const * math.sqrt((math.log(total + 1) + 1) / visited)) * 100)/100
    activation2 = round(reward / visited * 1000)/10
    print(f"{activation}| {activation2} | {cardid} | {action}")

c.close()

nt = Network('1000px', '1000px', directed=True)
# populates the nodes and edges data structures
nt.from_nx(nx_graph)
nt.show_buttons(filter_=['physics'])
nt.show('nx.html')