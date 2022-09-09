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

        public override void Draw()
        {
            DrawBackground();
            if (OutlineColor != Color.Transparent) Graphics.DrawRectangle(Bounds, OutlineColor);
            base.Draw();
        }

        public Color OutlineColor = Color.Transparent;
    }
}
