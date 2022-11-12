# loading in modules
import sqlite3

# creating file path
dbfile = './cardData.cdb'
# Create a SQL connection to our SQLite database
con = sqlite3.connect(dbfile)

# creating cursor
cur = con.cursor()

# # reading all table names
# table_list = [a for a in cur.execute("SELECT name FROM sqlite_master WHERE type = 'table'")]
# # here is you table list
# print(table_list)

sql_delete_query = """DELETE from MCST"""
cur.execute(sql_delete_query)
sql_delete_query = """DELETE from WeightTree"""
cur.execute(sql_delete_query)
con.commit()

# Be sure to close the connection
con.close()