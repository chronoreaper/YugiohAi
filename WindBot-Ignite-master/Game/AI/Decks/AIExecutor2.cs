using YGOSharp.OCGWrapper.Enums;
using System.Collections.Generic;
using WindBot;
using WindBot.Game;
using WindBot.Game.AI;

namespace WindBot.Game.AI.Decks
{
    //[Deck("Random2", "AI_Swordsoul")]
    [Deck("Random2", "AI_Random2")]
    public class AIExecutor2 : AIExecutorBase
    {
        public AIExecutor2(GameAI ai, Duel duel)
            : base(ai, duel)
        {
        }
    }
}
