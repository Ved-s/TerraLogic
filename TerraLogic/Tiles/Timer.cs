using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using TerraLogic.Structures;

namespace TerraLogic.Tiles
{
    class Timer : Tile
    {
        public override string Id => "timer";

        public override string[] PreviewDataVariants => new string[] { "-0", "-1", "-2", "-3", "-4" };
        public override string DisplayName => $"{Intervals[(int)Type]} Second Timer";
        public override bool NeedsContinuousUpdate => true;

        static readonly string[] Intervals = new string[] { "1/4", "1/2", "1", "3", "5" };
        static readonly uint[] IntervalValues = new uint[] { 15, 30, 60, 120, 300 };

        static Texture2D Sprite = null!;

        internal TimerType Type = TimerType.Sec1;
        internal bool State = false;
        internal uint Counter = 0;

        public override void Draw(Transform transform)
        {
            Graphics.DrawTileSprite(Sprite, (int)Type, State?1:0, transform.WorldToScreen(new Rect(0, 0, 16, 16)), Color.White);
        }

        public override void Update()
        {
            if (!State) { Counter = 0; return; }
            Counter++;
            if (Counter > IntervalValues[(int)Type]) Counter = 0;
            if (Counter == 0) SendSignal();
        }

        public override void RightClick(bool held, bool preview)
        {
            if (held) return;
            State = !State;
        }

        public override void WireSignal(int wire, Point origin, Point inputPosition)
        {
            if (wire.Bits() % 2 == 1)
                State = !State;
        }

        public override void LoadContent(ContentManager content)
        {
            Sprite = content.Load<Texture2D>("Tiles/Timer");
        }

        public override Tile Copy()
        {
            return new Timer() 
            {
                Type = Type,
                State = State,
                Counter = Counter,
            };
        }

        public override Tile CreateTile(string? data, bool preview)
        {
            if (data is null || data.Length < 2) return new Timer();

            bool state = data[0] == '+';
            int type;
            if (!int.TryParse(data.Substring(1), out type)) type = 2;

            return new Timer() { Type = (TimerType)type, State = state };
        }

        internal override string GetData()
        {
            return (State? "+" : "-") + ((int)Type).ToString();
        }

        public override void Save(BinaryWriter writer)
        {
            byte state = (byte)(((int)Type << 1) | (State ? 1 : 0));
            writer.Write(state);
            writer.Write(Counter);
        }

        public override void Load(BinaryReader reader)
        {
            byte state = reader.ReadByte();
            State = (state & 1) != 0;
            Type = (TimerType)((state & 0b11111110) >> 1);
            Counter = reader.ReadUInt32();
        }

        internal enum TimerType { Quarter, Half, Sec1, Sec3, Sec5 }
    }
}
