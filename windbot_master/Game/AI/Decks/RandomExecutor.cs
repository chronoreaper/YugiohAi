using YGOSharp.OCGWrapper.Enums;
using System.Collections.Generic;
using WindBot;
using WindBot.Game;
using WindBot.Game.AI;
using System;

namespace WindBot.Game.AI.Decks
{
    [Deck("Random", "AI_Random")]
    public class RandomExecutor : RandomExecutorBase
    {
        public RandomExecutor(GameAI ai, Duel duel)
            : base(ai, duel)
        {
        }
    }
}