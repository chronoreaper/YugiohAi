using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WindBot.Game.AI.Decks.Util
{
    public class ActionWeight
    {
        public int ActivateDesc { get; private set; } = -1;
        public ClientCard Card { get; private set; } = null;
        public long Index { get; private set; } = 0;
        public double? Weight { get; private set; } = 0;//threshold
        public ExecutorType Action { get; private set; } = ExecutorType.GoToEndPhase;
        public int ActionId { get; private set; } = 0;

        public ActionWeight()
        {
        }

        public ActionWeight(double? weight, int actionId)
        {
            Weight = weight;
            ActionId = actionId;
        }

        public ActionWeight(int desc, ExecutorType action, ClientCard card, long index, double? weight, int actionId)
        {
            ActivateDesc = desc;
            Card = card;
            Index = index;
            Weight = weight;
            Action = action;
            ActionId = actionId;
        }
    }
}
