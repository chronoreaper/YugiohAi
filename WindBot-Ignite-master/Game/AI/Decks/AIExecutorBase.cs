using YGOSharp.OCGWrapper.Enums;
using System.Collections.Generic;
using WindBot;
using WindBot.Game;
using WindBot.Game.AI;
using static WindBot.MCST;

namespace WindBot.Game.AI.Decks
{
    public class AIExecutorBase : DefaultExecutor
    {
        MCST Tree;
        Node BestAction;

        public AIExecutorBase(GameAI ai, Duel duel)
            : base(ai, duel)
        {
            AddExecutor(ExecutorType.Activate, ShouldPerform);

            AddExecutor(ExecutorType.MonsterSet, ShouldPerform);
            AddExecutor(ExecutorType.Repos, ShouldPerform);
            AddExecutor(ExecutorType.SpellSet, ShouldPerform);
            AddExecutor(ExecutorType.SpSummon, ShouldPerform);
            AddExecutor(ExecutorType.Summon, ShouldPerform);
            AddExecutor(ExecutorType.SummonOrSet, ShouldPerform);

            AddExecutor(ExecutorType.GoToBattlePhase, ShouldPerform);
            AddExecutor(ExecutorType.GoToMainPhase2, ShouldPerform);
            AddExecutor(ExecutorType.GoToEndPhase, ShouldPerform);
            AddExecutor(ExecutorType.Repos, DefaultMonsterRepos);

            Tree = new MCST();
        }


        public override void SetMain(MainPhase main)
        {
            base.SetMain(main);
            BestAction = null;

            Tree.AddPossibleAction("",ExecutorType.GoToEndPhase.ToString());

            foreach (ClientCard card in main.MonsterSetableCards)
            {
                Tree.AddPossibleAction(card?.Name, ExecutorType.MonsterSet.ToString());
                //card.ActionIndex[(int)ExecutorType.MonsterSet];
            }
            //loop through cards that can change position
            foreach (ClientCard card in main.ReposableCards)
            {
                Tree.AddPossibleAction(card?.Name, ExecutorType.Repos.ToString());
            }
            //Loop through normal summonable monsters
            foreach (ClientCard card in main.SummonableCards)
            {
                Tree.AddPossibleAction(card?.Name, ExecutorType.Summon.ToString());
            }
            //loop through special summonable monsters
            foreach (ClientCard card in main.SpecialSummonableCards)
            {
                Tree.AddPossibleAction(card?.Name, ExecutorType.SpSummon.ToString());
            }
            //loop through activatable cards
            for (int i = 0; i < main.ActivableCards.Count; ++i)
            {
                ClientCard card = main.ActivableCards[i];
                Tree.AddPossibleAction(card?.Name, ExecutorType.Repos.ToString());
                //choice.SetBest(ExecutorType.Activate, card, card.ActionActivateIndex[main.ActivableDescs[i]]);
            }

            BestAction = Tree.GetNextAction();
        }

        public override void SetBattle(BattlePhase battle)
        {
            base.SetBattle(battle);

            if (battle.CanMainPhaseTwo) // check if should enter main phase 2 directly
            {
                //choice.SetBest(ExecutorType.GoToMainPhase2, null);
            }
            if (battle.CanEndPhase) // check if should enter end phase directly
            {
                //choice.SetBest(ExecutorType.GoToEndPhase, null);
            }

            for (int i = 0; i < battle.ActivableCards.Count; ++i)
            {
                ClientCard card = battle.ActivableCards[i];
                Tree.AddPossibleAction(card?.Name, ExecutorType.Repos.ToString());
                //choice.SetBest(ExecutorType.Activate, card, battle.ActivableDescs[i]);
            }

        }

        public override bool OnSelectHand()
        {
            bool choice = true;
            return choice;
        }

