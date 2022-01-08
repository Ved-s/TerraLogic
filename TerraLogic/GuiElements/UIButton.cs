using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
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

            bool clicked = Hover && Root.CurrentMouseKeys.LeftButton == ButtonState.Pressed && ClickColors.HasValue;

            if (Hover)
            {
                if (clicked) DrawBackground(spriteBatch, ClickColors.Value.Background);
                else DrawBackground(spriteBatch, HoverColors.Background);
            }
            else DrawBackground(spriteBatch);

            if (OutlineColor != Color.Transparent) Graphics.DrawRectangle(spriteBatch, Bounds, OutlineColor);

            Color text =
                clicked ? ClickColors.Value.Foreground :
                Hover ? HoverColors.Foreground :
                TextColor;

            spriteBatch.DrawString(Font, Text, Bounds.Location.ToVector2() + textOffset, text);


        }

        public virtual Color OutlineColor { get; set; } = Color.Transparent;

        public virtual Colors HoverColors { get; set; } = new Colors(Color.White, Color.Transparent);
        public virtual Colors? ClickColors { get; set; }
    }
}
