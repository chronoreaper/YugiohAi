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

            public Node(Node parent, Node child, string cardId, string action, ClientField[] field)
                :this(parent, child, cardId, action)
            {
                StateCurrent = new MLUtil.GameState(field);
            }

            public Node(Node parent, string cardId, string action)
                :this(parent, null, cardId, action)
            {
            }

            public Node(Node parent, Node child, string cardId, string action)
            {
                Children = new List<Node>();
                Parent = parent;
                CardId = cardId;
                Action = action;
                StateCurrent = new MLUtil.GameState();

                if (child != null)
                {
                    Children.Add(child);
                }

                if (!SQLComm.GetNodeInfo(this))
                {
                    SQLComm.InsertNode(this);
                    SQLComm.GetNodeInfo(this);
                }
            }

            public double Heuristic()
            {
                double value = 0;
                if (StateCurrent != null)
                {
                    if (StateAfterTurn != null)
                    {
                        double cardAdvantageHand = StateAfterTurn.BotField.HandCount - StateCurrent.BotField.HandCount + StateCurrent.EnemyField.HandCount - StateAfterTurn.EnemyField.HandCount;
                        double cardAdvantageField = StateAfterTurn.BotField.FieldCount - StateCurrent.BotField.FieldCount + StateCurrent.EnemyField.FieldCount - StateAfterTurn.EnemyField.FieldCount;

                        double fieldAdvantage = StateAfterTurn.BotField.FieldCount - StateAfterTurn.EnemyField.FieldCount;
                        double enemyFieldLoss = StateAfterTurn.EnemyField.FieldCount - StateCurrent.EnemyField.FieldCount;
                        double playerFieldGain = StateAfterTurn.BotField.FieldCount - StateCurrent.BotField.FieldCount;
                        double advantageGain = cardAdvantageHand + cardAdvantageField;
                        value += Math.Sign(fieldAdvantage);
                        value += Math.Sign(enemyFieldLoss);
                        value += Math.Sign(playerFieldGain);
                        value += Math.Sign(advantageGain);

                        /*value += StateAfterTurn.BotField.HandCount - StateCurrent.BotField.HandCount;
                        value += StateAfterTurn.BotField.FieldCount - StateCurrent.BotField.FieldCount;
                        value -= StateAfterTurn.EnemyField.FieldCount - StateCurrent.EnemyField.FieldCount;*/

                    }

                    if (StateAfterOtherTurn != null && false)
                    {
                        double cardAdvantageHand = StateAfterTurn.BotField.HandCount - StateCurrent.BotField.HandCount + StateCurrent.EnemyField.HandCount - StateAfterTurn.EnemyField.HandCount;
                        double cardAdvantageField = StateAfterTurn.BotField.FieldCount - StateCurrent.BotField.FieldCount + StateCurrent.EnemyField.FieldCount - StateAfterTurn.EnemyField.FieldCount;

                        double advantageGain = cardAdvantageHand + cardAdvantageField;
                        double cardAdvantage = StateAfterTurn.BotField.FieldCount - StateAfterTurn.EnemyField.FieldCount + StateAfterTurn.BotField.HandCount - StateAfterTurn.EnemyField.HandCount;

                        value += StateAfterOtherTurn.BotField.HandCount - StateAfterOtherTurn.EnemyField.HandCount;
                        value += StateAfterOtherTurn.BotField.FieldCount - StateAfterOtherTurn.EnemyField.FieldCount;
                    }
                }

                //value /= Math.Max(Visited,1);

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

        Dictionary<int, Node> TurnNodes;
        Node current;
        Node lastNode = null;
        public List<Node> possibleActions;
        int TotalGames = 0;
        int actionNumber = 0;
        Node nextActionNode = null;

        public MCST()
        {
            OnNewGame();
        }

        public void OnNewGame()
        {
            TurnNodes = new Dictionary<int, Node>();
            possibleActions = new List<Node>();
            TotalGames = SQLComm.GetTotalGames();
            current = new Node(null, "", "");
            current.NodeId = 0;
            lastNode = current;
        }

        public void OnNewAction(ClientField[] fields)
        {

        }

        public void OnNewTurn(ClientField[] fields, int turn)
        {
            actionNumber = 0;
            Node cur = current;
            Node pre = null;

            if (TurnNodes.ContainsKey(turn - 2))
            {
                pre = TurnNodes[turn - 2];
            }

            if (TurnNodes.ContainsKey(turn - 1))
            {
                cur = TurnNodes[turn - 1];
            }

            Node future = new Node(cur, $"turn:{turn}", "");
            current = future;
            TurnNodes.Add(turn, future);

            //Logger.WriteLine("Prev Turn actions");
            Queue<Node> q = new Queue<Node>();
            if (pre != null)
                q.Enqueue(pre);

            while (q.Count > 0)
            {
                Node n = q.Dequeue();
                n.StateAfterOtherTurn = new MLUtil.GameState(fields);
                //Logger.WriteLine(n.ToString());

                foreach(Node child in n.Children)
                {
                    q.Enqueue(child);
                }
            }

            //Logger.WriteLine("Cur Turn actions");
            if (cur != null)
                q.Enqueue(cur);

            while (q.Count > 0)
            {
                Node n = q.Dequeue();
                n.StateAfterTurn = new MLUtil.GameState(fields);
                //Logger.WriteLine(n.ToString());

                foreach (Node child in n.Children)
                {
                    q.Enqueue(child);
                }
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

        public void AddPossibleAction(string cardId, string action, ClientField[] field, int turn)
        {
            if (nextActionNode == null)
            {
                nextActionNode = new Node(null, $"Turn{turn}Action{actionNumber}", "", field);
            }

            Node node = new Node(current, nextActionNode, cardId, action, field);
            possibleActions.Add(node);
        }

        public bool ShouldActivate(string cardId, string action, ClientField[] field)
        {
            Node toActivate = new Node(current, cardId, action, field);
            possibleActions.Add(toActivate);
            possibleActions.Add(new Node(current, cardId, "Dont"+action, field));
            Node best = GetNextAction();
            return best == toActivate;
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
                foreach (Node n in possibleActions)
                {
                    double visited = Math.Max(0.0001, n.Visited);
                    double estimate = SQLComm.GetNodeEstimate(n);
                    double w = n.Rewards + c * Math.Sqrt((Math.Log(TotalGames + 1) + 1) / visited);
                    w += estimate;

                    if (w >= weight)
                    {
                        weight = w;
                        best = n;
                    }
                }

                if (best != null && possibleActions.Count > 1 && best != current)
                {
                    current.Children.Add(best);
                    if (best.Visited <= 0)
                    {
                        lastNode = best;
                        SQLComm.IsRollout = SQLComm.IsTraining;
                    }
                }

            }
            else if (possibleActions.Count > 0)
            {
                if (SQLComm.IsTraining)
                    best = possibleActions[Program.Rand.Next(0, possibleActions.Count)];
                else
                    best = possibleActions[0];
            }

            current = best.Children[0];

            possibleActions.Clear();
            nextActionNode = null;
            actionNumber++;

            return best;
        }

        private void Backpropagate(double reward)
        {
            SQLComm.Backpropagate(TurnNodes, lastNode, reward);
        }
    }
}

