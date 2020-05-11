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
        protected List<PreviousAction> ActionPerformedTurn = new List<PreviousAction>();

        protected class PreviousAction
        {
            public string Action = "";
            public string Value = "";
            public PreviousAction(string action,string value)
            {
                Action = action;
                Value = value;
            }
        }

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

        public override void OnNewTurn()
        {
            base.OnNewTurn();
            ActionPerformedTurn.Clear();
        }

        public override bool OnSelectHand()
        {
            bool choice = Program.Rand.Next(2) > 0;
            List<double> weights =  Logger.GetData("GoFirst");

            if (weights.Count > 0)
                choice = weights[0] > 0 ? true : false;

            Logger.RecordAction(action: "GoFirst", wins: choice ? 1 : -1);

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
            //add choices
            foreach(ClientCard clientCard in cards)
            {
                for (int quant = 1; quant <= Math.Min(cards.GetCardCountWithFaceDown(clientCard.Id),max); quant++) {
                    //if it doesnt exist in the list already
                    if (cardWeight.Find(cardInst => clientCard.Equals(cardInst.Card) && cardInst.Quantity == quant) == null)
                    {
                        cardWeight.Add(new ClientCardWeight()
                        {
                            Card = clientCard,
                            Weight = GetWeight($"Select", $"{clientCard.Name ?? "Set Monster" } {Card?.Position.ToString()} x{quant}"),
                            Quantity = quant
                        });
                    }
                }
            }

            cardWeight.Sort((pair1, pair2) => pair1.Weight.CompareTo(pair2.Weight));

            int count = 0;
            // choose the cards
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
                RecordAction($"Select", $"{card.Name ?? "Set Monster"} {Card?.Position.ToString()}");
                Logger.WriteLine($"Choosing {card.Name ?? "Set Monster"} {Card?.Position.ToString()}");
                //RecordAction($"Select",$"{card.Name} { card.Position.ToString()}");
                //RecordAction($"Select", $"{card.Name}");
                //RecordAction("Number Selected", count.ToString());
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
            //Remove later
            if (positions.Contains(CardPosition.FaceUpAttack))
                return CardPosition.FaceUpAttack;
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
                double yes = GetWeight(action, "");

                if (yes >0)
                {
                    perform = true;
                    ActionPerformedTurn.Add(new PreviousAction("Previous " + action,Card.Name));
                }
            }
            RecordAction(action,"",perform?1:-1);
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
            double yes = GetWeight(action, "");
            //double no = Math.Max(GetWeight(action, false.ToString()), 1);
            return yes;
        }

        public void RecordAction(string action, string result, double wins = 1)
        {
            //Bot

            var cardQuant = ListToQuantity(Bot.Hand);
            foreach (ClientCard CardInHand in cardQuant.Keys)
            {
                Logger.RecordAction(Card?.Name, Card?.Location.ToString(), action, result, "Card In Hand", CardInHand.Name.ToString(), cardQuant[CardInHand], wins);
            }

            cardQuant = ListToQuantity(Bot.GetMonsters());
            foreach (ClientCard Monster in cardQuant.Keys)
            {
                Logger.RecordAction(Card?.Name, Card?.Location.ToString() + " " + Card?.Position.ToString(), action, result, "Player Monsters", Monster.Name.ToString(), cardQuant[Monster], wins);
            }

            Logger.RecordAction(Card?.Name, Card?.Location.ToString(), action, result, "Number of Monsters Bot", Bot.GetMonsterCount().ToString(), 1, wins);
            Logger.RecordAction(Card?.Name, Card?.Location.ToString(), action, result, "Cards In Bot Hand", Bot.GetHandCount().ToString(), 1, wins);
            //Enemy
            foreach (ClientCard Monster in Enemy.GetMonsters())
            {
                Logger.RecordAction(Card?.Name, Card?.Location.ToString() + " " + Card?.Position.ToString(), action, result, "Enemy Monsters", Monster.Name?.ToString() ?? "Set Monster", 1, wins);
            }

            Logger.RecordAction(Card?.Name, Card?.Location.ToString(), action, result, "Cards In Opponent Hand", Enemy.GetHandCount().ToString(), 1, wins);
            Logger.RecordAction(Card?.Name, Card?.Location.ToString(), action, result, "Number of Monsters Opponent", Enemy.GetMonsterCount().ToString(), 1, wins);
            //Logger.RecordAction(Card.Id, Card.Location.ToString(), action, result, "Number of Spell,Trap", Enemy.GetSpellCountWithoutField().ToString(), 1, wins);
             
            //previous actions that turn
            foreach (PreviousAction previous in ActionPerformedTurn)
            {
                Logger.RecordAction(Card?.Name, Card?.Location.ToString(), action, result, previous.Action, previous.Value, 1, wins);
            }

            //record as previous action
            //if (wins>0)
            //    ActionPerformedTurn.Add(new PreviousAction("Previous " + action, Card.Name));
        }

        public double GetWeight(string action, string result)
        {
            double weight = 0;
            List<double> score = new List<double>();

            //Bot
            var cardQuant = ListToQuantity(Bot.Hand);
            foreach (ClientCard CardInHand in cardQuant.Keys)
            {
                score.AddRange(Logger.GetData(Card?.Name, Card?.Location.ToString(), action, result, "Card In Hand", CardInHand.Name.ToString(), cardQuant[CardInHand]));
            }

            cardQuant = ListToQuantity(Bot.GetMonsters());
            foreach (ClientCard Monster in cardQuant.Keys)
            {
                score.AddRange(Logger.GetData(Card?.Name, Card?.Location.ToString() + " " + Card?.Position.ToString(), action, result, "Player Monsters", Monster.Name.ToString(), cardQuant[Monster]));
            }

            score.AddRange(Logger.GetData(Card?.Name, Card?.Location.ToString(), action, result, "Number of Monsters Bot", Bot.GetMonsterCount().ToString(), 1));
            score.AddRange(Logger.GetData(Card?.Name, Card?.Location.ToString(), action, result, "Cards In Bot Hand", Bot.GetHandCount().ToString(), 1));


            //Enemy
            foreach (ClientCard Monster in Enemy.GetMonsters())
            {
                score.AddRange(Logger.GetData(Card?.Name, Card?.Location.ToString() + " " + Card?.Position.ToString(), action, result, "Enemy Monsters", Monster.Name?.ToString()??"Set Monster", 1));
            }
            score.AddRange(Logger.GetData(Card?.Name, Card?.Location.ToString(), action, result, "Cards In Opponent Hand", Enemy.GetHandCount().ToString(), 1));
            score.AddRange(Logger.GetData(Card?.Name, Card?.Location.ToString(), action, result, "Number of Monsters Opponent", Enemy.GetMonsterCount().ToString(), 1));

            //previous actions that turn
            foreach (PreviousAction previous in ActionPerformedTurn)
            {
                score.AddRange(Logger.GetData(Card?.Name, Card?.Location.ToString(), action, result, previous.Action, previous.Value, 1));
            }

            int count = 0;
            double totalWins = 0;
            double totalGames = 0;
            for (int i = 0; i<score.Count; i+=2)
            {
                double wins = score[i];
                double games = score[i + 1];
                totalWins += Math.Min(5,Math.Max(score[i],-5));
                totalGames += score[i + 1];
                if (score[i + 1] >= 1)
                {
                    weight += wins;
                    count +=(int) games;
                }
            }
           
            return weight;
        }

        private Dictionary<ClientCard,int> ListToQuantity(IList<ClientCard> clientCards)
        {
            Dictionary<string, CardToQuant> cardQuant = new Dictionary<string, CardToQuant>();
            foreach(ClientCard card in clientCards)
            {
                if (!cardQuant.ContainsKey(card.Name))
                {
                    cardQuant.Add(card.Name, new CardToQuant() { card = card, quant = 1 });
                }
                else
                {
                    CardToQuant count;
                    cardQuant.TryGetValue(card.Name, out count);
                    if (count != null) {
                        count.quant++;
                    }
                }
            }

            Dictionary<ClientCard, int> Result = new Dictionary<ClientCard, int>();
            foreach (CardToQuant value in cardQuant.Values)
            {
                Result.Add(value.card, value.quant);
            }
            return Result;
        }

        private class CardToQuant
        {
            public ClientCard card { get; set; }
            public int quant { get; set; }
        }
    }
}