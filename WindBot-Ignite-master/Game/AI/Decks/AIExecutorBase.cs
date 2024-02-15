using YGOSharp.OCGWrapper.Enums;
using System.Collections.Generic;
using WindBot;
using WindBot.Game;
using WindBot.Game.AI;
using static WindBot.MCST;
using static WindBot.NEAT;
using System.Linq;
using System;

namespace WindBot.Game.AI.Decks
{
    public class AIExecutorBase : DefaultExecutor
    {
        PlayHistory History;
        PlayHistory.ActionInfo BestAction;
        int ActionNumber = 0;

        MCST.Node NextAction;
        MCST Tree;

        public AIExecutorBase(GameAI ai, Duel duel)
            : base(ai, duel)
        {
            AddExecutor(ExecutorType.Activate, ShouldPerform);
            AddExecutor(ExecutorType.SpSummon, ShouldPerform);
            AddExecutor(ExecutorType.Summon, ShouldPerform);
            AddExecutor(ExecutorType.MonsterSet, ShouldPerform);
            AddExecutor(ExecutorType.SummonOrSet, ShouldPerform);
            //AddExecutor(ExecutorType.Repos, ShouldPerform);
            AddExecutor(ExecutorType.SpellSet, ShouldPerform);


            //AddExecutor(ExecutorType.GoToBattlePhase, ShouldPerform);
            //AddExecutor(ExecutorType.GoToMainPhase2, ShouldPerform);
            //AddExecutor(ExecutorType.GoToEndPhase, ShouldPerform);
            AddExecutor(ExecutorType.Repos, DefaultMonsterRepos);

            History = new PlayHistory();
            if (SQLComm.IsMCTS)
                Tree = new MCST();
        }

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
            base.SetMain(main);

            List<PlayHistory.ActionInfo> actions = new List<PlayHistory.ActionInfo>();

            if (Duel.Phase == DuelPhase.Main2)
            {
                //return;
            }

            ActionNumber++;
            var gameInfo = History.GenerateGameInfo(SQLComm.Id, Duel.Turn, ActionNumber);
            var compare = GetComparisons();

            foreach (ClientCard card in main.MonsterSetableCards)
            {
                actions.Add(History.GenerateActionInfo(BuildActionString(card, Duel.Phase.ToString()), ExecutorType.MonsterSet, card));
                if (SQLComm.IsMCTS)
                    Tree.AddPossibleAction(BuildActionString(card, Duel.Phase.ToString()), ExecutorType.MonsterSet.ToString());
                //card.ActionIndex[(int)ExecutorType.MonsterSet];
            }
            //loop through cards that can change position
            foreach (ClientCard card in main.ReposableCards)
            {
                //Tree.AddPossibleAction(BuildActionString(card, Duel.Phase.ToString()), ExecutorType.Repos.ToString(), Duel.Fields, Duel.Turn);
            }
            //Loop through normal summonable monsters
            foreach (ClientCard card in main.SummonableCards)
            {
                actions.Add(History.GenerateActionInfo(BuildActionString(card, Duel.Phase.ToString()), ExecutorType.Summon, card));
                if (SQLComm.IsMCTS)
                    Tree.AddPossibleAction(BuildActionString(card, Duel.Phase.ToString()), ExecutorType.Summon.ToString());
            }
            //loop through special summonable monsters
            foreach (ClientCard card in main.SpecialSummonableCards)
            {
                actions.Add(History.GenerateActionInfo(BuildActionString(card, Duel.Phase.ToString()), ExecutorType.SpSummon, card));
                if (SQLComm.IsMCTS)
                    Tree.AddPossibleAction(BuildActionString(card, Duel.Phase.ToString()), ExecutorType.SpSummon.ToString());
            }
            //loop through activatable cards
            for (int i = 0; i < main.ActivableCards.Count; ++i)
            {
                ClientCard card = main.ActivableCards[i];
                actions.Add(History.GenerateActionInfo(BuildActionString(card, Duel.Phase.ToString()), ExecutorType.Activate, card));
                if (SQLComm.IsMCTS)
                    Tree.AddPossibleAction(BuildActionString(card, Duel.Phase.ToString()), ExecutorType.Activate.ToString());
                //choice.SetBest(ExecutorType.Activate, card, card.ActionActivateIndex[main.ActivableDescs[i]]);
            }
            //loop through setable cards
            for (int i = 0; i < main.SpellSetableCards.Count; ++i)
            {
                ClientCard card = main.SpellSetableCards[i];
                actions.Add(History.GenerateActionInfo(BuildActionString(card, Duel.Phase.ToString()), ExecutorType.SpellSet, card));
                if (SQLComm.IsMCTS)
                    Tree.AddPossibleAction(BuildActionString(card, Duel.Phase.ToString()), ExecutorType.SpellSet.ToString());
            }


