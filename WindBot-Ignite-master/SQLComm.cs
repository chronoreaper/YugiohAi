using Mono.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using static System.Net.Mime.MediaTypeNames;
using static WindBot.MCST;
using static WindBot.NEAT;
using static WindBot.PlayHistory;

namespace WindBot
{
    public class SQLComm
    {
        public static bool HasParameters = false;
        public static bool IsFirst = true;
        public static bool IsTraining = true;
        public static bool IsManual = false;
        public static bool ShouldUpdate = true;
        public static bool IsMCTS = false;
        public static bool IsRollout = false;
        public static bool IsHardCoded = false;
        public static bool ShouldBackPropagate = false;
        public static int TotalGames = 201;
        public static int RolloutCount = 2;
        public static int Id = 0;

        public static int GamesPlayed = 0;
        public static int Wins = 0;
        public static double TotalRewards = 0;

        public static string Name = "Bot";
        public static string Opp = "Enemy";

        public static int PastWinsLimit = 10;
        public static int PastXWins = 0;
        public static Queue<int> PreviousWins = new Queue<int>();
        public static int WinsThreshold = 45;
        public static string sqlPath = $@"Data Source=./cardData.cdb";

        public class CardQuant
        {
            public string Name;
            public int Quant;
        }

        private static SqliteConnection ConnectToDatabase()
        {
            return new SqliteConnection(sqlPath);
        }
        #region MCST
        public static bool GetNodeInfo(Node node)
        {
            bool gotInfo = false;

            using(SqliteConnection conn = ConnectToDatabase())
            {
                conn.Open();
                string sql = $"SELECT rowid, Reward, Visited, AvgTurn FROM MCST WHERE " +
                    $"ParentId = \"{node.Parent?.NodeId ?? -4}\" AND CardId = \"{node.CardId}\" AND Action = \"{node.Action}\" AND IsFirst = \"{IsFirst}\" AND Name = \"{Name}\"";
                using (SqliteCommand cmd = new SqliteCommand(sql, conn))
                {
                    using (SqliteDataReader rdr = cmd.ExecuteReader())
                    while (rdr.Read())
                    {
                        node.NodeId = long.Parse(rdr["rowid"].ToString());
                        node.Rewards = rdr.GetDouble(1);
                        node.Visited = rdr.GetInt32(2);
                        node.AvgTurn = rdr.GetDouble(3);
                        gotInfo = true;
                    }
                }
                conn.Close();
            }

            return gotInfo;
        }

        public static double GetNodeEstimate(Node node)
        {
            double value = 0;
            if (node.Action == "GoToEndPhase")
                return value;
            using (SqliteConnection conn = ConnectToDatabase())
            {
                conn.Open();
                string sql = $"SELECT SUM(Reward), COUNT(Visited) from MCST WHERE CardId = \"{node.CardId}\" AND Action = \"{node.Action}\" AND IsFirst = \"{IsFirst}\" AND Visited > 0 AND Name = \"{Name}\"" +
                    $" AND rowid != \"{node.NodeId}\"";
                double reward = 0;
                int visited = 0;
                using (SqliteCommand cmd = new SqliteCommand(sql, conn))
                {
                    try
                    {
                        using (SqliteDataReader rdr = cmd.ExecuteReader())
                            while (rdr.Read())
                            {
                                reward = rdr.GetDouble(0);
                                visited = rdr.GetInt32(1);
                            }
                    }
                    catch(InvalidCastException)
                    {

                    }
                }
                conn.Close();

                value = reward / Math.Max(1,visited);
            }


            return value;
        }

        public static int GetTotalGames()
        {
            int total = 0;
            using (SqliteConnection conn = ConnectToDatabase())
            {
                conn.Open();
                string sql = $"SELECT SUM(Visited) FROM MCST WHERE ParentId = 1 AND IsFirst = \"{IsFirst}\" AND IsTraining = \"{ShouldUpdate}\" AND Name = \"{Name}\"";
                try
                {
                    using (SqliteCommand cmd = new SqliteCommand(sql, conn))
                    {
                        using (SqliteDataReader rdr = cmd.ExecuteReader())
                            while (rdr.Read())
                            {
                                total += rdr.GetInt32(0);
                            }
                    }
                }
                catch (InvalidCastException)
                {
                    Logger.WriteLine("Empty MCST Database");
                }
                conn.Close();
            }

            return total;
        }

