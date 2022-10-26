using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WindBot.Game;
using WindBot.Game.AI;
using WindBot.Game.AI.Decks.Util;

namespace WindBot
{
    public class MCST
    {
        public class Node
        {
            public List<Node> Children;
            public Node Parent;
            public double Rewards = 0;
            public int Visited = 0;
            public string CardId;
            public string Action;
            public long NodeId = -4;
            public MLUtil.GameState StateCurrent = null;
            public MLUtil.GameState StateAfterPlay = null;
            public MLUtil.GameState StateAfterTurn = null;
            public MLUtil.GameState StateAfterOtherTurn = null;

            public Node(Node parent, string cardId, string action, ClientField[] field)
                : this(parent, cardId, action)
            {
                StateCurrent = new MLUtil.GameState(field);
            }

            public Node(Node parent, string cardId, string action)
            {
                Children = new List<Node>();
                Parent = parent;
                CardId = cardId;
                Action = action;
                StateCurrent = new MLUtil.GameState();

                if (Parent == null)
                    NodeId = 0;

                SQLComm.GetNodeInfo(this);
            }

            public double Heuristic()
            {
                double value = 0;
                if (StateCurrent != null)
                {
                    if (StateAfterTurn != null)
                    {
                        value += StateAfterTurn.BotField.HandCount - StateCurrent.BotField.HandCount;
                        value += StateAfterTurn.BotField.FieldCount - StateAfterTurn.EnemyField.FieldCount;
                        value -= StateAfterTurn.EnemyField.FieldCount - StateCurrent.EnemyField.FieldCount;

                    }

                    if (StateAfterOtherTurn != null && false)
                    {
                        value += StateAfterOtherTurn.BotField.HandCount - StateAfterOtherTurn.EnemyField.HandCount;
                        value += StateAfterOtherTurn.BotField.FieldCount - StateAfterOtherTurn.EnemyField.FieldCount;
                    }
                }

                return value;
            }

            public override string ToString()
            {
                string s = "";
                s += NodeId.ToString() + " | ";
                s += CardId + ": " + Action + " | ";
                s += Heuristic();

                return s;
            }
        }

        Node current;
        int TotalGames = 0;

        public MCST()
        {
            OnNewGame();
        }

        public void OnNewGame()
        {
            TotalGames = SQLComm.GetTotalGames();
            current = new Node(null, "", "");
        }

        public void OnNewAction(ClientField[] fields)
        {

        }

        public void OnNewTurn(ClientField[] fields)
        {
            Node cur = current;

            while (cur != null && cur.StateAfterOtherTurn == null)
            {
                if (cur.StateAfterTurn != null)
                {
                    cur.StateAfterOtherTurn = new MLUtil.GameState(fields);

                    Logger.WriteLine(cur.ToString());
                }

                cur = cur.Parent;
            }

            cur = current;

            while (cur != null && cur.StateAfterTurn == null)
            {
                cur.StateAfterTurn = new MLUtil.GameState(fields);

                Logger.WriteLine(cur.ToString());

                cur = cur.Parent;
            }
        }

        public void OnGameEnd(int result, Duel duel)
        {
            //double reward = result == 0 ? (int)(SQLComm.RolloutCount/2) : (double)duel.Turn / 100;
            double reward = result == 0 ? 1 : 0;
            Backpropagate(reward);
        }


        /*
         * For Multiple Actions
         */

        public void AddPossibleAction(string cardId, string action, ClientField[] field)
        {
            current.Children.Add(new Node(current, cardId, action, field));
        }

        public bool ShouldActivate(string cardId, string action, ClientField[] field)
        {
            Node toActivate = new Node(current, cardId, action, field);
            current.Children.Add(toActivate);
            current.Children.Add(new Node(current, cardId, "", field));
            current = GetNextAction();
            return current == toActivate;
        }

        /**
         * Called after setting all possible actions
         */
        public Node GetNextAction()
        {
            Node best = current;
            double weight = -1;
            double c = 1;

            if (!SQLComm.IsRollout)
            {
                foreach (Node n in current.Children)
                {
                    double visited = Math.Max(0.0001, n.Visited);
                    double w = n.Rewards + c * Math.Sqrt((Math.Log(TotalGames + 1) + 1) / visited);
                    if (w >= weight)
                    {
                        weight = w;
                        best = n;
                    }
                }

                if (best != null)
                {
                    current = best;
                    if (best.Visited <= 0)
                    {
                        SQLComm.IsRollout = true;
                    }
                }
                current.Children.Clear();
            }
            else if (current.Children.Count > 0)
            {
                best = current.Children[Program.Rand.Next(0, current.Children.Count)];
                current.Children.Clear();
            }

            return best;
        }

        private void Backpropagate(double reward)
        {
            SQLComm.Backpropagate(current, reward);
        }
    }
}

