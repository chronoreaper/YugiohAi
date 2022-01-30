using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WindBot
{
    public class TreeActivation
    {
        public const double THRESHOLD = 99;

        public class Node
        {
            public Node children;

            public string id;
            public string action;
            public double? weight;
            public double? originalWeight { get; private set; }
            public int turn;
            public int actionId;
            public double games;
            public bool isFirst;
            public Node parent;

            public Node(string action, double? weight, int actionId, double games)
            {
                this.action = action;
                this.weight = weight;
                this.actionId = actionId;
                this.games = games;
            }

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
                    if (node.weight != result)
                        Console.WriteLine("Expected " + node.weight + " But Got " + result);
                    node.weight = result;
                }

                while (node.parent != null)
                {
                    node = node.parent;
                    //if (node.weight <= result)
                        node.weight = result;
                    //else
                    //    break;
                }
            }
            
            
        }

        public bool ShouldSave()
        {
            //if (!SqlComm.IsTraining)
                return true;
            // Only update if the last turn has no new values
            if (TurnActions.Keys.Count > 0)
            {
                int lastTurn = TurnActions.Keys.Max();
                Node check = TurnActions[lastTurn];
                while (check != null)
                {
                    if (check.originalWeight == null)
                        return false;
                    check = check.children;
                }
            }

            return true;
        }

        public bool ShouldPlayCard(int turn)
        {
            //if (!SqlComm.IsTraining)
                return true;
            // Only update if the last turn has no new values
            if (TurnActions.Keys.Contains(turn))
            {
                Node check = TurnActions[turn];
                while (check != null)
                {
                    if (check.weight >= THRESHOLD)
                        return false;
                    check = check.children;
                }
            }

            return true;
        }

        private string GetPrevAction(int turn)
        {
            string prevAction = "";
            Node node = null;
            if (TurnActions.ContainsKey(turn))
                node = TurnActions[turn];

            while (node != null)
            {
                if (node.weight == null)
                    return null;

                prevAction += node.actionId + ",";
                node = node.children;
            }

            return prevAction;
        }

        /**
         * Returns the next potential action to take and the weight of the result
         */
        public Node GetNextPotentialAction(int turn, bool isFirst, string actionsTaken = "")
        {
            Node result = null;
            string prevAction = GetPrevAction(turn);
            if (prevAction == null)
                return result;
            List<Node> nextAct = SqlComm.GetNextTreeNodes(turn, prevAction + actionsTaken, isFirst);

            if (nextAct.Count == 0)
                return result;

            for (int i = 0; i < nextAct.Count; i += 1)
            {
                Node cur = nextAct[i];
                Node potential = GetNextPotentialAction(turn, isFirst, actionsTaken + cur.actionId.ToString() + ",");


                if (potential != null)
                {
                    cur.action = potential.action;
                    cur.weight = potential.weight;
                    cur.games = potential.games;
                }

                if (!cur.action.Contains("GoTo"))
                {
                    if (result == null)
                        result = cur;
                    else if (result.weight < cur.weight)
                    {
                        result = cur;
                    }
                }

            }

            return result;
        }

        public List<double> GetTreeNode(int turn, int actionId, string id, string action, bool isFirst)
        {
            string prevAction = GetPrevAction(turn);
            if (prevAction == null)
                return new List<double>() { -1, -1 };
            //To change
            return SqlComm.GetTreeNode(turn, actionId, id, action, prevAction, isFirst);
        }

        public void SaveTreeNode(int turn, int actionId, string id, string action, double? weight, bool isFirst)
        {
            Node node = new Node(id, action, weight, turn, actionId, isFirst);

            if (!ShouldSave())
                return;

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
