using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace TerraLogic.Tiles
{
    abstract class LogicGate : Tile
    {
        internal bool State;
        internal bool IsFaulty;

        protected abstract int GateId { get; }
        protected abstract string GateName { get; }

        public override string Id => "gate" + GateName;
        public override string DisplayName => $"Logic Gate ({GateName.ToUpper()})";

        public override bool NeedsContinuousUpdate => true;

        static Texture2D Sprite; // Of On Faulty

        protected abstract bool Compute(bool[] lamps);

        internal void LampStateChanged(bool[] lamps, bool hasFaulty, bool faultyTriggered)
        {
            //Debug.Write($"Lamps: {string.Join("", lamps.Select(b => b ? "+" : "-"))}, HasFaulty: {hasFaulty}, ");
            //if (hasFaulty) Debug.WriteLine($"FaultyTrigger: {faultyTriggered}");
            //else Debug.WriteLine($"CurrentState: {State}");

            IsFaulty = hasFaulty;
            if (IsFaulty && faultyTriggered)
            {
                if (lamps.Length > 0 && lamps[new Random().Next(0, lamps.Length)]) SendSignal();
            }
            else if (!IsFaulty)
            {
                bool newstate = Compute(lamps);
                if (newstate != State) SendSignal();
                State = newstate;
            }
        }

        public override void Draw(Rectangle rect, bool isScreenPos = false)
        {
            if (IsFaulty) TerraLogic.SpriteBatch.DrawTileSprite(Sprite, 2, GateId, isScreenPos ? rect : PanNZoom.WorldToScreen(rect), Color.White);
            else TerraLogic.SpriteBatch.DrawTileSprite(Sprite, State? 1 : 0, GateId, isScreenPos ? rect : PanNZoom.WorldToScreen(rect), Color.White);
        }

        public override void Update()
        {
        }

        public override void LoadContent(ContentManager content)
        {
            Sprite = content.Load<Texture2D>($"Tiles/LogicGate");
        }

        public override void PlacedInWorld()
        {
            UpdateState();
        }

        internal void UpdateState()
        {
            int scanPos = Pos.Y - 1;

            List<bool> lamps = new List<bool>();
            bool foundFaulty = false;
            bool faultyTriggered = false;

            while (Gui.Logics.TileArray[Pos.X, scanPos] is LogicLamp)
            {
                LogicLamp lamp = Gui.Logics.TileArray[Pos.X, scanPos] as LogicLamp;
                switch (lamp.State)
                {
                    case LogicLamp.LampState.Off: lamps.Add(false); break;
                    case LogicLamp.LampState.On: lamps.Add(true); break;
                    case LogicLamp.LampState.Faulty: foundFaulty = true; break;
                    case LogicLamp.LampState.FaultyTriggered:
                        lamp.State = LogicLamp.LampState.Faulty;
                        foundFaulty = true;
                        faultyTriggered = true;
                        break;
                }
                scanPos--;
            }

            LampStateChanged(lamps.ToArray(), foundFaulty, faultyTriggered);
        }

        internal override Tile CreateTile(string data, bool preview)
        {
            LogicGate t = (LogicGate)Activator.CreateInstance(this.GetType());
            if (preview)
                t.State = true;
            else 
            {
                t.State = data == "+";
                t.IsFaulty = data == "?";

            }
            return t;
        }

        internal override string GetData()
        {
            return !Created? "-" : (IsFaulty) ? "?" : (State) ? "+" : "-";
        }
    }
}
