using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WindBot
{
    public class TreeActivation
    {
        public class Node
        {
            public Node children;

            public string id;
            public string action;
            public double? weight;
            public double? originalWeight { get; private set; }
            public int turn;
            public int actionId;
            public bool isFirst;
            public Node parent;

            public Node(string id, string action, double? weight, int turn, int actionId, bool isFirst, Node parent = null)
            {
                children = null;
                this.id = id;
                this.action = action;
                this.weight = weight;
                this.originalWeight = weight;
                this.turn = turn;
                this.actionId = actionId;
                this.isFirst = isFirst;
                this.parent = parent;
            }

            public int Count()
            {
                int count = 0;
                Node node = this;
                while(node.children != null)
                {
                    count++;
                    node = node.children;
                }
                return count;
            }
        }

        public Dictionary<int, Node> TurnActions;

        public TreeActivation()
        {
            BuildTree();
        }

        protected void BuildTree()
        {
            TurnActions = new Dictionary<int, Node>();
            // Connect to SQL Com
        }

        public void UpdateNode(int turn, double? result, bool all = false)
        {
            int preTurn = turn - 1;
            Node node = GetLastNode(preTurn);

            if (node != null)
            {
                if (node.weight == null)
                {
                    node.weight = result;
                }
                else
                {
                    if (node.weight == result)
                        Console.WriteLine("Expected " + node.weight + " But Got " + result);
                }

                while (node.parent != null)
                {
                    node = node.parent;
                    if (node.weight <= result)
                        node.weight = result;
                    else
                        break;
                }
            }
            
            
        }

        public void SaveTreeNode(int turn, int actionId, string id, string action, double? weight, bool isFirst)
        {
            Node node = new Node(id, action, weight, turn, actionId, isFirst);

            // Only update if the last turn has no new values
            if (TurnActions.Keys.Count > 0)
            {
                int lastTurn = TurnActions.Keys.Max();
                Node check = TurnActions[lastTurn];
                while(check != null)
                {
                    if (check.originalWeight == null)
                        return;
                    check = check.children;
                }
            }

            if (!TurnActions.Keys.Contains(turn))
                TurnActions.Add(turn, node);
            else
            {
                node.parent = GetLastNode(turn);
                GetLastNode(turn).children = node;
            }
        }

        public Node GetLastNode(int turn)
        {
            Node node = null;
            if (TurnActions.Keys.Contains(turn))
            {
                node = TurnActions[turn];
                while (node.children != null)
                {
                    node = node.children;
                }
            }
            return node;
        }
    }
}