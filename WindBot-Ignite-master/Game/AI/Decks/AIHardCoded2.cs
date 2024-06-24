using YGOSharp.OCGWrapper.Enums;
using System.Collections.Generic;
using WindBot;
using WindBot.Game;
using WindBot.Game.AI;

namespace WindBot.Game.AI.Decks
{
    [Deck("Hardcoded2", "AI_SnakeEyes")]
    public class Hardcoded2 : AIHardCodedBase
    {
        public Hardcoded2(GameAI ai, Duel duel)
            : base(ai, duel)
        {
        }
    }
}
