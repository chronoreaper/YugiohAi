using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WindBot.Game;
using WindBot.Game.AI;
using WindBot.Game.AI.Decks.Util;

namespace WindBot
{
    public class PlayHistory
    {
        public List<History> Records = new List<History>();
        public List<History> CurrentTurn = new List<History>();

        public class History
        {
            public GameInfo Info;

            public List<ActionInfo> ActionInfo = new List<ActionInfo>();
            public List<CompareTo> Compares = new List<CompareTo>();

            public int CurP1Hand = 0;
            public int CurP1Field = 0;
            public int CurP2Hand = 0;
            public int CurP2Field = 0;

            public int PostP1Hand = -1;
            public int PostP1Field = -1;
            public int PostP2Hand = -1;
            public int PostP2Field = -1;
        }

        public class GameInfo
        {
            public int Game = 0;
            public int Turn = 0;
            public int ActionNumber = 0;
        }

        public class ActionInfo
        {
            public string Name = "";
            public ExecutorType Action = ExecutorType.Activate;
            public long ActionId = 0;
            public bool Performed = false;
            public ClientCard Card = null;

            public double GetWeight(List<CompareTo> state)
            {
                double weight = 0;
                foreach (var compare in state)
                {
                    var compareId = compare.Id;
                    //Console.WriteLine("     " + weight + ": " + compare.ToString());
                    weight += SQLComm.GetActionWeight(ActionId, compareId);
                }

                return weight;
            }


            public override string ToString()
            {
                return "[" + ActionId + "]" + Name + " " + Action.ToString();
            }
        }

        public class CompareTo
        {
            public long Id = 0;
            public string Location = "";
            public string Compare = "";
            public string Value = "";

            public override string ToString()
            {
                return Location + " " + Compare + " " + Value;
            }
        }

        public GameInfo GenerateGameInfo(int game, int turn, int actionNumber)
        {
            return new GameInfo(){
                Game = game,
                Turn = turn,
                ActionNumber = actionNumber};
        }

        public CompareTo GenerateComparasion(string location, string compare, string value)
        {
            var c = new CompareTo() { Location = location, Compare = compare, Value = value };
            c.Id = SQLComm.GetComparisonId(c);
            return c;
        }

        public ActionInfo GenerateActionInfo(string name, ExecutorType action, ClientCard card)
        {
            ActionInfo actionInfo = new ActionInfo() { Name = name, Action = action, Card = card };
            actionInfo.ActionId = SQLComm.GetActionId(actionInfo);

            return actionInfo;
        }

        public History GenerateHistory(GameInfo info, List<CompareTo> compare, List<ActionInfo> actions)
        {
            return new History() { Info = info, ActionInfo = actions, Compares = compare};
        }


        public void AddHistory(History history, Duel duel)
        {
            history.CurP1Field = duel.Fields[0].GetFieldCount();
            history.CurP1Hand = duel.Fields[0].GetHandCount();
            history.CurP2Field = duel.Fields[1].GetFieldCount();
            history.CurP2Hand = duel.Fields[1].GetHandCount();

            Records.Add(history);
            CurrentTurn.Add(history);
        }

        public void SaveHistory(int result)
        {
            SQLComm.SavePlayHistory(Records, result);
        }

        public void EndOfTurn(Duel duel)
        {
            foreach(var info in CurrentTurn)
            {
                info.PostP1Field = duel.Fields[0].GetFieldCount();
                info.PostP1Hand = duel.Fields[0].GetHandCount();
                info.PostP2Field = duel.Fields[1].GetFieldCount();
                info.PostP2Hand = duel.Fields[1].GetHandCount();
            }
            
            CurrentTurn.Clear();
        }
    }
}