            if (main.CanBattlePhase)
            {
                actions.Add(History.GenerateActionInfo("", ExecutorType.GoToBattlePhase, null));
                if (SQLComm.IsMCTS)
                    Tree.AddPossibleAction("", ExecutorType.GoToBattlePhase.ToString());
            }
            else if (main.CanEndPhase)
            {
                actions.Add(History.GenerateActionInfo("", ExecutorType.GoToEndPhase, null));
                if (SQLComm.IsMCTS)
                    Tree.AddPossibleAction("", ExecutorType.GoToEndPhase.ToString());
            }

            if (SQLComm.IsMCTS)
                NextAction = Tree.GetNextAction(compare);

            BestAction = GetBestAction(actions, compare);
            if (BestAction != null)
                BestAction.Performed = true;

            History.AddHistory(History.GenerateHistory(gameInfo, compare, actions), Duel);
        }

        public override void SetChain(IList<ClientCard> cards, IList<long> descs, bool forced)
        {
            base.SetChain(cards, descs, forced);
            List<PlayHistory.ActionInfo> actions = new List<PlayHistory.ActionInfo>();


            ActionNumber++;
            var gameInfo = History.GenerateGameInfo(SQLComm.Id, Duel.Turn, ActionNumber);
            var compare = GetComparisons();

            for (int i = 0; i < cards.Count; i++)
            {
                actions.Add(History.GenerateActionInfo(BuildActionString(cards[i], Duel.Phase.ToString()) + "ShouldChain", ExecutorType.Activate, cards[i]));
                if (SQLComm.IsMCTS)
                    Tree.AddPossibleAction(BuildActionString(cards[i], Duel.Phase.ToString()) + "ShouldChain", ExecutorType.Activate.ToString());
            }

            if (!forced)
            {
                actions.Add((History.GenerateActionInfo(Duel.Phase.ToString() + "DontChain", ExecutorType.Activate, null)));
                if (SQLComm.IsMCTS)
                    Tree.AddPossibleAction(Duel.Phase.ToString() + "DontChain", ExecutorType.Activate.ToString());
            }

            if (SQLComm.IsMCTS)
                NextAction = Tree.GetNextAction(compare);

            BestAction = GetBestAction(actions, compare);
            if (BestAction != null)
                BestAction.Performed = true;

            History.AddHistory(History.GenerateHistory(gameInfo, compare, actions), Duel);
        }

