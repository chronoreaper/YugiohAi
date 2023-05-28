using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WindBot.Game;
using WindBot.Game.AI;
using WindBot.Game.AI.Decks.Util;

namespace WindBot
{
    public class NEAT
    {
        public int id = 0;
        public int wins = 0;
        public int games = 0;
        public Dictionary<int, NEATNode> Nodes = new Dictionary<int, NEATNode>();
        public List<Connection> Connections = new List<Connection>();
        public Dictionary<string, NEATNode> Input = new Dictionary<string, NEATNode>();
        public Dictionary<string, NEATNode> Output = new Dictionary<string, NEATNode>();
        public Dictionary<long, InnovationNumber> Innovation = new Dictionary<long, InnovationNumber>();

        public List<NEATNode> PossibleActions = new List<NEATNode>();


        public enum NodeType
        {
            Input = 1,
            Output = -1,
            Connection = 0
        }

        public class NEATNode
        {
            public int Id = SQLComm.Id;
            public int Type = 0;// -1 output, 1 input, 0 connection
            public string Name = "";
            public double Bias = 0;
            public int Depth = 0;
            public List<Connection> OutEdges = new List<Connection>();
            public List<Connection> InEdges = new List<Connection>();

            public double CurrentWeight = 0;

            public bool Visited = false;
            public List<Connection> ActivatedConnections = new List<Connection>();

            public double ActivationFunction()
            {
                return CurrentWeight;
                if (Depth == 0)
                    return Math.Sign(CurrentWeight);
                return Math.Tanh(CurrentWeight);
            }

            public void IncrementActivationCount()
            {
                foreach(var con in ActivatedConnections)
                {
                    con.ActivationCount++;
                }
            }
        }

        public class InnovationNumber
        {
            public long Id;
            public int Input;
            public int Output;
        }

        public class Connection
        {
            public NEATNode Input;
            public NEATNode Output;
            public bool Enabled = true;
            public double Weight = 0;
            public long Id = 0;
            public int ActivationCount = 0;
        }

        public NEAT()
        {
            SQLComm.Setup(this);
        }

        public void ResetConnections()
        {
            foreach (var node in Nodes.Values)
            {
                node.CurrentWeight = 0;
                node.Visited = false;
            }
            PossibleActions.Clear();
        }

        public void AddContext(string context)
        {
            if (!Input.Keys.Contains(context))
            {
                NEATNode node = new NEATNode() { Name = context };
                Input.Add(context, node);
            }
            Input[context].CurrentWeight = 1;
        }

        public List<NEATNode> GetBestAction(Duel duel)
        {
            SetInputs(duel);
            List<NEATNode> result = new List<NEATNode>();
            Queue<NEATNode> outQueue = new Queue<NEATNode>();
            Queue<NEATNode> queue = new Queue<NEATNode>();
            List<NEATNode> visited = new List<NEATNode>();

            foreach (var node in PossibleActions)
                outQueue.Enqueue(node);
            // Find the order of which to update the nodes
            while (outQueue.Count > 0)
            {
                var cur = outQueue.Dequeue();
                visited.Add(cur);
                queue.Enqueue(cur);

                foreach(var edge in cur.InEdges)
                {
                    var pre = edge.Input;
                    if (!visited.Contains(pre))
                        outQueue.Enqueue(pre);
                }
            }

            var path = queue.Reverse().ToList();


            foreach(var node in path)
            {
                var cur = node;

                if (cur.Visited)
                    continue;

                cur.Visited = true;
                // check if activated

                if (cur.ActivationFunction() > 0)
                {
                    if (cur.Type == (int)NodeType.Output)
                    {
                        result.Add(cur);
                    }
                    else
                    {
                        foreach (var edge in cur.OutEdges)
                        {
                            var next = edge.Output;
                            if (!next.Visited)
                            {
                                //queue.Enqueue(next);
                                next.CurrentWeight += edge.Weight;
                                next.ActivatedConnections.Clear();
                                next.ActivatedConnections.AddRange(cur.ActivatedConnections);
                                next.ActivatedConnections.Add(edge);
                            }
                        }
                    }
                }
            }

            if (result.Count <= 0 && PossibleActions.Count > 0)
            {
                //result.Add(PossibleActions[Program.Rand.Next(PossibleActions.Count)]);
                //result[0].CurrentWeight = 1;
            }

            ResetConnections();

            return result;
        }

        public void SetInputs(Duel duel)
        {
            AddNode("Default", true, true);
            foreach (var card in duel.Fields[1].GetMonsters())
            {
                string name = (card.Name ?? "Facedown");
                name += ";EnemyMonster";
                AddNode(name, true, true);
            }
        }

        public void AddNode(string name, bool isInput, bool activate = false)
        {
            int id = SQLComm.GetNodeId(name, isInput ? 1 : -1);

            if (!Nodes.ContainsKey(id))
            {
                NEATNode node = new NEATNode() { Id = id, Name = name, Type = (isInput ? 1 : -1) };

                if (isInput)
                    Input.Add(name, node);
                else
                    Output.Add(name, node);
                Nodes.Add(id, node);
            }
            if (activate)
                Nodes[id].CurrentWeight = 1;
            if (!isInput)
                PossibleActions.Add(Nodes[id]);
        }


        public void SaveNetwork(int win)
        {
            SQLComm.SaveNEAT(this, win);
        }

        public void AddConnection(long id, NEATNode input, NEATNode output, double weight)
        {
            var conn = new Connection() {Id = id, Input = input, Output = output, Weight = weight};
            Connections.Add(conn);
            input.OutEdges.Add(conn);
            output.InEdges.Add(conn);
        }

        public class PriorityQueue
        {
            private class Item
            {
                public NEATNode node;
                public int index;
            }
        }
    }
}

