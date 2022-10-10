using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Mono.Data.Sqlite;
using static WindBot.MCST;

namespace WindBot
{
    public class SQLComm
    {
        public static bool IsTraining = true;
        private static SqliteConnection ConnectToDatabase()
        {
            //@"URI=file:\windbot_master\windbot_master\bin\Debug\cards.cdb";
            string dir = Directory.GetCurrentDirectory();
            //Go to the YugiohAi Directory
            dir = dir.Remove(dir.IndexOf(@"windbot_master\bin\Debug")) + "cardData.cdb";
            string absolutePath = $@"URI = file: {dir}";
            return new SqliteConnection(absolutePath);
        }

        public static void GetNodeInfo(Node node)
        {
            using(SqliteConnection conn = ConnectToDatabase())
            {
                conn.Open();
                string sql = $"SELECT NodeId, Reward, Visited FROM MCST WHERE " +
                    $"ParentId = {node.Parent.NodeId} AND CardId = {node.CardId} and Action = {node.Action}";
                using (SqliteCommand cmd = new SqliteCommand(sql, conn))
                {
                    using (SqliteDataReader rdr = cmd.ExecuteReader())
                    while (rdr.Read())
                    {
                        node.NodeId = rdr.GetInt64(0);
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
                string sql = $"SELECT SUM(Visited) FROM MCST";
                using (SqliteCommand cmd = new SqliteCommand(sql, conn))
                {
                    using (SqliteDataReader rdr = cmd.ExecuteReader())
                        while (rdr.Read())
                        {
                            total = rdr.GetInt32(0);
                        }
                }
                conn.Close();
            }

            return total;
        }
    }
}
