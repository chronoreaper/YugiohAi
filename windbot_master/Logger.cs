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
        /// <param name="action">Action performed</param>
        /// <param name="verify">What value was verifyed</param>
        /// <param name="value">The value to verify</param>
        /// <param name="count">how many times it was found</param>
        /// <returns>the values from the database that matches the input.</returns>
        public static List<int> RecordAction(int id = -1, string action = "", string verify = "", string value = "", int count = 1)
        {
            ConnectToDatabase();
            SQLCon.Open();

            List<int> result = new List<int>();

            string sql =
                  $"INSERT INTO playCard (id,action,verify,value,count,inprogress) VALUES ({id},'{action}','{verify}','{value}',{count},'{Name}')";
            SqliteCommand cmd = new SqliteCommand(sql, SQLCon);
            cmd.ExecuteNonQuery();


            SQLCon.Close();
            return result;
        }

        /// <summary>
        /// Gets the weight of the input from the database.
        /// </summary>
        /// <param name="id">Id of the card</param>
        /// <param name="action">Action performed</param>
        /// <param name="verify">What value was verifyed</param>
        /// <param name="value">The value to verify</param>
        /// <param name="count">how many times it was found</param>
        public static void GetData(int id = -1, string action = "", string verify = "", string value = "", int count = 1)
        {
            ConnectToDatabase();
            SQLCon.Open();

            List<int> result = new List<int>();

            string sql =
                  $"SELECT wins, games FROM playCard WHERE id = {id} AND action = '{action}' AND verify = '{verify}' AND value = '{value}' AND count = {count} AND inprogress =  {BLANK}";
            SqliteCommand cmd = new SqliteCommand(sql, SQLCon);
            using (SqliteDataReader rdr = cmd.ExecuteReader())
                while (rdr.Read())
                {
                    result.Add(rdr.GetInt32(0));
                    result.Add(rdr.GetInt32(1));
                }

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
                      $"SELECT id,action,verify,value,count FROM playCard WHERE inprogress = '{Name}'";
                SqliteCommand cmd = new SqliteCommand(sql, SQLCon);
                try
                {
                    using (SqliteDataReader rdr = cmd.ExecuteReader())
                        while (rdr.Read())
                        {
                            int id = rdr.GetInt32(0);
                            string action = rdr.GetString(1);
                            string verify = rdr.GetString(2);
                            string value = rdr.GetString(3);
                            int count = rdr.GetInt32(4);
                            int wins = result == WIN ? 1 : 0;

                            sql = $"UPDATE playCard SET wins = wins + {wins}, games = games + 1, inprogress = {BLANK} WHERE " +
                                    $"id = {id} AND action = '{action}' AND verify = '{verify}' AND value = '{value}' AND count = {count} AND inprogress =  {BLANK}";
                            cmd = new SqliteCommand(sql, SQLCon);
                            int rowsUpdated = cmd.ExecuteNonQuery();
                            WriteLine($"{rowsUpdated} Rows Were updated.");
                            // If there was no update, add it instead
                            if (rowsUpdated <= 0)
                            {
                                //The master data isnt in the database yet so add it
                                sql = $"INSERT INTO playCard (id,action,verify,value,count,wins,games,inprogress) VALUES ({id},'{action}','{verify}','{value}',{count},{wins},1,'')";
                                cmd = new SqliteCommand(sql, SQLCon);
                                rowsUpdated = cmd.ExecuteNonQuery();
                                WriteLine($"{rowsUpdated} Rows were inserted.");
                            }
                        }
                    //Removed the inprogress rows
                    sql =
                      $"DELETE FROM playCard WHERE inprogress = '{Name}'";
                    cmd = new SqliteCommand(sql, SQLCon);
                    cmd.ExecuteNonQuery();
                }
                catch (SqliteException e)
                {
                    WriteLine("This ai probably never used the database");
                    WriteLine(e.Message);
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