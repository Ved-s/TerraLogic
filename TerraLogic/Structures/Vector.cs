using Microsoft.Xna.Framework;
using System;

namespace TerraLogic.Structures
{
    public struct Vector
    {
        public float X { get; set; }
        public float Y { get; set; }

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
}
