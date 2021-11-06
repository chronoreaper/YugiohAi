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
    public class RandomExecutor2 : RandomExecutorBase
    {

        public RandomExecutor2(GameAI ai, Duel duel)
            : base(ai, duel)
        {
        }
    }
}