        public static float GetAvgCount()
        {
            float total = 0;
            using (SqliteConnection conn = ConnectToDatabase())
            {
                conn.Open();
                string sql = $"SELECT AVG(AvgTurn) FROM MCST WHERE AvgTurn != 9999 AND IsFirst = \"{IsFirst}\" AND Name = \"{Name}\"";
                try
                {
                    using (SqliteCommand cmd = new SqliteCommand(sql, conn))
                    {
                        using (SqliteDataReader rdr = cmd.ExecuteReader())
                            while (rdr.Read())
                            {
                                total += rdr.GetFloat(0);
                            }
                    }
                }
                catch (InvalidCastException)
                {
                    Logger.WriteLine("Empty MCST Database AvgCount");
                }
                conn.Close();
            }

            return total;
        }

        public static void InsertNode(Node node)
        {
            if (!IsMCTS)
                return;
            using (SqliteConnection conn = ConnectToDatabase())
            {
                conn.Open();

                SqliteTransaction transaction = conn.BeginTransaction();

                long parentId = -4;
                long childId = -4;
                if (node.Parent != null && node.Parent.NodeId != -4 && node.SaveParent)
                {
                    parentId = node.Parent.NodeId;
                }
                if (node.Children.Count == 1 && node.SaveChild)
                {
                    childId = node.Children[0].NodeId;
                }

                string sql = $"INSERT INTO MCST (Name,ParentId,ChildId,CardId,Action,AvgTurn,Reward,Visited,IsFirst,IsTraining) VALUES (\"{Name}\",\"{parentId}\",\"{childId}\",\"{node.CardId}\",\"{node.Action}\",\"9999\",\"0\",\"0\",\"{IsFirst}\",\"{IsTraining}\")";
                using (SqliteCommand cmd2 = new SqliteCommand(sql, conn, transaction))
                {
                    cmd2.ExecuteNonQuery();
                }

                sql = "select last_insert_rowid()";
                using (SqliteCommand cmd2 = new SqliteCommand(sql, conn, transaction))
                {
                    node.NodeId = (long)cmd2.ExecuteScalar();
                }

                transaction.Commit();
                conn.Close();
            }
        }

        /***
         * Result, 0 = win, 1 = lose, 2 = tie
         */
        public static void Backpropagate(List<Node> nodes, Node node, double reward, int turn)
        {
            if (!IsMCTS)
                return;

            if (!ShouldBackPropagate && RolloutCount > 1)
            {
                TotalRewards += reward;
                //RecordWin(Math.Max(0, Math.Round(node.Heuristic() / RolloutCount) / 10), true);

                return;
            }
            else if(RolloutCount <= 1)
            {
                TotalRewards += reward;
            }

            double totalRewards = TotalRewards;
            int rolloutCount = RolloutCount;

            using (SqliteConnection conn = ConnectToDatabase())
            {
                conn.Open();

                SqliteTransaction transaction = conn.BeginTransaction();


                int rowsUpdated = 0;
                string sql = $"";

                Queue<Node> q = new Queue<Node>(nodes);

                while (q.Count > 0)
                {
                    Node n = q.Dequeue();
                    if (n.NodeId != -4)
                    {
                        sql = $"UPDATE MCST SET Reward = Reward + {totalRewards / rolloutCount}, " + //+ Math.Max(0, Math.Round(node.Heuristic() / RolloutCount) / 10)
                            $"Visited = Visited + 1";
                        if (totalRewards > 0)
                            sql += $", AvgTurn = min(AvgTurn,{turn})";
                        sql += $" WHERE rowid = \"{n.NodeId}\" AND IsFirst = \"{IsFirst}\" AND IsTraining = \"{IsTraining}\" AND Name = \"{Name}\"";

                        using (SqliteCommand cmd2 = new SqliteCommand(sql, conn, transaction))
                        {
                            rowsUpdated = cmd2.ExecuteNonQuery();
                        }
                    }
                    else
                    {
                        // This probably never runs
                        long parentId = 0;
                        long childId = -4;
                        if (n.Parent != null && n.Parent.NodeId != -4)
                        {
                            parentId = n.Parent.NodeId;
                        }
                        /*if (n.Children.Count == 1)
                        {
                            if (n.Children[0].NodeId == -4)
                                continue;
                            childId = n.Children[0].NodeId;
                        }*/

                        sql = $"INSERT INTO MCST (Name,ParentId,ChildId,CardId,Action,AvgTurn,Reward,Visited,IsFirst,IsTraining) VALUES (\"{Name}\",\"{parentId}\",\"{childId}\",\"{n.CardId}\",\"{n.Action}\",\"{turn}\",\"{totalRewards / rolloutCount}\",\"1\",\"{IsFirst}\",\"{IsTraining}\")";
                        using (SqliteCommand cmd2 = new SqliteCommand(sql, conn, transaction))
                        {
                            rowsUpdated = cmd2.ExecuteNonQuery();
                        }
                    }

                    /*foreach(Node c in n.Children)
                    {
                        q.Enqueue(c);
                    }*/
                }

                transaction.Commit();
                conn.Close();
            }

            UpdateWeightTree(nodes, totalRewards, rolloutCount);

            ShouldBackPropagate = false;
            IsRollout = false;
            TotalRewards = 0;
        }