        private PlayHistory.ActionInfo GetBestAction(List<PlayHistory.ActionInfo> actions, List<PlayHistory.CompareTo> comparisons)
        {
            if (actions.Count == 0)
                return null;

            List<long> input = new List<long>();
            List<long> data = new List<long>();

            foreach (var i in actions)
            {
                input.Add(i.ActionId);
            }
            foreach (var i in comparisons)
            {
                data.Add(i.Id);
            }
            List<double> best_predict = new List<double>(HttpComm.GetBestActionAsync(input, data, SQLComm.Name).Result);

            var max_id = (int)actions.Max(x => x.ActionId);
            if (best_predict.Count == 0)
                best_predict = new List<double>(new double[max_id + 1]);

            if (SQLComm.IsTraining)
            {
                List<double> base_values = CSVReader.GetBaseActionValues(best_predict.Count, actions, comparisons);
                for (var i = 0; i < best_predict.Count; i++)
                    best_predict[i] += base_values[i];
            }

            //Get Best Action

            if (!SQLComm.IsManual)
            {
                Console.WriteLine("Actions for Turn " + Duel.Turn + " Action # " + ActionNumber);

                foreach(var action in actions)
                {

                    //set go to next phase as lowest prio
                    if((action.Action == ExecutorType.GoToBattlePhase || action.Action == ExecutorType.GoToEndPhase))
                    {
                        best_predict[(int)action.ActionId] = Math.Min(0.5, best_predict[(int)action.ActionId]);
                    }

                    //Console.WriteLine(" " + action.ActionId + ":" + action.ToString());
                    if (best_predict.Count > 0 && best_predict.Count > (int)action.ActionId)
                        Console.WriteLine(" " + action.ActionId + ":" + action.ToString() + " Weight:" + best_predict[(int)action.ActionId].ToString("#.#####"));
                }

                //var chosen = actions[Rand.Next(actions.Count)];
                var chosen = actions[0];

                var result = best_predict.Select((v, i) => new { v, i }).OrderByDescending(x => x.v).ThenByDescending(x => x.i).Where(x => actions.Find(y => y.ActionId == x.i) != null).ToList();

                if (result.Count > 0)
                {
                    // Take the top percentile
                    var max_guess = result.Max(x => x.v);
                    result = result.Where(x => x.v >= max_guess - 0.1).ToList();
                }

                bool selected = false;
                while (!selected && result.Count > 0)
                {
                    var index = 0;// Rand.Next(result.Count);
                    var best = result[index];
                    if (actions.Find(x => x.ActionId == best.i) != null)
                    {
                        chosen = actions.Find(x => x.ActionId == best.i);
                        selected = true;
                    }
                    else
                    {
                        result.RemoveAt(index);
                    }
                    /*if ((chosen.Action == ExecutorType.GoToEndPhase || chosen.Action == ExecutorType.GoToBattlePhase) && actions.Count >= 3)
                    {
                        selected = false;
                        result.RemoveAt(index);
                    }*/
                }

                Console.WriteLine("Chose " + ":" + chosen.ToString());
                return chosen;
            }


            if (SQLComm.IsManual)
            {
                Console.WriteLine("Current State:---------");
                comparisons.Reverse();
                foreach (var i in comparisons)
                {
                    Console.WriteLine("     " + i.ToString());
                }
                Console.WriteLine("Select an action for Turn " + Duel.Turn + " Action # " + ActionNumber);
                foreach (var action in actions)
                {
                    double weight = 0;
                    if (best_predict.Count > (int)action.ActionId)
                        weight = best_predict[(int)action.ActionId];
                    Console.WriteLine(weight + ":" + action.ToString());
                }

                string str = "";

                foreach (var c in comparisons)
                {
                    str += c.Id + " ";
                }

                Console.WriteLine(str);

                for (int i = 0; i < actions.Count; i ++)
                {
                    Console.WriteLine(" " + i + ":" + actions[i].ToString());
                }

                int choice = -1;
                while(choice == -1)
                {
                    int result;
                    if (int.TryParse(Console.ReadLine(), out result))
                    {
                        if (result >= 0 && result < actions.Count)
                        {
                            choice = result;
                        }
                    }
                }
                Console.WriteLine("Chose " + choice + ":" + actions[choice].ToString());

                return actions[choice];
            }

            // Else automatic training
            return null;
        }

