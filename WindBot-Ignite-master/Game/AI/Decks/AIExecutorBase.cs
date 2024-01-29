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


            AddExecutor(ExecutorType.GoToBattlePhase, ShouldPerform);
            AddExecutor(ExecutorType.GoToMainPhase2, ShouldPerform);
            AddExecutor(ExecutorType.GoToEndPhase, ShouldPerform);
            AddExecutor(ExecutorType.Repos, DefaultMonsterRepos);

            History = new PlayHistory();
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
                return;
            }

            ActionNumber++;
            var gameInfo = History.GenerateGameInfo(SQLComm.Id, Duel.Turn, ActionNumber);
            var compare = GetComparisons();

            foreach (ClientCard card in main.MonsterSetableCards)
            {
                actions.Add(History.GenerateActionInfo(BuildActionString(card, Duel.Phase.ToString()), ExecutorType.MonsterSet));
                //card.ActionIndex[(int)ExecutorType.MonsterSet];
            }
            //loop through cards that can change position
            foreach (ClientCard card in main.ReposableCards)
            {
                //Tree.AddPossibleAction(card?.Name, ExecutorType.Repos.ToString(), Duel.Fields, Duel.Turn);
            }
            //Loop through normal summonable monsters
            foreach (ClientCard card in main.SummonableCards)
            {
                actions.Add(History.GenerateActionInfo(BuildActionString(card, Duel.Phase.ToString()), ExecutorType.Summon));
            }
            //loop through special summonable monsters
            foreach (ClientCard card in main.SpecialSummonableCards)
            {
                actions.Add(History.GenerateActionInfo(BuildActionString(card, Duel.Phase.ToString()), ExecutorType.SpSummon));
            }
            //loop through activatable cards
            for (int i = 0; i < main.ActivableCards.Count; ++i)
            {
                ClientCard card = main.ActivableCards[i];
                actions.Add(History.GenerateActionInfo(BuildActionString(card, Duel.Phase.ToString()), ExecutorType.Activate));
                //choice.SetBest(ExecutorType.Activate, card, card.ActionActivateIndex[main.ActivableDescs[i]]);
            }


            if (main.CanBattlePhase)
                actions.Add(History.GenerateActionInfo("", ExecutorType.GoToBattlePhase));
            else if (main.CanEndPhase)
                actions.Add(History.GenerateActionInfo("", ExecutorType.GoToEndPhase));

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
                    if (best_predict.Count > 0 && best_predict.Count >= (int)action.ActionId)
                        Console.WriteLine(" " + action.ActionId + ":" + action.ToString() + " Weight:" + best_predict[(int)action.ActionId].ToString("#.#####"));
                }

                var chosen = actions[Rand.Next(actions.Count)];

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
                    var index = Rand.Next(result.Count);
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
                foreach(var i in comparisons)
                {
                    Console.WriteLine("     " + i.ToString());
                }
                Console.WriteLine("Select an action for Turn " + Duel.Turn + " Action # " + ActionNumber);
                foreach (var action in actions)
                {
                    Console.WriteLine(" " + ":" + action.ToString());
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

            for(int i = 0; i < 1; i++)
                foreach(var card in Duel.Fields[i].Hand)
                {
                    c.Add(History.GenerateComparasion(
                        (i == 0 ? "Player" : "Enemy") + "Hand",
                        card?.Name ?? "",
                        ""));
                }

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

            return c;
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
                //choice.SetBest(ExecutorType.Activate, card, battle.ActivableDescs[i]);
            }

        }

        public override bool OnSelectHand()
        {
            bool choice = SQLComm.IsFirst;
            return choice;
        }

        public override void OnNewTurn()
        {
            base.OnNewTurn();
            ActionNumber = 0;
            History.EndOfTurn(Duel);
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
                    //Tree.AddPossibleAction(card, action, Duel.Fields, Duel.Turn);
            }

            if (min == 1 && false)
            {
                string toSelect = "";
                string name = toSelect.Split(';')[0];
                CardLocation location = (CardLocation) Enum.Parse(typeof(CardLocation), toSelect.Split(';')[1]);
                int position = int.Parse(toSelect.Split(';')[2]);
                int controller = int.Parse(toSelect.Split(';')[3]);

                ClientCard select = cards.Where(card =>
                    (card.Name == name || name == "Set Monster") &&
                    card.Location == location &&
                    card.Position == position &&
                    card.Controller == controller
                    ).ToList()[0];
                selected.Add(select);
                cards.Remove(select);
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

            return selected;
        }

        public override int OnSelectOption(IList<long> options)
        {
            return base.OnSelectOption(options);
            foreach(long o in options)
            {
                //Tree.AddPossibleAction(o.ToString(), "SelectOption", Duel.Fields, Duel.Turn);
            }

            //long best = long.Parse(Tree.GetNextAction().CardId);
            //return options.IndexOf(best);
        }

        public bool ShouldPerform()
        {
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
                var info = History.GenerateGameInfo(SQLComm.Id, Duel.Turn, ActionNumber);
                var compare = GetComparisons();
                PlayHistory.ActionInfo action = History.GenerateActionInfo(BuildActionString(Card, Duel.Phase.ToString()), Type);
                List<PlayHistory.ActionInfo> actions = new List<PlayHistory.ActionInfo>
                {
                    action,
                    History.GenerateActionInfo("[No]", ExecutorType.Activate)
                };

                var best = GetBestAction(actions, compare);
                if (best == null)
                {
                    return false;
                }
                if (best.Name != "[No]")
                {
                    best.Performed = true;
                }

                History.AddHistory(History.GenerateHistory(info, compare, actions), Duel);

                return action.Performed;
            }

            return false;
        }

        public override void OnWin(int result)
        {
            //NEAT.OnWin(result);
            //NEAT.games++;
            //NEAT.wins += result == 0 ? 1 : 0;
            //NEAT.SaveNetwork(result == 0 ? 1 : 0);
            History.EndOfTurn(Duel);
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
            return actionString;
        }

        private string BuildActionString(ClientCard card, string phase)
        {
            if (phase == "Main2")
                phase = "Main1";
            string actionString = card.Name ?? "Uknown";
            actionString += ";" + phase;
            return actionString;
        }
    }
}
