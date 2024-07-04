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

        public int[] reactiveEnemyTurn =
        {
                CardId.SnakeEyeAsh,
                CardId.DiabellstarBlackWitch,
                CardId.SnakeEyePoplar,
                CardId.SnakeEyeOak,
                CardId.PromethianPrincess,
                CardId.Apollusa,
        };

        public int[] protactiveEnemyTurn =
        {
                CardId.AccesscodeTalker,
        };

        public int[] reactivePlayerTurn =
        {
                CardId.Apollusa,
                CardId.IPMasquerena,
        };

        public int[] dontUseAsMaterial =
        {
            CardId.AccesscodeTalker,
            CardId.WorldseadragonZealantis,
            CardId.UnderworldGoddess,
            CardId.Apollusa
        };

        public int[] removeSpellTrap =
        {
            CardId.SkillDrain,
            CardId.SangenSummoning,
            CardId.AntiSpellFragrance,
            CardId.DivineTempleSnakeEyes,
            CardId.SnakeEyeDiabellstar
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
            public const int GhostOgre = 59438930;
            public const int GhostBelle = 73642296;
            public const int DrollnLockBird = 94145021;
            public const int Nibiru = 27204311;
            public const int DimensionShifter = 91800273;


            // Generic Spells
            public const int Bonfire = 85106525;
            public const int CrossoutDesignator = 65681983;
            public const int TripleTacticsTalent = 25311006;
            public const int OneForOne = 2295440;
            public const int CalledByTheGrave = 24224830;
            public const int FeatherDuster = 18144507;
            public const int LightningStorm = 14532163;
            public const int Terraforming = 73628505;
            public const int PotofProsperity = 84211599;
            public const int ForbiddenDroplet = 24299458;
            public const int CosmicCyclone = 8267140;
            public const int HeatWave = 45141013;

            // Generic Traps
            public const int InfiniteImpermanence = 10045474;
            public const int AntiSpellFragrance = 58921041;
            public const int SkillDrain = 82732705;
            public const int DimensionalBarrier = 83326048;

            // Generic Synchro
            public const int BlackRoseMoonlightDragon = 33698022;
            public const int BlackroseDragon = 73580471;
            public const int UltimayaTzolkin = 1686814;
            public const int CrystalWingSynchroDragon = 50954680;
            public const int KuibeltTheBladeDragon = 87837090;
            public const int AncientFairyDragon = 25862681;



            // Generic xyz
            public const int TyphonSkyCrisis = 93039339;
            public const int BeatriceLadyOfEnternal = 27552504


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
            public const int HieraticSealsOfSpheres = 24361622;
            public const int MoonOfTheClosedHeaven = 71818935l



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

            // Tenpai
            public const int TenpaiPaidra = 39931513;
            public const int TenpaiChundra = 91810826;
            public const int TenpaiFadra = 65326118;

            public const int SangenKaimen = 66730191;
            public const int SangenSummoning = 30336082;

            public const int SangenpaiTranscendentDragion = 18969888;
            public const int SangenpaiBidentDragion = 82570174;
            public const int TridentDragion = 39402797;

            // Kashtira
            public const int KashtiraFenrir = 32909498;
            public const int PlanetWraithsoth = 71832012;

            // Fiendsmith
            public const int TheFiendsmith = 60764609;
            
            public const int FiendsmithTractus = 98567237;
            public const int FiendsmithSanctus = 35552985;

            public const int FiendsmithDiesIrae = 82135803;
            public const int FiendsmithLacrimosa = 46640168;

            public const int FiendsmithRequiem = 02463794;
            public const int FiendsmithSequentia = 49867899;


            // Fabled
            public const int FabledLurrie = 97651498;
            

            // Bystial
            public const int BystialMagnamhut = 33854624;
            public const int BystialDruiswurm = 6637331;
            public const int BystialSaronir = 60242223;
        }

        public AIHardCodedBase(GameAI ai, Duel duel)
            : base(ai, duel)
        {
            // Reactive
            AddExecutor(ExecutorType.Activate, CardId.EffectVeiler, FaceUpEffectNegate);
            AddExecutor(ExecutorType.Activate, CardId.GhostMourner, FaceUpEffectNegate);
            AddExecutor(ExecutorType.Activate, CardId.GhostOgre, GhostOgreActivate);
            AddExecutor(ExecutorType.Activate, CardId.GhostBelle, DefaultGhostBelleAndHauntedMansion);
            AddExecutor(ExecutorType.Activate, CardId.InfiniteImpermanence, FaceUpEffectNegate);
            AddExecutor(ExecutorType.Activate, CardId.AshBlossom, AshBlossomActivate);
            AddExecutor(ExecutorType.Activate, CardId.DrollnLockBird, DrollActivate);
            AddExecutor(ExecutorType.Activate, CardId.Nibiru);
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
            ClientCard currentCard = GetCurrentCardResolveInChain();

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
                if (CardId.CalledByTheGrave == currentCard.Id)
                {
                    selected.Add(_cards.Where(x => x.Controller == 1 && x.Location == CardLocation.Grave && Duel.CurrentChain.Any(y => y.IsCode(x.Id))).FirstOrDefault());
                }
                int[] GYBanish =
                {
                    CardId.BystialMagnamhut,
                    CardId.BystialDruiswurm,
                    CardId.BystialSaronir
                };
                if (GYBanish.Any(x => x == currentCard.Id))
                    selected.Add(_cards.Where(x => x.Location == CardLocation.Grave && x.HasAttribute(CardAttribute.Dark | CardAttribute.Light) && Duel.ChainTargets.Any(y => y.Id == x.Id))
                                        .OrderBy(x => x.Owner == 1? 0: 1)
                                        .FirstOrDefault()
                                 );
            }

            #region Fiendsmith Selection
            if (CardId.TheFiendsmith == currentCard)
            {
                if (hint == HintMsg.AddToHand)
                {
                    selected.Add(_cards.FirstOrDefault(x => x.Id == CardId.FiendsmithTractus));
                }
                // Shuffle into deck
                if (hint == HintMsg.ShuffleToDeck)
                    foreach(var id in FiendsmithShuffleToDeck)
                    {
                        selected.Add(_cards.FirstOrDefault(x => x.Id == id));
                    }
            }
            else if (CardId.FiendsmithLacrimosa)
            {
                if (hint == HintMsg.Special)
                    selected.Add(_cards.FirstOrDefault(x => x.Id == CardId.TheFiendsmith));
                                // Shuffle into deck
                if (hint == HintMsg.ShuffleToDeck)
                    foreach(var id in FiendsmithShuffleToDeck)
                    {
                        selected.Add(_cards.FirstOrDefault(x => x.Id == id));
                    }
            }
            else if (CardId.FiendsmithTractus)
            {
                selected.Add(_cards.FirstOrDefault(x => x.Id == CardId.FabledLurrie));
            }
            #endregion

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

            // Add to previousActions
            foreach (var card in selected)
            {
                if (!used.Contains(card.Name))
                    used.Add(card.Name);
                previousActionsEnemy.Add(new PreviousAction() { cardId = card.Id, type = ExecutorType.Select, description = hint });
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
            if (!activate)
                return;
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

        public bool DefaultNegate()
        {
            if (Duel.LastChainPlayer == 0)
                return false;

            // Tenpai field spell
            ClientCard last = Util.GetLastChainCard();

            if (last == null)
                return false;

            bool isTenpaiType = (last.HasAttribute(CardAttribute.Fire) && last.HasRace(CardRace.Dragon) && last.Location == CardLocation.MonsterZone);
            if (Enemy.HasInSpellZone(CardId.SangenSummoning) && isTenpaiType && Duel.Phase == DuelPhase.Main1 && Duel.Player == 1)
                return false;


            return true;
        }

        public bool FaceUpEffectNegate()
        {
            
            // Apo negate
            if (Duel.Player == 0 && Enemy.GetMonsters().Where(x => x.Id == CardId.Apollusa && x.Attack >= 800 && !Util.IsChainTarget(x)).Any())
                return true;


            foreach (int id in protactiveEnemyTurn)
                if (Duel.Player == 1 && Enemy.GetMonsters().Where(x => x.Id == id && !x.IsDisabled()).Any())
                    return true;

            if (!DefaultNegate())
                return false;

            if (Duel.Player == 1)
                foreach (int id in reactiveEnemyTurn)
                    if (Duel.CurrentChain.Where(x => x.Id == id && !x.IsDisabled() && !Util.IsChainTarget(x)).Any())
                        return true;

            if (Duel.Player == 0)
                foreach (int id in reactivePlayerTurn)
                    if (Duel.CurrentChain.Where(x => x.Id == id && !x.IsDisabled() && !Util.IsChainTarget(x)).Any())
                        return true;

            return false;
        }

        #endregion


        #region Generic Monsters
        public bool AshBlossomActivate()
        {
            if (!DefaultNegate())
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

        public bool GhostOgreActivate()
        {
            if (!DefaultNegate())
                return false;

            int[] dont =
            {
                CardId.SnakeEyeFlamberge,
                CardId.SnakeEyePoplar
            };

            return true;
        }

        public bool DrollActivate()
        {
            if (Duel.Player == 0)
                return false;
            return true;
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

        public bool BystialActivate()
        {
            if (Duel.ChainTargets.Where(x => x.Location == CardLocation.Grave && x.HasAttribute(CardAttribute.Dark | CardAttribute.Light)).Any())
                return true;
            if (Duel.Phase == DuelPhase.End)
                return true;
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

        public bool CalledByActivate()
        {
            if (Duel.CurrentChain.Where(x => x.Controller == 1 && x.Location == CardLocation.Grave && x.HasType(CardType.Monster)).Any())
                return true;
            return false;
        }

        public bool CosmicActivate()
        {
            if (Duel.CurrentChain.Any(x => removeSpellTrap.Contains(x.Id) && !Duel.ChainTargets.Any()))
                return true;
            if (Duel.Player == 1 && Enemy.SpellZone.Any(x => x.HasPosition(CardPosition.FaceDown)))
                return true;
            if (Duel.Player == 0 && Enemy.SpellZone.Count(x => x.HasPosition(CardPosition.FaceDown)) == 1)
                return true;

            return false;
        }

        public bool DropletActivate()
        {
            return FaceUpEffectNegate();
        }
        #endregion


        #region Generic Traps
        #endregion

        #region Fiendsmith
        
        public int[] FiendsmithShuffleToDeck = 
        {
            CardId.FiendsmithLacrimosa,
            CardId.FiendsmithDiesIrae,
            CardId.FiendsmithRequiem,
            CardId.FiendsmithSequentia,
            CardId.FabledLurrie,
            CardId.MoonOfTheClosedHeaven
        }

        public bool TheFiendsmithActivate()
        {
            if (Card.Location == CardLocation.Hand)
                return true;
            if (Card.Location == CardLocation.Grave)
                return true;
            if (Card.Location == CardLocation.MonsterZone && Enemy.GetMonsterCount() > 0)
                return true;
            return false;
        }

        public bool FiendsmithTractusActivate()
        {
            if (Card.Location != CardLocation.Grave)
                return true;
            return false;
        }

        public bool FiendsmithDiesIraeActivate()
        {
            return FaceUpEffectNegate()   
            // TODO add faceup spell trap negate as well
        }


        #endregion


        #region Util
        // check basic action
        protected bool HasPerformedPreviously(ExecutorType action)
        {
            return previousActions.Where(x => x.type == action).Any();
        }

        protected bool HasPerformedPreviously(long cardId)
        {
            return previousActions.Any(x => x.cardId == cardId);
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

        protected bool HasPerformedPreviously(long cardId, ExecutorType action, long option)
        {
            return previousActions.Where(x => x.cardId == cardId && x.type == action && x.description == option).Any();
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

            int[] Tenpai =
            {
                CardId.TenpaiChundra,
                CardId.TenpaiFadra,
                CardId.TenpaiPaidra,
                CardId.SangenKaimen,
                CardId.SangenSummoning,
            };
            if (Tenpai.Any(x => seenCards.Contains(x)))
                return Archetypes.Tenpai;

            return Archetypes.Unknown;
        }

        /// <summary>
        /// Add as many of the given cards from the main/side list to the cards to add list
        /// </summary>
        /// <param name="toAddTo">The list to add to</param>
        /// <param name="cardsToAdd">Cards you want to add</param>
        /// <param name="pool">the pool of cards to take from</param>
        protected void AddCardsToList(IList<int> toAddTo, IList<int> pool, int limit, int[] cardsToAdd = null)
        {
            if (cardsToAdd != null)
            {
                foreach (int card in cardsToAdd)
                {
                    if (toAddTo.Count() >= limit)
                        break;
                    if (pool.Contains(card))
                    {
                        toAddTo.Add(card);
                        pool.Remove(card);
                    }
                }
            }
            else
            {
                while (toAddTo.Count() < limit && pool.Count() > 0)
                {
                    var card = pool.ElementAt(0);
                    pool.RemoveAt(0);
                    toAddTo.Add(card);
                }
            }
        }
        #endregion
    }
}
