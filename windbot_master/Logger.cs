using Mono.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

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
        private const string BLANK = "''";

        private static void ConnectToDatabase()
        {
            if (SQLCon == null)
            {
                WriteLine("Create Connection to Database");
                //@"URI=file:\windbot_master\windbot_master\bin\Debug\cards.cdb";
                string dir = Directory.GetCurrentDirectory();
                //Go to the YugiohAi Directory
                dir = dir.Remove(dir.IndexOf(@"windbot_master\bin\Debug")) + "cardData.cdb";
                string absolutePath = $@"URI = file: {dir}";
                SQLCon = new SqliteConnection(absolutePath);
            }
        }
        /// <summary>
        /// Adds the action to the actions performed this game.
        /// </summary>
        /// <param name="id">Id of the card</param>
        /// <param name="location">The location of the card</param>
        /// <param name="action">Action performed</param>
        /// <param name="result">The value of the action performed</param>
        /// <param name="verify">What value was verifyed</param>
        /// <param name="value">The value to verify</param>
        /// <param name="count">how many times it was found</param>
        public static void RecordAction(string id = "",string location = "",  string action = "", string result = "", string verify = "", string value = "", int count = 1)
        {
            ConnectToDatabase();
            using (SQLCon)
            {
                SQLCon.Open();

                string sql =
                      $"INSERT INTO playCard (id,location,action,result,verify,value,count,inprogress) VALUES ('{id}','{location}','{action}','{result}','{verify}','{value}',{count},'{Name}')";
                SqliteCommand cmd = new SqliteCommand(sql, SQLCon);
                cmd.ExecuteNonQuery();

                SQLCon.Close();
            }
        }

        /// <summary>
        /// Gets the weight of the input from the database.
        /// </summary>
        /// <param name="id">Id of the card</param>
        /// <param name="location">The location of the card</param>
        /// <param name="action">Action performed</param>
        /// <param name="result">The value of the action performed</param>
        /// <param name="verify">What value was verifyed</param>
        /// <param name="value">The value to verify</param>
        /// <param name="count">how many times it was found</param>
        public static List<int> GetData(string id = "", string location = "", string action = "", string result = "", string verify = "", string value = "", int count = 1)
        {
            List<int> row = new List<int>();

            ConnectToDatabase();
            using (SQLCon)
            {
                SQLCon.Open();

                string sql =
                      $"SELECT wins, games FROM playCard WHERE id = '{id}' AND location = '{location}' AND action = '{action}' AND result = '{result}' AND verify = '{verify}' AND value = '{value}' AND count = {count} AND inprogress =  {BLANK}";
                using (SqliteCommand cmd = new SqliteCommand(sql, SQLCon))
                {
                    using (SqliteDataReader rdr = cmd.ExecuteReader())
                        while (rdr.Read())
                        {
                            row.Add(rdr.GetInt32(0));
                            row.Add(rdr.GetInt32(1));
                        }
                }

                SQLCon.Close();
            }
            return row;
        }

        /// <summary>
        /// Updates the data base result
        /// </summary>
        /// <param name="gameResult">0 = win, 1 = lose, 2 = tie</param>
        /// <param name="otherName">Name of the opponent</param>
        public static void UpdateDatabase(int gameResult, string otherName)
        {
            WriteLine("Updating Database with result");
            //since two programs cant access the database at the same time,
            //run if you lost the game.
            if (gameResult != TIE && gameResult != WIN)
            {
                RecordPlayerGameData(gameResult, Name);
                RecordPlayerGameData(gameResult == WIN?LOSE:WIN, otherName);
            }
        }

        /// <summary>
        /// Updates the data base result
        /// </summary>
        /// <param name="gameResult">0 = win, 1 = lose, 2 = tie</param>
        /// <param name="Player">Name of player</param>
        private static void RecordPlayerGameData(int gameResult, string Player)
        {
            ConnectToDatabase();

            using (SQLCon)
            {
                SQLCon.Open();

                //Select all the data that was stored
                string sql =
                      $"SELECT id,location,action,result,verify,value,count FROM playCard WHERE inprogress = '{Player}'";
                using (SqliteCommand cmd = new SqliteCommand(sql, SQLCon))
                {
                    using (SqliteDataReader rdr = cmd.ExecuteReader())
                    {
                        while (rdr.Read())
                        {
                            string id = rdr.GetString(0);
                            string location = rdr.GetString(1);
                            string action = rdr.GetString(2);
                            string result = rdr.GetString(3);
                            string verify = rdr.GetString(4);
                            string value = rdr.GetString(5);
                            int count = rdr.GetInt32(6);
                            int wins = gameResult == WIN ? 1 : 0;

                            sql = $"UPDATE playCard SET wins = wins + {wins}, games = games + 1, inprogress = {BLANK} WHERE " +
                                    $"id = '{id}' AND location = '{location}' AND action = '{action}'  AND result = '{result}' AND verify = '{verify}' AND value = '{value}' AND count = {count} AND inprogress =  {BLANK}";

                            int rowsUpdated = 0;

                            using (SqliteCommand cmd2 = new SqliteCommand(sql, SQLCon))
                            {
                                rowsUpdated = cmd2.ExecuteNonQuery();
                                //WriteLine($"{rowsUpdated} Rows Were updated.");
                            }
                            // If there was no update, add it instead
                            if (rowsUpdated <= 0)
                            {
                                //The master data isnt in the database yet so add it
                                sql = $"INSERT INTO playCard (id,location,action,result,verify,value,count,wins,games,inprogress) VALUES ('{id}','{location}','{action}','{result}','{verify}','{value}',{count},{wins},1,{BLANK})";
                                using (SqliteCommand cmd2 = new SqliteCommand(sql, SQLCon))
                                {
                                    rowsUpdated = cmd2.ExecuteNonQuery();
                                    //WriteLine($"{rowsUpdated} Rows were inserted.");
                                }
                            }
                        }
                    }
                }
                //Removed the inprogress rows
                sql =
                    $"DELETE FROM playCard WHERE inprogress = '{Player}'";
                using (SqliteCommand cmd = new SqliteCommand(sql, SQLCon))
                {
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