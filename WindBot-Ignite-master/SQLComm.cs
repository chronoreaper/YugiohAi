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
        public static bool IsFirst = true;
        public static bool IsTraining = true;
        public static bool IsRollout = false;
        public static bool ShouldBackPropagate = false;
        public static int TotalGames = 201;
        public static int RolloutCount = 1;

        public static int GamesPlayed = 0;
        public static int Wins = 0;
        private static SqliteConnection ConnectToDatabase()
        {
            //@"URI=file:\windbot_master\windbot_master\bin\Debug\cards.cdb";
            string dir = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
            //Go to the YugiohAi Directory
            dir = dir.Remove(dir.IndexOf(@"WindBot-Ignite-master\bin\Debug")) + "cardData.cdb";
            string absolutePath = $@"URI = file: {dir}";
            return new SqliteConnection(absolutePath);
        }

        public static bool GetNodeInfo(Node node)
        {
            bool gotInfo = false;

            using(SqliteConnection conn = ConnectToDatabase())
            {
                conn.Open();
                string sql = $"SELECT rowid, Reward, Visited FROM MCST WHERE " +
                    $"ParentId = \"{node.Parent?.NodeId ?? -4}\" AND CardId = \"{node.CardId}\" AND Action = \"{node.Action}\"";
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
                string sql = $"SELECT SUM(Reward), SUM(Visited) from MCST WHERE CardId = \"{node.CardId}\" AND Action = \"{node.Action}\"";
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

                value = reward / Math.Max(1,visited) * 1000;
            }


            return value;
        }

        public static int GetTotalGames()
        {
            int total = 0;
            using (SqliteConnection conn = ConnectToDatabase())
            {
                conn.Open();
                string sql = $"SELECT SUM(Visited) FROM MCST WHERE CardId != \"Result\" AND ParentId = 0";
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
            using (SqliteConnection conn = ConnectToDatabase())
            {
                conn.Open();

                SqliteTransaction transaction = conn.BeginTransaction();

                long parentId = -4;
                long childId = -4;
                if (node.Parent != null && node.Parent.NodeId != -4)
                {
                    parentId = node.Parent.NodeId;
                }
                if (node.Children.Count == 1)
                {
                    childId = node.Children[0].NodeId;
                }

                string sql = $"INSERT INTO MCST (ParentId,ChildId,CardId,Action,Reward,Visited) VALUES (\"{parentId}\",\"{childId}\",\"{node.CardId}\",\"{node.Action}\",\"0\",\"0\")";
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
            IsRollout = false;
            if (!ShouldBackPropagate && RolloutCount != 1)
            {
                RecordWin(reward);

                RecordWin(Math.Max(0, Math.Round(node.Heuristic() / RolloutCount) / 10), true);

                return;
            }
            
            using (SqliteConnection conn = ConnectToDatabase())
            {
                conn.Open();

                SqliteTransaction transaction = conn.BeginTransaction();
                double rewards = reward;
                int rolloutCount = 1;

                int rowsUpdated = 0;
                string sql = $"SELECT Reward, Visited FROM MCST WHERE CardId = \"Result\"";

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
                            rewards = r;
                            rolloutCount = c;
                        }
                    }
                }
                Queue<Node> q = new Queue<Node>();
                foreach (int turn in nodes.Keys)
                    q.Enqueue(nodes[turn]);

                while (q.Count > 0)
                {
                    Node n = q.Dequeue();
                    if (n.NodeId != -4)
                    {
                        sql = $"UPDATE MCST SET Reward = Reward + {rewards / rolloutCount}, " +
                            $"Visited = Visited + 1 WHERE " +
                            $"rowid = \"{n.NodeId}\"";

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

                        sql = $"INSERT INTO MCST (ParentId,ChildId,CardId,Action,Reward,Visited) VALUES (\"{parentId}\",\"{childId}\",\"{n.CardId}\",\"{n.Action}\",\"{rewards / rolloutCount}\",\"1\")";
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
                sql = $"DELETE FROM MCST WHERE CardId = \"Result\"";
                using (SqliteCommand cmd2 = new SqliteCommand(sql, conn, transaction))
                {
                    rowsUpdated = cmd2.ExecuteNonQuery();
                }

                transaction.Commit();
                conn.Close();
            }

            ShouldBackPropagate = false;
            IsRollout = false;
        }

        private static void RecordWin(double reward, bool isHeurstic = false)
        {
            int rowsUpdated = 0;
            string lable = isHeurstic ? "Heurstic" : "Result";

            using (SqliteConnection conn = ConnectToDatabase())
            {
                conn.Open();

                SqliteTransaction transaction = conn.BeginTransaction();

                string sql = $"UPDATE MCST SET Reward = Reward + {reward}, " +
                           $"Visited = Visited + 1 WHERE " +
                           $"CardId = \"Result\" AND Action = \"{lable}\"";

                using (SqliteCommand cmd2 = new SqliteCommand(sql, conn, transaction))
                {
                    rowsUpdated = cmd2.ExecuteNonQuery();
                }

                // If there was no update, add it instead
                if (rowsUpdated <= 0)
                {
                    sql = $"INSERT INTO MCST (ParentId,ChildId,CardId,Action,Reward,Visited) VALUES (\"-4\",\"-4\",\"Result\",\"{lable}\",\"{reward}\",\"1\")";
                    using (SqliteCommand cmd2 = new SqliteCommand(sql, conn, transaction))
                    {
                        rowsUpdated = cmd2.ExecuteNonQuery();
                    }
                }

                transaction.Commit();
                conn.Close();
            }
        }
    }
}
