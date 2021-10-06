using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TerraLogic.GuiElements;

namespace TerraLogic
{
    static class Ext
    {
        public static Vector2 Mod(this Vector2 v, int m) => new Vector2(v.X % m, v.Y % m);
        public static Rectangle WithOffset(this Rectangle rect, Point offset) 
        {
            return new Rectangle(rect.X + offset.X, rect.Y + offset.Y, rect.Width, rect.Height);
        }
        public static Rectangle PixelStretch(this Rectangle rect) 
        {
            return new Rectangle(rect.X - 1, rect.Y - 1, rect.Width + 2, rect.Height + 2);
        }

        public static Rectangle Mul(this Rectangle rect, int value)
        {
            return new Rectangle(rect.X * value, rect.Y * value, rect.Width * value, rect.Height * value);
        }

        public static Rectangle Mul(this Rectangle rect, Point value)
        {
            return new Rectangle(rect.X * value.X, rect.Y * value.Y, rect.Width * value.X, rect.Height * value.Y);
        }

        public static Color Div(this Color c, int value, bool divAlpha = false)
        {
            return new Color(c.R / value, c.G / value, c.B / value, divAlpha? c.A / value : c.A);
        }

        public static void DrawStringShaded(this SpriteBatch spriteBatch, SpriteFont font, string text, Vector2 pos, Color textColor, Color shadowColor)
        {
            spriteBatch.DrawString(font, text, pos + new Vector2(0, -1), shadowColor);
            spriteBatch.DrawString(font, text, pos + new Vector2(-1, 0), shadowColor);
            spriteBatch.DrawString(font, text, pos + new Vector2(0, 1), shadowColor);
            spriteBatch.DrawString(font, text, pos + new Vector2(1, 0), shadowColor);
            spriteBatch.DrawString(font, text, pos, textColor);
        }

        public static void DrawStringShaded(this SpriteBatch spriteBatch, SpriteFont font, string text, Vector2 pos, Color textColor, Color shadowColor, float angle)
        {
            spriteBatch.DrawString(font, text, pos + new Vector2(0, -1), shadowColor, angle, Vector2.Zero, 1, SpriteEffects.None, 0);
            spriteBatch.DrawString(font, text, pos + new Vector2(-1, 0), shadowColor, angle, Vector2.Zero, 1, SpriteEffects.None, 0);
            spriteBatch.DrawString(font, text, pos + new Vector2(0, 1), shadowColor, angle, Vector2.Zero, 1, SpriteEffects.None, 0);
            spriteBatch.DrawString(font, text, pos + new Vector2(1, 0), shadowColor, angle, Vector2.Zero, 1, SpriteEffects.None, 0);
            spriteBatch.DrawString(font, text, pos, textColor, angle, Vector2.Zero, 1, SpriteEffects.None, 0);
        }

        public static void DrawTileSprite(this SpriteBatch spriteBatch, Texture2D sprite, int spriteX, int spriteY, Rectangle destinationRectangle, Color color, int tileWidth = 1, int tileHeight = 1)
        {
            spriteBatch.Draw(sprite, destinationRectangle, new Rectangle(spriteX * tileWidth * Gui.Logics.TileSize.X, spriteY * tileHeight * Gui.Logics.TileSize.Y, tileWidth * Gui.Logics.TileSize.X, tileHeight * Gui.Logics.TileSize.Y), color);
        }

        public static void DrawStringShadedCentered(this SpriteBatch spriteBatch, SpriteFont font, string text, Rectangle rect, Color textColor, Color shadowColor)
        {
            Point size = font.MeasureString(text).ToPoint();
            Point textPos = new Point(rect.X + (rect.Width - size.X) / 2, rect.Y + (rect.Height - size.Y) / 2);
            rect = new Rectangle(textPos.X, textPos.Y, size.X, size.Y);

            int y = rect.Y;

            foreach (string sub in text.Split('\n')) 
            {
                size = font.MeasureString(sub).ToPoint();
                spriteBatch.DrawStringShaded(font, sub, new Vector2(rect.X + (rect.Width - size.X) / 2, y), textColor, shadowColor);
                y += size.Y;
            }
        }

        public static int Bits(this int value) 
        {
            int bits = 0;
            for (int i = 0; i < 32; i++) 
            {
                if ((value & 1) == 1) bits++;
                value >>= 1;
            }
            return bits;
        }
    }
}
