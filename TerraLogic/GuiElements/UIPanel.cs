using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TerraLogic.GuiElements
{
    public class UIPanel : UIElement
    {
        public UIPanel(string name) : base(name) { }

        public override void Draw(SpriteBatch spriteBatch)
        {
            DrawBackground(spriteBatch);
            if (OutlineColor != Color.Transparent) Graphics.DrawRectangle(spriteBatch, Bounds, OutlineColor);
            base.Draw(spriteBatch);
        }

        public Color OutlineColor = Color.Transparent;
    }
}
