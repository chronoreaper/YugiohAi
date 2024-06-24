using YGOSharp.OCGWrapper.Enums;
using System.Collections.Generic;
using WindBot;
using WindBot.Game;
using WindBot.Game.AI;

namespace WindBot.Game.AI.Decks
{
    [Deck("Hardcoded1", "AI_SnakeEyes")]
    public class Hardcoded1 : AIHardCodedBase
    {
        public Hardcoded1(GameAI ai, Duel duel)
            : base(ai, duel)
        {
        }
    }
}
