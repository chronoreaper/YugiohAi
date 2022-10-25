using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WindBot.Game.AI.Decks.Util
{
    public class MLUtil
    {
        public class GameState
        {
            public FieldState EnemyField;
            public FieldState BotField;

            public class FieldState
            {
                public int HandCount = 0;
                public int FieldCount = 0;
            }

            public GameState(ClientField[] fields)
            {
                ClientField bot = fields[0];
                ClientField enemy = fields[1];

                BotField = new FieldState()
                {
                    HandCount = bot.GetHandCount(),
                    FieldCount = bot.GetFieldCount(),
                };

                EnemyField = new FieldState()
                {
                    HandCount = enemy.GetHandCount(),
                    FieldCount = enemy.GetFieldCount(),
                };
            }

            public GameState()
            {
                BotField = new FieldState()
                {
                    HandCount = 5,
                    FieldCount = 0,
                };

                EnemyField = new FieldState()
                {
                    HandCount = 5,
                    FieldCount = 0,
                };
            }
        }
    }
}
