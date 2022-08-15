using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System.IO;

namespace TerraLogic.Tiles
{
    class GemsparkBlock : Tile
    {
        public override string Id => "gemblock";
        public override string DisplayName => $"{(State? "" : "Offline ")}{ColorUils.ClosestColor(Color).Name} Gemspark Block ({Color.R:x2}{Color.G:x2}{Color.B:x2})\nShift+Right Click to change color";

        public override string[] PreviewDataVariants => new string[] { "+ffffff" };

        public bool State;
        public Color Color = Color.White;

        static Texture2D Sprite;

        public override void LoadContent(ContentManager content)
        {
            Sprite = content.Load<Texture2D>("Tiles/GemsparkBlock");
        }

        public override void Draw(TransformedGraphics graphics)
        {
            Color c = Color;

            if (!State)
            {
                c.R /= 3;
                c.G /= 3;
                c.B /= 3;
            }

            if (Created)
            {

                byte neighbours = 0;

                if (World.Tiles[Pos.X + 0, Pos.Y - 1] is GemsparkBlock) neighbours |= 1;
                if (World.Tiles[Pos.X + 1, Pos.Y - 1] is GemsparkBlock) neighbours |= 2;
                if (World.Tiles[Pos.X + 1, Pos.Y + 0] is GemsparkBlock) neighbours |= 4;
                if (World.Tiles[Pos.X + 1, Pos.Y + 1] is GemsparkBlock) neighbours |= 8;

                if (World.Tiles[Pos.X + 0, Pos.Y + 1] is GemsparkBlock) neighbours |= 16;
                if (World.Tiles[Pos.X - 1, Pos.Y + 1] is GemsparkBlock) neighbours |= 32;
                if (World.Tiles[Pos.X - 1, Pos.Y + 0] is GemsparkBlock) neighbours |= 64;
                if (World.Tiles[Pos.X - 1, Pos.Y - 1] is GemsparkBlock) neighbours |= 128;

              
                Rectangle spriteRect = new Rectangle(
                    (int)(Gui.Logics.TileSize.X * PackedSprite.CalculatedPositions[neighbours].X),
                    (int)(Gui.Logics.TileSize.Y * PackedSprite.CalculatedPositions[neighbours].Y),
                    (int)(Gui.Logics.TileSize.X),
                    (int)(Gui.Logics.TileSize.Y));

                graphics.Draw(Sprite, Vector2.Zero, spriteRect, c);
            }
            else graphics.Draw(Sprite, Vector2.Zero, new Rectangle(0,0,16,16), c);
        }

        public override Tile Copy()
        {
            return new GemsparkBlock() { State = State, Color = Color };
        }

        public override Tile CreateTile(string data, bool preview)
        {
            if (data is null || data.Length != 7) return new GemsparkBlock();

            bool state = data[0] == '+';
            uint color = uint.Parse(data.Substring(1), System.Globalization.NumberStyles.HexNumber) | 0xff000000;

            return new GemsparkBlock() { State = state, Color = new Color() { PackedValue = color } };
        }

        internal override string GetData()
        {
            return $"{(State ? "+" : "-")}{Color.B:x2}{Color.G:x2}{Color.R:x2}";
        }

        public override void RightClick(bool held, bool preview)
        {
            if (held) return;
            if (TerraLogic.Root.CurrentKeys.IsKeyDown(Keys.LeftShift)) 
            {
                Gui.ColorSelector.Instance.ShowDialog(Color, (cancel, color) =>
                {
                    Color = (Color)color;
                },
                (c) => Color = c, Sprite, new Rectangle(0,0,16,16));
            }
            else State = !State;
        }

        public override void WireSignal(int wire, Point origin, Point inputPosition)
        {
            if (wire.Bits() % 2 == 1)
                State = !State;
        }

        public override void Save(BinaryWriter writer)
        {
            writer.Write(State);
            writer.Write(Color.PackedValue);
        }

        public override void Load(BinaryReader reader)
        {
            State = reader.ReadBoolean();
            Color.PackedValue = reader.ReadUInt32();
        }
    }
}
