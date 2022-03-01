using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Mono.Data.Sqlite;

namespace WindBot
{
    class SqlComm
    {
        // Set from WindBotInfo
        public static bool IsTraining = true;
        public static bool ShouldUpdate = true;

        public static string Path = "log.txt";
        public static string LogPath = "gameLog.txt";
        public static string Name = "ai";
        public static SqliteConnection SQLCon = null;
        public static Dictionary<int, Dictionary<int, ActionWeightCard>> actionWeight = new Dictionary<int, Dictionary<int, ActionWeightCard>>();
        public static TreeActivation TreeActivation = new TreeActivation();


        private static List<Data> data = new List<Data>();
        private const int WIN = 0;
        private const int LOSE = 1;
        private const int TIE = 2;

        public static string master { get; private set; } = "master";
        public static string noActivateValue { get => " nouse"; }

        public class ActionWeightCard
        {
            public string id;
            public double? weight;
            public string action;
            public double? activatePercent;
            public ActionWeightCard(string id, double? weight, string action, double? activatePercent)
            {
                this.id = id;
                this.weight = weight;
                this.action = action;
                this.activatePercent = activatePercent;
            }
        }

        public class Data
        {
            public string id;
            public string location;
            public string action;
            public string result;
            public string verify;
            public string value;
            public int count;
            public double? activation;
            public int turn;
            public int actionId;
            public int modified = 0; // 1 last turn, 2 turn before last, 3 other
            public Data(string id, string location, string action, string result, string verify, string value, int count, double? activation, int turn, int actionId)
            {
                this.id = id;
                this.location = location;
                this.action = action;
                this.result = result;
                this.verify = verify;
                this.value = value;
                this.count = count;
                this.activation = activation;
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
                this.activation = node.activation;
                this.turn = node.turn;
                this.actionId = node.actionId;
                this.modified = node.modified;
            }
        }

        private static void ConnectToDatabase()
        {
            if (SQLCon == null)
            {
                //@"URI=file:\windbot_master\windbot_master\bin\Debug\cards.cdb";
                string dir = Directory.GetCurrentDirectory();
                //Go to the YugiohAi Directory
                dir = dir.Remove(dir.IndexOf(@"windbot_master\bin\Debug")) + "cardData.cdb";
                string absolutePath = $@"URI = file: {dir}";
                SQLCon = new SqliteConnection(absolutePath);
            }
        }

