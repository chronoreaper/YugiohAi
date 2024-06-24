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
    public class AIHardCodedBase : DefaultExecutor
    {
        PlayHistory History;
        PlayHistory.ActionInfo BestAction;
        int ActionNumber = 0;

        MCST.Node NextAction;
        MCST Tree;

        public AIHardCodedBase(GameAI ai, Duel duel)
            : base(ai, duel)
        {
            //AddExecutor(ExecutorType.Activate, ShouldPerform);
            //AddExecutor(ExecutorType.SpSummon, ShouldPerform);
            //AddExecutor(ExecutorType.Summon, ShouldPerform);
            //AddExecutor(ExecutorType.MonsterSet, ShouldPerform);
            //AddExecutor(ExecutorType.SummonOrSet, ShouldPerform);
            //AddExecutor(ExecutorType.SpellSet, ShouldPerform);

            AddExecutor(ExecutorType.Repos, DefaultMonsterRepos);

            //AddExecutor(ExecutorType.Repos, ShouldPerform);
            //AddExecutor(ExecutorType.GoToBattlePhase, ShouldPerform);
            //AddExecutor(ExecutorType.GoToMainPhase2, ShouldPerform);
            //AddExecutor(ExecutorType.GoToEndPhase, ShouldPerform);

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
        }

        public override void SetChain(IList<ClientCard> cards, IList<long> descs, bool forced)
        {
            base.SetChain(cards, descs, forced);
        }

        // Activates anytime you could ACTIVATE a card. Set up only
        public override void OnChaining(int player, ClientCard card)
        {
            base.OnChaining(player, card);
        }


        // Return false if you don't want to activate a card last moment?
        public override bool OnPreActivate(ClientCard card)
        {
             return base.OnPreActivate(card);
        }


        public override void SetBattle(BattlePhase battle)
        {
        }

        public override bool OnSelectHand()
        {
            bool choice = true;
            return choice;
        }

        public override void OnNewTurn()
        {
            base.OnNewTurn();
        }

        public override void OnNewPhase()
        {
            base.OnNewPhase();
        }

        public override bool OnSelectYesNo(long desc)
        {
            return true;
        }

        public override IList<ClientCard> OnSelectCard(IList<ClientCard> _cards, int min, int max, long hint, bool cancelable)
        {
            //if (Duel.Phase == DuelPhase.BattleStart)
            //    return null;
            if (AI.HaveSelectedCards())
                return null;

            IList<ClientCard> selected = new List<ClientCard>();
            IList<ClientCard> cards = new List<ClientCard>(_cards);
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
            return 0;
        }

        public bool ShouldPerform()
        {
            return true;
        }

        public override void OnWin(int result)
        {
           
        }
    }
}
