import csv
import sqlite3


def export_database():
  dbfile = './cardData.cdb'
  con = sqlite3.connect(dbfile)
  cur = con.cursor()

  sql_delete_query = """SELECT rowid,* FROM GameStats"""
  cur.execute(sql_delete_query)
  write_db_to_file(cur, 'GameStats.csv')
  sql_delete_query = """SELECT rowid,* FROM GameTable"""
  cur.execute(sql_delete_query)
  write_db_to_file(cur, 'GameTable.csv')


  con.close()

def write_db_to_file(cursor, filename):
   with open(filename, "w", newline='') as csv_file:  # Python 3 version    
    csv_writer = csv.writer(csv_file)
    csv_writer.writerow([i[0] for i in cursor.description]) # write headers
    csv_writer.writerows(cursor)

if __name__ == '__main__':
    export_database()