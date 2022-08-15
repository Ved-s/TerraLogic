using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
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

        public static Rectangle Intersection(this Rectangle rect1, Rectangle rect2)
        {
            Point minBR = new(Math.Min(rect1.Right, rect2.Right), Math.Min(rect1.Bottom, rect2.Bottom));
            Point maxTL = new(Math.Max(rect1.Left, rect2.Left), Math.Max(rect1.Top, rect2.Top));

            return new Rectangle()
            {
                X = maxTL.X,
                Y = maxTL.Y,
                Width = minBR.X - maxTL.X,
                Height = minBR.Y - maxTL.Y
            };
        }

        public static void CopyExact(this Stream from, Stream to, int copyLength, int bufferLength = 81920) 
        {
            if (from.CanSeek)
            {
                long length = from.Length;
                long position = from.Position;
                if (length <= position)
                {
                    bufferLength = 1;
                }
                else
                {
                    long diff = length - position;
                    if (diff > 0L)
                    {
                        bufferLength = (int)Math.Min((long)bufferLength, diff);
                    }
                }
            }

            byte[] array = ArrayPool<byte>.Shared.Rent(bufferLength);
            try
            {
                int read;
                while (copyLength > 0)
                {
                    read = from.Read(array, 0, Math.Min(array.Length, copyLength));
                    if (read == 0) break;
                    copyLength -= read;
                    to.Write(array, 0, read);
                }
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(array, false);
            }
        }

        public static bool SequenceStartsWith<T>(this IEnumerable<T> seq, IEnumerable<T> compareTo) 
        {
            IEnumerator<T> seqEnum = seq.GetEnumerator();
            IEnumerator<T> compareToEnum = compareTo.GetEnumerator();

            while (true)
            {
                bool seqMoved = seqEnum.MoveNext();
                bool compareToMoved = compareToEnum.MoveNext();

                if (!compareToMoved) break;
                if (!seqMoved) return false;

                if (seqEnum.Current is null)
                {
                    if (compareToEnum.Current is null) continue;
                    if (!compareToEnum.Current.Equals(seqEnum.Current)) return false;
                }
                else if (!seqEnum.Current.Equals(compareToEnum.Current)) return false;
            }
            return true;
        }

        public static bool IsNullEmptyOrWhitespace(this string str)
            => string.IsNullOrWhiteSpace(str) || string.IsNullOrEmpty(str);

        public static Point Constrain(this Point p, Rectangle rect) 
        {
            if (p.X < rect.Left) 
                p.X = rect.Left;
            if (p.Y < rect.Top)
                p.Y = rect.Top;
            if (p.X >= rect.Right)
                p.X = rect.Right - 1;
            if (p.Y >= rect.Bottom)
                p.Y = rect.Bottom - 1;
            return p;
        }
    }
}