        private List<PlayHistory.CompareTo> GetComparisons()
        {
            List<PlayHistory.CompareTo> c = new List<PlayHistory.CompareTo>();

            //c.Add(History.GenerateComparasion("", "Turn", Duel.Turn.ToString()));
            c.Add(History.GenerateComparasion("", "ActionNumber", ActionNumber.ToString()));

            c.Add(History.GenerateComparasion("", "PlayerFieldCount", Duel.Fields[0].GetMonsterCount().ToString()));
            c.Add(History.GenerateComparasion("", "EnemyFieldCount", Duel.Fields[1].GetMonsterCount().ToString()));

            //Hand
            for(int i = 0; i < 1; i++)
                foreach(var card in Duel.Fields[i].Hand)
                {
                    c.Add(History.GenerateComparasion(
                        (i == 0 ? "Player" : "Enemy") + "Hand",
                        card?.Name ?? "",
                        ""));
                }

            // Monster Zone
            for (int i = 0; i <= 1; i++)
                foreach (var card in Duel.Fields[i].MonsterZone)
                {
                    if (card != null)
                    {
                        c.Add(History.GenerateComparasion(
                            (i == 0 ? "Player" : "Enemy") + "Field",
                            (card?.Name ?? "") + ";" + (CardPosition)card?.Position,
                            ""));
                    }
                }

            // Spell Trap Zone
            for (int i = 0; i <= 1; i++)
                foreach (var card in Duel.Fields[i].SpellZone)
                {
                    if (card != null)
                    {
                        c.Add(History.GenerateComparasion(
                            (i == 0 ? "Player" : "Enemy") + "SpellTrap",
                            (card?.Name ?? "") + ";" + (CardPosition)card?.Position,
                            ""));
                    }
                }

            // GraveYard
            for (int i = 0; i <= 1; i++)
                foreach (var card in Duel.Fields[i].Graveyard)
                {
                    if (card != null)
                    {
                        c.Add(History.GenerateComparasion(
                            (i == 0 ? "Player" : "Enemy") + "GY",
                            (card?.Name ?? ""),
                            ""));
                    }
                }

            // Banished
            for (int i = 0; i <= 1; i++)
                foreach (var card in Duel.Fields[i].Banished)
                {
                    if (card != null)
                    {
                        c.Add(History.GenerateComparasion(
                            (i == 0 ? "Player" : "Enemy") + "Banish",
                            (card?.Name ?? ""),
                            ""));
                    }
                }

            // Previous Turn Actions
            {
                foreach (var history in History.CurrentTurn)
                {
                    var action = history.ActionInfo.Find(x => x.Performed);
                    if (action == null)
                    {
                        continue;
                    }
                    c.Add(History.GenerateComparasion(
                        "PreviousAction",
                        (action.ToString()),
                        ""));
                }
            }


            // Last Card On Chain
            {
                int i = Duel.LastChainPlayer;
                var card = Util.GetLastChainCard();
                c.Add(History.GenerateComparasion(
                            (i == 0 ? "Player" : "Enemy") + "LastChain",
                            (card?.Name ?? ""),
                            ""));
            }

            return c;
        }

        public override void SetBattle(BattlePhase battle)
        {
            base.SetBattle(battle);

            ActionNumber++;
            var gameInfo = History.GenerateGameInfo(SQLComm.Id, Duel.Turn, ActionNumber);
            var compare = GetComparisons();
            List<PlayHistory.ActionInfo> actions = new List<PlayHistory.ActionInfo>();

            if (battle.CanMainPhaseTwo) // check if should enter main phase 2 directly
            {
                //actions.Add(History.GenerateActionInfo("", ExecutorType.GoToMainPhase2, null));
            }
            if (battle.CanEndPhase) // check if should enter end phase directly
            {
                //actions.Add(History.GenerateActionInfo("", ExecutorType.GoToEndPhase, null));
            }

            if (battle.ActivableCards.Count > 0)
            {
                for (int i = 0; i < battle.ActivableCards.Count; ++i)
                {
                    ClientCard card = battle.ActivableCards[i];
                    actions.Add(History.GenerateActionInfo(BuildActionString(card, Duel.Phase.ToString()), ExecutorType.Activate, card));
                    //choice.SetBest(ExecutorType.Activate, card, battle.ActivableDescs[i]);
                    if (SQLComm.IsMCTS)
                        Tree.AddPossibleAction(BuildActionString(card, Duel.Phase.ToString()), ExecutorType.Activate.ToString());
                }
            }

            if (SQLComm.IsMCTS)
                NextAction = Tree.GetNextAction(compare);

            BestAction = GetBestAction(actions, compare);
            if (BestAction != null)
                BestAction.Performed = true;

            History.AddHistory(History.GenerateHistory(gameInfo, compare, actions), Duel);
        }

        public override bool OnSelectHand()
        {
            bool choice = SQLComm.IsFirst;
            //Logger.WriteLine(SQLComm.Name + " Is First " + choice);
            return choice;
        }

        public override void OnNewTurn()
        {
            base.OnNewTurn();
            ActionNumber = 0;
            History.EndOfTurn(Duel);
        }

        public override void OnNewPhase()
        {
            base.OnNewPhase();
            BestAction = null;
            ActionNumber = 0;
        }

