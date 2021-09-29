using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TerraLogic
{
    public struct RectangleF
    {
        public float X, Y, Width, Height;

        public RectangleF(float x, float y, float width, float height)
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
        }

        public Vector2 Location
        {
            get => new Vector2(X, Y);
            set { X = value.X; Y = value.Y; }
        }

        public Vector2 Size
        {
            get => new Vector2(X, Y);
            set { Width = value.X; Height = value.Y; }
        }

        public static implicit operator RectangleF(Rectangle r) => new RectangleF(r.X, r.Y, r.Width, r.Height);
    }
}
