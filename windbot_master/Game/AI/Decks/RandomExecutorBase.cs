using YGOSharp.OCGWrapper.Enums;
using System.Collections.Generic;
using WindBot;
using WindBot.Game;
using WindBot.Game.AI;
using System;
using System.Linq;

namespace WindBot.Game.AI.Decks
{
    //[Deck("Random", "AI_Random")]
    public class RandomExecutorBase : DefaultExecutor
    {
        public class CardId
        {
            public const int LeoWizard = 4392470;
        }

        public RandomExecutorBase(GameAI ai, Duel duel)
            : base(ai, duel)
        {
            AddExecutor(ExecutorType.Activate, ShouldActivate);
            AddExecutor(ExecutorType.SpSummon, ShouldSpSummon);
            AddExecutor(ExecutorType.Summon, ShouldNormalSummon);
            AddExecutor(ExecutorType.MonsterSet, ShouldMonsterSet);
            AddExecutor(ExecutorType.Repos, ShouldRepos);
            AddExecutor(ExecutorType.SpellSet, ShouldSpellSet);

            AddExecutor(ExecutorType.SummonOrSet, ShouldSummonOrSet);

        }

        public override bool OnSelectHand()
        {
            bool choice = Program.Rand.Next(2) > 0;

            Logger.RecordAction(action:"GoFirst",value:choice.ToString());

            return choice;
        }

        public override IList<ClientCard> OnSelectCard(IList<ClientCard> cards, int min, int max, int hint, bool cancelable)
        {
            if (Duel.Phase == DuelPhase.BattleStart)
                return null;

            // the number of cards you should select
            int numberSelected = max;
            double maxWeight = 0;

            Random random = new Random();
            IList<ClientCard> selected = new List<ClientCard>();

            // select random card, defalut option
            /*for (int i = 1; i <= numberSelected; ++i)
            {
                int rand = random.Next(cards.Count);
                while (selected.Contains(cards[rand]))
                    rand = random.Next(cards.Count);
                selected.Add(cards[rand]);
            }*/

            //get number of cards to select
            for (int i = min; i <= max; i++)
            {
                double weight = GetWeight("Number Selected", i.ToString());
                if (weight >= maxWeight)
                {
                    maxWeight = weight;
                    numberSelected = i;
                }
            }

            List<ClientCardWeight> cardWeight = new List<ClientCardWeight>();
            //ai choice
            foreach(ClientCard clientCard in cards)
            {
                //if it doesnt exist in the list already
                if (cardWeight.Find(cardInst => clientCard.Equals(cardInst)) == null)
                {
                    for (int quant = 1; quant <= cards.GetCardCount(clientCard.Id); quant++) {
                        cardWeight.Add(new ClientCardWeight()
                        {
                            Card = clientCard,
                            Weight = GetWeight($"Select", $"{clientCard.Name} x{quant}"),
                            Quantity = quant
                        });
                    }
                }
            }

            cardWeight.Sort((pair1, pair2) => pair1.Weight.CompareTo(pair2.Weight));

            int count = 0;
            for (int i = 0; i < cardWeight.Count; i++)
            {
                if (count + cardWeight[i].Quantity <= numberSelected)
                {
                    for(int j = 0; j < cardWeight[i].Quantity; j++)
                    {
                        selected.Add(cardWeight[i].Card);
                    }
                    count += cardWeight[i].Quantity;
                }
            }

            foreach (ClientCard card in selected)
            {
                //RecordAction($"Select",$"{card.Name} { card.Position.ToString()}");
                RecordAction($"Select", $"{card.Name}");
                RecordAction("Number Selected", count.ToString());
            }

            return selected;
        }

        private class ClientCardWeight
        {
            public ClientCard Card;
            public double Weight;
            public int Quantity;
        }

        public override CardPosition OnSelectPosition(int cardId, IList<CardPosition> positions)
        {
            Random random = new Random();
            CardPosition pos = positions[random.Next(positions.Count)];
            double greatestWeight = 0;

            foreach (CardPosition position in positions)
            {
                double weight = GetWeight($"Position", position.ToString());
                if (weight >= greatestWeight)
                {
                    weight = greatestWeight;
                    pos = position;
                }
            }

            RecordAction($"Position", pos.ToString());
            return pos;
        }

        public override int OnSelectOption(IList<int> options)
        {
            int option = -1;
            double greatestWeight = 0;
            foreach (int choice in options)
            {
                double weight = GetWeight($"Option", choice.ToString());
                if (weight >= greatestWeight)
                {
                    weight = greatestWeight;
                    option = choice;
                }
            }

            if (option == -1)
            {
                Random random = new Random();
                option = random.Next(options.Count);
            }

            RecordAction($"Option", option.ToString());

            return option;
        }

        protected bool ShouldNormalSummon()
        {
            return ShouldPerformAction("Normal Summon");
        }

