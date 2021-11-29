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
                BestAction = new ActionWeight();
        }

        public void SetBest(ExecutorType action, ClientCard card, int index = -1, int desc = -1)
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
                    Console.WriteLine($"Setting Best Action as {action} {card?.Name} {weight}");
                }
                else //if (weight < 0)
                {
                    NotActivated.Add(new ActionWeight(desc, action, card, index, weight, actionCount));
                    Console.WriteLine($"Did not {action} {card?.Name} {weight} as the better choice is {BestAction.Action} {BestAction.Card?.Name} {BestAction.Weight}");
                }
            }
            else
            {
                //if (BestAction != null && BestAction.Weight  < 3)
                {
                    List<double> weights = SqlComm.GetTreeNode(Executor.Duel.Turn, actionCount, card?.Name, actionString, Executor.Duel.IsFirst);
                    if (action == ExecutorType.GoToBattlePhase || action == ExecutorType.GoToEndPhase || action == ExecutorType.GoToMainPhase2)
                        weights = new List<double>() { 0, 0 };
                    Console.WriteLine(string.Format($"    {actionCount} {(weights.Count > 0 ? weights[0].ToString() : "null")} | {action} {card?.Name} | {index}"));
                    if ((weights.Count == 0 || actionCount == 2 ) && action != ExecutorType.Repos )
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

                        if (!IsBestSet && (BestAction == null || BestAction.Weight <= weight))
                        {
                            NotActivated.Add(BestAction);
                            BestAction = new ActionWeight(desc, action, card, index, weight, actionCount);
                        }
                        else
                            NotActivated.Add(new ActionWeight(desc, action, card, index, weight, actionCount));
                    }
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
            int Index = BestAction.Index;
            double? Weight = BestAction.Weight;

            if (actionCount > 1)
            {
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
                RecordAction(Action, Card, ActivateDesc, Weight);
            }
            //if (Action == ExecutorType.GoToEndPhase)
            //else
            {
                // Record only the bad choices
                foreach (ActionWeight actionWeight in NotActivated)
                {
                    string n = actionWeight.Card?.Name;
                    if (actionWeight.Weight < 0)
                    {
                        //RecordAction(actionWeight.Action, actionWeight.Card, actionWeight.ActivateDesc, actionWeight.Weight);
                    }
                    else
                    {
                        //RecordAction(actionWeight.Action, actionWeight.Card, actionWeight.ActivateDesc, -actionWeight.Weight/2);
                    }
                }
            }
            NotActivated.Clear();
            IsBestSet = false;
            actionCount = 0;

            return BestAction;
        }
    }
}
