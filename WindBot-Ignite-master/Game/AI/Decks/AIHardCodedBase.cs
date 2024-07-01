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
        protected int materialSelected = 0; // Used to count how many matrials used for summoning, resets on call of SetMain();
        protected int chainLinkCount = 0;
        protected Stack<int> playerChainIndex = new Stack<int>();
        protected bool isChainResolving = false;
        protected int winResult = -1;

        protected bool postSide = false;

        protected List<PreviousAction> previousActions = new List<PreviousAction>();
        protected List<PreviousAction> previousActionsEnemy = new List<PreviousAction>();
        protected List<string> used = new List<string>();
        protected List<int> usedEnemy = new List<int>();
        protected List<int> seenCards = new List<int>();

        // Try and take out dangerous targets
        public long[] FIELD_TARGETS = {
            CardId.SnakeEyeFlamberge,
            CardId.PromethianPrincess,
            CardId.SalamangreatRagingPhoenix
        };

        public int[] reactiveTargets =
        {
                CardId.SnakeEyeAsh,
                CardId.DiabellstarBlackWitch,
                CardId.SnakeEyePoplar,
                CardId.Apollusa,
                CardId.SnakeEyeOak
        };

        public int[] dontUseAsMaterial =
        {
            CardId.AccesscodeTalker,
            CardId.WorldseadragonZealantis,
            CardId.UnderworldGoddess,
            CardId.Apollusa
        };

        public enum ActivatedEffect
        {
            None = 0x0,
            First = 0x1,
            Second = 0x2,
            Third = 0x4
        }

        public enum Archetypes
        {
            Unknown,
            SnakeEyes,
            Labrynth,
            Branded,
            Tenpai,
            Yubel
        }

        public class PreviousAction
        {
            public ExecutorType type;
            public long cardId;
            public long description;
        }

        protected class CardId
        {
            // Generic Monsters
            public const int AshBlossom = 14558128;
            public const int EffectVeiler = 97268402;
            public const int GhostMourner = 52038441;
            public const int DrollnLockBird = 94145021;
            public const int Nibiru = 27204311;


            // Generic Spells
            public const int Bonfire = 85106525;
            public const int CrossoutDesignator = 65681983;
            public const int TripleTacticsTalent = 25311006;
            public const int OneForOne = 2295440;
            public const int CalledByTheGrave = 24224830;
            public const int FeatherDuster = 18144507;
            public const int LightningStorm = 14532163;


            // Generic Traps
            public const int InfiniteImpermanence = 10045474;
            public const int AntiSpellFragrance = 58921041;
            public const int SkillDrain = 82732705;


            // Generic xyz
            public const int TyphonSkyCrisis = 93039339;


            // Generic Links
            public const int RelinquishdAnima = 94259633;
            public const int LinkSpider = 98978921;
            public const int AccesscodeTalker = 86066372;
            public const int SalamangreatRagingPhoenix = 57134592;
            public const int KnightmarePhoenix = 2857636;
            public const int PromethianPrincess = 2772337;
            public const int HiitaCharmerAblaze = 48815792;
            public const int DharcCharmerGloomy = 8264361;
            public const int IPMasquerena = 65741786;
            public const int SPLittleKnight = 29301450;
            public const int WorldseadragonZealantis = 45112597;
            public const int SeleneQueenofMasterMagicians = 45819647;
            public const int Apollusa = 4280259;
            public const int UnderworldGoddess = 98127546;

            // Snake Eyes
            public const int SnakeEyeAsh = 9674034;
            public const int SnakeEyeFlamberge = 48452496;
            public const int SnakeEyePoplar = 90241276;
            public const int SnakeEyeDiabellstar = 27260347;
            public const int SnakeEyeOak = 45663742;
            public const int DiabellstarBlackWitch = 72270339;

            public const int WANTEDSinfulSpoils = 80845034;
            public const int OriginalSinfulSpoilsSnakeEyes = 89023486;
            public const int DivineTempleSnakeEyes = 53639887;

            public const int SinfulSpoilSilvera = 38511382;


            // Dragons
            public const int BystialMagnamhut = 33854624;
            public const int BystialDruiswurm = 6637331;
            public const int BystialSaronir = 60242223;
        }

        public AIHardCodedBase(GameAI ai, Duel duel)
            : base(ai, duel)
        {
            // Add in children class
        }

        protected List<long> HintMsgForEnemy = new List<long>
        {
            HintMsg.Release, HintMsg.Destroy, HintMsg.Remove, HintMsg.ToGrave, HintMsg.ReturnToHand, HintMsg.ToDeck,
            HintMsg.FusionMaterial, HintMsg.SynchroMaterial, HintMsg.XyzMaterial, HintMsg.LinkMaterial
        };

        protected List<long> HintMsgForDeck = new List<long>
        {
            HintMsg.SpSummon, HintMsg.ToGrave, HintMsg.Remove, HintMsg.AddToHand, HintMsg.FusionMaterial
        };

        protected List<long> HintMsgForSelf = new List<long>
        {
            HintMsg.Equip
        };

        protected List<long> HintMsgForMaterial = new List<long>
        {
            HintMsg.FusionMaterial, HintMsg.SynchroMaterial, HintMsg.XyzMaterial, HintMsg.LinkMaterial, HintMsg.Release
        };

        protected List<long> HintMsgForMaxSelect = new List<long>
        {
            HintMsg.SpSummon, HintMsg.ToGrave, HintMsg.AddToHand, HintMsg.FusionMaterial, HintMsg.Destroy
        };

        // Choose Go first or second
        public override bool OnSelectHand()
        {
            bool choice = true;
            return choice;
        }

        public override void OnNewTurn()
        {
            base.OnNewTurn();
            previousActions.Clear();
            previousActionsEnemy.Clear();
        }

        public override void SetMain(MainPhase main)
        {
            base.SetMain(main);
            materialSelected = 0;
        }


        public override bool OnSelectYesNo(long desc)
        {
            var option = Util.GetOptionFromDesc(desc);
            var cardId = Util.GetCardIdFromDesc(desc);
            return true;
        }

        public override IList<ClientCard> OnSelectCard(IList<ClientCard> _cards, int min, int max, long hint, bool cancelable)
        {
            //if (Duel.Phase == DuelPhase.BattleStart)
            //    return null;
            if (AI.HaveSelectedCards())
                return null;

            IList<ClientCard> selected = new List<ClientCard>();
 
            return SelectMinimum(selected, _cards, min, max, hint);
        }

        public override IList<ClientCard> OnCardSorting(IList<ClientCard> cards)
        {
            return base.OnCardSorting(cards);
        }

        // Do not touch _cards list
        public IList<ClientCard> SelectMinimum(IList<ClientCard> selected, IList<ClientCard> _cards, int min, int max, long hint)
        {
 
            IList<ClientCard> cards = new List<ClientCard>(_cards);

            // Default selection
            if (HintMsg.Target == hint)
            {
                if (Duel.CurrentChain.Count() >= 2)
                {
                    if (CardId.InfiniteImpermanence == Card.Id ||
                        CardId.EffectVeiler == Card.Id ||
                        CardId.GhostMourner == Card.Id)
                        selected.Add(_cards.Where(x => x.Id == Duel.CurrentChain[Duel.CurrentChain.Count - 2].Id).FirstOrDefault());
                }
            }

            #region Random selection
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
            #endregion

            // Clear null values
            selected = selected.Where(item => item != null).ToList();
            // Remove duplicates
            selected = selected.Distinct().ToList();

            cards = new List<ClientCard>(_cards);
            // select random cards
            while (selected.Count < min)
            {
                ClientCard card = cards[0];//cards[Program.Rand.Next(cards.Count)];
                if (!selected.Contains(card))
                    selected.Add(card);
                cards.Remove(card);
            }

            // Don't over select
            if (selected.Count > max)
            {
                selected = selected.Take(max).ToList();
            }

            return selected;
        }

        // Called when a chain is about to happen
        public override void SetChain(IList<ClientCard> cards, IList<long> descs, bool forced)
        {
            base.SetChain(cards, descs, forced);
        }

        // As Chain activates
        public override void OnChaining(int player, ClientCard card)
        {
            chainLinkCount += 1;
            if (player == 1)
            {
                if (!usedEnemy.Contains(card.Id))
                    usedEnemy.Add(card.Id);
                if (!seenCards.Contains(card.Id))
                    seenCards.Add(card.Id);
                previousActionsEnemy.Add(new PreviousAction() { cardId = card.Id, type = ExecutorType.Activate });
            }
            else
            {
                playerChainIndex.Push(chainLinkCount);
            }
            base.OnChaining(player, card);
        }

        public override void OnChainSolving()
        {
            isChainResolving = true;
        }

        // As a Chain link resolves
        public override void OnChainSolved()
        {
            if (playerChainIndex.Count > 0)
                playerChainIndex.Pop();
        }

        public override void OnChainEnd()
        {
            isChainResolving = false;
            base.OnChainEnd();
            if (chainLinkCount != 0)
                chainLinkCount = 0;
        }

        public override int OnSelectOption(IList<long> options)
        {
            int index = 0;
            foreach(long desc in options)
            {
                var option = Util.GetOptionFromDesc(desc);
                var cardId = Util.GetCardIdFromDesc(desc);

                if (CardId.TripleTacticsTalent == cardId)
                {
                    // Draw 2
                    if (option == 0)
                        return index;
                }
                index++;
            }

            return 0;
        }

        public override int OnAnnounceCard(IList<int> avail)
        {
            if (Util.GetLastChainCard().Id == CardId.CrossoutDesignator)
            {
                if (Duel.CurrentChain.Count >= 2)
                {
                    int crossoutTarget = Duel.CurrentChain[Duel.CurrentChain.Count - 2].Id;
                    if (avail.Contains(crossoutTarget))
                        return (crossoutTarget);

                }
            }
            return base.OnAnnounceCard(avail);
        }

        public override void OnPostActivate(bool activate)
        {
            if (!used.Contains(Card.Name))
                used.Add(Card.Name);
            previousActions.Add(new PreviousAction()
            {
                cardId = Card.Id,
                type = Type,
                description = ActivateDescription
            });
        }

        public override void OnWin(int result, List<string> _deck)
        {
            winResult = result;

            List<SQLComm.CardQuant> deckQuant = new List<SQLComm.CardQuant>();
            List<string> deck = new List<string>(_deck);
            while(deck.Count > 0)
            {
                string name = deck[0];
                int quant = deck.Where(x => x == name).Count();
                deck.RemoveAll(x => x == name);
                deckQuant.Add(new SQLComm.CardQuant() { Name = name, Quant = quant });
            }


            SQLComm.SavePlayedCards(Duel.IsFirst, postSide, result, used, deckQuant);

            used.Clear();
            usedEnemy.Clear();


            postSide = false;
        }


        #region Generic Actions
        public bool FaceUpEffectNegate()
        {
            
            // Apo negate
            if (Duel.Player == 0 && Enemy.GetMonsters().Where(x => x.Id == CardId.Apollusa && x.Attack >= 800 && !Util.IsChainTarget(x)).Any())
                return true;

            // Accesscode negate
            if (Duel.Player == 1 && Enemy.GetMonsters().Where(x => x.Id == CardId.AccesscodeTalker && !Util.IsChainTarget(x)).Any())
                return true;

            // Premtive negates
            int[] preEmptiveTargets =
            {
            };
            foreach (int id in preEmptiveTargets)
                if (Duel.Player == 1 && Enemy.GetMonsters().Where(x => x.Id == id && !x.IsDisabled()).Any())
                    return true;

            if (Duel.LastChainPlayer == 0)
                return false;

            foreach (int id in reactiveTargets)
                if (Duel.CurrentChain.Where(x => x.Id == id && !x.IsDisabled() && !Util.IsChainTarget(x)).Any())
                    return true;

            return false;
        }
        #endregion


        #region Generic Monsters
        public bool AshBlossomActivate()
        {
            if (Duel.LastChainPlayer == 0)
                return false;

            int[] ashTargets =
            {
                CardId.OriginalSinfulSpoilsSnakeEyes,
                CardId.SnakeEyeAsh //Effect 2
            };

            if (Duel.CurrentChain.LastOrDefault().IsCode(ashTargets))
                return true;

            return false;
        }

        public bool ApollusaSummon()
        {
            if (Bot.GetLinkMaterialWorth(dontUseAsMaterial) >= 4)
                return true;
            return true;
        }

        public bool ApollusaActivate()
        {
            if (Duel.LastChainPlayer == 1)
                return true;
            return false;
        }

        public bool TyphonSummon()
        {
            return false;
        }

        public bool TyphonActivate()
        {
            return false;
        }

        #endregion


        #region Generic Spells
        public bool BonfireActivate()
        {
            return true;
        }

        public bool CrossoutActivate()
        {
            if (Duel.LastChainPlayer == 0)
                return false;
            if (Duel.CurrentChain.Count() <= 0)
                return false;
            if (Bot.Deck.Where(x => x.Name == Duel.CurrentChain.LastOrDefault()?.Name).Any())
                return true;
            return false;
        }
        #endregion


        #region Generic Traps
        #endregion


        #region Util
        // check basic action
        protected bool HasPerformedPreviously(ExecutorType action)
        {
            return previousActions.Where(x => x.type == action).Any();
        }

        protected bool HasPerformedPreviously(long cardId, ExecutorType action)
        {
            return previousActions.Where(x => x.cardId == cardId && x.type == action).Any();
        }

        // Check activation
        protected bool HasPerformedPreviously(long cardId, int option)
        {
            long hint = Util.GetStringId(cardId, option);


            // Specific fixes
            if (cardId == CardId.SnakeEyeAsh && option == 0)
                hint = 62205969853120512;


            return previousActions.Where(x => x.cardId == cardId && x.description == hint && x.type == ExecutorType.Activate).Any();
        }

        // Returns the card that is currently resolving that you need to resolve
        protected ClientCard GetCurrentCardResolveInChain()
        {
            if (isChainResolving)
            {
                if (playerChainIndex.Count() > 0)
                {
                    var index = playerChainIndex.Peek();
                    return Duel.CurrentChain[index - 1];
                }
            }
            else
            {
                return Util.GetLastChainCard();
            }
            return null;
        }

        protected Archetypes GetEnemyDeckType()
        {
            int[] SnakeEyes =
            {
                CardId.SnakeEyeAsh,
                CardId.SnakeEyeOak,
                CardId.SnakeEyePoplar,
                CardId.SnakeEyeDiabellstar,
                CardId.SnakeEyeFlamberge,
                CardId.OriginalSinfulSpoilsSnakeEyes,
                CardId.DiabellstarBlackWitch,
                CardId.WANTEDSinfulSpoils,
                CardId.DivineTempleSnakeEyes
            };

            if (SnakeEyes.Any(x => seenCards.Contains(x)))
                return Archetypes.SnakeEyes;

            int[] Labrynth =
            {

            };

            int[] Branded =
            {

            };


            return Archetypes.Unknown;
        }
        #endregion
    }
}