        protected bool ShouldSpSummon()
        {
            return ShouldPerformAction("Special Summon");
        }

        protected bool ShouldActivate()
        {
            return ShouldPerformAction("Activate", DefaultDontChainMyself());
        }

        protected bool ShouldSummonOrSet()
        {
            return ShouldPerformAction("SummonOrSet");
        }

        protected bool ShouldRepos()
        {
            return ShouldPerformAction("Repos",DefaultMonsterRepos());
        }

        protected bool ShouldSpellSet()
        {
            return ShouldPerformAction("Set Spell Trap");
        }

        protected bool ShouldMonsterSet()
        {
            return ShouldPerformAction("Set Monster");
        }

        /// <summary>
        /// Checks if the bot should perform the given action
        /// </summary>
        /// <param name="action">action given</param>
        /// <param name="shouldPerform">any external checkers.</param>
        /// <returns>true if it should perform.</returns>
        protected bool ShouldPerformAction(string action, bool shouldPerform = true)
        {
            bool perform = false;

            if (shouldPerform)
            {
                double yes = GetWeight(action, true.ToString());
                double no = GetWeight(action, false.ToString());
                if (yes == 0)
                    yes = Program.Rand.NextDouble();
                if (no == 0)
                    no = Program.Rand.NextDouble();
                if (yes >= 0.35)
                if ((Math.Abs(yes-no) >= 0.05 && yes>=no )|| Program.Rand.NextDouble()>=0.5)
                {
                    perform =  true;
                }
            }
            RecordAction(action,perform.ToString());
            return perform;
        }

        protected void RecordAction(string action, string result)
        {
            //TODO: fix the count.
            //Bot
            foreach (ClientCard CardInHand in Bot.Hand)
            {
                Logger.RecordAction(Card.Name, Card.Location.ToString(), action, result, "Card In Hand", CardInHand.Name.ToString(), 1);
            }

            foreach (ClientCard Monster in Bot.GetMonsters())
            {
                Logger.RecordAction(Card.Name, Card.Location.ToString(), action, result, "Player Monsters", Monster.Name.ToString(), 1);
            }

            Logger.RecordAction(Card.Name, Card.Location.ToString(), action, result, "Number of Monsters Bot", Bot.GetMonsterCount().ToString(), 1);
            Logger.RecordAction(Card.Name, Card.Location.ToString(), action, result, "Cards In Bot Hand", Bot.GetHandCount().ToString(), 1);
            //Enemy
            foreach (ClientCard Monster in Enemy.GetMonsters())
            {
                Logger.RecordAction(Card.Name, Card.Location.ToString(), action, result, "Enemy Monsters", Monster.Name?.ToString() ?? "Set Monster", 1);
            }

            Logger.RecordAction(Card.Name, Card.Location.ToString(), action, result, "Cards In Opponent Hand", Enemy.GetHandCount().ToString(), 1);
            Logger.RecordAction(Card.Name, Card.Location.ToString(), action, result, "Number of Monsters Opponent", Enemy.GetMonsterCount().ToString(), 1);
            //Logger.RecordAction(Card.Id, Card.Location.ToString(), action, result, "Number of Spell,Trap", Enemy.GetSpellCountWithoutField().ToString(), 1);
        }

        protected  double GetWeight(string action, string result)
        {
            double weight = 0;
            List<int> score = new List<int>();

            //Bot
            foreach (ClientCard CardInHand in Bot.Hand)
            {
                score.AddRange(Logger.GetData(Card.Name, Card.Location.ToString(), action, result, "Card In Hand", CardInHand.Name.ToString(), 1));
            }

            foreach (ClientCard Monster in Bot.GetMonsters())
            {
                score.AddRange(Logger.GetData(Card.Name, Card.Location.ToString(), action, result, "Player Monsters", Monster.Name.ToString(), 1));
            }

            //Enemy
            foreach (ClientCard Monster in Enemy.GetMonsters())
            {
                score.AddRange(Logger.GetData(Card.Name, Card.Location.ToString(), action, result, "Enemy Monsters", Monster.Name?.ToString()??"Set Monster", 1));
            }

            score.AddRange(Logger.GetData(Card.Name, Card.Location.ToString(), action, result, "Cards In Opponent Hand", Enemy.GetHandCount().ToString(), 1));

            int count = 0;
            double wins = 0;
            double games = 0;
            double total = 0;
            for (int i = 0; i<score.Count; i+=2)
            {
                wins += score[i];
                games += score[i + 1];
                if (score[i + 1] >= 1)
                {
                    total += (double)score[i] / score[i + 1];
                    count++;
                }
            }

            if (total >= 0)
                total = (total / count);
            if (games >= 10)
                weight = wins / games;
            else weight = Program.Rand.NextDouble();

            return weight;
        }
    }
}