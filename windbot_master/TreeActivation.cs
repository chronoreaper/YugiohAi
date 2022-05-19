using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WindBot
{
    public class TreeActivation
    {
        public const double THRESHOLD = 3;

        public class Node
        {
            public List<Node> children;

            public string id;
            public string action;
            public double? weight;
            public double? originalWeight { get; private set; }
            public int turn;
            public int actionId;
            public double games;
            public bool isFirst;
            public bool activated;
            public Node parent;
            public string preActions;

            public Node(string action, double? weight, int actionId, double games, string preActions = "")
            {
                this.action = action;
                this.weight = weight;
                this.originalWeight = weight;
                this.actionId = actionId;
                this.games = games;
                this.activated = true;
                this.preActions = preActions;
            }

            public Node(string id, string action, double? weight, int turn, int actionId, bool isFirst, bool activated, Node parent = null)
            {
                children = new List<Node>();
                this.id = id;
                this.action = action;
                this.weight = weight;
                this.originalWeight = weight;
                this.turn = turn;
                this.actionId = actionId;
                this.isFirst = isFirst;
                this.parent = parent;
                this.activated = activated;
            }

            public string GetPrevActions()
            {
                string prevAction = "";
                var node = this.parent;

                while (node != null)
                {
                    prevAction = node.actionId + "," + prevAction;
                    node = node.parent;
                }

                return prevAction;
            }

            public int Depth()
            {
                int count = 0;
                var node = this.parent;

                while (node != null)
                {
                    count++;
                    node = node.parent;
                }
                return count;
            }

            public List<Node> GetNodePath()
            {
                Node node = this;
                List<Node> path = new List<Node>();
                path.Add(this);

                while(node.parent != null)
                {
                    node = node.parent;
                    path.Add(node);
                }

                return path;
            }

        }

        public Dictionary<int, List<Node>> TurnActions;

        public TreeActivation()
        {
            BuildTree();
        }

        protected void BuildTree()
        {
            TurnActions = new Dictionary<int, List<Node>>();
            // Connect to SQL Com
        }

        public void UpdateNode(int turn, double? result, bool all = false)
        {
            if (all)
            {
                foreach(var key in TurnActions.Keys)
                {
                    var node = TurnActions[key].FirstOrDefault(x => x.activated);
                    while (node != null)
                    {
                        node.weight = result;
                        var child = node.children.FirstOrDefault(x => x.activated);
                        node = child;
                    }
                }
            }
            else
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

                    node = node.parent;

                    while (node != null)
                    {
                        if (node.weight == null || node.weight <= result)
                            node.weight = result;
                        //else
                        //    break;
                        node = node.parent;
                    }
                }
            }
        }

        public bool ShouldSave(int turn)
        {
            //if (!SqlComm.IsTraining)
                return true;
            // Only update if the last turn has no new values
            if (TurnActions.Keys.Count > 0)
            {
                int lastTurn = TurnActions.Keys.Max();
                if (lastTurn != turn)
                {
                    Node check = TurnActions[lastTurn].FirstOrDefault(x => x.activated);
                    while (check != null)
                    {
                        if (check.originalWeight == null)
                            return false;
                        check = check.children.FirstOrDefault(x => x.activated);
                    }
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
                Node check = TurnActions[turn].FirstOrDefault(x => x.activated);
                while (check != null)
                {
                    if (check.weight >= THRESHOLD)
                        return false;
                    check = check.children.FirstOrDefault(x => x.activated);
                }
            }

            return true;
        }

        /**
         * Returns true if the base node of a is greater than b
         * If there is a tie, go up a child.
         * If you hit a before b, return true, and vice versa for false.
         * If there is a tie, return a.
         * Returns true if there is a null weight
         */
        private bool IsBaseGreater(Node a, Node b)
        {
            bool result = true;

            List<Node> a_path = a.GetNodePath();
            List<Node> b_path = b.GetNodePath();

            int i = 0;

            while (a_path.Count < i && b_path.Count < i)
            {
                if (a_path[i].weight == null || b_path[i].weight == null)
                {
                    result = true;
                    break;
                }
                else if (a_path[i].weight > b_path[i].weight)
                {
                    result = true;
                    break;
                }
                else if (a_path[i].weight < b_path[i].weight)
                {
                    result = false;
                    break;
                }

                i++;

                // If at end of path
                if (a_path.Count == i)
                {
                    result = true;
                    break;
                }
                else if (b_path.Count == i)
                {
                    result = false;
                    break;
                }
            }

            return result;
        }

        private Node GetPrevActionNode(int turn)
        {
            Node node = null;
            if (TurnActions.ContainsKey(turn))
                node = TurnActions[turn].FirstOrDefault(x => x.activated);
            return node;
        }

        private string GetPrevAction(int turn)
        {
            string prevAction = "";
            Node node = GetPrevActionNode(turn);

            while (node != null)
            {
                //if (node.weight == null)
                //    return null;

                prevAction += node.actionId + ",";
                node = node.children.FirstOrDefault(x => x.activated);
            }

            return prevAction;
        }

        /**
         * Returns the next potential action to take and the weight of the result
         */
        public Node GetNextPotentialAction(int turn, bool isFirst)
        {
            Node result = null;
            Queue<Node> actions = new Queue<Node>();
            List<string> visited = new List<string>();


            string previousActions = "";
            Node lastNode = GetLastNode(turn);
            if (lastNode != null)
            {
                previousActions = lastNode.GetPrevActions() + lastNode.actionId.ToString() + ",";
            }
            
            List<Node> potentialActions = SqlComm.GetBestTreeNodeActions(turn, previousActions, isFirst);

            if (potentialActions.Count == 0)
                return null;

            List<Node> nullActions = potentialActions.Where(x => x.weight == null).ToList();
            List<Node> nextActions = potentialActions.Where(x => x.preActions == previousActions).ToList();
            List<Node> nullNextActions = nextActions.Where(x => x.weight == null).ToList();
            double maxWeight = potentialActions[0].weight ?? 0;
            List<Node> bestActions = potentialActions.Where(x => x.weight == maxWeight).ToList();

            Random rnd = new Random();

            if ((rnd.NextDouble()) * THRESHOLD < maxWeight || nullNextActions.Count < 2)
            {
                if (bestActions.Count > 0)
                    result = bestActions[rnd.Next(bestActions.Count)];
            }
            else if (nextActions.Count > 0)
                result = nextActions[rnd.Next(nextActions.Count)];

            if (result != null)
            {
                string futureActions = result.preActions.Substring(previousActions.Length);

                if (futureActions.Contains(','))
                    result.actionId = int.Parse(futureActions.Split(',')[0]);
            }

            return result;
        }

        public List<double?> GetTreeNode(int turn, int actionId, string id, string action, bool isFirst)
        {
            string prevAction = GetPrevAction(turn);
            //if (prevAction == null)
            //    return new List<double>() { -1, -1 };
            //To change
            return SqlComm.GetTreeNode(turn, actionId, id, action, prevAction, isFirst);
        }

        public void SaveTreeNode(int turn, int actionId, string id, string action, double? weight, bool isFirst, bool activated, Node parent)
        {
            Node node = new Node(id, action, weight, turn, actionId, isFirst, activated, parent);

            if (!ShouldSave(turn))
                return;

            if (parent == null)
            {
                List<Node> startNode = null;
                if (TurnActions.ContainsKey(turn))
                {
                    startNode = TurnActions[turn];
                }
                else
                {
                    startNode = new List<Node>();
                    TurnActions.Add(turn, startNode);
                }
                startNode.Add(node);
            }
            else
            {
                node.parent = parent;
                parent.children.Add(node);
            }
        }

        public Node GetLastNode(int turn)
        {
            Node node = null;
            if (TurnActions.Keys.Contains(turn))
            {
                node = TurnActions[turn].FirstOrDefault(x => x.activated);
                while (node != null && node.children.Count > 0)
                {
                    var child = node.children.FirstOrDefault(x => x.activated);
                    if (child == null)
                        break;
                    node = child;
                }
            }
            return node;
        }
    }
}
