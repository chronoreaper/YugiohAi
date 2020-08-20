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
        public static string LogPath = "gameLog.txt";
        public static string Name = "ai";
        public static SqliteConnection SQLCon = null;

        private static List<Data> data = new List<Data>();
        private static Dictionary<int, Dictionary<int, double>> actionWeight = new Dictionary<int, Dictionary<int, double>>();
        private const int WIN = 0;
        private const int LOSE = 1;
        private const int TIE = 2;

        public static string master { get; private set; } = "";
        public static string noActivateValue { get => " nouse"; }

        private static void ConnectToDatabase()
        {
            master = Name;
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
        public static void RecordAction(string id = "", string location = "", string action = "", string result = "", string verify = "", string value = "", int count = 1, double wins = 1, int turn = 0, int actionId = 0)
        {
            data.Add(new Data(id, location, action, result, verify, value, count, wins, turn, actionId));
        }

        /// <summary>
        /// Saves the action weight
        /// </summary>
        /// <param name="turn">Turn the action is executed</param>
        /// <param name="actionId">the Id of the action</param>
        /// <param name="weight">The confidence weight of the action performed</param>
        public static void SaveActionWeight(int turn, int actionId, double weight)
        {
            //check if the turn is in the dictonary
            if (!actionWeight.ContainsKey(turn))
            {
                actionWeight.Add(turn, new Dictionary<int, double>());
            }

            //There should only be one call per action Id
            if (actionWeight[turn].ContainsKey(actionId))
            {
                WriteErrorLine($"The action Id {actionId} called this method again");
            }
            else
            {
                actionWeight[turn].Add(actionId, weight);
            }

        }

        /// <summary>
        /// Modify the Specified Turn's action weight
        /// </summary>
        /// <param name="turn">The turn to modify</param>
        /// <param name="weight">The weight to multiply by</param>
        public static void ModifyAction(int turn, double weight)
        {
            ConnectToDatabase();
            using (SQLCon)
            {
                SQLCon.Open();
                string sql = $"UPDATE playCard SET wins = wins * {weight}" +
                    $" WHERE games = {turn} AND inprogress = '{Name}'";
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
        /// <param name="depth">How deep the recursion is</param>
        public static List<double> GetData(string inprogress, string id = "", string location = "", string action = "", string result = "", string verify = "", string value = "", int count = 1, int depth = 0)
        {
            List<double> row = new List<double>();

            //break if checking the card you are trying to play
            if (id == value)
            {
                return row;
            }

            double games = 0;
            double wins = 0;
            ConnectToDatabase();
            using (SQLCon)
            {
                SQLCon.Open();

                string sql =
                      $"SELECT wins, games FROM playCard WHERE id = \"{id}\" AND location = \"{location}\" " +
                      $"AND action = \"{action}\" AND result = \"{result}\" AND verify = \"{verify}\" " +
                      $"AND value = \"{value}\" AND count = {count} AND " +
                      $"inprogress =  \"{inprogress}\"";
                //Console.WriteLine(sql);
                //+ $"AND (wins > 10 OR wins < -10)";
                using (SqliteCommand cmd = new SqliteCommand(sql, SQLCon))
                {
                    using (SqliteDataReader rdr = cmd.ExecuteReader())
                        while (rdr.Read())
                        {
                            row.Add(rdr.GetDouble(0));
                            row.Add(rdr.GetDouble(1));
                            wins += rdr.GetDouble(0);
                            games += rdr.GetDouble(1);
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
        /// <param name="otherName">Name of the opponent, Unused</param>
        public static void UpdateDatabase(int gameResult, string otherName, int turns)
        {
            WriteLine("Updating Database with result");
            ConnectToDatabase();

            using (SQLCon)
            {
                SQLCon.Open();
                SqliteTransaction transaction = SQLCon.BeginTransaction();

                //Select all the data that was stored
                string sql =
                      $"SELECT id,location,action,result,verify,value,count,wins,games FROM playCard " +
                      $"WHERE inprogress = \"{Name}\"";
                foreach(Data info in data) {
                    string id = info.id;
                    string location = info.location;
                    string action = info.action;
                    string result = info.result;
                    string verify = info.verify;
                    string value = info.value;
                    int count = info.count;
                    double wins = Math.Sign(info.wins);
                    double games = info.games;

                    // Checks how confident this is a good move 
                    double confidence = 1;
                    if (actionWeight[info.games].ContainsKey(info.actionId + 1))
                        confidence = Math.Abs(actionWeight[info.games][info.actionId]);
                    wins *= confidence;

                    master = Name;
                    if (wins < 0)
                    {
                        wins = -wins;
                    }

                    // round(({Math.Sign(wins)} * 20 - wins)/5)
                    sql = $"UPDATE playCard SET wins = wins + {wins}, games = games + 1 WHERE " +
                            $"id = \"{id}\" AND location = \"{location}\" AND action = \"{action}\"  AND result LIKE \"{result}\" " +
                            $"AND verify = \"{verify}\" AND value = \"{value}\" AND count = {count} " +
                            $"AND inprogress =  \"{master}\" " +
                            $"AND wins + {wins} <= 10 AND wins + {wins} >= -10";

                    int rowsUpdated = 0;

                    using (SqliteCommand cmd2 = new SqliteCommand(sql, SQLCon, transaction))
                    {
                        rowsUpdated = cmd2.ExecuteNonQuery();
                        //WriteLine($"{rowsUpdated} Rows Were updated.");
                    }
                    // If there was no update, add it instead
                    if (rowsUpdated <= 0)
                    {
                        //The master data isnt in the database yet so add it
                        sql = $"INSERT INTO playCard (id,location,action,result,verify,value,count,wins,games,inprogress) VALUES (\"{id}\",\"{location}\",\"{action}\",\"{result}\",\"{verify}\",\"{value}\",{count},{wins},1,\"{master}\")";
                        using (SqliteCommand cmd2 = new SqliteCommand(sql, SQLCon, transaction))
                        {
                            rowsUpdated = cmd2.ExecuteNonQuery();
                            //WriteLine($"{rowsUpdated} Rows were inserted.");
                        }
                    }
                }
                transaction.Commit();
                SQLCon.Close();
            }
        }

        public static void DeleteFile()
        {
            if (File.Exists(Path))
                File.Delete(Path);
            if (File.Exists(Name + LogPath))
                File.Delete(Name + LogPath);
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
            using (StreamWriter sw = File.AppendText(Name + LogPath))
            {
                sw.WriteLine("[" + DateTime.Now.ToString("yy-MM-dd HH:mm:ss") + "] " + message);
            }
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

        public class Data{
            public string id;
            public string location;
            public string action;
            public string result;
            public string verify;
            public string value;
            public int count;
            public double wins;
            public int games;
            public int actionId;
            public Data(string id, string location, string action, string result, string verify, string value, int count, double wins, int turn, int actionId)
            {
                this.id = id;
                this.location = location;
                this.action = action;
                this.result = result;
                this.verify = verify;
                this.value = value;
                this.count = count;
                this.wins = wins;
                this.games = turn;
                this.actionId = actionId;
            }
        }
    }
}