        public override bool OnSelectYesNo(long desc)
        {
            ActionNumber++;
            var gameInfo = History.GenerateGameInfo(SQLComm.Id, Duel.Turn, ActionNumber);
            var compare = GetComparisons();
            List<PlayHistory.ActionInfo> actions = new List<PlayHistory.ActionInfo>();

            var option = Util.GetOptionFromDesc(desc);
            var cardId = Util.GetCardIdFromDesc(desc);

            actions.Add(History.GenerateActionInfo($"Yes;{cardId};{option}", ExecutorType.Activate, null));
            actions.Add(History.GenerateActionInfo($"No;{cardId};{option}", ExecutorType.Activate, null));

            if (SQLComm.IsMCTS)
            {
                Tree.AddPossibleAction($"Yes;{cardId};{option}", ExecutorType.Activate.ToString());
                Tree.AddPossibleAction($"No;{cardId};{option}", ExecutorType.Activate.ToString());
            }

            var yes = true;
            var result = GetBestAction(actions, compare);
            if (result != null)
                result.Performed = true;

            History.AddHistory(History.GenerateHistory(gameInfo, compare, actions), Duel);
            yes = result.Name.Contains("Yes");

            if (SQLComm.IsMCTS)
            {
                NextAction = Tree.GetNextAction(compare);
                yes = NextAction.CardId.Contains("Yes");

            }

            return yes;
        }

        public override IList<ClientCard> OnSelectCard(IList<ClientCard> _cards, int min, int max, long hint, bool cancelable)
        {
            //if (Duel.Phase == DuelPhase.BattleStart)
            //    return null;
            if (AI.HaveSelectedCards())
                return null;

            IList<ClientCard> selected = new List<ClientCard>();
            IList<ClientCard> cards = new List<ClientCard>(_cards);
            // AI Selection

            ActionNumber++;
            List<PlayHistory.ActionInfo> actions = new List<PlayHistory.ActionInfo>();
            var gameInfo = History.GenerateGameInfo(SQLComm.Id, Duel.Turn, ActionNumber);
            var compare = GetComparisons();

            int numToSelect = min;

            //get number of cards to select
            for (int i = min; i < max; i++)
            {
                //Tree.AddPossibleAction(i.ToString(), "NumberSelected", Duel.Fields, Duel.Turn);
            }

            foreach (ClientCard clientCard in cards)
            {
                string action = $"Select" + hint.ToString();
                string card = SelectStringBuilder(clientCard);
                actions.Add(History.GenerateActionInfo(card + ";" + hint.ToString(), ExecutorType.Select, clientCard));
                //Tree.AddPossibleAction(card, action, Duel.Fields, Duel.Turn);
                if (SQLComm.IsMCTS)
                    Tree.AddPossibleAction(card + ";" + hint.ToString(), ExecutorType.Select.ToString(), clientCard);

            }

            if (SQLComm.IsMCTS)
            {
                while (selected.Count < min)
                {
                    ClientCard card = Tree.GetNextAction(compare, true).Card; //cards[Program.Rand.Next(cards.Count)];
                    selected.Add(card);
                    cards.Remove(card);
                }
            }
            else
            {
                var choice = GetBestAction(actions, compare);
                if (choice != null)
                {
                    choice.Performed = true;
                    selected.Add(choice.Card);
                    cards.Remove(choice.Card);
                }
            }

            History.AddHistory(History.GenerateHistory(gameInfo, compare, actions), Duel);

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

            if (SQLComm.IsMCTS)
                Tree.Clear();

            return selected;
        }

        public override int OnSelectOption(IList<long> options)
        {
            ActionNumber++;
            var info = History.GenerateGameInfo(SQLComm.Id, Duel.Turn, ActionNumber);
            var compare = GetComparisons();
            string cardName = BuildActionString(Card, Duel.Phase.ToString());
            List<PlayHistory.ActionInfo> actions = new List<PlayHistory.ActionInfo>();

            foreach (long o in options)
            {
                var option = Util.GetOptionFromDesc(o);
                var cardId = Util.GetCardIdFromDesc(o);
                actions.Add(History.GenerateActionInfo($"{cardId};{option}", ExecutorType.Select, Card));
                if (SQLComm.IsMCTS)
                    Tree.AddPossibleAction($"{cardId};{option}", ExecutorType.Select.ToString());
            }

            var bestName = "";
            var best = GetBestAction(actions, compare);
            if (best == null)
            {
                base.OnSelectOption(options);
            }

            best.Performed = true;
            History.AddHistory(History.GenerateHistory(info, compare, actions), Duel);
            bestName = best.Name;

            if (SQLComm.IsMCTS)
            {
                NextAction = Tree.GetNextAction(compare);
                bestName = NextAction.CardId;
            }

            return options.IndexOf(long.Parse(bestName.Split(';')[0]));
        }

