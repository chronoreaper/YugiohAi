using Mono.Data.Sqlite;
using System;
using System.IO;
using static WindBot.MCST;

namespace WindBot
{
    public class SQLComm
    {
        public static bool IsFirst = false;
        public static bool IsTraining = true;
        public static bool IsRollout = false;
        public static bool ShouldBackPropagate = false;
        public static int TotalGames = 201;
        public static int RolloutCount = 2;

        public static int GamesPlayed = 0;
        public static int Wins = 0;
        private static SqliteConnection ConnectToDatabase()
        {
            //@"URI=file:\windbot_master\windbot_master\bin\Debug\cards.cdb";
            string dir = Directory.GetCurrentDirectory();
            //Go to the YugiohAi Directory
            dir = dir.Remove(dir.IndexOf(@"WindBot-Ignite-master\bin\Debug")) + "cardData.cdb";
            string absolutePath = $@"URI = file: {dir}";
            return new SqliteConnection(absolutePath);
        }

        public static void GetNodeInfo(Node node)
        {
            using(SqliteConnection conn = ConnectToDatabase())
            {
                conn.Open();
                string sql = $"SELECT rowid, Reward, Visited FROM MCST WHERE " +
                    $"ParentId = \"{node.Parent?.NodeId}\" AND CardId = \"{node.CardId}\" AND Action = \"{node.Action}\"";
                using (SqliteCommand cmd = new SqliteCommand(sql, conn))
                {
                    using (SqliteDataReader rdr = cmd.ExecuteReader())
                    while (rdr.Read())
                    {
                        node.NodeId = long.Parse(rdr["rowid"].ToString());
                        node.Rewards = rdr.GetDouble(1);
                        node.Visited = rdr.GetInt32(2);
                    }
                }
                conn.Close();
            }
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

        /***
         * Result, 0 = win, 1 = lose, 2 = tie
         */
        public static void Backpropagate(Node node, double reward)
        {
            IsRollout = false;
            if (!ShouldBackPropagate)
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

                while (node != null)
                {
                    if (node.NodeId != -4)
                    {
                        sql = $"UPDATE MCST SET Reward = Reward + {rewards / rolloutCount}, " +
                            $"Visited = Visited + 1 WHERE " +
                            $"rowid = \"{node.NodeId}\"";

                        using (SqliteCommand cmd2 = new SqliteCommand(sql, conn, transaction))
                        {
                            rowsUpdated = cmd2.ExecuteNonQuery();
                        }
                    }
                    else
                    {
                        long parentId = 0;
                        if (node.Parent != null)
                        {
                            parentId = node.Parent.NodeId;
                        }
                        sql = $"INSERT INTO MCST (ParentId,CardId,Action,Reward,Visited) VALUES (\"{parentId}\",\"{node.CardId}\",\"{node.Action}\",\"{rewards / rolloutCount}\",\"1\")";
                        using (SqliteCommand cmd2 = new SqliteCommand(sql, conn, transaction))
                        {
                            rowsUpdated = cmd2.ExecuteNonQuery();
                        }
                    }

                    node = node.Parent;
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
                    sql = $"INSERT INTO MCST (ParentId,CardId,Action,Reward,Visited) VALUES (\"-4\",\"Result\",\"{lable}\",\"{reward}\",\"1\")";
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
