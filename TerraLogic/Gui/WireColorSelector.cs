using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TerraLogic.GuiElements;

namespace TerraLogic.Gui
{
    internal class WireColorSelector : GuiElements.UIElement
    {
        public override Pos Width => 18;
        public override Pos Height => (Gui.Logics.WireColorMapping.Length) * 18;

        public WireColorSelector(string name) : base(name)
        {
            BackColor = new Color(32,32,32);
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            DrawBackground(spriteBatch);

            int ypos = 3;
            Rectangle hover;

            for (int i = 0; i < Gui.Logics.WireColorMapping.Length; i++)
            {
                hover = new Rectangle(0, ypos - 3, 18, 18);
                if (hover.Contains(MousePosition)) Graphics.FillRectangle(spriteBatch, hover.WithOffset(Bounds.Location), new Color(64, 64, 64));

                Rectangle rect = new Rectangle(3, ypos, 12, 12).WithOffset(Bounds.Location);

                Color c = Logics.WireColorMapping[i];
                if (Logics.SelectedWireColor != i)
                {
                    c.R /= 2;
                    c.G /= 2;
                    c.B /= 2;
                }
                else
                {
                    Graphics.DrawRectangle(spriteBatch, rect, Color.White);
                    rect.X++;
                    rect.Y++;
                    rect.Width -= 2;
                    rect.Height -= 2;
                }
                Graphics.FillRectangle(spriteBatch, rect, c);

                ypos += 18;
            }

        }

        protected internal override void MouseKeyStateUpdate(MouseKeys key, EventType @event, Point pos)
        {
            if (@event == EventType.Presssed && key == MouseKeys.Left)
            {
                Logics.SelectedWireColor = (byte)(pos.Y / 18);
                Logics.SelectedTileId = null;
                Logics.SelectedTilePreview = null;
                Logics.SelectedToolId = -1;

            }
        }

    }
}
