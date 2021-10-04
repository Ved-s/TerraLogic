using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TerraLogic.Tiles
{
    class Timer : Tile
    {
        public override string Id => "timer";

        public override string[] PreviewVariants => new string[] { "-0", "-1", "-2", "-3", "-4" };
        public override string DisplayName => $"{Intervals[(int)Type]} Second Timer";
        public override bool NeedsContinuousUpdate => true;

        static readonly string[] Intervals = new string[] { "1/4", "1/2", "1", "3", "5" };
        static readonly uint[] IntervalValues = new uint[] { 15, 30, 60, 120, 300 };

        static readonly Texture2D[] TexturesOn = new Texture2D[5];
        static readonly Texture2D[] TexturesOff = new Texture2D[5];

        internal TimerType Type = TimerType.Sec1;
        internal bool State = false;
        internal uint Counter = 0;

        public override void Draw(Rectangle rect, bool isScreenPos = false)
        {
            TerraLogic.SpriteBatch.Draw((State? TexturesOn : TexturesOff)[(int)Type], isScreenPos ? rect : PanNZoom.WorldToScreen(rect), Color.White);
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

        public override void WireSignal(int wire, Point origin)
        {
            State = !State;
        }

        public override void LoadContent(ContentManager content)
        {
            TexturesOn[0] = content.Load<Texture2D>("Tiles/TimerQ");
            TexturesOff[0] = content.Load<Texture2D>("Tiles/TimerQOff");

            TexturesOn[1] = content.Load<Texture2D>("Tiles/TimerH");
            TexturesOff[1] = content.Load<Texture2D>("Tiles/TimerHOff");

            TexturesOn[2] = content.Load<Texture2D>("Tiles/Timer1S");
            TexturesOff[2] = content.Load<Texture2D>("Tiles/Timer1SOff");

            TexturesOn[3] = content.Load<Texture2D>("Tiles/Timer3S");
            TexturesOff[3] = content.Load<Texture2D>("Tiles/Timer3SOff");

            TexturesOn[4] = content.Load<Texture2D>("Tiles/Timer5S");
            TexturesOff[4] = content.Load<Texture2D>("Tiles/Timer5SOff");
        }

        internal override Tile CreateTile(string data, bool preview)
        {
            if (data.Length < 2) return new Timer();

            bool state = data[0] == '+';
            int type;
            if (!int.TryParse(data.Substring(1), out type)) type = 2;

            return new Timer() { Type = (TimerType)type, State = state };
        }

        internal override string GetData()
        {
            return (State? "+" : "-") + ((int)Type).ToString();
        }

        internal enum TimerType { Quarter, Half, Sec1, Sec3, Sec5 }
    }
}
