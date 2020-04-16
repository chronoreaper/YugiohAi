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
                if (cardWeight.Find(cardInst => clientCard.Equals(cardInst.Card)) == null)
                {
                    for (int quant = 1; quant <= Math.Min(cards.GetCardCount(clientCard.Id),max); quant++) {
                        cardWeight.Add(new ClientCardWeight()
                        {
                            Card = clientCard,
                            Weight = GetWeight($"Select", $"{clientCard.Name ?? "Set Monster"} x{quant}"),
                            Quantity = quant
                        });
                    }
                }
            }

            cardWeight.Sort((pair1, pair2) => pair1.Weight.CompareTo(pair2.Weight));

            int count = 0;
            for (int i = 0; i < cardWeight.Count; i++)
            {
                if (count + cardWeight[i].Quantity < numberSelected)
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
            double greatestWeight = 1;

            foreach (CardPosition position in positions)
            {
                double weight = GetWeight($"Position", position.ToString());
                if (weight > greatestWeight)
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
            double greatestWeight = 1;
            foreach (int choice in options)
            {
                double weight = GetWeight($"Option", choice.ToString());
                if (weight > greatestWeight)
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
            return ShouldPerformAction($"Normal Summon {Duel.Phase.ToString()}");
        }

        protected bool ShouldSpSummon()
        {
            return ShouldPerformAction($"Special Summon {Duel.Phase.ToString()}");
        }

        protected bool ShouldActivate()
        {
            return ShouldPerformAction($"Activate {Duel.Phase.ToString()}", DefaultDontChainMyself());
        }

        protected bool ShouldSummonOrSet()
        {
            return ShouldPerformAction($"SummonOrSet {Duel.Phase.ToString()}");
        }

        protected bool ShouldRepos()
        {
            return ShouldPerformAction($"Repos from {Card.Position} {Duel.Phase.ToString()}",DefaultMonsterRepos());
        }

        protected bool ShouldSpellSet()
        {
            return ShouldPerformAction($"Set Spell Trap {Duel.Phase.ToString()}");
        }

        protected bool ShouldMonsterSet()
        {
            return ShouldPerformAction($"Set Monster {Duel.Phase.ToString()}");
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
                string v = Card.Name;
                double yes = Math.Max(GetWeight(action, true.ToString()),1);
                double no = Math.Max(GetWeight(action, false.ToString()),1);

                int minGames = 5;// must be greater than 0
                double threshold = 0.17;
                double diffThresh = 10;
                double k = no/yes;
                double m = yes - no;
                if ((yes >= minGames && no >= minGames) && (no / yes < threshold || yes-no > diffThresh)
                    || Program.Rand.NextDouble() >= 0.5 && (yes < minGames || no < minGames))
                {
                    perform =  true;
                }
            }
            RecordAction(action,perform.ToString());
            return perform;
        }

        /// <summary>
        /// Checks if the bot should perform the given action
        /// </summary>
        /// <param name="action">action given</param>
        /// <returns>true if it should perform.</returns>
        public double ActionWeight(string action)
        {
            string v = Card.Name;
            double yes = GetWeight(action, true.ToString());
            //double no = Math.Max(GetWeight(action, false.ToString()), 1);
            return yes;
        }

        public void RecordAction(string action, string result)
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

        public double GetWeight(string action, string result)
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

            score.AddRange(Logger.GetData(Card.Name, Card.Location.ToString(), action, result, "Number of Monsters Bot", Bot.GetMonsterCount().ToString(), 1));
            score.AddRange(Logger.GetData(Card.Name, Card.Location.ToString(), action, result, "Cards In Bot Hand", Bot.GetHandCount().ToString(), 1));


            //Enemy
            foreach (ClientCard Monster in Enemy.GetMonsters())
            {
                score.AddRange(Logger.GetData(Card.Name, Card.Location.ToString(), action, result, "Enemy Monsters", Monster.Name?.ToString()??"Set Monster", 1));
            }
            score.AddRange(Logger.GetData(Card.Name, Card.Location.ToString(), action, result, "Cards In Opponent Hand", Enemy.GetHandCount().ToString(), 1));
            score.AddRange(Logger.GetData(Card.Name, Card.Location.ToString(), action, result, "Number of Monsters Opponent", Enemy.GetMonsterCount().ToString(), 1));

            int count = 0;
            double totalWins = 0;
            double totalGames = 0;
            for (int i = 0; i<score.Count; i+=2)
            {
                double wins = score[i];
                double games = score[i + 1];
                totalWins += score[i];
                totalGames += score[i + 1];
                if (score[i + 1] >= 1)
                {
                    weight += wins * wins / games;
                    count +=(int) games;
                }
            }
            weight = totalWins / totalGames;
            if (totalGames < 10)
                weight = Program.Rand.NextDouble();
            /*if (count < 10)
                weight = Program.Rand.Next(100);*/
            else if (totalWins / totalGames < 0.3) weight = -1;
            
            return weight;
        }
    }
}