using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using TerraLogic.GuiElements;

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

        int RedFade = 0;

        protected abstract bool Compute(bool[] lamps);

        internal void LampStateChanged(bool[] lamps, bool hasFaulty, bool faultyTriggered)
        {
            IsFaulty = hasFaulty;
            if (IsFaulty && faultyTriggered)
            {
                if (lamps.Length > 0 && lamps[new Random().Next(0, lamps.Length)])
                {
                    if (!CurrentWireUpdateStack.Contains(this))
                    {
                        SendSignal();
                    }
                    else RedFade = 120;
                }
            }
            else if (!IsFaulty)
            {
                bool newstate = Compute(lamps);

                if (newstate != State)
                {
                    State = newstate;
                    if (!CurrentWireUpdateStack.Contains(this)) { SendSignal(); }
                    else RedFade = 120;
                }
            }
        }
        public override void Draw(Rectangle rect, bool isScreenPos = false)
        {
            rect = isScreenPos ? rect : PanNZoom.WorldToScreen(rect);
            if (IsFaulty) TerraLogic.SpriteBatch.DrawTileSprite(Sprite, 2, GateId, rect, Color.White);
            else TerraLogic.SpriteBatch.DrawTileSprite(Sprite, State? 1 : 0, GateId, rect, Color.White);

            if (RedFade > 0) 
            {
                Color c = Color.Red * (RedFade / 120f);
                Graphics.DrawRectangle(TerraLogic.SpriteBatch, rect, c);
                RedFade--;
            }
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
