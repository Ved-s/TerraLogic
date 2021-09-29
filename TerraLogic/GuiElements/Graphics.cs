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

    }
}
