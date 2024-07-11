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
    [Deck("Runick", "AI_Runick")]
    public class AIRunick : AIHardCodedBase
    {

        public AIRunick(GameAI ai, Duel duel)
            : base(ai, duel)
        {
            // Basically First Actions

            // Normal Priority
            AddExecutor(ExecutorType.Activate, CardId.UpstartGoblin);
            AddExecutor(ExecutorType.GoToBattlePhase, GoToBattlePhase);
            AddExecutor(ExecutorType.SpSummon, CardId.LavaGolemn);
            AddExecutor(ExecutorType.Summon, CardId.MajestyFiend);
            AddExecutor(ExecutorType.Summon, CardId.AmanoIwato);
            AddExecutor(ExecutorType.Activate, CardId.Terraforming);
            AddExecutor(ExecutorType.Activate, CardId.InstantFusion, InstantFusionActivate);

            AddExecutor(ExecutorType.Activate, CardId.RunickGoldenDroplet, GoldenDropletActivate);
            AddExecutor(ExecutorType.Activate, CardId.RunickFreezingCurse, FreezingCurseActivate);
            AddExecutor(ExecutorType.Activate, CardId.RunickTip, TipActivate);
            AddExecutor(ExecutorType.Activate, CardId.RunickDispelling, DispellingActivate);
            AddExecutor(ExecutorType.Activate, CardId.RunickSlumber, SlumberActivate);
            AddExecutor(ExecutorType.Activate, CardId.RunickFlashingFire, FlashingFireActivate);
            AddExecutor(ExecutorType.Activate, CardId.RunickSmitingStorm, StormActivate);
            AddExecutor(ExecutorType.Activate, CardId.RunickDestruction, DestructionActivate);
            AddExecutor(ExecutorType.Activate, CardId.DrawMuscle);
            AddExecutor(ExecutorType.Activate, CardId.ChickenGame);
            AddExecutor(ExecutorType.Activate, CardId.CardScanner);
            AddExecutor(ExecutorType.Activate, CardId.RunickFountain, FountainActivate);
            AddExecutor(ExecutorType.Activate, CardId.ThereCanBeOnlyOne);
            AddExecutor(ExecutorType.Activate, CardId.GozenMatch);
            AddExecutor(ExecutorType.Activate, CardId.SynchroZone);
            AddExecutor(ExecutorType.Activate, CardId.SkillDrain);
            AddExecutor(ExecutorType.Activate, CardId.RivalyOfWarlords);
            AddExecutor(ExecutorType.Activate, CardId.TripleTacticsTalent);
            AddExecutor(ExecutorType.Activate, CardId.GraveOfTheSuperAncient);
            AddExecutor(ExecutorType.Activate, CardId.EvenlyMatched);
            AddExecutor(ExecutorType.Activate, CardId.SolemnJudgment, SolemnJudgmentActivate);
            AddExecutor(ExecutorType.Summon, CardId.Bagooska);
            AddExecutor(ExecutorType.Activate, CardId.SleipnirRunick, SleipnirActivate);
            AddExecutor(ExecutorType.Activate, CardId.FrekiRunick);
            AddExecutor(ExecutorType.Activate, CardId.GeriRunick);
            AddExecutor(ExecutorType.Activate, CardId.MuninRunick, MuninActivate);
            AddExecutor(ExecutorType.Activate, CardId.HuginRunick, HuginActivate);

            // Low Priority

            // Reactive
            AddExecutor(ExecutorType.Activate, CardId.ForbiddenDroplet, DropletActivate);

            AddExecutor(ExecutorType.SpellSet, SpellSet);

            AddExecutor(ExecutorType.Repos, DefaultMonsterRepos);
            AddExecutor(ExecutorType.GoToBattlePhase);
        }


        // Choose Go first or second
        public override bool OnSelectHand()
        {
            bool choice = true;
            return choice;
        }


        // Assume no extra deck side
        public override void OnChangeSide(IList<int> _main, IList<int> _extra, IList<int> _side)
        {
            IList<int> pool = new List<int>(_main);
            pool = pool.Concat(_side).ToList();
            int mainCount = _main.Count();
            int sideCount = _side.Count();

            _main.Clear();
            _side.Clear();

            Archetypes enemyDeck = GetEnemyDeckType();

            {
                int[] minMain =
                {
               
                };

                AddCardsToList(_main, pool, mainCount, minMain);
            }

            switch(enemyDeck)
            {
                case Archetypes.SnakeEyes:
                    if (winResult == 0) // win
                    {
                        int[] toAdd =
                        {
                         
                        };
                        AddCardsToList(_main, pool, mainCount, toAdd);
                    }
                    else // lose
                    {
                        int[] toAdd =
                        {

                        };
                        AddCardsToList(_main, pool, mainCount, toAdd);
                    }
                    break;
            }



            // Fill out the remaining main deck and side deck slots.
            AddCardsToList(_main, pool, mainCount);
            AddCardsToList(_side, pool, sideCount);

            postSide = true;
        }

        public override bool OnSelectYesNo(long desc)
        {
            var option = Util.GetOptionFromDesc(desc);
            var cardId = Util.GetCardIdFromDesc(desc);
            return base.OnSelectYesNo(desc);
        }

        public override IList<ClientCard> OnSelectCard(IList<ClientCard> _cards, int min, int max, long hint, bool cancelable)
        {
            if (AI.HaveSelectedCards())
                return null;

            ClientCard currentCard = GetCurrentCardResolveInChain();
            IList<ClientCard> selected = new List<ClientCard>();

            #region AI Selected
            if (currentCard != null)
            {
                
            }
            else
            {

            }
            #endregion

            return SelectMinimum(selected, _cards, min, max, hint);
        }

        public override IList<ClientCard> OnCardSorting(IList<ClientCard> cards)
        {
            return base.OnCardSorting(cards);
        }


        public override int OnSelectOption(IList<long> options)
        {
            foreach(long desc in options)
            {
                var option = Util.GetOptionFromDesc(desc);
                var cardId = Util.GetCardIdFromDesc(desc);
            }

            if (options.Count == 2)
            {
                var hint = options[0];
                var cardId = Util.GetCardIdFromDesc(options[1]);

            }

            return base.OnSelectOption(options);
        }

        public override IList<ClientCard> OnSelectSum(IList<ClientCard> cards, int sum, int min, int max, long hint, bool mode)
        {
            return base.OnSelectSum(cards, sum, min, max, hint, mode);
        }

        public override int OnAnnounceCard(IList<int> avail)
        {
            return base.OnAnnounceCard(avail);
        }


        #region Generic Actions
        public bool SpellSet()
        {
            return DefaultSpellSet();
        }
        #endregion


        #region Generic Monsters



        #endregion


        #region Generic Spells
        public bool InstantFusionActivate()
        {
            return true;
        }
        #endregion


        #region Generic Traps
        public bool SolemnJudgmentActivate()
        {
            return DefaultSolemnJudgment();
        }
        #endregion

        #region Runick
        public bool GoldenDropletActivate()
        {
            return true;
        }

        public bool FreezingCurseActivate()
        {
            return true;
        }

        public bool TipActivate()
        {
            return true;
        }

        public bool DispellingActivate()
        {
            return true;
        }

        public bool SlumberActivate()
        {
            return true;
        }

        public bool FlashingFireActivate()
        {
            return true;
        }

        public bool StormActivate()
        {
            return true;
        }

        public bool DestructionActivate()
        {
            return true;
        }

        public bool FountainActivate()
        {
            return true;
        }

        public bool SleipnirActivate()
        {
            return true;
        }

        public bool MuninActivate()
        {
            return true;
        }

        public bool HuginActivate()
        {
            return true;
        }
        #endregion
    }
}
