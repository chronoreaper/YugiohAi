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
print(total)

nx_graph = nx.DiGraph()

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

    const = 0.5
    activation = reward + const * math.sqrt((math.log(total + 1) + 1) / visited)

    nx_graph.add_node(25, label=f'{action} {cardid}', title=f'{action} {cardid}', group=group_count)
    nx_graph.add_edge(parentid, rowid, label=f'{action} {cardid}', weight=activation, group=group_count)

    if (action == "GoToEndPhase"):
      group_count += 1

# conn.commit()
c.close()

nt = Network('1000px', '1000px', directed=True)
# populates the nodes and edges data structures
nt.from_nx(nx_graph)
nt.show_buttons(filter_=['physics'])
nt.show('nx.html')