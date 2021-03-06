using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TerraLogic.Tiles
{
    class Switch : Tile
    {
        public override string Id => "switch";
        public override string DisplayName => "Switch";

        bool State = false;

        static Texture2D Sprite;

        public override void Draw(Rectangle rect, bool isScreenPos = false)
        {
            TerraLogic.SpriteBatch.DrawTileSprite(Sprite, State?1:0, 0, isScreenPos? rect: PanNZoom.WorldToScreen(rect), Color.White);
        }

        internal override Tile CreateTile(string data, bool preview)
        {
            return new Switch() { State = data == "+" };
        }

        public override void RightClick(bool held, bool preview)
        {
            if (held) return;
            State = !State;
            SendSignal();
        }

        internal override string GetData()
        {
            return State ? "+" : null;
        }

        public override void LoadContent(ContentManager content)
        {
            Sprite = content.Load<Texture2D>("Tiles/Switch");
        }
    }
}
