using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TerraLogic.Structures
{
    public struct Rect
    {
        public float X;
        public float Y;
        public float Width;
        public float Height;

        public float Left => X;
        public float Top => Y;
        public float Right => X + Width;
        public float Bottom => Y + Height;

        public Rect(float x, float y, float width, float height)
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
        }

        public Rect(Vector2 pos, Vector2 size)
        {
            X = pos.X;
            Y = pos.Y;
            Width = size.X;
            Height = size.Y;
        }

        public Vector2 Location
        {
            get => new Vector2(X, Y);
            set { X = value.X; Y = value.Y; }
        }

        public Vector2 Size
        {
            get => new Vector2(Width, Height);
            set { Width = value.X; Height = value.Y; }
        }

        public bool Contains(Vector2 v)
        {
            return Left <= v.X && Top <= v.Y && Right >= v.X && Bottom >= v.Y;
        }

        public Rect Intersection(Rect rect)
        {
            Vector2 minBR = new(Math.Min(Right, rect.Right), Math.Min(Bottom, rect.Bottom));
            Vector2 maxTL = new(Math.Max(Left, rect.Left), Math.Max(Top, rect.Top));

            return new Rect()
            {
                X = maxTL.X,
                Y = maxTL.Y,
                Width = minBR.X - maxTL.X,
                Height = minBR.Y - maxTL.Y
            };
        }

        public static implicit operator Rect(Rectangle r) => new Rect(r.X, r.Y, r.Width, r.Height);
        public static explicit operator Rectangle(Rect r) => new Rectangle((int)r.X, (int)r.Y, (int)r.Width, (int)r.Height);
    }
}
