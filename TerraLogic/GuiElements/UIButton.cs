using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TerraLogic.GuiElements
{
    public class UIButton : UIElement
    {
        public UIButton(string name = null) : base(name) { }

        public override void Draw(SpriteBatch spriteBatch)
        {
            Vector2 textSize = Font.MeasureString(Text);
            Vector2 textOffset = new Vector2(Bounds.Width, Bounds.Height) / 2 - textSize / 2;

            textOffset.X = (int)textOffset.X;
            textOffset.Y = (int)textOffset.Y;

            

            if (Hover) DrawBackground(spriteBatch, HoverBackColor);
            else DrawBackground(spriteBatch);

            if (OutlineColor != Color.Transparent) Graphics.DrawRectangle(spriteBatch, Bounds, OutlineColor);

            spriteBatch.DrawString(Font, Text, Bounds.Location.ToVector2() + textOffset, (Hover && HoverTextColor != Color.Transparent) ? HoverTextColor : TextColor);
        }

        public Color OutlineColor = Color.Transparent;

        public Color HoverBackColor = Color.Transparent;
        public Color HoverTextColor = Color.Transparent;
    }
}
