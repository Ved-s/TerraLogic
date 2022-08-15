using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
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

        public override void Draw(TransformedGraphics graphics)
        {
            graphics.DrawTileSprite(Sprite, State?1:0, 0, Vector2.Zero, Color.White);
        }

        public override Tile Copy()
        {
            return new Switch() { State = State };
        }

        public override Tile CreateTile(string data, bool preview)
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

        public override void Save(BinaryWriter writer)
        {
            writer.Write(State);
        }

        public override void Load(BinaryReader reader)
        {
            State = reader.ReadBoolean();
        }

        public override void LoadContent(ContentManager content)
        {
            Sprite = content.Load<Texture2D>("Tiles/Switch");
        }
    }
}
