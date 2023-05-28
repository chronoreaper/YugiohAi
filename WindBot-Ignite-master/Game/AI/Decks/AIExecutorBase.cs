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
    public class AIExecutorBase : DefaultExecutorun
    {
        NEAT NEAT;
        NEATNode BestAction;

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

            NEAT = new NEAT();
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
            BestAction = null;


            if (Duel.Phase == DuelPhase.Main2)
            {
                //
                return;
            }

            foreach (ClientCard card in main.MonsterSetableCards)
            {
                NEAT.AddNode(BuildActionString(ExecutorType.MonsterSet, card, Duel.Phase.ToString()), false);
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
                NEAT.AddNode(BuildActionString(ExecutorType.Summon, card, Duel.Phase.ToString()), false);
            }
            //loop through special summonable monsters
            foreach (ClientCard card in main.SpecialSummonableCards)
            {
                NEAT.AddNode(BuildActionString(ExecutorType.SpSummon, card, Duel.Phase.ToString()), false);
            }
            //loop through activatable cards
            for (int i = 0; i < main.ActivableCards.Count; ++i)
            {
                ClientCard card = main.ActivableCards[i];
                NEAT.AddNode(BuildActionString(ExecutorType.Activate, card, Duel.Phase.ToString()), false);
                //choice.SetBest(ExecutorType.Activate, card, card.ActionActivateIndex[main.ActivableDescs[i]]);
            }

            NEAT.SetInputs(Duel);

            var bestActions = NEAT.GetBestAction(Duel);
            if (bestActions.Count > 0)
                BestAction = bestActions[0];
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
                NEAT.AddNode(BuildActionString(ExecutorType.Activate, card, Duel.Phase.ToString()), false);
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
            NEAT.ResetConnections();
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
                if (min == 1) // Single card seletion
                    NEAT.AddNode(BuildActionString("Select", clientCard, Duel.Phase.ToString()), false);
                    //Tree.AddPossibleAction(card, action, Duel.Fields, Duel.Turn);
            }

            if (min == 1 && false)
            {
                string toSelect = NEAT.GetBestAction(Duel)[0].Name;
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
            if (SQLComm.IsTraining)
                return true;
            if (BestAction != null)
            {
                string id = BestAction.Name.Split(';')[0];
                string action = BestAction.Name.Split(';')[1];
                //ActivateDescription
                if (Card.Name == id && Type.ToString() == action)
                {
                    BestAction.IncrementActivationCount();
                    BestAction = null;
                    return true;
                }
                else
                {
                    return false;
                }
            }

            return false;
        }

        public override void OnWin(int result)
        {
            //NEAT.OnWin(result);
            NEAT.games++;
            NEAT.wins += result == 0 ? 1 : 0;
            NEAT.SaveNetwork(result == 0 ? 1 : 0);
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
    }
}
