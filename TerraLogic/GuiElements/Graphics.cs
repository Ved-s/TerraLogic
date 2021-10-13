using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TerraLogic.GuiElements
{
    static class Graphics
    {
        public struct Angle 
        {
            public float Radians { get; set; }
            public float Degrees 
            {
                get => Radians / (float)Math.PI * 180f;
                set => Radians = value / 180f * (float)Math.PI;
            }

            public static Angle FromRadians(float rad) => new Angle() { Radians = rad };
            public static Angle FromDegrees(float deg) => new Angle() { Degrees = deg };

            public static Angle operator +(Angle a, Angle b) => new Angle() { Radians = a.Radians + b.Radians };
            public static Angle operator -(Angle a, Angle b) => new Angle() { Radians = a.Radians - b.Radians };
        }

        public struct Vector 
        {
            public float X { get; set; }
            public float Y{ get; set; }

            public Vector(float x, float y)
            {
                X = x;
                Y = y;
            }
            public Vector(Vector vec)
            {
                X = vec.X;
                Y = vec.Y;
            }
            public Vector(float length, Angle angle) 
            {
                X = length * (float)Math.Cos(angle.Radians);
                Y = length * (float)Math.Sin(angle.Radians);
            }

            public float Length 
            {
                get => (float)Math.Sqrt(X * X + Y * Y);
                set 
                {
                    float angle = Angle.Radians;
                    X = value * (float)Math.Cos(angle);
                    Y = value * (float)Math.Sin(angle);
                }
            }
            public Angle Angle  
            {
                get => Angle.FromRadians((float)Math.Atan2(Y, X));
                set 
                {
                    float length = Length;
                    X = length * (float)Math.Cos(value.Radians);
                    Y = length * (float)Math.Sin(value.Radians);
                }
            }

            public static Vector operator +(Vector a, Vector b) => new Vector(a.X + b.X, a.Y + b.Y);
            public static Vector operator -(Vector a, Vector b) => new Vector(a.X - b.X, a.Y - b.Y);

            public static Vector operator +(Vector a, Vector2 b) => new Vector(a.X + b.X, a.Y + b.Y);
            public static Vector operator -(Vector a, Vector2 b) => new Vector(a.X - b.X, a.Y - b.Y);

            public static Vector operator +(Vector a, Point b) => new Vector(a.X + b.X, a.Y + b.Y);
            public static Vector operator -(Vector a, Point b) => new Vector(a.X - b.X, a.Y - b.Y);

            public static implicit operator Vector(Vector2 v) => new Vector(v.X, v.Y);
            public static implicit operator Vector2(Vector v) => new Vector2(v.X, v.Y);

            public Vector2 ToVector2() => new Vector2(X, Y);
        }

        public static void FillRectangle(SpriteBatch spriteBatch, Rectangle rect, Color color) 
        {
            spriteBatch.Draw(TerraLogic.Pixel, rect, color);
        }
        public static void FillRectangle(SpriteBatch spriteBatch, int x, int y, int w, int h, Color color)
        {
            spriteBatch.Draw(TerraLogic.Pixel, new Rectangle(x,y,w,h), color);
        }
        public static void DrawRectangle(SpriteBatch spriteBatch, Rectangle rect, Color color, int thickness = 1)
        {
            spriteBatch.Draw(TerraLogic.Pixel, new Rectangle(rect.X, rect.Y, rect.Width, thickness),  color);
            spriteBatch.Draw(TerraLogic.Pixel, new Rectangle(rect.X, rect.Y, thickness, rect.Height), color);
            spriteBatch.Draw(TerraLogic.Pixel, new Rectangle(rect.X, rect.Bottom - thickness, rect.Width, thickness), color);
            spriteBatch.Draw(TerraLogic.Pixel, new Rectangle(rect.Right - thickness, rect.Y, thickness, rect.Height), color);
        }

        public static void DrawLine(SpriteBatch spriteBatch, Point a, Point b, Color color)
        {
            Vector vec = new Vector(a.X - b.X, a.Y - b.Y);

            spriteBatch.Draw(TerraLogic.Pixel, new Rectangle(b.X, b.Y, (int)vec.Length, 1), null, color, vec.Angle.Radians, Vector2.Zero, SpriteEffects.None, 0);
        }

        public static void DrawLineWithText(SpriteBatch spriteBatch, Point a, Point b, SpriteFont font, string text, Color color)
        {
            Vector vec = new Vector(b.X - a.X, b.Y - a.Y);
            float lineAngle = vec.Angle.Radians;

            spriteBatch.Draw(TerraLogic.Pixel, new Rectangle(a.X, a.Y, (int)vec.Length, 1), null, color, lineAngle, Vector2.Zero, SpriteEffects.None, 0);

            Point textSize = font.MeasureString(text).ToPoint();

            vec.Length = vec.Length / 2 - textSize.X / 2;

            Vector textOffset = new Vector();
            textOffset.Length = textSize.Y / 2;
            textOffset.Angle = vec.Angle - Angle.FromDegrees(90);

            vec += textOffset;
            vec += a;

            spriteBatch.DrawStringShaded(font, text, vec.ToVector2(), color, new Color(0,0,0,color.A), lineAngle);
        }

    }
}
