﻿using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using TerraLogic.Structures;

namespace TerraLogic.Tiles
{
    class LogicLamp : Tile
    {
        public override string Id => "logicLamp";
        public override string[] PreviewDataVariants => new string[] { "+", "-", "?" };

        public override string DisplayName => $"Logic Gate Lamp ({State})";

        static Texture2D Sprite;

        internal LampState State;

        public override void Draw(Transform transform)
        {
            Graphics.DrawTileSprite(
                Sprite, Math.Min((int)State, 2), 0,
                transform.WorldToScreen(new Rect(0, 0, 16, 16)), Color.White);
        }

        public override void PlacedInWorld()
        {
            int scanPos = Pos.Y;
            while (World.Tiles[Pos.X, scanPos] is LogicLamp) scanPos--;
            scanPos++;

            List<bool> lamps = new List<bool>();
            bool foundFaulty = false;
            bool faultyTriggered = false;

            while (World.Tiles[Pos.X, scanPos] is LogicLamp lamp)
            {
                switch (lamp.State)
                {
                    case LampState.Off: lamps.Add(false); break;
                    case LampState.On: lamps.Add(true); break;
                    case LampState.Faulty: foundFaulty = true; break;
                    case LampState.FaultyTriggered: foundFaulty = true; faultyTriggered = true; break;
                }
                scanPos++;
            }

            if (World.Tiles[Pos.X, scanPos] is LogicGate lg) lg.LampStateChanged(lamps.ToArray(), foundFaulty, faultyTriggered);
        }

        public override void LoadContent(ContentManager content)
        {
            Sprite = content.Load<Texture2D>("Tiles/LogicLamp");
        }

        public override void WireSignal(int wire, Point from, Point inputPosition)
        {
            if (wire.Bits() % 2 == 1)
                switch (State)
                {
                    case LampState.On: State = LampState.Off; break;
                    case LampState.Off: State = LampState.On; break;
                    case LampState.Faulty: State = LampState.FaultyTriggered; break;
                }
        }

        public override Tile Copy()
        {
            return new LogicLamp() { State = State };
        }

        public override Tile CreateTile(string? data, bool preview)
        {
            return new LogicLamp() { State = (data == "+") ? LampState.On : (data == "?") ? LampState.Faulty : LampState.Off };
        }

        public override void BeforeDestroy()
        {
            int scanPos = Pos.Y + 1;

            List<bool> lamps = new List<bool>();
            bool foundFaulty = false;
            bool faultyTriggered = false;

            while (World.Tiles[Pos.X, scanPos] is LogicLamp)
            {
                LogicLamp lamp = World.Tiles[Pos.X, scanPos] as LogicLamp;
                switch (lamp.State)
                {
                    case LampState.Off: lamps.Add(false); break;
                    case LampState.On: lamps.Add(true); break;
                    case LampState.Faulty: foundFaulty = true; break;
                    case LampState.FaultyTriggered: foundFaulty = true; faultyTriggered = true; break;
                }
                scanPos++;
            }

            if (World.Tiles[Pos.X, scanPos] is LogicGate lg) lg.LampStateChanged(lamps.ToArray(), foundFaulty, faultyTriggered);

        }

        internal override string GetData()
        {
            return (State == LampState.On) ? "+" : (State == LampState.Faulty) ? "?" : null;
        }

        public override void Save(BinaryWriter writer)
        {
            writer.Write((byte)State);
        }

        public override void Load(BinaryReader reader)
        {
            State = (LampState)reader.ReadByte();
        }

        internal enum LampState { Off, On, Faulty, FaultyTriggered }
    }
}
