using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TerraLogic.GuiElements
{
    public class UILabel : UIElement
    {
        public UILabel(string? name = null) : base(name) { }

        public override string Text
        {
            get => base.Text;
            set
            {
                base.Text = value;
                RecalculateSize();
            }
        }

        public override UIElement? Parent 
        { 
            get => base.Parent;
            set
            {
                base.Parent = value;
                Recalculate();
            }
        }

        public override SpriteFont Font
        {
            get => base.Font;
            set
            {
                base.Font = value;
                RecalculateSize();
            }
        }

        private void RecalculateSize()
        {
            if (Text is not null && AutoSize && Font is not null)
            {
                Point wh = Font.MeasureString(Text).ToPoint();
                if (Bounds.Width != wh.X) Width = wh.X;
                if (Bounds.Height != wh.Y) Height = wh.Y;
            }
        }

        public override void Draw()
        {

            if (Font is null) return;
            DrawBackground();
            Vector2 pos = Bounds.Location.ToVector2();
            if (CenterText)
            {
                Vector2 textSize = Font.MeasureString(Text);
                pos += new Vector2(Bounds.Width, Bounds.Height) / 2 - textSize / 2;
            }

            pos.X = (int)pos.X;
            pos.Y = (int)pos.Y;

            if (Shadow)
            {
                TerraLogic.SpriteBatch.DrawString(Font, Text, pos + new Vector2(0, -1), ShadowColor);
                TerraLogic.SpriteBatch.DrawString(Font, Text, pos + new Vector2(-1, 0), ShadowColor);
                TerraLogic.SpriteBatch.DrawString(Font, Text, pos + new Vector2(0, 1), ShadowColor);
                TerraLogic.SpriteBatch.DrawString(Font, Text, pos + new Vector2(1, 0), ShadowColor);
            }

            TerraLogic.SpriteBatch.DrawString(Font, Text, pos, TextColor);

        }

        public bool AutoSize = true;
        public bool CenterText = false;
        public bool Shadow = false;
        public Color ShadowColor = Color.Black;

        
    }
}
