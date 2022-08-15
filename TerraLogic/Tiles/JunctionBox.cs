﻿using Microsoft.Xna.Framework;
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

        static Texture2D Sprite;

        public override void LoadContent(ContentManager content)
        {
            Sprite = content.Load<Texture2D>("Tiles/JunctionBox");
        }

        public override void Draw(TransformedGraphics graphics)
        {
            Rectangle spriteRect = new Rectangle((int)Type * Gui.Logics.TileSize.X, 0, Gui.Logics.TileSize.X, Gui.Logics.TileSize.Y);

            graphics.Draw(Sprite, Vector2.Zero, spriteRect, Color.White);
        }

        public override void RightClick(bool held, bool preview)
        {
            if (held) return;
            Type++;
            if (Type > JunctionType.TR) Type = 0;
        }

        public override Tile Copy()
        {
            return new JunctionBox() { Type =  Type };
        }

        public override Tile CreateTile(string data, bool preview)
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