        public static void RecordUpdateAction(string value, double mesure, string turnPlayer)
        {
            Data d = new Data("UpdateAction", "", value, "", turnPlayer, "", 0, mesure, 0, -1);
            d.modified = -2;
            data.Add(d);
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
        public static void RecordAction(string id = "", string location = "", string action = "", string result = "", string verify = "", string value = "", int count = 1, double? activation = 1, int turn = 0, int actionId = 0)
        {
            data.Add(new Data(id, location, action, result, verify, value, count, activation, turn, actionId));
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
        public static void SaveActionWeight(int turn, int actionId, double? weight, string id, string action, double? activatePercent)
        {
            //check if the turn is in the dictonary
            if (!actionWeight.ContainsKey(turn))
            {
                actionWeight.Add(turn, new Dictionary<int, ActionWeightCard>());
            }

            //There should only be one call per action Id
            if (actionWeight[turn].ContainsKey(actionId))
            {
                //WriteErrorLine($"The action Id {actionId} called this method again");
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
        public static void RecordActual(int turn, double weight, int changeTag)
        {
            List<Data> toAdd = new List<Data>();

            List<Data> history;
            history = data.FindAll(x => x.turn == turn && x.modified == 0);

            foreach (var node in history)
            {
                if (node.activation == null)
                    continue;
                Data newNode = new Data(node);
                Console.WriteLine($"Changing {node.id} {node.action} {node.activation} -> {weight}");
                newNode.activation = weight;
                newNode.modified = changeTag;
                toAdd.Add(newNode);
            }
            data.AddRange(toAdd);
        }

        public static List<TreeActivation.Node> GetBestTreeNodeActions(int turn, string prevAction, bool isFirst)
        {
            List<TreeActivation.Node> row = new List<TreeActivation.Node>();

            ConnectToDatabase();
            using (SQLCon)
            {
                SQLCon.Open();

                string sql = $"SELECT actionId, activation, action, games, preAction from playCardTree WHERE " +
                      $"preAction like \"{prevAction}%\" AND isFirst = \"{isFirst}\" " +
                      $"AND turn = \"{turn}\" " +
                      $"ORDER BY activation desc, actionId desc";
                using (SqliteCommand cmd = new SqliteCommand(sql, SQLCon))
                {
                    using (SqliteDataReader rdr = cmd.ExecuteReader())
                        while (rdr.Read())
                        {
                            int actionId = rdr.GetInt32(0);
                            double? weight = null;
                            if (!rdr.IsDBNull(1))
                                weight = rdr.GetDouble(1);
                            string action = rdr.GetString(2);
                            double games = rdr.GetDouble(3);
                            string preActions = rdr.GetString(4);
                            row.Add(new TreeActivation.Node(action, weight, actionId, games, preActions));
                        }
                }
                SQLCon.Close();
            }

            return row;
        }

        /**
         * Returns the list of potential next action ids followed by their weights
         */
        public static List<TreeActivation.Node> GetNextTreeNodes(int turn, string prevAction, bool isFirst)
        {
            List<TreeActivation.Node> row = new List<TreeActivation.Node>();

            ConnectToDatabase();
            using (SQLCon)
            {
                SQLCon.Open();

                string sql = $"SELECT actionId, activation, action, games from playCardTree WHERE " +
                      $"preAction = \"{prevAction}\" AND isFirst = \"{isFirst}\" " +
                      $"AND turn = \"{turn}\"";
                using (SqliteCommand cmd = new SqliteCommand(sql, SQLCon))
                {
                    using (SqliteDataReader rdr = cmd.ExecuteReader())
                        while (rdr.Read())
                        {
                            int actionId = rdr.GetInt32(0);
                            double? weight = null;
                            if (!rdr.IsDBNull(1))
                                weight = rdr.GetDouble(1);
                            string action = rdr.GetString(2);
                            double games = rdr.GetDouble(3);
                            row.Add(new TreeActivation.Node(action, weight, actionId, games));
                        }
                }
                SQLCon.Close();
            }

            return row;
        }
        

        public static List<double?> GetTreeNode(int turn, int actionId, string id, string action, string prevAction, bool isFirst)
        {
            List<double?> row = new List<double?>();

            double games = 0;
            double? activation = 0;
            ConnectToDatabase();
            using (SQLCon)
            {
                SQLCon.Open();

                string sql = $"SELECT activation, games FROM playCardTree WHERE id = \"{id}\" " +
                      $"AND action = \"{action}\"" +
                      //$" AND preId = \"{}\"" +
                      $" AND preAction = \"{prevAction}\" AND isFirst = \"{isFirst}\" " +
                      $"AND turn = \"{turn}\" and actionID = \"{actionId}\"";
                using (SqliteCommand cmd = new SqliteCommand(sql, SQLCon))
                {
                    using (SqliteDataReader rdr = cmd.ExecuteReader())
                        while (rdr.Read())
                        {
                            if (rdr.IsDBNull(0))
                                activation = null;
                            else
                            {
                                activation = rdr.GetDouble(0);
                                games = rdr.GetDouble(1);
                            }

                            row.Add(activation);
                            row.Add(games);
                        }
                }
                SQLCon.Close();
            }

            string weight = row.Count > 0 ? row[0].ToString() : "null";
            //Console.WriteLine(string.Format($"    {actionId} {weight} | {action} {id} | {preAction} {preId}"));
            if (row.Count == 0)
            {
                var avg = GetAvgTreeNodeAction(id, action);
                //row = avg;
            }
            else if (row.Count > 0 && Program.Rand.NextDouble() < (2 - games) / 2)
            {
                //row[0] = 10;
            }
            return row;
        }

        static List<double?> GetAvgTreeNodeAction(string id, string action)
        {
            List<double?> row = new List<double?>();

            double games = 0;
            double activation = 0;
            ConnectToDatabase();
            using (SQLCon)
            {
                SQLCon.Open();

                string sql = $"SELECT avg(activation), avg(games) FROM playCardTree WHERE id = \"{id}\" " +
                      $"AND action = \"{action}\" " +
                      $"GROUP BY action, id";
                using (SqliteCommand cmd = new SqliteCommand(sql, SQLCon))
                {
                    using (SqliteDataReader rdr = cmd.ExecuteReader())
                        while (rdr.Read())
                        {
                            if (!rdr.IsDBNull(0))
                            {
                                activation = rdr.GetDouble(0);
                                games = rdr.GetDouble(1);
                                row.Add(activation);
                                row.Add(games);
                            }
                        }
                }
                SQLCon.Close();
            }

            string weight = row.Count > 0 ? row[0].ToString() : "null";
            //Console.WriteLine(string.Format($"    {actionId} {weight} | {action} {id} | {preAction} {preId}"));

            return row;
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
            double activation = 0;
            //master = "master";//inprogress
            ConnectToDatabase();
            using (SQLCon)
            {
                SQLCon.Open();

                string sql = $"SELECT activation, games FROM playCard WHERE id = \"{id}\" AND location = \"{location}\" " +
                      $"AND action = \"{action}\" AND result = \"{result}\" AND verify = \"{verify}\" " +
                      $"AND value = \"{value}\"" +
                      //$" AND count = {count}" +
                      $" AND " +
                      $"inprogress =  \"{master}\" " +
                      $"AND games > 1";
                using (SqliteCommand cmd = new SqliteCommand(sql, SQLCon))
                {
                    using (SqliteDataReader rdr = cmd.ExecuteReader())
                        while (rdr.Read())
                        {
                            activation += rdr.GetDouble(0);
                            games += rdr.GetDouble(1);
                        }
                }
                Console.WriteLine(string.Format($"          {activation,-5},{games,-3},{id,-3},{location,-3},{action,-3},{result,-3},{verify,-3},{value,-3},{count,-3}"));
                SQLCon.Close();
            }
            row.Add(activation);
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

                string sql = $"SELECT location,result,verify,value,count,sum(activation),sum(games) FROM playCard WHERE id = \"{id}\" " +
                      $"AND action = \"Select\" AND inprogress =  \"{master}\" GROUP BY result,verify,value,count,activation,games";
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
                            double? activation = rdr.GetDouble(5);
                            int games = (int)rdr.GetDouble(6);
                            row.Add(new Data(id, location, "Select", result, verify, value, count, activation, games, -1));
                        }
                }
                SQLCon.Close();
            }
            return row;
        }

        public static void UpdateTreeNode(int gameResult)
        {
            if (TreeActivation.TurnActions.Count > 0)
            {
                if (gameResult == WIN)
                {
                    TreeActivation.UpdateNode(TreeActivation.TurnActions.Keys.Max() + 1, TreeActivation.THRESHOLD, true);
                }
                else if (gameResult == LOSE)
                    TreeActivation.UpdateNode(TreeActivation.TurnActions.Keys.Max() + 1, -1);
            }

            ConnectToDatabase();
            using (SQLCon)
            {
                SQLCon.Open();
                //SQLCon.State
                SqliteTransaction transaction = SQLCon.BeginTransaction();

                //Select all the data that was stored
                string sql = "";
                foreach (var turn in TreeActivation.TurnActions.Keys)
                {
                    Queue<TreeActivation.Node> q = new Queue<TreeActivation.Node>();
                    foreach(var node in TreeActivation.TurnActions[turn])
                        q.Enqueue(node);

                    while (q.Count > 0)
                    {
                        var node = q.Dequeue();
                        string preId = "";
                        string preAction = node.GetPrevActions();

                        string weight_string = node.weight.ToString();
                        if (node.weight == null)
                        {
                            if (node.originalWeight == null)
                                weight_string = "null";
                            else
                                weight_string = node.originalWeight.ToString();
                        }

                        int games = 0;
                        if (node.activated)
                            games = 1;

                        sql = $"UPDATE playCardTree SET activation = {weight_string}, " +
                            $"games = games + {games} WHERE " +
                                $"id = \"{node.id}\" AND action = \"{node.action}\" AND " +
                                $"preId = \"{preId}\" AND preAction = \"{preAction}\" AND " +
                                $"turn = \"{turn}\" AND actionId = \"{node.actionId}\"" +
                                $" AND isFirst = \"{node.isFirst}\"";

                        int rowsUpdated = 0;

                        if (node.actionId != 0 && (node.activated || (!node.activated && node.weight == null)))
                        {
                            using (SqliteCommand cmd2 = new SqliteCommand(sql, SQLCon, transaction))
                            {
                                rowsUpdated = cmd2.ExecuteNonQuery();
                            }
                            // If there was no update, add it instead
                            if (rowsUpdated <= 0)
                            {
                                //node.actionId = 0;
                                //The master data isnt in the database yet so add it
                                sql = $"INSERT INTO playCardTree (id,action,preId,preAction,turn,actionId,isFirst,activation,games) VALUES (\"{node.id}\",\"{node.action}\",\"{preId}\",\"{preAction}\",\"{turn}\",\"{node.actionId}\",\"{node.isFirst}\"," +
                                    $"{weight_string},\"{games}\")";
                                using (SqliteCommand cmd2 = new SqliteCommand(sql, SQLCon, transaction))
                                {
                                    rowsUpdated = cmd2.ExecuteNonQuery();
                                }
                            }
                        }

                        foreach(var child in node.children)
                        {
                            q.Enqueue(child);
                        }
                    }
                }
                transaction.Commit();
                SQLCon.Close();
            }
        }

        /// <summary>
        /// Updates the data base result
        /// </summary>
        /// <param name="gameResult">0 = win, 1 = lose, 2 = tie</param>
        /// <param name="otherName">Name of the opponent, Unused</param>
        public static void UpdateDatabase(int gameResult, string otherName, int turns, bool isFirst)
        {
            if (!IsTraining)
                return;
            UpdateTreeNode(gameResult);
            ConnectToDatabase();
            using (SQLCon)
            {
                SQLCon.Open();
                //SQLCon.State
                SqliteTransaction transaction = SQLCon.BeginTransaction();

                //Select all the data that was stored
                string sql =
                      $"SELECT id,location,action,result,verify,value,count,activation,games FROM playCard " +
                      $"WHERE inprogress = \"{Name}\"";

                List<Data> toAdd = new List<Data>();

                // Replace the original data
                List<Data> original = data.FindAll(x => x.modified == 0);
                foreach (Data d in original)
                {
                    data.Remove(d);
                    Data newNode = new Data(d);
                    d.activation = 0;//w;
                    toAdd.Add(d);
                }
                toAdd.Clear();

                // Don't save the last turn if you lost on opp turn
                if (turns % 2 != (isFirst ? 1 : 0))
                {
                    foreach (Data d in data.FindAll(x => x.turn == turns))
                    {
                        data.Remove(d);
                    }
                }

                // Merge all the weights together
                foreach (Data d in data)
                {
                    int index = toAdd.FindIndex(x => x.id == d.id && x.location == d.location && x.action == d.action
                                && x.result == d.result && x.verify == d.verify && x.value == d.value
                                && x.count == d.count);
                    if (index < 0)
                    {
                        Data newNode = new Data(d);
                        if (newNode.modified != -2)
                        {
                            newNode.modified = 0;
                            newNode.activation = 0;
                            newNode.turn = 0;
                        }

                        List<Data> result = data.FindAll(x => x.id == d.id && x.location == d.location && x.action == d.action
                                && x.result == d.result && x.verify == d.verify && x.value == d.value && x.count == d.count); //TO CHANGE

                        int i = 0;
                        foreach (Data r in result)
                        {
                            if (r.activation != null)
                            {
                                newNode.activation += r.activation;
                                i++;
                            }
                            //newNode.activation = (newNode.activation * (i - 1) + r.activation) / i;
                        }
                        if (i > 0)
                            newNode.activation /= i;
                        else
                            newNode.activation = null;

                        toAdd.Add(newNode);
                    }
                }

                data = toAdd;

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
                    double? activation = (info.activation);
                    double games = 1;
                    double originalWin = 0;
                    string sql_string = $"SELECT activation FROM playCard WHERE id = \"{id}\" AND location = \"{location}\" " +
                                          $"AND action = \"{action}\" AND result = \"{result}\" AND verify = \"{verify}\" " +
                                          $"AND value = \"{value}\" AND " +
                                          $"inprogress =  \"{master}\"";

                    using (SqliteCommand cmd = new SqliteCommand(sql_string, SQLCon))
                    {
                        using (SqliteDataReader rdr = cmd.ExecuteReader())
                            while (rdr.Read())
                            {
                                originalWin += rdr.GetDouble(0);
                            }
                    }

                    if (info.modified == -2) // update weights modifiers
                    {
                        if (gameResult == 1)
                            result = "l";

                    }


                    Console.WriteLine($"    {id}, {location}, {action}, {result}, {verify}, {value}, {count}, Act:{activation}");

                    sql = $"UPDATE playCard SET activation = (activation * games + {activation}) / (games + {games}), " +
                        $"games = games + {games} WHERE " +
                            $"id = \"{id}\" AND location = \"{location}\" AND action = \"{action}\"  AND result LIKE \"{result}\" " +
                            $"AND verify = \"{verify}\" AND value = \"{value}\" AND count = {count} " +
                            $"AND inprogress =  \"{Name}\" ";

                    int rowsUpdated = 0;
                    if (activation != null && ShouldUpdate)
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
                            string activation_string = activation.ToString();
                            sql = $"INSERT INTO playCard (id,location,action,result,verify,value,count,activation,games,inprogress) VALUES (\"{id}\",\"{location}\",\"{action}\",\"{result}\",\"{verify}\",\"{value}\",{count}," +
                                $"{activation_string},{games},\"{Name}\")";
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
                foreach (int turn in actionWeight.Keys)
                {
                    Console.WriteLine($"Turn {turn} -----------");
                    foreach (int actionId in actionWeight[turn].Keys)
                    {
                        ActionWeightCard action = actionWeight[turn][actionId];
                        Console.WriteLine($"   {actionId}:{action.weight} - {action.id}, {action.action}");
                    }
                }
            WriteToFile("");
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
            /*using (StreamWriter sw = File.AppendText(Path))
            {
                sw.WriteLine(str);
            }*/
        }
    }
}
