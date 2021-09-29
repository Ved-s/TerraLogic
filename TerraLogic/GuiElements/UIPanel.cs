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
            spriteBatch.Begin();
            DrawBackground(spriteBatch);
            spriteBatch.End();

            base.Draw(spriteBatch);
        }
    }
}
