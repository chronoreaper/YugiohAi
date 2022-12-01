using Mono.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using static System.Net.Mime.MediaTypeNames;
using static WindBot.MCST;

namespace WindBot
{
    public class SQLComm
    {
        public static bool HasParameters = false;
        public static bool IsFirst = true;
        public static bool IsTraining = true;
        public static bool IsMCTS = true;
        public static bool IsRollout = false;
        public static bool ShouldBackPropagate = false;
        public static int TotalGames = 201;
        public static int RolloutCount = 1;

        public static int GamesPlayed = 0;
        public static int Wins = 0;

        public static string Name = "Bot";

        public static int PastWinsLimit = 10;
        public static int PastXWins = 0;
        public static Queue<int> PreviousWins = new Queue<int>();
        public static int WinsThreshold = 45;
        public static string sqlPath = $@"Data Source=./cardData.cdb";

        private static SqliteConnection ConnectToDatabase()
        {
            return new SqliteConnection(sqlPath);
        }

        public static bool GetNodeInfo(Node node)
        {
            bool gotInfo = false;

            using(SqliteConnection conn = ConnectToDatabase())
            {
                conn.Open();
                string sql = $"SELECT rowid, Reward, Visited FROM MCST WHERE " +
                    $"ParentId = \"{node.Parent?.NodeId ?? -4}\" AND CardId = \"{node.CardId}\" AND Action = \"{node.Action}\" AND IsFirst = \"{IsFirst}\" AND IsTraining = \"{IsTraining}\"";
                using (SqliteCommand cmd = new SqliteCommand(sql, conn))
                {
                    using (SqliteDataReader rdr = cmd.ExecuteReader())
                    while (rdr.Read())
                    {
                        node.NodeId = long.Parse(rdr["rowid"].ToString());
                        node.Rewards = rdr.GetDouble(1);
                        node.Visited = rdr.GetInt32(2);
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
                string sql = $"SELECT SUM(Reward), COUNT(Visited) from MCST WHERE CardId = \"{node.CardId}\" AND Action = \"{node.Action}\" AND IsFirst = \"{IsFirst}\" AND Visited > 0" +
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
                string sql = $"SELECT SUM(Visited) FROM MCST WHERE CardId != \"Result\" AND ParentId = 0 AND IsFirst = \"{IsFirst}\" AND IsTraining = \"{IsTraining}\"";
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

        public static void InsertNode(Node node)
        {
            if (!IsTraining)
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

                string sql = $"INSERT INTO MCST (ParentId,ChildId,CardId,Action,Reward,Visited,IsFirst,IsTraining) VALUES (\"{parentId}\",\"{childId}\",\"{node.CardId}\",\"{node.Action}\",\"0\",\"0\",\"{IsFirst}\",\"{IsTraining}\")";
                using (SqliteCommand cmd2 = new SqliteCommand(sql, conn, transaction))
                {
                    cmd2.ExecuteNonQuery();
                }

                transaction.Commit();
                conn.Close();
            }
        }

        /***
         * Result, 0 = win, 1 = lose, 2 = tie
         */
        public static void Backpropagate(Dictionary<int, Node> nodes, Node node, double reward)
        {
            if (!IsTraining)
                return;

            IsRollout = false;
            if (!ShouldBackPropagate && RolloutCount > 1)
            {
                RecordWin(reward);

                RecordWin(Math.Max(0, Math.Round(node.Heuristic() / RolloutCount) / 10), true);

                return;
            }

            double totalRewards = reward;
            int rolloutCount = 1;

            using (SqliteConnection conn = ConnectToDatabase())
            {
                conn.Open();

                SqliteTransaction transaction = conn.BeginTransaction();


                int rowsUpdated = 0;
                string sql = $"SELECT Reward, Visited FROM MCST WHERE CardId = \"Result\" AND IsFirst = \"{IsFirst}\" AND IsTraining = \"{IsTraining}\"";

                using (SqliteCommand cmd = new SqliteCommand(sql, conn))
                {
                    using (SqliteDataReader rdr = cmd.ExecuteReader())
                    {
                        double r = 0;
                        int c = 0;
                        while (rdr.Read())
                        {
                            r += rdr.GetDouble(0);
                            c = rdr.GetInt32(1);
                        }

                        if (c != 0)
                        {
                            totalRewards = r;
                            rolloutCount = c;
                        }
                    }
                }
                Queue<Node> q = new Queue<Node>();
                //foreach (int turn in nodes.Keys)
                //    q.Enqueue(nodes[turn]);
                q.Enqueue(nodes[1]);

                while (q.Count > 0)
                {
                    Node n = q.Dequeue();
                    if (n.NodeId != -4)
                    {
                        sql = $"UPDATE MCST SET Reward = Reward + {totalRewards / rolloutCount + Math.Max(0, Math.Round(n.Heuristic() / Math.Max(1, n.Visited), 3)) }, " + //+ Math.Max(0, Math.Round(node.Heuristic() / RolloutCount) / 10)
                            $"Visited = Visited + 1 WHERE " +
                            $"rowid = \"{n.NodeId}\" AND IsFirst = \"{IsFirst}\" AND IsTraining = \"{IsTraining}\"";

                        using (SqliteCommand cmd2 = new SqliteCommand(sql, conn, transaction))
                        {
                            rowsUpdated = cmd2.ExecuteNonQuery();
                        }
                    }
                    else
                    {
                        long parentId = 0;
                        long childId = -4;
                        if (n.Parent != null && n.Parent.NodeId != -4)
                        {
                            parentId = n.Parent.NodeId;
                        }
                        if (n.Children.Count == 1)
                        {
                            if (n.Children[0].NodeId == -4)
                                continue;
                            childId = n.Children[0].NodeId;
                        }

                        sql = $"INSERT INTO MCST (ParentId,ChildId,CardId,Action,Reward,Visited,IsFirst,IsTraining) VALUES (\"{parentId}\",\"{childId}\",\"{n.CardId}\",\"{n.Action}\",\"{totalRewards / rolloutCount}\",\"1\",\"{IsFirst}\".\"{IsTraining}\")";
                        using (SqliteCommand cmd2 = new SqliteCommand(sql, conn, transaction))
                        {
                            rowsUpdated = cmd2.ExecuteNonQuery();
                        }
                    }

                    foreach(Node c in n.Children)
                    {
                        q.Enqueue(c);
                    }
                }

                // Remove the rollout
                sql = $"DELETE FROM MCST WHERE CardId = \"Result\" AND IsFirst = \"{IsFirst}\" AND IsTraining = \"{IsTraining}\"";
                using (SqliteCommand cmd2 = new SqliteCommand(sql, conn, transaction))
                {
                    rowsUpdated = cmd2.ExecuteNonQuery();
                }

                transaction.Commit();
                conn.Close();
            }

            if (false)
                UpdateWeightTree(nodes, totalRewards, rolloutCount);

            ShouldBackPropagate = false;
            IsRollout = false;
        }

        public static void Cleanup()
        {
            using (SqliteConnection conn = ConnectToDatabase())
            {
                conn.Open();

                SqliteTransaction transaction = conn.BeginTransaction();
                // Remove the rollout
                string sql = $"DELETE FROM MCST WHERE CardId = \"Result\" AND IsFirst = \"{IsFirst}\" AND IsTraining = \"{IsTraining}\"";
                using (SqliteCommand cmd2 = new SqliteCommand(sql, conn, transaction))
                {
                    cmd2.ExecuteNonQuery();
                }
                transaction.Commit();
                conn.Close();
            }
        }

        public static void Reset()
        {
            using (SqliteConnection conn = ConnectToDatabase())
            {
                conn.Open();

                SqliteTransaction transaction = conn.BeginTransaction();
                // Remove the rollout
                string sql = $"DELETE FROM MCST WHERE IsFirst = \"{IsFirst}\" AND IsTraining = \"False\"";
                using (SqliteCommand cmd2 = new SqliteCommand(sql, conn, transaction))
                {
                    cmd2.ExecuteNonQuery();
                }

                sql = $"UPDATE MCST SET IsTraining = \"False\" WHERE IsFirst = \"{IsFirst}\" AND IsTraining = \"True\"";
                using (SqliteCommand cmd2 = new SqliteCommand(sql, conn, transaction))
                {
                    var v = cmd2.ExecuteNonQuery();
                }

                transaction.Commit();
                conn.Close();
            }
        }

        private static void RecordWin(double reward, bool isHeurstic = false)
        {
            if (!IsTraining)
                return;

            int rowsUpdated = 0;
            string lable = isHeurstic ? "Heurstic" : "Result";

            using (SqliteConnection conn = ConnectToDatabase())
            {
                conn.Open();

                SqliteTransaction transaction = conn.BeginTransaction();

                string sql = $"UPDATE MCST SET Reward = Reward + {reward}, " +
                           $"Visited = Visited + 1 WHERE " +
                           $"CardId = \"Result\" AND Action = \"{lable}\" AND IsFirst = \"{IsFirst}\" AND IsTraining = \"{IsTraining}\"";

                using (SqliteCommand cmd2 = new SqliteCommand(sql, conn, transaction))
                {
                    rowsUpdated = cmd2.ExecuteNonQuery();
                }

                // If there was no update, add it instead
                if (rowsUpdated <= 0)
                {
                    sql = $"INSERT INTO MCST (ParentId,ChildId,CardId,Action,Reward,Visited,IsFirst) VALUES (\"-4\",\"-4\",\"Result\",\"{lable}\",\"{reward}\",\"1\", \"{IsFirst}\",\"{IsTraining}\")";
                    using (SqliteCommand cmd2 = new SqliteCommand(sql, conn, transaction))
                    {
                        rowsUpdated = cmd2.ExecuteNonQuery();
                    }
                }

                transaction.Commit();
                conn.Close();
            }
        }

        private static void UpdateWeightTree(Dictionary<int, Node> nodes, double reward, double visited)
        {
            if (!IsTraining)
                return;

            using (SqliteConnection conn = ConnectToDatabase())
            {
                conn.Open();

                SqliteTransaction transaction = conn.BeginTransaction();


                int rowsUpdated = 0;
                string sql;

                Queue<Node> q = new Queue<Node>();
                foreach (int turn in nodes.Keys)
                    q.Enqueue(nodes[turn]);

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
    }
}
