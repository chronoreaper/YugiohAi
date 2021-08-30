using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using YGOSharp.OCGWrapper.Enums;

namespace WindBot.Game.AI.Decks.Util
{
    class Choice
    {
        public ActionWeight BestAction = null;
        // List of not activated actions
        private List<ActionWeight> NotActivated;
        public RandomExecutorBase Executor { get; private set; }
        public DuelPhase Phase { get; private set; }
        public int Turn { get; set; }

        public Choice(RandomExecutorBase executor, DuelPhase phase, int turn)
        {
            Executor = executor;
            Phase = phase;
            Turn = turn;
            NotActivated = new List<ActionWeight>();
            if (phase == DuelPhase.Main1 || phase == DuelPhase.Main2)
                BestAction = new ActionWeight();
        }

        public void SetBest(ExecutorType action, ClientCard card, int index = -1, int desc = -1)
        {
            Executor.SetCard((ExecutorType)(int)action, card, index);
            string phase = Phase.ToString();
            if (Phase == DuelPhase.Main2)
                phase = DuelPhase.Main1.ToString();
            string actionString = Executor.BuildActionString(action, card, phase);
            double weight = Executor.ActionWeight(actionString);

            if (action == ExecutorType.Repos)
                weight = Executor.ShouldRepos() ? 1 : -1;

            if (weight >= BestAction.Weight)
            {
                // update to be the better one
                NotActivated.Add(BestAction);
                BestAction = new ActionWeight(desc, action, card, index, weight);
                Logger.WriteLine($"     Setting Best Action as {action} {card?.Name} {weight}");
            }
            else //if (weight < 0)
            {
                NotActivated.Add(new ActionWeight(desc, action, card, index, weight));
                Logger.WriteLine($"     Did not {action} {card?.Name} {weight} as the better choice is {BestAction.Action} {BestAction.Card?.Name} {BestAction.Weight}");
            }
        }

        public void RecordAction(ExecutorType action, ClientCard card, int desc, double weight)
        {
            if (action != ExecutorType.GoToBattlePhase && action != ExecutorType.GoToEndPhase
                && action != ExecutorType.Repos)
            {
                string phase = Phase.ToString();
                if (Phase == DuelPhase.Main2)
                    phase = DuelPhase.Main2.ToString();
                string actionString = Executor.BuildActionString(action, card, phase);
                string value = (desc == -1) ? "" : desc.ToString();

                Executor.SetCard((ExecutorType)(int)action, card, -1);
                Executor.RecordAction(actionString, value, weight);
            }
        }

        public ActionWeight ReturnBestAction()
        {
            int ActivateDesc = BestAction.ActivateDesc;
            ExecutorType Action = BestAction.Action;
            ClientCard Card = BestAction.Card;
            int Index = BestAction.Index;
            double Weight = BestAction.Weight;

            if (Action != ExecutorType.GoToEndPhase)
                RecordAction(Action, Card, ActivateDesc, Weight);
            else
            {
                // Record only the bad choices
                foreach (ActionWeight actionWeight in NotActivated)
                {
                    string n = actionWeight.Card?.Name;
                    if (actionWeight.Weight < 0)
                    {
                        RecordAction(actionWeight.Action, actionWeight.Card, actionWeight.ActivateDesc, actionWeight.Weight);
                    }
                    else
                    {
                        //RecordAction(actionWeight.Action, actionWeight.Card, actionWeight.ActivateDesc, -actionWeight.Weight/2);
                    }
                }
            }
            NotActivated.Clear();

            return BestAction;
        }
    }
}
