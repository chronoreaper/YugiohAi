using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WindBot.Game.AI;

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
            public long NodeId = -1;

            public Node(Node parent, string cardId, string action)
            {
                Children = new List<Node>();
                Parent = parent;
                CardId = cardId;
                Action = action;

                if (Parent == null)
                    NodeId = 0;

                SQLComm.GetNodeInfo(this);
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

        /*
         * For Multiple Actions
         */

        public void AddPossibleAction(string cardId, string action)
        {
            current.Children.Add(new Node(current, cardId, action));
        }

        public bool ShouldActivate(string cardId, string action)
        {
            Node toActivate = new Node(current, cardId, action);
            current.Children.Add(toActivate);
            current.Children.Add(new Node(current, cardId, ""));
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
            double c = 0.5;

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
                        SQLComm.IsRollout = true;
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

        public void Backpropagate(int result)
        {
            SQLComm.Backpropagate(current, result);
        }
    }
}

