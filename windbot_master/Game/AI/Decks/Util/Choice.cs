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
        private bool IsBestSet = false;
        private int actionCount = 0;

        public Choice(RandomExecutorBase executor, DuelPhase phase, int turn)
        {
            Executor = executor;
            Phase = phase;
            Turn = turn;
            NotActivated = new List<ActionWeight>();
            if (phase == DuelPhase.Main1 || phase == DuelPhase.Main2)
            {
                if (SqlComm.IsTraining)
                {
                    if (SqlComm.TreeActivation.ShouldPlayCard(Executor.Duel.Turn) && false)
                    {
                        TreeActivation.Node results = SqlComm.TreeActivation.GetNextPotentialAction(Executor.Duel.Turn, Executor.Duel.IsFirst);
                        if (results != null)
                            BestAction = new ActionWeight(results.weight, results.actionId);
                    }
                    else
                        BestAction = new ActionWeight();
                }
                else
                {
                    BestAction = new ActionWeight();
                }
            }
        }

        public void SetBest(ExecutorType action, ClientCard card, long index = -1, int desc = -1)
        {
            Executor.ActionId++;
            actionCount = Executor.ActionId;
            Executor.SetCard((ExecutorType)(int)action, card, index);
            string phase = Phase.ToString();
            if (Phase == DuelPhase.Main2)
                phase = DuelPhase.Main1.ToString();
            string actionString = Executor.BuildActionString(action, card, phase);

            if (!SqlComm.IsTraining)
            {
                double weight = Executor.ActionWeight(actionString);

                if (action == ExecutorType.Repos)
                    weight = Executor.ShouldRepos() ? 1 : -1;
                if (action == ExecutorType.GoToBattlePhase || action == ExecutorType.GoToEndPhase || action == ExecutorType.GoToMainPhase2)
                    weight = 0;

                if (weight >= BestAction.Weight)
                {
                    // update to be the better one
                    NotActivated.Add(BestAction);
                    BestAction = new ActionWeight(desc, action, card, index, weight, actionCount);
                    Console.WriteLine($"    Setting Best Action as {action} {card?.Name} {weight}");
                }
                else //if (weight < 0)
                {
                    NotActivated.Add(new ActionWeight(desc, action, card, index, weight, actionCount));
                    Console.WriteLine($"    Did not {action} {card?.Name} {weight} as the better choice is {BestAction.Action} {BestAction.Card?.Name} {BestAction.Weight}");
                }
            }
            else if (SqlComm.TreeActivation.ShouldPlayCard(Executor.Duel.Turn))
            {
                List<double> weights = SqlComm.TreeActivation.GetTreeNode(Executor.Duel.Turn, actionCount, card?.Name, actionString, Executor.Duel.IsFirst);
                //if (action == ExecutorType.GoToBattlePhase || action == ExecutorType.GoToEndPhase || action == ExecutorType.GoToMainPhase2)
                //    weights = new List<double>() { 0, 0 };
                Console.WriteLine(string.Format($"    {actionCount} {(weights.Count > 0 ? weights[0].ToString() : "null")} | {action} {card?.Name} | {index}"));
                // perform action if there is no weights to it or if it is the first action of the turn
                if (((weights.Count == 0) && action != ExecutorType.Repos))// || BestAction.ActionId == 0 )
                {
                    if (BestAction != null)
                        NotActivated.Add(BestAction);
                    IsBestSet = true;
                    double? weight = null;
                    if (weights.Count > 0)
                    {
                        weight = weights[0];
                        IsBestSet = false;
                    }
                    BestAction = new ActionWeight(desc, action, card, index, weight, actionCount);
                }
                else
                {
                    double weight = 0;
                    if (action == ExecutorType.Repos)  weight = Executor.ShouldRepos() ? 1 : -1;
                    if (weights.Count > 0)
                        weight = weights[0];

                    if (!IsBestSet && (BestAction == null || BestAction.Weight <= weight) || BestAction.ActionId == actionCount)
                    {
                        NotActivated.Add(BestAction);
                        BestAction = new ActionWeight(desc, action, card, index, weight, actionCount);
                    }
                    else
                        NotActivated.Add(new ActionWeight(desc, action, card, index, weight, actionCount));
                }      
            }
        }

        public void RecordAction(ExecutorType action, ClientCard card, int desc, double? weight)
        {
            if (action != ExecutorType.Repos)
            {
                string phase = Phase.ToString();
                if (Phase == DuelPhase.Main2)
                    phase = DuelPhase.Main1.ToString();
                string actionString = Executor.BuildActionString(action, card, phase);
                string value = (desc == -1) ? "" : desc.ToString();

                Executor.SetCard((ExecutorType)(int)action, card, -1);
                Executor.RecordAction(actionString, value, weight??0);
            }
        }

        public ActionWeight ReturnBestAction()
        {
            int ActivateDesc = BestAction.ActivateDesc;
            ExecutorType Action = BestAction.Action;
            ClientCard Card = BestAction.Card;
            long Index = BestAction.Index;
            double? Weight = BestAction.Weight;

            if (actionCount > 1)
            {
                RecordAction(Action, Card, ActivateDesc, Weight);
                if (SqlComm.IsTraining)
                {
                    if (Action != ExecutorType.Repos)
                    {
                        string phase = Phase.ToString();
                        if (Phase == DuelPhase.Main2)
                            phase = DuelPhase.Main1.ToString();
                        string actionString = Executor.BuildActionString(Action, Card, phase);

                        SqlComm.TreeActivation.SaveTreeNode(Executor.Duel.Turn, BestAction.ActionId, Card?.Name, actionString, Weight, Executor.Duel.IsFirst);
                    }
                }
            }

            foreach (var notPlayed in NotActivated)
            {
                RecordAction(notPlayed.Action, notPlayed.Card, notPlayed.ActivateDesc, notPlayed.Weight);
            }
            
            NotActivated.Clear();
            IsBestSet = false;
            //actionCount = 0;

            return BestAction;
        }
    }
}
