using YGOSharp.OCGWrapper.Enums;
using System.Collections.Generic;
using WindBot;
using WindBot.Game;
using WindBot.Game.AI;
using System;

namespace WindBot.Game.AI.Decks
{
    [Deck("Random", "AI_Random")]
    public class RandomExecutor : DefaultExecutor
    {
        public class CardId
        {
            public const int LeoWizard = 4392470;
            public const int Bunilla = 69380702;
        }

        public RandomExecutor(GameAI ai, Duel duel)
            : base(ai, duel)
        {
            AddExecutor(ExecutorType.Summon, ShouldNormalSummon);
            AddExecutor(ExecutorType.SpSummon);
            AddExecutor(ExecutorType.Activate, DefaultDontChainMyself);
            AddExecutor(ExecutorType.SummonOrSet);
            AddExecutor(ExecutorType.Repos, DefaultMonsterRepos);
            AddExecutor(ExecutorType.SpellSet);
        }

        private bool ShouldNormalSummon()
        {
            foreach (ClientCard Monster in Enemy.GetMonsters())
            {

            }
            return true;
        }

        public override bool OnSelectHand()
        {
            bool choice = Program.Rand.Next(2) > 0;

            Logger.RecordAction(action:"GoFirst",value:choice.ToString());

            return choice;
        }

        public override IList<ClientCard> OnSelectCard(IList<ClientCard> cards, int min, int max, int hint, bool cancelable)
        {
            if (Duel.Phase == DuelPhase.BattleStart)
                return null;

            IList<ClientCard> selected = new List<ClientCard>();
            int rand = 0;
            // select random card
            for (int i = 1; i <= max; ++i)
            {
                rand = Program.Rand.Next(cards.Count);
                while (selected.Contains(cards[rand]))
                    rand = Program.Rand.Next(cards.Count);
                selected.Add(cards[rand]);
            }

            return selected;
        }

        public override int OnSelectOption(IList<int> options)
        {
            return Program.Rand.Next(options.Count);
        }

    }
}