using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using TerraLogic.Structures;

namespace TerraLogic.Tiles
{
    public abstract class LogicGate : Tile
    {
        internal bool State = false;
        internal bool IsFaulty = false;
        internal bool Display = false;

        internal bool NewState = false;
        internal bool PrevoiousTickStateChanged = false;

        protected abstract int GateId { get; }
        protected abstract string GateName { get; }

        public override string Id => "gate" + GateName;
        public override string DisplayName => $"Logic Gate ({GateName.ToUpper()})";

        public override string[] PreviewDataVariants => new string[] { "!" };

        public override bool NeedsContinuousUpdate => true;

        static Texture2D Sprite = null!;

        internal int RedFade = 0;

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
                if (newstate != State)
                {
                    if (!CurrentWireUpdateStack.Contains(this)) NewState = newstate;
                    else RedFade = 120;
                }
            }
        }
        public override void Draw(Transform transform)
        {
            if (IsFaulty) Graphics.DrawTileSprite(Sprite, 2, GateId, transform.WorldToScreen(new Rect(0,0,16,16)), Color.White);
            else Graphics.DrawTileSprite(Sprite, (State | Display) ? 1 : 0, GateId, transform.WorldToScreen(new Rect(0, 0, 16, 16)), Color.White);

            if (RedFade > 0)
            {
                Color c = Color.Red * (RedFade / 256f);
                
                Graphics.DrawRectangle(transform.WorldToScreen(new Rect(0, 0, 16, 16)), c);
                RedFade--;
            }
        }

        public override void Update()
        {
            UpdateState();
            if (NewState != State)
            {
                if (PrevoiousTickStateChanged)
                {
                    PrevoiousTickStateChanged = false;
                    State = NewState;
                    RedFade = 120;
                    return;
                }
                PrevoiousTickStateChanged = true;
                SendSignal();
                State = NewState;
            }
            else PrevoiousTickStateChanged = false;
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

            while (World.Tiles[Pos.X, scanPos] is LogicLamp lamp)
            {
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

        public override Tile Copy()
        {
            LogicGate t = (LogicGate)Activator.CreateInstance(GetType())!;
            t.State = State;
            t.NewState = State;
            t.IsFaulty = IsFaulty;
            return t;
        }

        public override Tile CreateTile(string? data, bool preview)
        {
            LogicGate t = (LogicGate)Activator.CreateInstance(GetType())!;
            if (data is not null && data.Length >= 1)
            {
                if (data[0] == '!') { t.Display = true; data = data.Substring(1); }

                t.State = data == "+";
                t.IsFaulty = data == "?";
            }

            return t;
        }

        internal override string? GetData()
        {
            return (IsFaulty) ? "?" : (State) ? "+" : null;
        }

        public override void Save(BinaryWriter writer)
        {
            byte v = Util.ZipBools(State, IsFaulty, NewState);
            writer.Write(v);
        }

        public override void Load(BinaryReader reader)
        {
            byte v = reader.ReadByte();
            bool[] values = Util.UnzipBools(v);
            State = values[0];
            IsFaulty = values[1];
            NewState = values[2];
        }
    }
}
