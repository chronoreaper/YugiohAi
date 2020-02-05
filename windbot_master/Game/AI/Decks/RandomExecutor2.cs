using YGOSharp.OCGWrapper.Enums;
using System.Collections.Generic;
using WindBot;
using WindBot.Game;
using WindBot.Game.AI;
using System.IO;
using System;

namespace WindBot.Game.AI.Decks
{
    [Deck("Random2", "AI_Random2")]
    public class RandomExecutor2 : DefaultExecutor
    {
        public class CardId
        {
            public const int LeoWizard = 4392470;
            public const int Bunilla = 69380702;
        }

        public RandomExecutor2(GameAI ai, Duel duel)
            : base(ai, duel)
        {
            AddExecutor(ExecutorType.SpSummon);
            AddExecutor(ExecutorType.Activate, DefaultDontChainMyself);
            AddExecutor(ExecutorType.SummonOrSet);
            AddExecutor(ExecutorType.Repos, DefaultMonsterRepos);
            AddExecutor(ExecutorType.SpellSet);
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
            //reader = new StreamReader(new FileStream("Decks/" + name + ".ydk", FileMode.Open, FileAccess.Read));
            string path = "Decks/text.txt";
            if (!File.Exists(path))
            {
                // Create a file to write to.
                using (StreamWriter sw = File.CreateText(path))
                {
                    sw.WriteLine("Hello");
                    sw.WriteLine("And");
                    sw.WriteLine("Welcome");
                }
            }

            return selected;
        }

        public override int OnSelectOption(IList<int> options)
        {
            return Program.Rand.Next(options.Count);
        }

    }
}