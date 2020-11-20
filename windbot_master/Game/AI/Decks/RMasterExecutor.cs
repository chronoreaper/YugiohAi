using YGOSharp.OCGWrapper.Enums;
using System.Collections.Generic;
using WindBot;
using WindBot.Game;
using WindBot.Game.AI;
using System;
using System.Collections;

namespace WindBot.Game.AI.Decks
{
    [Deck("Master", "AI_Random2")]
    public class RMasterExecutor : DefaultExecutor
    {
        public class CardId
        {
            public const int Alexandrite = 43096270;
            public const int Avram = 84754430;
            public const int Luster = 11091375;
            public const int Sabersaurus = 37265642;
            public const int Artorigus = 92125819;
            public const int CyberDragon = 70095154;
            public const int Tricky = 14778250;
            public const int SnipeHunter = 84290642;
            public const int ExiledForce = 74131780;
            public const int Snowman = 91133740;
            public const int Krawler = 88316955;
            public const int BlockAttack = 25880422;
            public const int PotOfGreed = 55144522;
            public const int StopDefence = 63102017;

        }

        public RMasterExecutor(GameAI ai, Duel duel)
            : base(ai, duel)
        {
            AddExecutor(ExecutorType.Activate, CardId.PotOfGreed);

            AddExecutor(ExecutorType.SpSummon, CardId.CyberDragon);

            AddExecutor(ExecutorType.Summon, CardId.SnipeHunter);
            AddExecutor(ExecutorType.Summon, CardId.ExiledForce);

            AddExecutor(ExecutorType.MonsterSet, CardId.Krawler);

            AddExecutor(ExecutorType.Summon,CardId.Avram);
            AddExecutor(ExecutorType.Summon, CardId.Alexandrite);
            AddExecutor(ExecutorType.Summon, CardId.Luster);
            AddExecutor(ExecutorType.Summon, CardId.Sabersaurus);
            AddExecutor(ExecutorType.Summon, CardId.Artorigus);
            AddExecutor(ExecutorType.MonsterSet, CardId.Snowman);

            AddExecutor(ExecutorType.SpSummon, CardId.Tricky);

            AddExecutor(ExecutorType.Repos, DefaultMonsterRepos);

            AddExecutor(ExecutorType.Activate, CardId.StopDefence, ActivateStopDefence);
            AddExecutor(ExecutorType.Activate, CardId.BlockAttack, ActivateBlockAttack);
            AddExecutor(ExecutorType.Activate, CardId.SnipeHunter, ActivateExiledForce);
            AddExecutor(ExecutorType.Activate, CardId.ExiledForce, ActivateExiledForce);
            AddExecutor(ExecutorType.Activate, CardId.Krawler, ActivateExiledForce);
            AddExecutor(ExecutorType.Activate, CardId.Snowman, ActivateExiledForce);
        }

        private bool ActivateBlockAttack()
        {
            ClientCard target = null;
            foreach (ClientCard card in Util.Enemy.GetMonsters())
                if (card.Position == (int)CardPosition.Attack)
                    if (card.Attack > card.Defense && (target == null || card.Attack > target.Attack))
                        target = card;
            if (target != null)
            {
                AI.SelectCard(target);
                return true;
            }
            return false;
        }

        private bool ActivateStopDefence()
        {
            ClientCard target = null;
            foreach (ClientCard card in Util.Enemy.GetMonsters())
                if (card.Position == (int)CardPosition.Defence)
                    if (card.Defense > card.Attack && (target == null ||card.Defense > target.Defense))
                        target = card;
            if (target != null)
            {
                AI.SelectCard(target);
                return true;
            }
            return false;
        }

        private bool ActivateExiledForce()
        {
            ClientCard target = Util.GetBestEnemyMonster(true, true);
            if (target != null)
            {
                AI.SelectCard(target);
                return true;
            }
            return false;
        }

        private bool SnipeHunterSummon()
        {
            IList<int> atkTarg = new[] {
                CardId.CyberDragon,
                CardId.Tricky
            };

            IList<int> defTarg = new[] {
                CardId.Krawler
            };

            return (Util.Enemy.HasInMonstersZone(atkTarg) && !Bot.HasInHand(CardId.BlockAttack))
                ||(Util.Enemy.HasInMonstersZone(defTarg) && !Bot.HasInHand(CardId.StopDefence));
        }

        public override IList<ClientCard> OnSelectCard(IList<ClientCard> cards, int min, int max, int hint, bool cancelable)
        {
            if (Duel.Phase == DuelPhase.BattleStart)
                return null;

            IList<ClientCard> selected = new List<ClientCard>();

            // select the last cards
            for (int i = 1; i <= max; ++i)
                selected.Add(cards[cards.Count-i]);

            return selected;
        }

        public override int OnSelectOption(IList<int> options)
        {
            return Program.Rand.Next(options.Count);
        }

    }
}