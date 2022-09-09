using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TerraLogic.Gui;
using TerraLogic.Structures;

namespace TerraLogic
{
    static class Graphics
    {
        public static SpriteBatch? SpriteBatchOverride { get; set; }
        public static SpriteBatch SpriteBatch => SpriteBatchOverride ?? TerraLogic.SpriteBatch;

        public static void FillRectangle(Rect rect, Color color)
        {
            SpriteBatch.Draw(TerraLogic.Pixel, rect.Location, null, color, 0f, Vector2.Zero, rect.Size, SpriteEffects.None, 0);
        }
        public static void FillRectangle(float x, float y, float w, float h, Color color)
        {
            FillRectangle( new(x, y, w, h), color);
        }

        public static void DrawRectangle(Rect rect, Color color, int thickness = 1)
        {
            FillRectangle(rect.X + thickness, rect.Y, rect.Width - thickness, thickness, color);
            FillRectangle(rect.X, rect.Y, thickness, rect.Height - thickness, color);
            FillRectangle(rect.X, rect.Bottom - thickness, rect.Width - thickness, thickness, color);
            FillRectangle(rect.Right - thickness, rect.Y + thickness, thickness, rect.Height - thickness, color);
        }

        public static void DrawLine(Point a, Point b, Color color)
        {
            Vector vec = new(a.X - b.X, a.Y - b.Y);

            SpriteBatch.Draw(TerraLogic.Pixel, new Rectangle(b.X, b.Y, (int)vec.Length, 1), null, color, vec.Angle.Radians, Vector2.Zero, SpriteEffects.None, 0);
        }

        public static void DrawLineWithText(Point a, Point b, SpriteFont font, string text, Color color)
        {
            Vector vec = new Vector(b.X - a.X, b.Y - a.Y);
            float lineAngle = vec.Angle.Radians;

            SpriteBatch.Draw(TerraLogic.Pixel, new Rectangle(a.X, a.Y, (int)vec.Length, 1), null, color, lineAngle, Vector2.Zero, SpriteEffects.None, 0);

            Point textSize = font.MeasureString(text).ToPoint();

            vec.Length = vec.Length / 2 - textSize.X / 2;

            Vector textOffset = new Vector();
            textOffset.Length = textSize.Y / 2;
            textOffset.Angle = vec.Angle - Angle.FromDegrees(90);

            vec += textOffset;
            vec += a;

            SpriteBatch.DrawStringShaded(font, text, vec.ToVector2(), color, new Color((byte)0, (byte)0, (byte)0, color.A), lineAngle);
        }
        public static void DrawTileSprite(Texture2D sprite, int spriteX, int spriteY, Rect rect, Color color, int tileWidth = 1, int tileHeight = 1)
        {
            Rectangle source = new Rectangle(spriteX * tileWidth * Logics.TileSize.X, spriteY * tileHeight * Logics.TileSize.Y, tileWidth * Logics.TileSize.X, tileHeight * Logics.TileSize.Y);
            TerraLogic.SpriteBatch.Draw(sprite, rect.Location, source, color, 0f, Vector2.Zero, rect.Size / source.Size.ToVector2(), SpriteEffects.None, 0);
        }

    }
}
