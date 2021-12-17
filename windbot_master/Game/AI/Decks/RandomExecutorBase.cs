using YGOSharp.OCGWrapper.Enums;
using System.Collections.Generic;
using WindBot;
using WindBot.Game;
using WindBot.Game.AI;
using System;
using System.Linq;
using WindBot.Game.AI.Decks.Util;

namespace WindBot.Game.AI.Decks
{
    public class RandomExecutorBase : DefaultExecutor
    {
        public RandomExecutorBase(GameAI ai, Duel duel)
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
        }

        class PreviousAction
        {
            public string Action = "";
            public string Value = "";
            public PreviousAction(string action, string value)
            {
                Action = action;
                Value = value;
            }
        }

        List<PreviousAction> ActionPerformedTurn = new List<PreviousAction>();
        Stack<ClientCard> ActivatedCardName = new Stack<ClientCard>();
        ActionWeight BestAction = null;

        int CardAdvStartSelfHand = 0;
        int CardAdvStartOppHand = 0;
        int CardAdvStartSelfHandPre = 0;
        int CardAdvStartOppHandPre = 0;
        int CardAdvStartSelfField = 0;
        int CardAdvStartOppField = 0;
        int CardAdvStartSelfFieldPre = 0;
        int CardAdvStartOppFieldPre = 0;
        public int ActionId = 0;
        int PlayerLpPre = 8000;
        int OppLpPre = 8000;
        int LpStart = 8000;
        double PreGameState = 0;
        double PreTurnWeight = 0;

        private List<long> HintMsgForEnemy = new List<long>
        {
            HintMsg.Release, HintMsg.Destroy, HintMsg.Remove, HintMsg.ToGrave, HintMsg.ReturnToHand, HintMsg.ToDeck,
            HintMsg.FusionMaterial, HintMsg.SynchroMaterial, HintMsg.XyzMaterial, HintMsg.LinkMaterial
        };

        private List<long> HintMsgForDeck = new List<long>
        {
            HintMsg.SpSummon, HintMsg.ToGrave, HintMsg.Remove, HintMsg.AddToHand, HintMsg.FusionMaterial
        };

        private List<long> HintMsgForSelf = new List<long>
        {
            HintMsg.Equip
        };

        private List<long> HintMsgForMaterial = new List<long>
        {
            HintMsg.FusionMaterial, HintMsg.SynchroMaterial, HintMsg.XyzMaterial, HintMsg.LinkMaterial, HintMsg.Release
        };

        private List<long> HintMsgForMaxSelect = new List<long>
        {
            HintMsg.SpSummon, HintMsg.ToGrave, HintMsg.AddToHand, HintMsg.FusionMaterial, HintMsg.Destroy
        };

        public override void SetMain(MainPhase main)
        {
            //ActionId++;
            base.SetMain(main);

            Choice choice = new Choice(this, Duel.Phase, Duel.Turn);
            if (main.CanBattlePhase && Duel.Fields[0].HasAttackingMonster())
                choice.SetBest(ExecutorType.GoToBattlePhase, null);       
            else
                choice.SetBest(ExecutorType.GoToEndPhase, null);

            if (Duel.Phase == DuelPhase.Main2)
                BestAction = choice.ReturnBestAction();

            //choice.SetBest(ExecutorType.GoToEndPhase, null);
            //loop through setable monsters
            foreach (ClientCard card in main.MonsterSetableCards)
            {
                choice.SetBest(ExecutorType.MonsterSet, card, card.ActionIndex[(int)ExecutorType.MonsterSet]);
            }
            //loop through cards that can change position
            foreach (ClientCard card in main.ReposableCards)
            {
                choice.SetBest(ExecutorType.Repos, card, card.ActionIndex[(int)ExecutorType.Repos]);
            }
            //Loop through normal summonable monsters
            foreach (ClientCard card in main.SummonableCards)
            {
                choice.SetBest(ExecutorType.Summon, card, card.ActionIndex[(int)ExecutorType.Summon]);
            }
            //loop through special summonable monsters
            foreach (ClientCard card in main.SpecialSummonableCards)
            {
                choice.SetBest(ExecutorType.SpSummon, card, card.ActionIndex[(int)ExecutorType.SpSummon]);
            }
            //loop through activatable cards
            for (int i = 0; i < main.ActivableCards.Count; ++i)
            {
                ClientCard card = main.ActivableCards[i];
                choice.SetBest(ExecutorType.Activate, card, card.ActionActivateIndex[main.ActivableDescs[i]]);
            }

            switch (choice.BestAction.Action)
            {
                case ExecutorType.Activate:
                    // _dialogs.SendActivate(choice.BestCard.Name);
                    break;
                case ExecutorType.Repos:
                    break;
                case ExecutorType.MonsterSet:
                    // _dialogs.SendSetMonster();
                    break;
                case ExecutorType.SpellSet:
                    break;
                case ExecutorType.SpSummon:
                    //_dialogs.SendSummon(choice.BestCard.Name);
                    break;
                case ExecutorType.Summon:
                    //_dialogs.SendSummon(choice.BestCard.Name);
                    break;
                default:
                    /*if (main.CanBattlePhase && Duel.Fields[0].HasAttackingMonster())
                    {
                        choice.RecordAction(ExecutorType.GoToBattlePhase, null, -1, 1);
                    }
                    else
                        choice.RecordAction(ExecutorType.GoToEndPhase, null, -1, 1);*/
                    break;
            }
            Console.WriteLine(choice.BestAction.Action + " " + choice.BestAction.Card?.Name);
            BestAction = choice.ReturnBestAction();
        }