        public static void Reset()
        {
            using (SqliteConnection conn = ConnectToDatabase())
            {
                conn.Open();

                SqliteTransaction transaction = conn.BeginTransaction();
                // Remove the rollout
                string sql = $"DELETE FROM MCST WHERE IsFirst = \"{IsFirst}\" AND IsTraining = \"False\" AND Name = \"{Name}\"";
                using (SqliteCommand cmd2 = new SqliteCommand(sql, conn, transaction))
                {
                    cmd2.ExecuteNonQuery();
                }

                sql = $"UPDATE MCST SET IsTraining = \"False\" WHERE IsFirst = \"{IsFirst}\" AND IsTraining = \"True\" AND Name = \"{Name}\"";
                using (SqliteCommand cmd2 = new SqliteCommand(sql, conn, transaction))
                {
                    var v = cmd2.ExecuteNonQuery();
                }

                transaction.Commit();
                conn.Close();
            }
        }

        private static void UpdateWeightTree(List<Node> nodes, double reward, double visited)
        {
            if (!IsTraining)
                return;

            using (SqliteConnection conn = ConnectToDatabase())
            {
                conn.Open();

                SqliteTransaction transaction = conn.BeginTransaction();


                int rowsUpdated = 0;
                string sql;

                Queue<Node> q = new Queue<Node>(nodes);

                while (q.Count > 0)
                {
                    Node n = q.Dequeue();

                    foreach (string verify in n.StateCurrent.CardsInPlay)
                    {
                        sql = $"UPDATE WeightTree SET Reward = Reward + {reward / visited}, " +
                            $"Visited = Visited + 1 WHERE " +
                            $"CardId = \"{n.CardId}\" AND " +
                            $"Action = \"{n.Action}\" AND " +
                            $"Verify = \"{verify}\"";

                        using (SqliteCommand cmd2 = new SqliteCommand(sql, conn, transaction))
                        {
                            rowsUpdated = cmd2.ExecuteNonQuery();
                        }

                        if (rowsUpdated <= 0)
                        {

                            sql = $"INSERT INTO WeightTree (CardId,Action,Verify,Reward,Visited) VALUES (\"{n.CardId}\",\"{n.Action}\",\"{verify}\",\"{reward / visited}\",\"1\")";
                            using (SqliteCommand cmd2 = new SqliteCommand(sql, conn, transaction))
                            {
                                rowsUpdated = cmd2.ExecuteNonQuery();
                            }
                        }
                    }

                    foreach (Node c in n.Children)
                    {
                        q.Enqueue(c);
                    }
                }

                transaction.Commit();
                conn.Close();
            }
        }
        #endregion

