using YGOSharp.OCGWrapper.Enums;
using System.Collections.Generic;
using WindBot;
using WindBot.Game;
using WindBot.Game.AI;

namespace WindBot.Game.AI.Decks
{
    //[Deck("Random1", "AI_Swordsoul")]
    [Deck("Random1", "AI_Random1")]
    public class AIExecutor1 : AIExecutorBase
    {
        public AIExecutor1(GameAI ai, Duel duel)
            : base(ai, duel)
        {
        }
    }
}
