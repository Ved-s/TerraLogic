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
    public abstract class LogicGate : Tile
    {
        internal bool State = false;
        internal bool IsFaulty = false;
        internal bool Display = false;

        internal bool NewState = false;

        protected abstract int GateId { get; }
        protected abstract string GateName { get; }

        public override string Id => "gate" + GateName;
        public override string DisplayName => $"Logic Gate ({GateName.ToUpper()})";

        public override string[] PreviewVariants => new string[] { "!" };

        public override bool NeedsContinuousUpdate => true;

        static Texture2D Sprite;

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
                        NewState = !State;
                    }
                    else RedFade = 120;
                }
            }
            else if (!IsFaulty)
            {
                bool newstate = Compute(lamps);
                //Debug.WriteLine($"[{this}] {string.Join("", lamps.Select(b => b ? "1" : "0"))} -> {newstate}");
                if (newstate != State)
                {
                    if (!CurrentWireUpdateStack.Contains(this)) NewState = newstate;
                    else RedFade = 120;
                }
            }
        }
        public override void Draw(Rectangle rect, bool isScreenPos = false)
        {
            rect = isScreenPos ? rect : PanNZoom.WorldToScreen(rect);
            if (IsFaulty) TerraLogic.SpriteBatch.DrawTileSprite(Sprite, 2, GateId, rect, Color.White);
            else TerraLogic.SpriteBatch.DrawTileSprite(Sprite, (State | Display) ? 1 : 0, GateId, rect, Color.White);

            if (RedFade > 0)
            {
                Color c = Color.Red * (RedFade / 120f);
                Graphics.DrawRectangle(TerraLogic.SpriteBatch, rect, c);
                RedFade--;
            }
        }

        public override void Update()
        {
            if (NewState != State)
            {
                SendSignal();
                State = NewState;
            }
        }

        public override void LoadContent(ContentManager content)
        {
            Sprite = content.Load<Texture2D>($"Tiles/LogicGate");
        }

        public override void PlacedInWorld()
        {
            UpdateState();
        }

        public void UpdateState()
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
            if (data is not null && data.Length >= 1)
            {
                if (data[0] == '!') { t.Display = true; data = data.Substring(1); }

                t.State = data == "+";
                t.IsFaulty = data == "?";
            }

            return t;
        }

        internal override string GetData()
        {
            return (IsFaulty) ? "?" : (State) ? "+" : null;
        }
    }
}