        public override IList<ClientCard> OnSelectCard(IList<ClientCard> _cards, int min, int max, long hint, bool cancelable)
        {
            //if (Duel.Phase == DuelPhase.BattleStart)
            //    return null;
            if (AI.HaveSelectedCards())
                return null;


            SetCard(ExecutorType.Activate, null, 0);

            IList<ClientCard> selected = new List<ClientCard>();
            IList<ClientCard> cards = new List<ClientCard>(_cards);


            // AI Selection

            int numToSelect = min;

            //get number of cards to select
            for (int i = min; i <= max; i++)
            {
                Tree.AddPossibleAction(i.ToString(), "NumberSelected");
            }
            numToSelect = int.Parse(Tree.GetNextAction().CardId);

            foreach (ClientCard clientCard in cards)
            {
                string action = $"Select" + hint.ToString();
                double? weight = GetWeight(action, SelectStringBuilder(clientCard));
                Console.WriteLine(string.Format($"    {actionId} {(weight != null ? weight.ToString() : "null")} | {action} {clientCard?.Name}"));
                cardWeight.Add(new ClientCardWeight()
                {
                    Card = clientCard,
                    Weight = weight,
                    Quantity = 0,
                    ActionId = actionId
                });
            }


            // Fill in the remaining with defaults

            if (HintMsgForEnemy.Contains(hint))
            {
                IList<ClientCard> enemyCards = cards.Where(card => card.Controller == 1).ToList();

                // select enemy's card first
                while (enemyCards.Count > 0 && selected.Count < max)
                {
                    ClientCard card = enemyCards[Program.Rand.Next(enemyCards.Count)];
                    selected.Add(card);
                    enemyCards.Remove(card);
                    cards.Remove(card);
                }
            }

            if (HintMsgForDeck.Contains(hint))
            {
                IList<ClientCard> deckCards = cards.Where(card => card.Location == CardLocation.Deck).ToList();

                // select deck's card first
                while (deckCards.Count > 0 && selected.Count < max)
                {
                    ClientCard card = deckCards[Program.Rand.Next(deckCards.Count)];
                    selected.Add(card);
                    deckCards.Remove(card);
                    cards.Remove(card);
                }
            }

            if (HintMsgForSelf.Contains(hint))
            {
                IList<ClientCard> botCards = cards.Where(card => card.Controller == 0).ToList();

                // select bot's card first
                while (botCards.Count > 0 && selected.Count < max)
                {
                    ClientCard card = botCards[Program.Rand.Next(botCards.Count)];
                    selected.Add(card);
                    botCards.Remove(card);
                    cards.Remove(card);
                }
            }

            if (HintMsgForMaterial.Contains(hint))
            {
                IList<ClientCard> materials = cards.OrderBy(card => card.Attack).ToList();

                // select low attack first
                while (materials.Count > 0 && selected.Count < min)
                {
                    ClientCard card = materials[0];
                    selected.Add(card);
                    materials.Remove(card);
                    cards.Remove(card);
                }
            }

            // select random cards
            while (selected.Count < min)
            {
                ClientCard card = cards[Program.Rand.Next(cards.Count)];
                selected.Add(card);
                cards.Remove(card);
            }

            if (HintMsgForMaxSelect.Contains(hint))
            {
                // select max cards
                while (selected.Count < max)
                {
                    ClientCard card = cards[Program.Rand.Next(cards.Count)];
                    selected.Add(card);
                    cards.Remove(card);
                }
            }
            //RecordAction("Number Selected", numToSelect.ToString(), maxWeight);
            var parent = SqlComm.TreeActivation.GetLastNode(Duel.Turn);
            foreach (ClientCard card in selected)
            {
                double? weight = cardWeight.Find(x => x.Card.Id == card.Id).Weight;
                RecordAction($"Select" + hint.ToString(), SelectStringBuilder(card), weight);
                if (SqlComm.IsTraining)
                {
                    SqlComm.TreeActivation.SaveTreeNode(Duel.Turn, cardWeight.Find(x => x.Card.Id == card.Id).ActionId, Card?.Name, $"Select" + hint.ToString() + SelectStringBuilder(card), weight, Duel.IsFirst, true, parent);
                }
                Console.WriteLine($"Choosing {SelectStringBuilder(card)} for Action {hint}");
                //RecordAction($"Select",$"{card.Name} { card.Position.ToString()}");
                //RecordAction($"Select", $"{card.Name}");
                //RecordAction("Number Selected", count.ToString());

            }
            foreach (ClientCard card in cards)
            {
                // Not selected
                RecordAction($"Select" + hint.ToString(), SelectStringBuilder(card), null);
                if (SqlComm.IsTraining)
                {
                    SqlComm.TreeActivation.SaveTreeNode(Duel.Turn, cardWeight.Find(x => x.Card.Id == card.Id).ActionId, Card?.Name, $"Select" + hint.ToString() + SelectStringBuilder(card), cardWeight.Find(x => x.Card.Id == card.Id).Weight, Duel.IsFirst, false, parent);
                }
                Console.WriteLine($"Did not select {SelectStringBuilder(card)} for Action {hint}");
            }


            return selected;
        }

        public override int OnSelectOption(IList<long> options)
        {
        }

        public bool ShouldPerform()
        {
            if (BestAction != null)
            {
                //ActivateDescription
                if (Card.Name == BestAction.CardId && Type == BestAction.Action)
                {
                    BestAction = null;
                }
                else
                {
                    return false;
                }
            }

            return Tree.ShouldActivate(Card.Name, Type);

        }
    }
}
