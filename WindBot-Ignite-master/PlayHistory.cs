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

        public class History
        {
            public GameInfo Info;

            public List<ActionInfo> ActionInfo = new List<ActionInfo>();
            public List<CompareTo> Compares = new List<CompareTo>();

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

        public ActionInfo GenerateActionInfo(string name, ExecutorType action)
        {
            ActionInfo actionInfo = new ActionInfo() { Name = name, Action = action };
            actionInfo.ActionId = SQLComm.GetActionId(actionInfo);

            return actionInfo;
        }

        public History GenerateHistory(GameInfo info, List<CompareTo> compare, List<ActionInfo> actions)
        {
            return new History() { Info = info, ActionInfo = actions, Compares = compare};
        }

        public void AddHistory(List<History> history)
        {
            Records.AddRange(history);
        }

        public void AddHistory(History history)
        {
            Records.Add(history);
        }

        public void SaveHistory()
        {
            SQLComm.SavePlayHistory(Records);
        }
    }
}

