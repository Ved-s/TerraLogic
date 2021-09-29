using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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
    }
}