        #region NEAT
        public static void Setup(NEAT neat)
        {
            using (SqliteConnection conn = ConnectToDatabase())
            {
                conn.Open();
                // Get input nodes
                string sql = $"SELECT Id, Name FROM NodeName WHERE Type = \"1\"";
                using (SqliteCommand cmd = new SqliteCommand(sql, conn))
                {
                    using (SqliteDataReader rdr = cmd.ExecuteReader())
                        while (rdr.Read())
                        {
                            int id = rdr.GetInt32(0);
                            string name = rdr.GetString(1);
                            neat.AddNode(name, true);
                        }
                }

                // Add Output Nodes
                sql = $"SELECT Id, Name FROM NodeName WHERE Type = \"-1\"";
                using (SqliteCommand cmd = new SqliteCommand(sql, conn))
                {
                    using (SqliteDataReader rdr = cmd.ExecuteReader())
                        while (rdr.Read())
                        {
                            int id = rdr.GetInt32(0);
                            string name = rdr.GetString(1);
                            neat.AddNode(name, false);
                        }
                }

                // Add InnovationNumbers
                sql = $"SELECT rowid, Input, Output FROM InnovationNumber";
                using (SqliteCommand cmd = new SqliteCommand(sql, conn))
                {
                    using (SqliteDataReader rdr = cmd.ExecuteReader())
                        while (rdr.Read())
                        {
                            long id = long.Parse(rdr["rowid"].ToString());
                            int input = rdr.GetInt32(1);
                            int output = rdr.GetInt32(2);
                            neat.Innovation.Add(id, new InnovationNumber() { Id = id, Input = input, Output = output });
                        }
                }

                // Add connections
                sql = $"SELECT InnovationId, Weight, Enabled FROM Connections WHERE SpeciesId = {Id}";
                using (SqliteCommand cmd = new SqliteCommand(sql, conn))
                {
                    using (SqliteDataReader rdr = cmd.ExecuteReader())
                        while (rdr.Read())
                        {
                            long id = rdr.GetInt32(0);
                            float weight = rdr.GetFloat(1);
                            bool enabled = rdr.GetBoolean(2);
                            try
                            {
                                if (enabled)
                                {
                                    InnovationNumber number = neat.Innovation[id];
                                    NEATNode input = neat.Nodes[number.Input];
                                    NEATNode output = neat.Nodes[number.Output];
                                    neat.AddConnection(id, input, output, weight);
                                }
                            }
                            catch (Exception e)
                            {
                                Logger.WriteErrorLine($"Error when adding connection: {e}");
                            }
                        }
                }

                conn.Close();
            }
        }

        public static void SaveNEAT(NEAT neat, int win)
        {
            //if (!IsTraining)
            //    return;
            win = Math.Max(Math.Min(win, 1), 0);

            using (SqliteConnection conn = ConnectToDatabase())
            {
                conn.Open();

                SqliteTransaction transaction = conn.BeginTransaction();


                int rowsUpdated = 0;
                string sql;

                sql = $"UPDATE SpeciesRecord SET Games = Games + 1, " +
                           $"Wins = Wins + {win} WHERE " +
                           $"Id = \"{Id}\"";

                using (SqliteCommand cmd2 = new SqliteCommand(sql, conn, transaction))
                {
                    rowsUpdated = cmd2.ExecuteNonQuery();
                }

                if (rowsUpdated <= 0)
                {

                    sql = $"INSERT INTO SpeciesRecord (Id,Games,Wins) VALUES (\"{Id}\",\"{1}\",\"{win}\")";
                    using (SqliteCommand cmd2 = new SqliteCommand(sql, conn, transaction))
                    {
                        rowsUpdated = cmd2.ExecuteNonQuery();
                    }
                }

                
                foreach (var edge in neat.Connections)
                {

                    sql = $"UPDATE Connections SET Wins = Wins + {edge.ActivationCount * win}, Games = Games + {edge.ActivationCount} WHERE InnovationId = {edge.Id} AND SpeciesId = {Id}";

                    using (SqliteCommand cmd2 = new SqliteCommand(sql, conn, transaction))
                    {
                        {
                            rowsUpdated = cmd2.ExecuteNonQuery();
                        }

                        if (rowsUpdated <= 0)
                        {

                        }
                    }

                }
                transaction.Commit();
                conn.Close();
            }
        }

        public static int GetNodeId(string inputName, int type)
        {
            int id = -4;

            using (SqliteConnection conn = ConnectToDatabase())
            {
                conn.Open();
                string sql = $"SELECT Id FROM NodeName WHERE " +
                    $"Name = \"{inputName}\" AND Type = \"{type}\"";
                using (SqliteCommand cmd = new SqliteCommand(sql, conn))
                {
                    using (SqliteDataReader rdr = cmd.ExecuteReader())
                        while (rdr.Read())
                        {
                            id = rdr.GetInt32(0);
                        }
                }
                conn.Close();
            }

            if (id == -4)
                return NewNode(inputName, type);
            return id;
        }