        public override void SetBattle(BattlePhase battle)
        {
            base.SetBattle(battle);
            
            Choice choice = new Choice(this, Duel.Phase, Duel.Turn);

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
                choice.SetBest(ExecutorType.Activate, card, battle.ActivableDescs[i]);
            }

            if (choice.BestAction != null)
            {
                switch (choice.BestAction.Action)
                {
                    case ExecutorType.Activate:
                        // _dialogs.SendActivate(choice.BestCard.Name);
                        break;
                    default:
                        /*if (main.CanBattlePhase && Duel.Fields[0].HasAttackingMonster())
                        {
                            choice.RecordAction(ExecutorType.GoToBattlePhase, null, -1, 1);
                        }
                        else
                            choice.RecordAction(ExecutorType.GoToEndPhase, null, -1, 1);*/
                        break;
                }
                Console.WriteLine(choice.BestAction.Action + " " + choice.BestAction.Card?.Name);
                BestAction = choice.ReturnBestAction();
            }
            else
                BestAction = null;
        }

        public string BuildActionString(ExecutorType action, ClientCard card, string Phase)
        {
            if (Phase == "Main2")
                Phase = "Main1";
            string actionString = action.ToString();
            if (action == ExecutorType.Repos && card != null)
                actionString += $";{card.Position}";
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
                double advantageGain = GetCardAdvantageHand() + GetCardAdvantageField();
                double advantageGain2 = GetCardAdvantageHand() * 0.5 + GetCardAdvantageField();
                int w = GetCardAdvantageFieldPre();
                int ww = GetCardAdvantageHandPre();
                int advGainPre = GetCardAdvantageFieldPre() + GetCardAdvantageHandPre();
                int playerGain = playerHandGain - playerFieldLoss;
                int enemyGain = -enemyHandLoss - enemyFieldLoss;
                double playerLpLoss = PlayerLpPre - Bot.LifePoints;
                double oppLpLoss = OppLpPre - Enemy.LifePoints;

                /*SqlComm.RecordUpdateAction("cardAdvantage", cardAdvantage, (Duel.Turn % 2).ToString());
                SqlComm.RecordUpdateAction("cardAdvantagePre", cardAdvantagePre, (Duel.Turn % 2).ToString());
                SqlComm.RecordUpdateAction("fieldAdvantage", fieldAdvantage, (Duel.Turn % 2).ToString());
                SqlComm.RecordUpdateAction("enemyFieldLoss", enemyFieldLoss, (Duel.Turn % 2).ToString());
                SqlComm.RecordUpdateAction("playerFieldLoss", playerFieldLoss, (Duel.Turn % 2).ToString());
                SqlComm.RecordUpdateAction("advantageGain", advantageGain, (Duel.Turn % 2).ToString());
                SqlComm.RecordUpdateAction("advantageGain2", advantageGain2, (Duel.Turn % 2).ToString());
                SqlComm.RecordUpdateAction("advGainPre", advGainPre, (Duel.Turn % 2).ToString());
                SqlComm.RecordUpdateAction("playerGain", playerGain, (Duel.Turn % 2).ToString());
                SqlComm.RecordUpdateAction("enemyGain", enemyGain, (Duel.Turn % 2).ToString());*/

                int cardDiffPre = CardAdvStartSelfHand + CardAdvStartSelfField - CardAdvStartOppHand + CardAdvStartOppField;

                // number of actions taken 
                int turnActions = 1;
                int preTurnActions = 1;
                if (SqlComm.actionWeight.ContainsKey(Duel.Turn - 1))
                    turnActions = SqlComm.actionWeight[Duel.Turn - 1].Count(x => x.Key != -1);
                if (SqlComm.actionWeight.ContainsKey(Duel.Turn - 2))
                    preTurnActions = SqlComm.actionWeight[Duel.Turn - 2].Count(x => x.Key != -1);

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
                    //    SqlComm.ModifyAction(Duel.Turn - 1, oppLpLoss / 1000.0, 1);
                    double weight = 0;// -playerFieldLoss
                    weight += (Math.Sign(enemyFieldLoss) - Math.Sign(playerFieldLoss) * 0.5) * 0.5;
                    weight += (advantageGain2) * 0.5;

                    //weight +=cardAdvantage;
                    //weight += oppLpLoss / 2000.0;

                    if (Duel.Turn > 2) weight--;
                    if (Math.Abs(weight) > 0)
                    {
                        SqlComm.RecordActual(Duel.Turn - 1, weight, 1);
                        // Save weight for debugging
                        SqlComm.SaveActionWeight(Duel.Turn - 1, -1, weight, "Result", "", 0);
                    }
                    //SqlComm.SetTreeNodeResult(Duel.Turn, weight);
                    PreTurnWeight = weight;
                }

                if (Duel.Turn % 2 == (Duel.IsFirst ? 1 : 0))//Calculate advatage of two turns
                {

                    //SqlComm.ModifyAction(Duel.Turn - 2, advGainPre, 2);
                    //if (cardDiff >= 0)
                    {
                        double weight = 0;
                        //weight += (enemyFieldLoss - playerFieldLoss * 0.5) * 0.5;
                        weight += advantageGain;

                        //weight += cardAdvantage;
                        //if (Duel.Turn != 3)
                        //    weight -= playerLpLoss / 2000.0;

                        if (Math.Abs(weight) > 0)
                        {
                            SqlComm.RecordActual(Duel.Turn - 2, weight, 2);
                            SqlComm.RecordActual(Duel.Turn - 1, weight, 2);
                            // Save weight for debugging
                            SqlComm.SaveActionWeight(Duel.Turn - 1, -1, weight, "Result", "", 0);
                        }
                        SqlComm.TreeActivation.UpdateNode(Duel.Turn - 2, weight + PreTurnWeight);
                        SqlComm.TreeActivation.UpdateNode(Duel.Turn - 1, weight + PreTurnWeight);
                        PreTurnWeight = weight;
                    }
                }
            }

            //reset
            base.OnNewTurn();
            ActionPerformedTurn.Clear();
            ActivatedCardName.Clear();

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
            BestAction = null;
            Console.WriteLine("-------------Turn:" + Duel.Turn + "-------------");
        }

        public override void OnChainEnd()
        {
            base.OnChainEnd();
            ActivatedCardName.Clear();
        }

        public bool ShouldRepos()
        {
            return DefaultMonsterRepos();//ShouldPerformAction($"Repos from {Card.Position} {Duel.Phase.ToString()}",DefaultMonsterRepos());
        }

        protected bool ShouldPerform()
        {
            bool result = false;
            if (BestAction != null)
                result = Card == BestAction.Card && Type == BestAction.Action;
            else if (Card != null && Type == ExecutorType.Activate)
            {
                ActionId++;
                string phase = Duel.Phase.ToString();
                if (Duel.Phase == DuelPhase.Main2)
                    phase = DuelPhase.Main1.ToString();
                string actionString = BuildActionString(Type, Card, phase);
                double? weight = null;

                if (SqlComm.IsTraining)
                {
                    List<double> weights = SqlComm.TreeActivation.GetTreeNode(Duel.Turn, ActionId, Card?.Name, actionString, Duel.IsFirst);
                    if (weights.Count > 0)
                    {
                        weight = weights[0];
                    }
                }
                else
                {
                    weight = ActionWeight(actionString);
                }

                result = weight >= 0 || weight == null;

                if (result && Card != null )
                {
                    RecordAction(actionString, "", weight ?? 0);
                    SqlComm.TreeActivation.SaveTreeNode(Duel.Turn, ActionId, Card?.Name, actionString, weight, Duel.IsFirst);
                }
            }

            if (result)
            {
                //ActionId++;
                BestAction = null;
            }
            return result;
        }

        public override bool OnSelectHand()
        {
            bool choice = Program.Rand.Next(2) > 0;

            List<double> weights = SqlComm.GetData(SqlComm.master, action: "GoFirst");

            if (weights.Count > 0)
                choice = weights[0] > 0 ? true : false;

            if (SqlComm.IsTraining)
                choice = true;

            SqlComm.RecordAction(action: "GoFirst", activation: choice ? 1 : -1, turn: Duel.Turn);

            // Set Lp
            /*PlayerLpPre = Bot.LifePoints;
            OppLpPre = Enemy.LifePoints;
            LpStart = Bot.LifePoints;*/

            return choice;
        }
        public override IList<ClientCard> OnSelectCard(IList<ClientCard> _cards, int min, int max, long hint, bool cancelable)
        {
            if (Duel.Phase == DuelPhase.BattleStart)
                return null;
            if (AI.HaveSelectedCards())
                return null;
            //ActionId++;
            SetCard(ExecutorType.Activate, null, 0);

            double MAX = 4;
            IList<ClientCard> selected = new List<ClientCard>();
            IList<ClientCard> cards = new List<ClientCard>(_cards);

            List<ClientCardWeight> selected_card = new List<ClientCardWeight>();
            if (max > cards.Count)
                max = cards.Count;

            // AI Selection

            int numToSelect = min;
            double? maxWeight = 0;

            //get number of cards to select
            for (int i = min; i <= max; i++)
            {
                double? weight = GetWeight("Number Selected", i.ToString()) ?? 0;
                if (weight == null || (maxWeight != null && weight >= maxWeight))
                {
                    maxWeight = weight;
                    numToSelect = i;
                }
            }

            List<ClientCardWeight> cardWeight = new List<ClientCardWeight>();

            // Add Top choices
            foreach (ClientCard clientCard in cards)
            {
                int actionId = ++ActionId;
                string action = $"Select" + hint.ToString();
                double? weight = GetWeight(action, SelectStringBuilder(clientCard)) ?? MAX;
                Console.WriteLine(string.Format($"    {actionId} {(weight != null ? weight.ToString() : "null")} | {action} {clientCard?.Name}"));
                cardWeight.Add(new ClientCardWeight()
                {
                    Card = clientCard,
                    Weight = weight ?? MAX,
                    Quantity = 0,
                    ActionId = actionId
                });
            }

            cardWeight = cardWeight/*.Where(card => card.Weight >= 1)*/.OrderBy(card => card.Weight).Reverse().ToList();

            foreach(ClientCardWeight card in cardWeight)
            {
                if (selected.Count >= min)
                    break;
                //ClientCardWeight card = cardWeight[0];
                //selected_card.Add(card);
                selected.Add(card.Card);
                //cardWeight.Remove(card);
                cards.Remove(card.Card);
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
            foreach (ClientCard card in selected)
            {
                double? weight = 1;
                if (selected.IndexOf(card) < selected_card.Count)
                    weight = selected_card[selected.IndexOf(card)].Weight;
                if (weight == MAX)
                    weight = null;
                RecordAction($"Select" + hint.ToString(), SelectStringBuilder(card), weight ?? 0);
                if (SqlComm.IsTraining)
                {
                    SqlComm.TreeActivation.SaveTreeNode(Duel.Turn, cardWeight.Find(x => x.Card.Id == card.Id).ActionId, Card?.Name, $"Select" + hint.ToString() + SelectStringBuilder(card), weight, Duel.IsFirst);
                }
                Console.WriteLine($"Choosing {SelectStringBuilder(card)} for Action {hint}");
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

        private string SelectStringBuilder(long hint)
        {
            return hint.ToString();
        }

        private class ClientCardWeight
        {
            public ClientCard Card;
            public double Weight;
            public int Quantity;
            public int ActionId;
        }

        public override CardPosition OnSelectPosition(int cardId, IList<CardPosition> positions)
        {
            //Remove later
            if (positions.Contains(CardPosition.FaceUpAttack))
                return CardPosition.FaceUpAttack;
            CardPosition pos = positions[Program.Rand.Next(positions.Count)];
            double greatestWeight = 1;

            foreach (CardPosition position in positions)
            {
                double weight = GetWeight($"Position", position.ToString())??0;
                if (weight > greatestWeight)
                {
                    weight = greatestWeight;
                    pos = position;
                }
            }
            Console.WriteLine("Position "+pos.ToString());
            RecordAction($"Position", pos.ToString());
            return pos;
        }

        public override int OnSelectOption(IList<long> options)
        {
            int option = -1;
            double greatestWeight = 1;
            for (int choice = 0; choice < options.Count; choice++)
            {
                double weight = GetWeight($"Option", choice.ToString())??0;
                if (weight > greatestWeight)
                {
                    greatestWeight = weight;
                    option = choice;
                }
            }

            if (option == -1)
            {
                option = Program.Rand.Next(options.Count);
            }

            RecordAction($"Option", option.ToString());

            return option;
        }

        /// <summary>
        /// Checks if the bot should perform the given action
        /// </summary>
        /// <param name="action">action given</param>
        /// <returns>true if it should perform.</returns>
        public double ActionWeight(string action)
        {
            return GetWeight(action, "")??0;
        }

        public void RecordAction(string action, string result, double wins = 1)
        {
            if (!SqlComm.TreeActivation.ShouldSave())
                return;
            DataModifier(action, result, wins, false);
        }

        public double? GetWeight(string action, string result)
        {
            double? weight = null;
            if (SqlComm.IsTraining)
            {
                List<double> weights = SqlComm.TreeActivation.GetTreeNode(Duel.Turn, ActionId, Card?.Name, action + result, Duel.IsFirst);
                if (weights.Count > 0)
                    weight = weights[0];
            }
            else
            {
                weight = DataModifier(action, result);
            }
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
            foreach (ClientCard CardInHand in cardQuant.Keys)
            {
                score.AddRange(SQLCom(GetData, Card?.Name, Card?.Location.ToString() + " " + Card?.Position.ToString(), action, result, "Card In Hand", CardInHand.Name.ToString(), cardQuant[CardInHand], win));
            }
            
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
            
            if (Bot.GetMonsterCount() == 0)
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
            /*if (SqlComm.actionWeight.ContainsKey(Duel.Turn))
                foreach (SqlComm.ActionWeightCard previous in SqlComm.actionWeight[Duel.Turn].Values)
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

                    //if (Math.Abs(weight) == 0)
                    //    weight = Program.Rand.Next(2) - 1;

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
                //Console.Write("     " + x + ",");
                //activator += x;
            }
            //Console.WriteLine();

            //if (GetData)
            {
                //if (activator > 0)
                if (action != "Select")
                {
                    double potential = 0;// GetPotentialBestChoice(win, GetData);
                    //Console.WriteLine("     Potential best choice:" + potential);
                    if (Math.Abs(potential) > 1)
                        activator = activator * 0.0 + potential;
                }
            }

            if (Program.Rand.NextDouble() < 0.01 || (Program.Rand.NextDouble() < 0.05 && (totalGames < 10 || Duel.Turn > 25)))
                if (SqlComm.Name.ToLower() != SqlComm.master) 
            {
                //Console.WriteLine("Random Variation");
                //activator = 999;
            }

            if (GetData)
            {
                Console.WriteLine($"    {activator} Activater");
                //Console.WriteLine($"{gamesWon} games won");
                Console.WriteLine($"    {totalWon} Total won");
                Console.WriteLine($"    Number of games: {totalGames}");
            }
            else //recording action
            {
                //only if you activated it or its a select action
                //if (action == "Select" || win >= 0)
                SqlComm.SaveActionWeight(Duel.Turn, ActionId, win, Card?.Name, action, totalWon);
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
            List<SqlComm.Data> row = SqlComm.GetDataSelect(cardName);

            foreach (SqlComm.Data data in row)
            {
                if (IsValidResult(data.result))
                {
                    if (IsValidCheck(data.verify, data.value, data.count))
                    {
                        if (result_weights.ContainsKey(data.result))
                        {
                            result_weights[data.result] += data.activation;

                            // For select action
                            if (data.result != "" && data.result.Split(';').Length == 4)
                            {
                                var owner = data.result.Split(';')[3];
                                result_weights[data.result] += SQLCom(GetData, Card?.Name, data.location, "Select", owner, "Owner", "", 0, win)[0];
                            }
                        }
                        else
                        {
                            result_weights.Add(data.result, data.activation);
                        }
                        SQLCom(GetData, Card?.Name, data.location, "Select", data.result, data.verify, data.value, data.count, win);
                        Console.WriteLine($"      Select weight:{data.result},{data.activation}");
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
                return SqlComm.GetData(SqlComm.master,id, location, action, result, verify, value, count);
            }
            else // record action
            {
                SqlComm.RecordAction(id, location, action, result, verify, value, count, wins, Duel.Turn, ActionId);
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
