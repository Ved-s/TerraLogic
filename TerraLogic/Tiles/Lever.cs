using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TerraLogic.Tiles
{
    class Lever : Tile
    {
        public override string Id => "lever";
        public override Point Size => new Point(2,2);
        public override string DisplayName => "Lever";

        bool state = false;

        static Texture2D Up, Down;

        public override void Draw(Rectangle rect, bool isScreenPos = false)
        {
            TerraLogic.SpriteBatch.Draw(state?Down:Up, isScreenPos? rect: PanNZoom.WorldToScreen(rect), Color.White);
        }

        internal override Tile CreateTile(string data, bool preview)
        {
            return new Lever() { state = data == "+" };
        }

        public override void RightClick(bool held, bool preview)
        {
            if (held) return;
            state = !state;
            SendSignal();
        }

        internal override string GetData()
        {
            return state ? "+" : null;
        }

        public override void LoadContent(ContentManager content)
        {
            Up = content.Load<Texture2D>("Tiles/BigSwitchUp");
            Down = content.Load<Texture2D>("Tiles/BigSwitchDown");
        }
    }
}
