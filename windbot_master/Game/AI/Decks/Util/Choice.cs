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
        public ActionWeight BestPotentialAction = null;
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
                    if (SqlComm.TreeActivation.ShouldPlayCard(Executor.Duel.Turn))
                    {
                        TreeActivation.Node results = SqlComm.TreeActivation.GetNextPotentialAction(Executor.Duel.Turn, Executor.Duel.IsFirst);
                        if (results != null)
                        {
                            Console.WriteLine("Setting Best Action id as " + results.actionId + " to follow " + results.preActions);
                            BestPotentialAction = new ActionWeight(results.weight, results.actionId);
                            //IsBestSet = true;
                        }
                    }
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
                    weight = Math.Min(weight, 0);

                if (BestAction == null || BestAction.Weight == null || weight >= BestAction.Weight)
                {
                    // update to be the better one
                    if (BestAction != null)
                    NotActivated.Add(BestAction);
                    BestAction = new ActionWeight(desc, action, card, index, weight, actionCount);
                    Console.WriteLine($"    Setting Best Action as {action} {card?.Name} {weight}");
                }
                else if (BestAction != null)//if (weight < 0)
                {
                    NotActivated.Add(new ActionWeight(desc, action, card, index, weight, actionCount));
                    Console.WriteLine($"    Did not {action} {card?.Name} {weight} as the better choice is {BestAction.Action} {BestAction.Card?.Name} {BestAction.Weight}");
                }
            }
            else if (SqlComm.TreeActivation.ShouldPlayCard(Executor.Duel.Turn))
            {
                List<double?> weights = SqlComm.TreeActivation.GetTreeNode(Executor.Duel.Turn, actionCount, card?.Name, actionString, Executor.Duel.IsFirst);
                //if (action == ExecutorType.GoToBattlePhase || action == ExecutorType.GoToEndPhase || action == ExecutorType.GoToMainPhase2)
                //    weights = new List<double>() { 0, 0 };
                if (action == ExecutorType.Repos)
                    weights = new List<double?>() { Executor.ShouldRepos() ? 1 : -1, 0 };
                Console.WriteLine(string.Format($"    {actionCount} {(weights.Count > 0 && weights[0] != null ? weights[0].ToString() : "null")} | {action} {card?.Name} | {index}"));

                if (BestPotentialAction != null && BestPotentialAction.ActionId == actionCount)
                {
                    BestAction = new ActionWeight(desc, action, card, index, null, actionCount);
                    IsBestSet = true;
                }

                double? weight = null;
                if (weights.Count > 0 && weights[0] != null)
                {
                    weight = weights[0];
                }

                if (!IsBestSet)
                {
                    if (action != ExecutorType.Repos || weight >= 0)
                    {
                        if (BestAction != null)
                            NotActivated.Add(BestAction);
                        BestAction = new ActionWeight(desc, action, card, index, weight, actionCount);
                    }
                }
                else
                {
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
                Executor.RecordAction(actionString, value, weight);

            }
        }

        public ActionWeight ReturnBestAction()
        {
            string phase = Phase.ToString();
            if (Phase == DuelPhase.Main2)
                phase = DuelPhase.Main1.ToString();


            TreeActivation.Node parent = SqlComm.TreeActivation.GetLastNode(Executor.Duel.Turn);

            if (actionCount > 1)
            {
                RecordAction(BestAction.Action, BestAction.Card, BestAction.ActivateDesc, 1);//BestAction.Weight);
                if (SqlComm.IsTraining)
                {
                    if (BestAction.Action != ExecutorType.Repos)
                    {
                        string actionString = Executor.BuildActionString(BestAction.Action, BestAction.Card, phase);
                        SqlComm.TreeActivation.SaveTreeNode(Executor.Duel.Turn, BestAction.ActionId, BestAction.Card?.Name, actionString, BestAction.Weight, Executor.Duel.IsFirst, true, parent);
                    }
                }
            }

            foreach (var notPlayed in NotActivated)
            {
                if (notPlayed == null)
                    continue;
                RecordAction(notPlayed.Action, notPlayed.Card, notPlayed.ActivateDesc, -1);//notPlayed.Weight);
                if (SqlComm.IsTraining)
                {
                    double? weight = notPlayed.Weight;
                    //if (weight != null)
                    //    weight = 0;
                    string actionString = Executor.BuildActionString(notPlayed.Action, notPlayed.Card, phase);
                    SqlComm.TreeActivation.SaveTreeNode(Executor.Duel.Turn, notPlayed.ActionId, notPlayed.Card?.Name, actionString, weight, Executor.Duel.IsFirst, false, parent);
                }
            }
            
            NotActivated.Clear();
            IsBestSet = false;
            //actionCount = 0;

            return BestAction;
        }
    }
}