        public bool ShouldPerform()
        {
            if (SQLComm.IsMCTS)
            {
                if (NextAction != null)
                {
                    string id = NextAction.CardId.Split(';')[0];
                    string action = NextAction.Action.ToString();
                    //ActivateDescription
                    if (Card.Name == id && Type.ToString() == action)
                    {
                        NextAction = null;
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
                else
                {
                    return Tree.ShouldActivate(BuildActionString(Card, Duel.Phase.ToString()), Type.ToString(), GetComparisons());
                }
            }

            if (BestAction != null)
            {
                string id = BestAction.Name.Split(';')[0];
                string action = BestAction.Action.ToString();
                //ActivateDescription
                if (Card.Name == id && Type.ToString() == action)
                {
                    BestAction = null;
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                ActionNumber++;
                var info = History.GenerateGameInfo(SQLComm.Id, Duel.Turn, ActionNumber);
                var compare = GetComparisons();
                string cardName = BuildActionString(Card, Duel.Phase.ToString());
                PlayHistory.ActionInfo action = History.GenerateActionInfo(BuildActionString(Card, Duel.Phase.ToString()), Type, Card);
                List<PlayHistory.ActionInfo> actions = new List<PlayHistory.ActionInfo>
                {
                    action,
                    History.GenerateActionInfo(cardName + "[No]", Type, Card)
                };

                var best = GetBestAction(actions, compare);
                if (best == null)
                {
                    return false;
                }
                //if (!best.Name.Contains("[No]"))
                {
                    best.Performed = true;
                }

                History.AddHistory(History.GenerateHistory(info, compare, actions), Duel);

                return action.Performed;
            }
        }

        public override void OnWin(int result)
        {
            if (SQLComm.IsMCTS)
            {
                SQLComm.ShouldUpdate = false;
                if (SQLComm.GamesPlayed >= SQLComm.TotalGames - 1 && SQLComm.TotalGames > 0)
                {
                    SQLComm.ShouldUpdate = true;
                    int index = Tree.Path.Count;
                    if (index < History.Records.Count)
                        History.Records.RemoveRange(index, History.Records.Count - index);
                    else
                        Logger.WriteErrorLine("History entry is less than tree entry?");
                }

                Tree.OnGameEnd(result, Duel);
                Tree.OnNewGame();
            }
            History.SaveHistory(result);
            History.Records.Clear();
        }

        private string SelectStringBuilder(ClientCard Card, int Quant = 1)
        {
            return $"{Card.Name ?? "Set Monster" };{Card?.Location.ToString()};{Card?.Position.ToString()};{Card.Controller}";// x{Quant}";
        }

        private string BuildActionString(ExecutorType action, ClientCard card, string phase)
        {
            return BuildActionString(action.ToString(), card, phase);
        }

        private string BuildActionString(string action, ClientCard card, string phase)
        {
            if (phase == "Main2")
                phase = "Main1";
            string actionString = card.Name ?? "Uknown";
            actionString += ";";
            actionString += action.ToString();
            
            if (action == ExecutorType.Repos.ToString() && card != null)
                actionString += $";{card.Position}";
            actionString += ";" + phase;
            actionString += ";" + card.Location.ToString();
            actionString += ";" + Duel.Player;
            return actionString;
        }

        private string BuildActionString(ClientCard card, string phase)
        {
            if (phase == "Main2")
                phase = "Main1";
            string actionString = card.Name ?? "Uknown";
            actionString += ";" + card.Id;
            actionString += ";" + card.Location;
            actionString += ";" + phase;
            return actionString;
        }
    }
}
