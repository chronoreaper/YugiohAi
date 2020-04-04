using Mono.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.IO;

namespace WindBot
{
    public static class Logger
    {
        public static string Path = "log.txt";
        public static string Name = "ai";
        public static SqliteConnection SQLCon = null;

        private const int WIN = 0;
        private const int LOSE = 1;
        private const int TIE = 2;

        private static void ConnectToDatabase()
        {
            if (SQLCon == null)
            {
                //@"URI=file:\windbot_master\windbot_master\bin\Debug\cards.cdb";
                string absolutePath =@"\cardData.cdb";
                SQLCon = new SqliteConnection(absolutePath);
            }
        }

        public static List<int> CheckDatabase(int id = -1, string action = "", string check = "", string value = "", int count = 1)
        {
            List<int> result = new List<int>();
            ConnectToDatabase();

            SQLCon.Open();
            string sql =
                  $"SELECT wins, games FROM playCard WHERE id = {id} AND action = {action} AND check = {check} AND value = {value} AND count = {count} AND inprogress =  ''";
            SqliteCommand cmd = new SqliteCommand(sql, SQLCon);
            using (SqliteDataReader rdr = cmd.ExecuteReader())
                while (rdr.Read())
                {
                    result.Add(rdr.GetInt32(0));
                    result.Add(rdr.GetInt32(1));
                }
            SQLCon.Close();
            return result;
        }

        /// <summary>
        /// Adds the action to the database
        /// </summary>
        /// <param name="id">Id of the card</param>
        /// <param name="action">Action performed</param>
        /// <param name="check">What value was checked</param>
        /// <param name="value">The value to check</param>
        /// <param name="count">how many times it was found</param>
        public static void AddToDatabase(int id = -1, string action = "", string check = "", string value = "", int count = 1)
        {
            ConnectToDatabase();
            SQLCon.Open();
            string sql =
                  $"INSERT INTO playCard (id,action,check,value,count,inprogress) VALUES ({id},{action},{check},{value},{count},{Name}";
            SqliteCommand cmd = new SqliteCommand(sql, SQLCon);
            cmd.ExecuteNonQuery();

            SQLCon.Close();
        }

        /// <summary>
        /// Updates the data base result
        /// </summary>
        /// <param name="result">0 = win, 1 = lose, 2 = tie</param>
        public static void UpdateDatabase(int result)
        {
            if (result != TIE)
            {
                ConnectToDatabase();
                List<int> query = new List<int>();

                SQLCon.Open();

                //Select all the data that was stored
                string sql =
                      $"SELECT id,action,check,value,count,wins,games FROM playCard WHERE inprogress = {Name}";
                SqliteCommand cmd = new SqliteCommand(sql, SQLCon);
                using (SqliteDataReader rdr = cmd.ExecuteReader())
                    while (rdr.Read())
                    {
                        int id = rdr.GetInt32(0);
                        string action = rdr.GetString(1);
                        string check = rdr.GetString(1);
                        string value = rdr.GetString(1);
                        int count = rdr.GetInt32(0);
                        int wins = rdr.GetInt32(0) + result == WIN ? 1 : 0;
                        int games = rdr.GetInt32(0) + 1;

                        sql = $"INSERT INTO playCard (id,action,check,value,count,wins,gamesinprogress) " +
                                $"VALUES ({id},{action},{check},{value},{count},{wins},{games}''";
                        cmd = new SqliteCommand(sql, SQLCon);
                        cmd.ExecuteNonQuery();
                    }
                SQLCon.Close();
            }
        }

        public static void DeleteFile()
        {
            if (File.Exists(Path))
                File.Delete(Path);
        }

        public static void WriteToFile(string str)
        {
            using (StreamWriter sw = File.AppendText(Path))
            {
                sw.WriteLine(str);
            }
        }

        public static void WriteLine(string message)
        {
            Console.WriteLine("[" + DateTime.Now.ToString("yy-MM-dd HH:mm:ss") + "] " + message);
        }
        public static void DebugWriteLine(string message)
        {
#if DEBUG
            Console.WriteLine("[" + DateTime.Now.ToString("yy-MM-dd HH:mm:ss") + "] " + message);
#endif
        }
        public static void WriteErrorLine(string message)
        {
            Console.BackgroundColor = ConsoleColor.Red;
            Console.ForegroundColor = ConsoleColor.White;
            Console.Error.WriteLine("[" + DateTime.Now.ToString("yy-MM-dd HH:mm:ss") + "] " + message);
            Console.ResetColor();
        }
    }
}