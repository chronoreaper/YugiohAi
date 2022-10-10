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

                SQLComm.GetNodeInfo(this);
            }
        }

        Node current;
        Node best;
        int TotalGames = 0;

        public MCST()
        {
            current = new Node(null, "", "");
            best = null;
            TotalGames = SQLComm.GetTotalGames();
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
            Node best = null;
            double weight = 0;
            double c = 0.5;

            foreach(Node n in current.Children)
            {
                double w = n.Rewards + c * Math.Sqrt(Math.Log(TotalGames) / n.Visited);
                if (w > weight)
                {
                    weight = w;
                    best = n;
                }
            }

            if (best != null)
                current = best;

            return best;
        }
    }
}