        private static int NewNode(string inputName, int type)
        {
            int id = 0;
            using (SqliteConnection conn = ConnectToDatabase())
            {
                conn.Open();
                string sql = "SELECT max(Id) from NodeName";
                try
                {
                   
                    using (SqliteCommand cmd = new SqliteCommand(sql, conn))
                    {
                        using (SqliteDataReader rdr = cmd.ExecuteReader())
                            while (rdr.Read())
                            {
                                id = rdr.GetInt32(0);
                            }
                    }

                    id++;
                }
                catch(System.InvalidCastException)
                {

                }

                SqliteTransaction transaction = conn.BeginTransaction();

                sql = $"INSERT INTO NodeName (Id, Name, Type) VALUES (\"{id}\", \"{inputName}\", \"{type}\")";
                using (SqliteCommand cmd2 = new SqliteCommand(sql, conn, transaction))
                {
                    cmd2.ExecuteNonQuery();
                }

                transaction.Commit();
                conn.Close();
            }

            return id;
        }
        #endregion

        #region Recording

        public static void SavePlayedCards(bool isFirst, bool postSide, int result, List<string> playedCards, List<CardQuant> allCards)
        {
            playedCards = playedCards.Distinct().ToList();
            using (SqliteConnection conn = ConnectToDatabase())
            {
                conn.Open();

                SqliteTransaction transaction = conn.BeginTransaction();

                string sql = $"INSERT INTO GameTable (Name, IsFirst, PostSide, Pool, Result, Enemy) VALUES (\"{Name}\", \"{isFirst}\", \"{postSide}\", \"{Id}\", \"{result}\", \"{Opp}\")";
                using (SqliteCommand cmd2 = new SqliteCommand(sql, conn, transaction))
                {
                    cmd2.ExecuteNonQuery();
                }

                long rowid = 0;
                sql = "select last_insert_rowid()";
                using (SqliteCommand cmd2 = new SqliteCommand(sql, conn, transaction))
                {
                    rowid = (long)cmd2.ExecuteScalar();
                }

                foreach(CardQuant card in allCards)
                {
                    bool played = playedCards.Contains(card.Name);
                    sql = $"INSERT INTO GameStats (GameId, CardId, Played, Quant) VALUES (\"{rowid}\", \"{card.Name}\", \"{played}\", \"{card.Quant}\")";
                    using (SqliteCommand cmd2 = new SqliteCommand(sql, conn, transaction))
                    {
                        cmd2.ExecuteNonQuery();
                    }
                }

                transaction.Commit();
                conn.Close();
            }
        }

        public static void SavePlayHistory(List<History> records, int result)
        {
            //if (!IsTraining && !IsManual)
            //    return;
            if (!ShouldUpdate)
                return;

            //if (result != 0)
            //    return;
            float reward = 0;
            if (result == 0)
                reward = 1;
            else if (result == 1)
                reward = -1;
            else
                reward = 0f;

            if (IsManual)
                reward = 1;

            using (SqliteConnection conn = ConnectToDatabase())
            {
                conn.Open();

                SqliteTransaction transaction = conn.BeginTransaction();

                string sql;
                long id;

                long gameId = 0;

                sql = $"INSERT INTO L_GameResult (Name, Result) VALUES (\"{Name}\", \"{reward}\")";
                using (SqliteCommand cmd2 = new SqliteCommand(sql, conn, transaction))
                {
                    cmd2.ExecuteNonQuery();
                }

                sql = "select last_insert_rowid()";
                using (SqliteCommand cmd2 = new SqliteCommand(sql, conn, transaction))
                {
                    gameId = (long)cmd2.ExecuteScalar();
                }

                foreach (var record in records)
                {
                    sql = $"INSERT INTO L_PlayRecord (GameId, TurnId, ActionId, CurP1Hand, CurP1Field, CurP2Hand, CurP2Field, PostP1Hand, PostP1Field, PostP2Hand, PostP2Field) " +
                        $"VALUES (\"{gameId}\", \"{record.Info.Turn}\", \"{record.Info.ActionNumber}\", " +
                        $"\"{record.CurP1Hand}\", \"{record.CurP1Field}\", \"{record.CurP2Hand}\", \"{record.CurP2Field}\"," +
                        $"\"{record.PostP1Hand}\", \"{record.PostP1Field}\", \"{record.PostP2Hand}\", \"{record.PostP2Field}\")";
                    using (SqliteCommand cmd2 = new SqliteCommand(sql, conn, transaction))
                    {
                        cmd2.ExecuteNonQuery();
                    }

                    sql = "select last_insert_rowid()";
                    using (SqliteCommand cmd2 = new SqliteCommand(sql, conn, transaction))
                    {
                        id = (long)cmd2.ExecuteScalar();
                    }

                    foreach (var action in record.ActionInfo)
                    {
                        sql = $"INSERT INTO L_ActionState (ActionId, HistoryId, Performed) VALUES (\"{action.ActionId}\", \"{id}\", \"{action.Performed}\")";
                        using (SqliteCommand cmd2 = new SqliteCommand(sql, conn, transaction))
                        {
                            cmd2.ExecuteNonQuery();
                        }
                    }


                    foreach (var compare in record.Compares)
                    {
                        sql = $"INSERT INTO L_FieldState (CompareId, HistoryId) VALUES (\"{compare.Id}\", \"{id}\")";
                        using (SqliteCommand cmd2 = new SqliteCommand(sql, conn, transaction))
                        {
                            cmd2.ExecuteNonQuery();
                        }
                    }
                }

                transaction.Commit();
                conn.Close();
            }
        }

