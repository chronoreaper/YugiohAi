using YGOSharp.OCGWrapper.Enums;
using System.Collections.Generic;
using WindBot;
using WindBot.Game;
using WindBot.Game.AI;
using static WindBot.MCST;
using System.Linq;

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
                string card = SelectStringBuilder(clientCard);
                Tree.AddPossibleAction(card, action);
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
            foreach(long o in options)
            {
                Tree.AddPossibleAction(o.ToString(), "SelectOption");
            }

            long best = long.Parse(Tree.GetNextAction().CardId);
            return options.IndexOf(best);
        }

        public bool ShouldPerform()
        {
            if (BestAction != null)
            {
                //ActivateDescription
                if (Card.Name == BestAction.CardId && Type.ToString() == BestAction.Action)
                {
                    BestAction = null;
                }
                else
                {
                    return false;
                }
            }

            return Tree.ShouldActivate(Card.Name, Type.ToString());

        }

        public override void OnWin(int result)
        {
            Tree.Backpropagate(result);
        }

        private string SelectStringBuilder(ClientCard Card, int Quant = 1)
        {
            return $"{Card.Name ?? "Set Monster" };{Card?.Location.ToString()};{Card?.Position.ToString()};{Card.Controller}";// x{Quant}";
        }

        private string BuildActionString(ExecutorType action, ClientCard card, string Phase)
        {
            if (Phase == "Main2")
                Phase = "Main1";
            string actionString = action.ToString();
            if (action == ExecutorType.Repos && card != null)
                actionString += $";{card.Position}";
            actionString += ";" + Phase;
            return actionString;
        }
    }
}
