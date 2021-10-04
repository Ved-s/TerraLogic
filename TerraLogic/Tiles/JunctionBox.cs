using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TerraLogic.Tiles
{
    class JunctionBox : Tile
    {
        public override string Id => "junction";
        public override string DisplayName => "Junction Box";

        public JunctionType Type = JunctionType.Cross;

        static Texture2D Cross, TL;

        public override void LoadContent(ContentManager content)
        {
            Cross = content.Load<Texture2D>("Tiles/JunctionBoxCross");
            TL = content.Load<Texture2D>("Tiles/JunctionBoxTL");
        }

        public override void Draw(Rectangle rect, bool isScreenPos = false)
        {
            TerraLogic.SpriteBatch.Draw(
                (Type == JunctionType.Cross) ? Cross : TL,
                isScreenPos ? rect : PanNZoom.WorldToScreen(rect), null, Color.White, 0f, Vector2.Zero,
                Type == JunctionType.TR ? SpriteEffects.FlipHorizontally : SpriteEffects.None, 0);
        }

        public override void RightClick(bool held, bool preview)
        {
            if (held) return;
            Type++;
            if (Type > JunctionType.TR) Type = 0;
        }

        internal override Tile CreateTile(string data, bool preview)
        {
            return new JunctionBox() { Type = int.TryParse(data, out int type)? (JunctionType)type : JunctionType.Cross };
        }

        internal override string GetData()
        {
            return ((int)Type).ToString();
        }

        public enum JunctionType { Cross, TL, TR }
    }
}
