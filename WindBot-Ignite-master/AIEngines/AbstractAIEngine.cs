using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WindBot.Game;
using WindBot.Game.AI;
using WindBot.Game.AI.Decks.Util;
using YGOSharp.OCGWrapper.Enums;

namespace WindBot
{
    public abstract class AbstractAIEngine
    {
        public class FieldStateValues
        {
            public long Id = 0;
            public string Location = "";
            public string Compare = "";
            public string Value = "";

            public FieldStateValues(string location, string compare, string value)
            {
                Location = location;
                Compare = compare;
                Value = value;
                Id = SQLComm.GetComparisonId(this);
            }

            public FieldStateValues()
            {

            }
            
            public override string ToString()
            {
                return "[" + Id + "]" + Location + " " + Compare + " " + Value;
            }
        }

        public class History
        {
            public GameInfo Info;

            public List<ActionInfo> ActionInfo = new List<ActionInfo>();
            public List<FieldStateValues> FieldState = new List<FieldStateValues>();

            public int CurP1Hand = 0;
            public int CurP1Field = 0;
            public int CurP2Hand = 0;
            public int CurP2Field = 0;

            public int PostP1Hand = -1;
            public int PostP1Field = -1;
            public int PostP2Hand = -1;
            public int PostP2Field = -1;

            public History(GameInfo info, List<ActionInfo> actions, List<FieldStateValues> fieldState)
            {
                Info = info;
                ActionInfo = actions;
                FieldState = fieldState;
            }
        }

        public class GameInfo
        {
            public int Game = 0;
            public int Turn = 0;
            public int ActionNumber = 0;

            public GameInfo(int game, int turn, int actionNumber)
            {
                Game = game;
                Turn = turn;
                ActionNumber = actionNumber;
            }
        }

        public class ActionInfo
        {
            public string Name = "";
            public string Action = "";
            public long ActionId = 0;
            public bool Performed = false;
            public ClientCard Card = null;
            public long Desc = -1;

            public double Weight = -1;

            public ActionInfo(long actionId, string name, string action)
            {
                ActionId = actionId;
                Name = name;
                Action = action;
            }

            public ActionInfo(string name, string action, double weight)
                : this(name, action, null, -1)
            {
                Weight = weight;
            }

            public ActionInfo(string name, string action, ClientCard card)
                : this(name, action, card, -1) { }

            public ActionInfo(string name, string action, ClientCard card, long desc)
            {
                Name = name;
                Action = action;
                Card = card;
                Desc = desc;
                ActionId = SQLComm.GetActionId(this);
            }

            public override string ToString()
            {
                string str = "[" + ActionId + "]" + Action?.ToString() + " " + Name;
                if (Desc >= 0)
                    str += " " + Desc.ToString();
                return str;
            }
        }

        protected List<ActionInfo> allSelectActions = SQLComm.GetAllActions();

        protected Executor source;
        public AbstractAIEngine(Executor source)
        {
            this.source = source;
        }

        public abstract void OnNewTurn(Duel duel);
        public abstract void OnNewPhase();
        public abstract void OnChainSolved();
        public abstract void OnChainSolving();
        public abstract void SetMain(MainPhase main, List<FieldStateValues> fieldState, Duel duel);
        public abstract void SetChain(IList<ClientCard> cards, IList<long> descs, bool forced, List<FieldStateValues> fieldState, Duel duel, AIUtil Util);
        public abstract void SetBattle(BattlePhase battle, List<FieldStateValues> fieldState, Duel duel);
        public abstract bool ShouldPerform(ClientCard card, string action, long desc, List<FieldStateValues> fieldState, Duel duel);
        public abstract IList<ClientCard> SelectCards(ClientCard card, int min, int max, long hint, bool cancelable, IList<ClientCard> selectable, List<FieldStateValues> fieldState, Duel duel);
        public abstract int SelectOption(IList<long> options, List<FieldStateValues> fieldState, Duel duel, AIUtil Util);

        public abstract ClientCard OnSelectAttacker(IList<ClientCard> attackers, List<FieldStateValues> fieldState, Duel duel);

        public abstract ClientCard OnSelectAttackTarget(ClientCard attacker, IList<ClientCard> defenders, List<FieldStateValues> fieldState, Duel duel);

        public abstract CardPosition OnSelectPosition(int cardId, IList<CardPosition> positions, List<FieldStateValues> fieldState, Duel duel);


        public abstract int OnAnnounceCard(ClientCard card, IList<int> avail, List<FieldStateValues> fieldState, Duel duel);
        public abstract void OnWin(int result);

        protected string SelectStringBuilder(ClientCard Card, int Quant = 1)
        {
            return $"{Card.Name ?? "Set Card" };{Card?.Location.ToString()};{Card?.Position.ToString()};{Card.Controller}";// x{Quant}";
        }

        protected string BuildActionString(ClientCard card, Duel duel)
        {
            //if (phase == "Main2")
            //    phase = "Main1";
            if (card == null)
                return "";
            string actionString = card.Name ?? "Uknown";
            actionString += ";" + card.Id;
            actionString += ";" + card.Location;
            actionString += ";" + duel.Phase.ToString();
            actionString += ";" + duel.Player.ToString();
            return actionString;
        }

        // TODO Sort out the card to be a comparison list instead of all the details in one list
        protected List<ActionInfo> GetCardComparisonDetails(ClientCard card)
        {
            return null;
        }

        protected float GetCardComparisonWeight(ClientCard card, List<ActionInfo> details)
        {
            return 0;
        }
    }
}

