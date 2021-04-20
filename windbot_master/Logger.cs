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
        public static Dictionary<int, Dictionary<int, ActionWeightCard>> actionWeight = new Dictionary<int, Dictionary<int, ActionWeightCard>>();

        private static List<Data> data = new List<Data>();
        private const int WIN = 0;
        private const int LOSE = 1;
        private const int TIE = 2;

        public static string master { get; private set; } = "master";
        public static string noActivateValue { get => " nouse"; }

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
        /// <param name="id">The card id that performs this action</param>
        /// <param name="action">The action the card is performing</param>
        /// <param name="activatePercent">The percentage of weights who activated this action</param>
        public static void SaveActionWeight(int turn, int actionId, double weight, string id, string action, double activatePercent)
        {
            //check if the turn is in the dictonary
            if (!actionWeight.ContainsKey(turn))
            {
                actionWeight.Add(turn, new Dictionary<int, ActionWeightCard>());
            }

            //There should only be one call per action Id
            if (actionWeight[turn].ContainsKey(actionId))
            {
                WriteErrorLine($"The action Id {actionId} called this method again");
            }
            else
            {
                actionWeight[turn].Add(actionId, new ActionWeightCard(id, weight, action, activatePercent));
            }
        }

        /// <summary>
        /// Modify the Specified Turn's action weight
        /// </summary>
        /// <param name="turn">The turn to modify</param>
        /// <param name="weight">The weight to multiply by</param>
        /// <param name="changeTag"> the tag of the change</param>
        public static void ModifyAction(int turn, double weight, int changeTag)
        {
            //if (weight < 0)
            //    weight = 0;
            List<Data> toAdd = new List<Data>();

            if (weight != 0)
            foreach (var node in data.FindAll(x => x.turn == turn && x.modified == 0))
            {
                Data newNode = new Data(node);
                double actual = 0;
                newNode.modified = changeTag;
                if (actionWeight.ContainsKey(turn))
                {
                    if (actionWeight[turn].ContainsKey(node.actionId))
                    {
                        actual = actionWeight[turn][node.actionId].weight;
                    }
                }
                if (!(actual < 0 && weight > 0))
                {
                    if (actual == 0)
                        actual = 1;
                    newNode.wins = 0.2 * weight * (Math.Sign(actual) - 0.1 * actual);//Math.Sign(actual) * weight;
                    toAdd.Add(newNode);
                }
            }
            data.AddRange(toAdd);
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
        public static List<double> GetData(string inprogress, string id = "", string location = "", string action = "", string result = "", string verify = "", string value = "", int count = -1, int depth = 0)
        {
            List<double> row = new List<double>();

            //break if checking the card you are trying to play
            if (id == value)
            {
                return row;
            }

            double games = 0;
            double wins = 0;
            //master = "master";//inprogress
            ConnectToDatabase();
            using (SQLCon)
            {
                SQLCon.Open();

                string sql = $"SELECT wins, games FROM playCard WHERE id = \"{id}\" AND location = \"{location}\" " +
                      $"AND action = \"{action}\" AND result = \"{result}\" AND verify = \"{verify}\" " +
                      $"AND value = \"{value}\" AND count = {count} AND " +
                      $"inprogress =  \"{master}\"";
                using (SqliteCommand cmd = new SqliteCommand(sql, SQLCon))
                {
                    using (SqliteDataReader rdr = cmd.ExecuteReader())
                        while (rdr.Read())
                        {
                            wins += rdr.GetDouble(0);
                            games += rdr.GetDouble(1);
                        }
                }
                Console.WriteLine(string.Format($"{wins,-5},{games,-3},{id,-3},{location,-3},{action,-3},{result,-3},{verify,-3},{value,-3},{count,-3}"));
                SQLCon.Close();
            }
            row.Add(wins);
            row.Add(games);
            return row;
        }

        /// <summary>
        /// Gets the weight of the input's select action (if any) from the database.
        /// </summary>
        /// <param name="id">Id of the card</param>
        public static List<Data> GetDataSelect(string id)
        {
            List<Data> row = new List<Data>();

            ConnectToDatabase();
            using (SQLCon)
            {
                SQLCon.Open();

                string sql = $"SELECT location,result,verify,value,count,sum(wins),sum(games) FROM playCard WHERE id = \"{id}\" " +
                      $"AND action = \"Select\" AND inprogress =  \"{master}\" GROUP BY result,verify,value,count,wins,games";
                using (SqliteCommand cmd = new SqliteCommand(sql, SQLCon))
                {
                    using (SqliteDataReader rdr = cmd.ExecuteReader())
                        while (rdr.Read())
                        {
                            string location = rdr.GetString(0);
                            string result = rdr.GetString(1);
                            string verify = rdr.GetString(2);
                            string value = rdr.GetString(3);
                            int count = (int)rdr.GetDouble(4);
                            double wins = rdr.GetDouble(5);
                            int games = (int)rdr.GetDouble(6);
                            row.Add(new Data(id,location,"Select",result,verify,value,count,wins,games,-1));
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
            Random rand = new Random();

            using (SQLCon)
            {
                SQLCon.Open();
                //SQLCon.State
                SqliteTransaction transaction = SQLCon.BeginTransaction();

                //Select all the data that was stored
                string sql =
                      $"SELECT id,location,action,result,verify,value,count,wins,games FROM playCard " +
                      $"WHERE inprogress = \"{Name}\"";

                //Removes all duplicates
                List<Data> temp = new List<Data>();

                foreach (Data d in data)
                {
                    int index = temp.FindIndex(x => x.id == d.id && x.location == d.location && x.action == d.action
                                    && x.result == d.result && x.verify == d.verify && x.value == d.value
                                    && x.count == d.count && x.modified == d.modified
                                    && x.turn == d.turn// && x.wins == d.wins
                                    );
                    if (index < 0)
                    {
                        temp.Add(d);
                    }
                    else if (Math.Abs(temp[index].wins) >  Math.Abs(d.wins))
                    {
                        temp[index].wins = d.wins;
                    }
                }

                data = temp;

                for (int i = 0; i < data.Count; i++)
                {
                    Data info = data[i];
                    string id = info.id;
                    string location = info.location;
                    string action = info.action;
                    string result = info.result;
                    string verify = info.verify;
                    string value = info.value;
                    int count = info.count;
                    double wins =(info.wins);
                    double games = 0;
                    double actual = 0;
                    //double games = info.turn; //stored as games since the database column is named games
                    double confidence = -1;

                    double turnActions = 0;

                    //if (action.Contains("Activate"))
                    //    wins = 0;

                    if (info.turn != 0 && (info.turn < turns - 1 || gameResult == WIN))
                    {
                        // Checks how confident this is a good move 
                       
                        if (actionWeight.ContainsKey(info.turn))
                        {
                            turnActions = actionWeight[info.turn].Count;
                            // See if any future actions contain the select action
                            foreach (int actionId in actionWeight[info.turn].Keys)
                            {
                                ActionWeightCard selectAction = actionWeight[info.turn][actionId];
                                actual = selectAction.weight;

                                if (selectAction.action == "Select"// check for select action
                                && actionId > info.actionId  //make sure the action is after
                                && selectAction.id.Equals(id)) // make sure its the same card
                                {
                                    //wins *= selectAction.activatePercent;
                                    //if (Math.Abs(selectAction.weight) < 5 // If not confident
                                    //)// Check if its the random variation
                                    if (action == "Activate")
                                    {
                                        wins = 0;
                                    }
                                    // If select action is bad
                                    /*if (selectAction.weight <= -5)
                                    {
                                        wins = -1;
                                        info.modified = 3;
                                    }*/
                                    break;
                                }
                            }
                            /*if (actionWeight[info.turn].ContainsKey(info.actionId + 1))
                            {
                                ActionWeightCard nextAction = actionWeight[info.turn][info.actionId + 1];
                                if (nextAction.action == "Select"// check for select action
                                && nextAction.id == id //make sure its the same id
                                && (Math.Abs(nextAction.weight) < 5 // If not confident
                                || nextAction.weight == 999))// Check if its the random variation
                                {
                                    wins = 0;
                                }
                            }*/
                            if (actionWeight[info.turn].ContainsKey(info.actionId))
                                confidence = Math.Abs(actionWeight[info.turn][info.actionId].weight);
                        }
                    }

                    if (info.modified == 0)
                    {
                        if (wins != 0)
                            wins = 0.2 * (1 - 2 * gameResult) * (Math.Sign(actual) - 0.1 * actual);//actual?
                       
                        wins = 0;
                    }
                    else
                    {

                    }
                    games = 1;
                   
                    // Random chance to update
                    wins *= 0.5;//rand.NextDouble() > 0.5 ? 1 : 0;
                    double ratio = 1;


                    sql = $"UPDATE playCard SET wins = wins * {ratio} + {wins}, " +
                        $"games = games + {games} WHERE " +
                            $"id = \"{id}\" AND location = \"{location}\" AND action = \"{action}\"  AND result LIKE \"{result}\" " +
                            $"AND verify = \"{verify}\" AND value = \"{value}\" AND count = {count} " +
                            $"AND inprogress =  \"{Name}\" ";

                    int rowsUpdated = 0;
                    if (wins != 0)
                    {
                        using (SqliteCommand cmd2 = new SqliteCommand(sql, SQLCon, transaction))
                        {
                           rowsUpdated = cmd2.ExecuteNonQuery();
                            //WriteLine($"{rowsUpdated} Rows Were updated." );
                        }
                        // If there was no update, add it instead
                        if (rowsUpdated <= 0)
                        {
                            //The master data isnt in the database yet so add it
                            sql = $"INSERT INTO playCard (id,location,action,result,verify,value,count,wins,games,inprogress) VALUES (\"{id}\",\"{location}\",\"{action}\",\"{result}\",\"{verify}\",\"{value}\",{count},{wins}," +
                                $"{games},\"{Name}\")";
                            using (SqliteCommand cmd2 = new SqliteCommand(sql, SQLCon, transaction))
                            {
                                rowsUpdated = cmd2.ExecuteNonQuery();
                                //WriteLine($"{rowsUpdated} Rows were inserted.");
                            }
                        }
                    }
                }
                transaction.Commit();
                SQLCon.Close();
            }

            //Print Game log
            if (true)
            foreach(int turn in actionWeight.Keys)
            {
                Console.WriteLine($"Turn {turn} -----------");
                foreach(int actionId in actionWeight[turn].Keys)
                {
                        ActionWeightCard action = actionWeight[turn][actionId];
                        Console.WriteLine($"   {actionId}:{action.weight} - {action.id}, {action.action}");
                }
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

        public class ActionWeightCard{
            public string id;
            public double weight;
            public string action;
            public double activatePercent;
            public ActionWeightCard(string id, double weight, string action, double activatePercent)
            {
                this.id = id;
                this.weight = weight;
                this.action = action;
                this.activatePercent = weight > 0 ? activatePercent : 1 - activatePercent;
            }
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
            public int turn;
            public int actionId;
            public int modified = 0; // 1 last turn, 2 turn before last, 3 other
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
                this.turn = turn;
                this.actionId = actionId;
            }

            public Data(Data node)
            {
                this.id = node.id;
                this.location = node.location;
                this.action = node.action;
                this.result = node.result;
                this.verify = node.verify;
                this.value = node.value;
                this.count = node.count;
                this.wins = node.wins;
                this.turn = node.turn;
                this.actionId = node.actionId;
            }
        }
    }
}