        public static long GetComparisonId(CompareTo compare)
        {
            long id = -4;

            using (SqliteConnection conn = ConnectToDatabase())
            {
                conn.Open();
                string sql = $"SELECT rowid FROM L_CompareTo WHERE " +
                    $"Location = \"{compare.Location}\" AND Compare = \"{compare.Compare}\" AND Value = \"{compare.Value}\"";
                using (SqliteCommand cmd = new SqliteCommand(sql, conn))
                {
                    using (SqliteDataReader rdr = cmd.ExecuteReader())
                        while (rdr.Read())
                        {
                            id = rdr.GetInt64(0);
                        }
                }
                conn.Close();
            }

            if (id == -4)
                return NewComparisonId(compare);
            return id;
        }

        private static long NewComparisonId(CompareTo compare)
        {
            long id = 0;
            using (SqliteConnection conn = ConnectToDatabase())
            {
                conn.Open();

                SqliteTransaction transaction = conn.BeginTransaction();

                string sql = $"INSERT INTO L_CompareTo (Location, Compare, Value) VALUES (\"{compare.Location}\", \"{compare.Compare}\", \"{compare.Value}\")";
                using (SqliteCommand cmd2 = new SqliteCommand(sql, conn, transaction))
                {
                    cmd2.ExecuteNonQuery();
                }

                sql = "select last_insert_rowid()";
                using (SqliteCommand cmd2 = new SqliteCommand(sql, conn, transaction))
                {
                    id = (long)cmd2.ExecuteScalar();
                }

                transaction.Commit();
                conn.Close();
            }

            return id;
        }

        public static long GetActionId(ActionInfo action)
        {
            long id = -4;

            using (SqliteConnection conn = ConnectToDatabase())
            {
                conn.Open();
                string sql = $"SELECT rowid FROM L_ActionList WHERE " +
                    $"Name = \"{action.Name}\" AND Action = \"{action.Action}\"";
                using (SqliteCommand cmd = new SqliteCommand(sql, conn))
                {
                    using (SqliteDataReader rdr = cmd.ExecuteReader())
                        while (rdr.Read())
                        {
                            id = rdr.GetInt64(0);
                        }
                }
                conn.Close();
            }

            if (id == -4)
                return NewActionId(action);
            return id;
        }

        private static long NewActionId(ActionInfo action)
        {
            long id = 0;
            using (SqliteConnection conn = ConnectToDatabase())
            {
                conn.Open();

                SqliteTransaction transaction = conn.BeginTransaction();

                string sql = $"INSERT INTO L_ActionList (Name, Action) VALUES (\"{action.Name}\", \"{action.Action}\")";
                using (SqliteCommand cmd2 = new SqliteCommand(sql, conn, transaction))
                {
                    cmd2.ExecuteNonQuery();
                }

                sql = "select last_insert_rowid()";
                using (SqliteCommand cmd2 = new SqliteCommand(sql, conn, transaction))
                {
                    id = (long)cmd2.ExecuteScalar();
                }

                transaction.Commit();
                conn.Close();
            }

            return id;
        }

        public static double GetActionWeight(long actionId, long compareId)
        {
            double weight = 0;

            using (SqliteConnection conn = ConnectToDatabase())
            {
                conn.Open();
                string sql = $"SELECT Weight FROM L_Weights WHERE " +
                    $"ActionId = \"{actionId}\" AND CompareId = \"{compareId}\"";
                using (SqliteCommand cmd = new SqliteCommand(sql, conn))
                {
                    using (SqliteDataReader rdr = cmd.ExecuteReader())
                        while (rdr.Read())
                        {
                            weight = rdr.GetDouble(0);
                        }
                }
                conn.Close();
            }

            return weight;
        }

#endregion
    }
}
