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
        public UILabel(string name = null) : base(name) { }

        public override string Text
        {
            get => base.Text;
            set
            {
                base.Text = value;
                RecalculateSize();
            }
        }

        public override UIElement Parent 
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
            if (Text != null && AutoSize && Font != null)
            {
                Point wh = Font.MeasureString(Text).ToPoint();
                Width = wh.X;
                Height = wh.Y;
            }
        }

        public override void Draw(SpriteBatch spriteBatch)
        {

            if (Font is null) return;
            DrawBackground(spriteBatch);
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
                spriteBatch.DrawString(Font, Text, pos + new Vector2(0, -1), ShadowColor);
                spriteBatch.DrawString(Font, Text, pos + new Vector2(-1, 0), ShadowColor);
                spriteBatch.DrawString(Font, Text, pos + new Vector2(0, 1), ShadowColor);
                spriteBatch.DrawString(Font, Text, pos + new Vector2(1, 0), ShadowColor);
            }

            spriteBatch.DrawString(Font, Text, pos, TextColor);

        }

        public bool AutoSize = true;
        public bool CenterText = false;
        public bool Shadow = false;
        public Color ShadowColor = Color.Black;

        
    }
}
