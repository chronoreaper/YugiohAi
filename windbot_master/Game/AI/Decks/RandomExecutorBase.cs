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

        protected int CardAdvStartSelfHand = 0;
        protected int CardAdvStartOppHand = 0;
        protected int CardAdvStartSelfHandPre = 0;
        protected int CardAdvStartOppHandPre = 0;
        protected int CardAdvStartSelfField = 0;
        protected int CardAdvStartOppField = 0;
        protected int CardAdvStartSelfFieldPre = 0;
        protected int CardAdvStartOppFieldPre = 0;
        protected int ActionId = 0;
        protected int PlayerLpPre = 8000;
        protected int OppLpPre = 8000;
        protected int LpStart = 8000;
        protected double PreGameState = 0;

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

        public string BuildActionString(MainPhaseAction.MainAction action, ClientCard card, string Phase)
        {
            if (Phase == "Main2")
                Phase = "Main1";
            string actionString = action.ToString();
            if (action == MainPhaseAction.MainAction.Repos && card != null)
                actionString += $";{card.Position.ToString()}";
            actionString += ";" + Phase;
            return actionString;
        }

        public int GetCardAdvantageHand()
        {
            return Bot.GetHandCount() - CardAdvStartSelfHand + CardAdvStartOppHand - Enemy.GetHandCount();
        }

        public int GetCardAdvantageHandPre()
        {
            return Bot.GetHandCount() - CardAdvStartSelfHandPre + CardAdvStartOppHandPre - Enemy.GetHandCount();
        }

        public int GetCardAdvantageField()
        {
            return Bot.GetFieldCount() - CardAdvStartSelfField + CardAdvStartOppField - Enemy.GetFieldCount();
        }

        public int GetCardAdvantageFieldPre()
        {
            return Bot.GetFieldCount() - CardAdvStartSelfFieldPre + CardAdvStartOppFieldPre - Enemy.GetFieldCount();
        }

        public int CardAdvDiff()
        {
            return CardAdvStartSelfHand + CardAdvStartSelfField - (CardAdvStartOppField + CardAdvStartOppHand);
        }

        public override void OnNewTurn()
        {
            double gameState = 0;

            //update results based on previous turn
            if (Duel.Turn > 1)
            {
                int cardAdvantage = Bot.GetFieldHandCount() - Enemy.GetFieldHandCount();
                int fieldAdvantage = Bot.GetFieldCount() - Enemy.GetFieldCount();
                int cardAdvantagePre = GetCardAdvantageFieldPre() - GetCardAdvantageHandPre();
                int enemyHandLoss = CardAdvStartOppHand - Enemy.GetHandCount();
                int enemyHandLossPre = CardAdvStartOppHandPre - Enemy.GetHandCount();
                int enemyFieldLoss = CardAdvStartOppField - Enemy.GetFieldCount();
                int enemyFieldLossPre = CardAdvStartOppFieldPre - Enemy.GetFieldCount();
                int enemyHandFieldLoss = enemyHandLoss + enemyFieldLoss;
                int playerFieldLoss = CardAdvStartSelfField - Bot.GetFieldCount();
                int playerFieldLossPre = CardAdvStartSelfFieldPre - Bot.GetFieldCount();
                int playerHandFieldLoss = CardAdvStartSelfField + CardAdvStartSelfHand - Bot.GetFieldCount() - Bot.GetHandCount();
                int playerHandGain = Bot.GetHandCount() - CardAdvStartSelfHand;
                int v = GetCardAdvantageHand();
                int vv = GetCardAdvantageField();
                double advantageGain = GetCardAdvantageHand()  + GetCardAdvantageField();
                double advantageGain2 = GetCardAdvantageHand() * 0.5 + GetCardAdvantageField();
                int w = GetCardAdvantageFieldPre();
                int ww = GetCardAdvantageHandPre();
                int advGainPre = GetCardAdvantageFieldPre() + GetCardAdvantageHandPre();
                int playerGain = playerHandGain - playerFieldLoss;
                int enemyGain = -enemyHandLoss - enemyFieldLoss;
                double playerLpLoss = PlayerLpPre - Bot.LifePoints;
                double oppLpLoss = OppLpPre - Enemy.LifePoints;

                Logger.RecordUpdateAction("cardAdvantage", cardAdvantage, (Duel.Turn % 2).ToString());
                Logger.RecordUpdateAction("cardAdvantagePre", cardAdvantagePre, (Duel.Turn % 2).ToString());
                Logger.RecordUpdateAction("fieldAdvantage", fieldAdvantage, (Duel.Turn % 2).ToString());
                Logger.RecordUpdateAction("enemyFieldLoss", enemyFieldLoss, (Duel.Turn % 2).ToString());
                Logger.RecordUpdateAction("playerFieldLoss", playerFieldLoss, (Duel.Turn % 2).ToString());
                Logger.RecordUpdateAction("advantageGain", advantageGain, (Duel.Turn % 2).ToString());
                Logger.RecordUpdateAction("advantageGain2", advantageGain2, (Duel.Turn % 2).ToString());
                Logger.RecordUpdateAction("advGainPre", advGainPre, (Duel.Turn % 2).ToString());
                Logger.RecordUpdateAction("playerGain", playerGain, (Duel.Turn % 2).ToString());
                Logger.RecordUpdateAction("enemyGain", enemyGain, (Duel.Turn % 2).ToString());

                int cardDiffPre = CardAdvStartSelfHand + CardAdvStartSelfField - CardAdvStartOppHand + CardAdvStartOppField;

                // number of actions taken 
                int turnActions = 1;
                int preTurnActions = 1;
                if (Logger.actionWeight.ContainsKey(Duel.Turn - 1))
                    turnActions = Logger.actionWeight[Duel.Turn - 1].Count(x => x.Key != -1);
                if (Logger.actionWeight.ContainsKey(Duel.Turn - 2))
                    preTurnActions = Logger.actionWeight[Duel.Turn - 2].Count(x => x.Key != -1);

                // -1 for loosing, 1 for winning
                if (Math.Abs(cardAdvantage) > 2)
                {
                    gameState = Math.Sign(cardAdvantage);
                }
                else if (Math.Abs(fieldAdvantage) > 2)
                {
                    gameState = Math.Sign(fieldAdvantage);
                }
                else if (Math.Abs(cardAdvantage) == 2)
                {
                    gameState = Math.Sign(cardAdvantage) * 0.5;
                }
                else if (Math.Abs(fieldAdvantage) == 2)
                {
                    gameState = Math.Sign(fieldAdvantage) * 0.5;
                }


                if (Duel.Turn % 2 == (Duel.IsFirst ? 0 : 1))//do this calculation on the start of opp turn, so all actions on your turn
                {
                    //if (oppLpLoss > 0)
                    //    Logger.ModifyAction(Duel.Turn - 1, oppLpLoss / 1000.0, 1);
                    double weight = 0;// -playerFieldLoss
                    //weight += 2 * enemyFieldLoss;
                    //weight += (Math.Sign(enemyFieldLoss) - Math.Sign(playerFieldLoss) * 0.5) ;
                    //weight += (advantageGain2  + advantageGain) * 0.5;
                    //weight += Math.Sign(advantageGain2);
                    weight += gameState;
                    //weight += fieldAdvantage * 0.5;
                    //weight += oppLpLoss / 2000.0;
                    // if (cardAdvantage < 0 && cardAdvantagePre > 0)
                    //   weight += advantageGain;

                    if (Duel.Turn > 2) weight--;
                    if (Math.Abs(weight) > 0)
                     {
                        Logger.ModifyAction(Duel.Turn - 1, weight, 1);
                        // Save weight for debugging
                        Logger.SaveActionWeight(Duel.Turn - 1, -1, weight, "Result", "", 0);
                    }
                }
                
                if (Duel.Turn % 2 == (Duel.IsFirst ? 1 : 0))//Calculate advatage of two turns
                {

                    //Logger.ModifyAction(Duel.Turn - 2, advGainPre, 2);
                    //if (cardDiff >= 0)
                    {
                        double weight = 1;
                        //weight += (enemyFieldLoss - playerFieldLoss * 0.5) * 0.5;
                        weight += advantageGain;
                        weight += Math.Sign(fieldAdvantage);
                        //weight -= playerLpLoss / 2000.0;
                        //if (enemyGain > 0)
                        //    weight /= enemyGain;
                        weight *= 0.5;

                        if (Math.Abs(weight) > 0)
                        {
                            Logger.ModifyAction(Duel.Turn - 2, weight, 2);
                            Logger.ModifyAction(Duel.Turn - 1, weight, 2);
                            // Save weight for debugging
                            Logger.SaveActionWeight(Duel.Turn - 1, -1, weight, "Result", "", 0);
                        }
                    }
                }
            }

            //reset
            base.OnNewTurn();
            ActionPerformedTurn.Clear();

            CardAdvStartSelfHandPre = CardAdvStartSelfField;
            CardAdvStartOppHandPre = CardAdvStartOppField;
            CardAdvStartSelfHand = Bot.GetHandCount();
            CardAdvStartOppHand = Enemy.GetHandCount();

            CardAdvStartSelfFieldPre = CardAdvStartSelfField;
            CardAdvStartOppFieldPre = CardAdvStartOppField;
            CardAdvStartSelfField = Bot.GetFieldCount();
            CardAdvStartOppField = Enemy.GetFieldCount();

            PlayerLpPre = Bot.LifePoints;
            OppLpPre = Enemy.LifePoints;

            PreGameState = gameState;

            ActionId = 0;
        }

        public override bool OnSelectHand()
        {
            bool choice = Program.Rand.Next(2) > 0;
            List<double> weights =  Logger.GetData(Logger.master,action: "GoFirst");

            if (weights.Count > 0)
                choice = weights[0] > 0 ? true : false;

            Logger.RecordAction(action: "GoFirst", wins: choice ? 1 : -1, turn: Duel.Turn);

            // Set Lp
            /*PlayerLpPre = Bot.LifePoints;
            OppLpPre = Enemy.LifePoints;
            LpStart = Bot.LifePoints;*/

            return choice;
        }

        public override IList<ClientCard> OnSelectCard(IList<ClientCard> cards, int min, int max, int hint, bool cancelable)
        {
            Logger.WriteLine("Hint" + hint);
            if (Duel.Phase == DuelPhase.BattleStart)
                return null;

            // the number of cards you should select
            int numberSelected = max;
            double maxWeight = 0;

            Random random = new Random();
            IList<ClientCard> selected = new List<ClientCard>();
            IList<double> selected_weight = new List<double>();

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
                            Weight = GetWeight($"Select", SelectStringBuilder(clientCard,quant)),
                            Quantity = quant
                        });
                    }
                }
            }

            cardWeight.Sort((pair1, pair2) => pair2.Weight.CompareTo(pair1.Weight));

            int count = 0;
            // choose the cards
            for (int i = 0; i < cardWeight.Count; i++)
            {
                Logger.WriteLine($"     {cardWeight[i].Weight}:{SelectStringBuilder(cardWeight[i].Card)},{cardWeight[i].Quantity}");
                if (count + cardWeight[i].Quantity <= numberSelected)
                {
                    for(int j = 0; j < cardWeight[i].Quantity; j++)
                    {
                        selected.Add(cardWeight[i].Card);
                        selected_weight.Add(cardWeight[i].Weight);
                    }
                    count += cardWeight[i].Quantity;
                }
                else
                {
                    //RecordAction($"Select", SelectStringBuilder(cardWeight[i].Card), 0);
                }
            }

            foreach (ClientCard card in selected)
            {
                double weight = selected_weight[selected.IndexOf(card)];
                if (hint != 501)
                    RecordAction($"Select", SelectStringBuilder(card), weight);
                Logger.WriteLine($"Choosing {SelectStringBuilder(card)} for Action id {ActivateDescription}");
                //RecordAction($"Select",$"{card.Name} { card.Position.ToString()}");
                //RecordAction($"Select", $"{card.Name}");
                //RecordAction("Number Selected", count.ToString());
            }

            return selected;
        }

        private string SelectStringBuilder(ClientCard Card, int Quant = 1)
        {
            return $"{Card.Name ?? "Set Monster" };{Card?.Location.ToString()};{Card?.Position.ToString()};{Card.Controller}";// x{Quant}";
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
            Logger.WriteLine("Position "+pos.ToString());
            RecordAction($"Position", pos.ToString());
            return pos;
        }

        public override int OnSelectOption(IList<int> options)
        {
            int option = -1;
            double greatestWeight = 1;
            for (int choice = 0; choice < options.Count; choice++)
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
                option = Program.Rand.Next(options.Count);
            }

            RecordAction($"Option", option.ToString());

            return option;
        }

        protected bool ShouldNormalSummon()
        {
            return ShouldPerformAction(BuildActionString(MainPhaseAction.MainAction.Summon,Card,Duel.Phase.ToString()));
        }

        protected bool ShouldSpSummon()
        {
            return ShouldPerformAction(BuildActionString(MainPhaseAction.MainAction.SpSummon, Card, Duel.Phase.ToString()));
        }

        protected bool ShouldActivate()
        {
            return ShouldPerformAction(BuildActionString(MainPhaseAction.MainAction.Activate, Card, Duel.Phase.ToString()), DefaultDontChainMyself());
        }

        protected bool ShouldSummonOrSet()
        {
            return ShouldPerformAction($"SummonOrSet {Duel.Phase.ToString()}");
        }

        public bool ShouldRepos()
        {
            return DefaultMonsterRepos();//ShouldPerformAction($"Repos from {Card.Position} {Duel.Phase.ToString()}",DefaultMonsterRepos());
        }

        protected bool ShouldSpellSet()
        {
            return ShouldPerformAction(BuildActionString(MainPhaseAction.MainAction.SetSpell, Card, Duel.Phase.ToString()));
        }

        protected bool ShouldMonsterSet()
        {
            return ShouldPerformAction(BuildActionString(MainPhaseAction.MainAction.SetMonster, Card, Duel.Phase.ToString()));
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

            string v = Card.Name;
            double yes = GetWeight(action, "");

            if (yes > 0 || (shouldPerform && Math.Abs(yes) < 0.3))
            {
                perform = true;
                ActionPerformedTurn.Add(new PreviousAction("Previous " + action,Card.Name));
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
            return GetWeight(action, "");;
        }

        public void RecordAction(string action, string result, double wins = 1)
        {
            DataModifier(action, result, wins, false);
            ActionId++;
        }

        public double GetWeight(string action, string result)
        {
            Random rand = new Random();
            double weight = DataModifier(action, result);
            return weight;
        }
        
        private double DataModifier(string action, string result, double win = 0,bool GetData = true)
        {
            List<double> score = new List<double>();

            // Add self to the weights
            score.AddRange(SQLCom(GetData, Card?.Name, Card?.Location.ToString() + " " + Card?.Position.ToString(), action, result, "Self", "", 0, win));

            if (result != "" && result.Split(';').Length == 4)
            {
                var owner = result.Split(';')[3];
                score.AddRange(SQLCom(GetData, Card?.Name, Card?.Location.ToString() + " " + Card?.Position.ToString(), action, owner, "Owner", "", 0, win));
            }

            //Bot
            var cardQuant = ListToQuantity(Bot.Hand);
            /*foreach (ClientCard CardInHand in cardQuant.Keys)
            {
                score.AddRange(SQLCom(GetData, Card?.Name, Card?.Location.ToString() + " " + Card?.Position.ToString(), action, result, "Card In Hand", CardInHand.Name.ToString(), cardQuant[CardInHand], win));
            }
            */
            /*cardQuant = ListToQuantity(Bot.GetMonsters());
            foreach (ClientCard Monster in cardQuant.Keys)
            {
                score.AddRange(SQLCom(GetData, Card?.Name, Card?.Location.ToString() + " " + Card?.Position.ToString(), action, result, "Player Monsters", Monster.Name.ToString() + Monster.Position.ToString(), cardQuant[Monster], win));
            }
            /*
            cardQuant = ListToQuantity(Bot.GetGraveyardMonsters());
            foreach (ClientCard Monster in cardQuant.Keys)
            {
                score.AddRange(SQLCom(GetData, Card?.Name, Card?.Location.ToString() + " " + Card?.Position.ToString(), action, result, "Player Monsters GY", Monster.Name.ToString(), cardQuant[Monster], win));
            }*/
            
            /*if (Bot.GetMonsterCount() == 0)
            score.AddRange(SQLCom(GetData, Card?.Name, Card?.Location.ToString() + " " + Card?.Position.ToString(), action, result, "Number of Monsters Bot", Bot.GetMonsterCount().ToString(), 1, win));
            //score.AddRange(SQLCom(GetData, Card?.Name, Card?.Location.ToString() + " " + Card?.Position.ToString(), action, result, "Cards In Bot Hand", Bot.GetHandCount().ToString(), 1, win));


            //Enemy
            cardQuant = ListToQuantity(Enemy.GetMonsters());
            foreach (ClientCard Monster in cardQuant.Keys)
            {
                score.AddRange(SQLCom(GetData, Card?.Name, Card?.Location.ToString() + " " + Card?.Position.ToString(), action, result, "Enemy Monsters", Monster.Name?.ToString() ?? "Set Monster" + Monster.Position.ToString(), cardQuant[Monster], win));
            }

            //gy monsters
            /*cardQuant = ListToQuantity(Enemy.GetGraveyardMonsters());
            foreach (ClientCard Monster in cardQuant.Keys)
            {
                score.AddRange(SQLCom(GetData, Card?.Name, Card?.Location.ToString() + " " + Card?.Position.ToString(), action, result, "Enemy Monsters GY", Monster.Name.ToString(), cardQuant[Monster], win));
            }*/
            //score.AddRange(SQLCom(GetData, Card?.Name, Card?.Location.ToString() + " " + Card?.Position.ToString(), action, result, "Cards In Opponent Hand", Enemy.GetHandCount().ToString(), 1, win));
            //if (Enemy.GetMonsterCount()==0)
            //    score.AddRange(SQLCom(GetData, Card?.Name, Card?.Location.ToString() + " " + Card?.Position.ToString(), action, result, "Number of Monsters Opponent", Enemy.GetMonsterCount().ToString(), 1, win));

            //previous actions that turn
            /*if (Logger.actionWeight.ContainsKey(Duel.Turn))
                foreach (Logger.ActionWeightCard previous in Logger.actionWeight[Duel.Turn].Values)
                {
                    score.AddRange(SQLCom(GetData, Card?.Name, Card?.Location.ToString() + " " + Card?.Position.ToString(), action, result, previous.id, previous.action, 1, win));
                }*/

            //record as previous action

            double gamesWon = 0;
            double totalWon = 0;
            double totalGames = 0;
            double activator = 0;
            double activatePercent = 0;
            double totalWeights = 0;

            List<double> greatest = new List<double>();
            List<double> least = new List<double>();
            int top = 1;

            for (int i = 0; i < score.Count; i += 2)
            {
                //if (Math.Abs(score[i]) > 3 || score[i+1] > 5) // Try and remove noise
                {
                    gamesWon += score[i];
                    totalWon += Math.Abs(score[i]);
                    totalGames += score[i + 1];
                    totalWeights++;
                    double games = score[i + 1];
                    double weight = score[i];

                    if (Math.Abs(weight) == 0)
                        weight = (new Random()).Next(2) - 1;

                    activatePercent++;
                    activator += weight;

                    // get the top few abs weights.
                    if (greatest.Count < top)
                    {
                        greatest.Add(weight);
                    }
                    else if (greatest.Min() < weight)
                    {
                        greatest.Remove(greatest.Min());
                        greatest.Add(weight);
                    }

                    if (least.Count < top)
                    {
                        least.Add(weight);
                    }
                    else if (least.Max() > weight)
                    {
                        least.Remove(least.Max());
                        least.Add(weight);
                    }
                }
            }
            if (totalWeights != 0)
                activatePercent /= totalWeights;
            else
                activatePercent = 1;
            //concat the two list
            greatest.AddRange(least);

            //activator = 0;
            foreach(double x in greatest)
            {
                Console.Write(x + ",");
                //activator += x;
            }
            Console.WriteLine();

            //if (GetData)
            {
                //if (activator > 0)
                if (action != "Select")
                {
                    double potential = GetPotentialBestChoice(win, GetData);
                    Console.WriteLine("Potential best choice:" + potential);
                    if (Math.Abs(potential) > 1)
                        activator = activator * 0.0 + potential;
                }
            }
            Random rand = new Random();

            if (rand.NextDouble() < 0.01 || (rand.NextDouble() < 0.05 && (totalGames < 10 || Duel.Turn > 25)))
                if (Logger.Name.ToLower() != Logger.master) 
            {
                //Logger.WriteLine("Random Variation");
                //activator = 999;
            }

            if (GetData)
            {
                Logger.WriteLine($"{activator} Activater");
                //Logger.WriteLine($"{gamesWon} games won");
                Logger.WriteLine($"{totalWon} Total won");
                Logger.WriteLine($"Number of games: {totalGames}");
            }
            else //recording action
            {
                //only if you activated it or its a select action
                //if (action == "Select" || win >= 0)
                Logger.SaveActionWeight(Duel.Turn, ActionId, win, Card?.Name, action, totalWon);
            }
            return activator;
            //return weight + totalGames * 0.001 * Math.Sign(weight);
        }

        /// <summary>
        /// Gets the potential best select option for the current card
        /// </summary>
        /// <returns></returns>
        private double GetPotentialBestChoice(double win = 0, bool GetData = true)
        {
            Dictionary<string, double> result_weights = new Dictionary<string, double>();
            string cardName = Card?.Name;
            string cardLocation = Card?.Location.ToString() + " " + Card?.Position.ToString();
            List<Logger.Data> row = Logger.GetDataSelect(cardName);

            foreach (Logger.Data data in row)
            {
                if (IsValidResult(data.result))
                {
                    if (IsValidCheck(data.verify, data.value, data.count))
                    {
                        if (result_weights.ContainsKey(data.result))
                        {
                            result_weights[data.result] += data.wins;

                            // For select action
                            if (data.result != "" && data.result.Split(';').Length == 4)
                            {
                                var owner = data.result.Split(';')[3];
                                result_weights[data.result] += SQLCom(GetData, Card?.Name, data.location, "Select", owner, "Owner", "", 0, win)[0];
                            }
                        }
                        else
                        {
                            result_weights.Add(data.result, data.wins);
                        }
                        SQLCom(GetData, Card?.Name, data.location, "Select", data.result, data.verify, data.value, data.count, win);
                        Console.WriteLine($"      Select weight:{data.result},{data.wins}");
                    }
                }
            }

            if (result_weights.Count == 0)
                return 0;
            return result_weights.Values.Max();
        }

        /// <summary>
        /// Checks if the Select result is valid
        /// </summary>
        /// <param name="result">the result to check for</param>
        /// <returns></returns>
        private bool IsValidResult(string result)
        {
            bool valid = true;

            if (result == "") return false;

            var parsed = result.Split(';');
            //Error will occur if the result is not 4!
            string id = "", location = "", position = "", owner = "";
            if (parsed.Length == 4)
            {
                id = parsed[0];
                location = parsed[1];
                position = parsed[2]; // unused
                owner = parsed[3];
            }
            else
                owner = result;

            if (owner == "0") // player card
            {
                switch (location)
                {
                    case "Deck": break;
                    case "Hand": valid = Bot.Hand.ContainsCardWithName(id); break;
                    case "MonsterZone":  valid = Bot.GetMonsters().ContainsCardWithName(id); break;
                    case "SpellZone": break;
                    case "Onfield": break;
                    case "Grave": break;
                    case "Removed": break;
                    case "Extra": break;
                    case "Overlay": break;
                    case "FieldZone": break;
                    case "PendulumZone": break;
                    default: break;//not a valid location so do nothing
                }
            }
            else //opponent's card
            {
                switch (location)
                {
                    case "Deck": break;
                    case "Hand": valid = Enemy.Hand.ContainsCardWithName(id); break;
                    case "MonsterZone": valid = Enemy.GetMonsters().ContainsCardWithName(id); break;
                    case "SpellZone": break;
                    case "Onfield": break;
                    case "Grave": break;
                    case "Removed": break;
                    case "Extra": break;
                    case "Overlay": break;
                    case "FieldZone": break;
                    case "PendulumZone": break;
                    default: break;//not a valid location so do nothing
                }
            }

            return valid;
        }

        /// <summary>
        /// Checks if the verify is valid
        /// </summary>
        /// <param name="verify"></param>
        /// <param name="value"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        private bool IsValidCheck(string verify, string value, int count)
        {
            bool valid = false;
            switch (verify)
            {
                case "Self": return true;
                case "Number of Monsters Opponent": valid = Enemy.GetMonsterCount() == int.Parse(value); break;
                case "Enemy Monsters": valid = Enemy.GetMonsters().ContainsCardWithName(value); break;
                case "Player Monsters": valid = Bot.GetMonsters().ContainsCardWithName(value); break;
                case "Number of Monsters Bot": valid = Bot.GetMonsterCount() == int.Parse(value); break;
                default: break;
            }
            return valid;
        }

        private List<double> SQLCom(bool GetData, string id, string location, string action, string result, string verify, string value, int count, double wins)
        {
            if (GetData)
            {
                return Logger.GetData(Logger.master,id, location, action, result, verify, value, count);
            }
            else // record action
            {
                Logger.RecordAction(id, location, action, result, verify, value, count, wins, Duel.Turn, ActionId);
                return new List<double>() { 0, 0 };
            }
        }

        private Dictionary<ClientCard,int> ListToQuantity(IList<ClientCard> clientCards)
        {
            Dictionary<string, CardToQuant> cardQuant = new Dictionary<string, CardToQuant>();
            foreach(ClientCard card in clientCards)
            {
                if (!cardQuant.ContainsKey(card.Name ?? "Set Monster"))
                {
                    cardQuant.Add(card.Name ?? "Set Monster", new CardToQuant() { card = card, quant = 1 });
                }
                else
                {
                    CardToQuant count;
                    cardQuant.TryGetValue(card.Name ?? "Set Monster", out count);
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