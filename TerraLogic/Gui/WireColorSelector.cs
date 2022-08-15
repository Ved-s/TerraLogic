using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using TerraLogic.GuiElements;

namespace TerraLogic.Gui
{
    internal class WireColorSelector : GuiElements.UIElement
    {
        public override Pos Width => 18;
        public override Pos Height => (Gui.Logics.WireColorMapping.Count) * 18 + (Locked || Gui.Logics.WireColorMapping.Count >= 32 ? 0 : 18);

        bool Locked = false;

        public WireColorSelector(string name) : base(name)
        {
            BackColor = new Color(32,32,32);
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            DrawBackground(spriteBatch);

            Graphics.DrawRectangle(spriteBatch, Bounds, new Color(48, 48, 48));

            int ypos = 3;
            Rectangle hover;

            HoverText = null;

            for (int i = 0; i < Gui.Logics.WireColorMapping.Count; i++)
            {
                Color c = Logics.WireColorMapping[i];

                hover = new Rectangle(0, ypos - 3, 18, 18);
                if (Hover && hover.Contains(MousePosition))
                {
                    Graphics.FillRectangle(spriteBatch, hover.WithOffset(Bounds.Location), new Color(64, 64, 64));
                    HoverText = c.PackedValue.ToString("x8").Substring(2) + (Locked? "" : "\nPress DEL to remove\nRight-Click to edit");
                }

                Rectangle rect = new Rectangle(3, ypos, 12, 12).WithOffset(Bounds.Location);

                if (!World.GetWire(Logics.SelectedWire, (byte)i))
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
            if (!Locked && Gui.Logics.WireColorMapping.Count < 32)
            {
                hover = new Rectangle(0, ypos - 3, 18, 18);
                if (hover.Contains(MousePosition))
                {
                    Graphics.FillRectangle(spriteBatch, hover.WithOffset(Bounds.Location), new Color(64, 64, 64));
                    HoverText = "Add wire";
                }

                Graphics.FillRectangle(spriteBatch, new Rectangle(8, ypos, 2, 12).WithOffset(Bounds.Location), Color.White);
                Graphics.FillRectangle(spriteBatch, new Rectangle(3, ypos + 5, 12, 2).WithOffset(Bounds.Location), Color.White);
            }
        }

        protected internal override void MouseKeyStateUpdate(MouseKeys key, EventType @event, Point pos)
        {
            if (@event == EventType.Presssed && key == MouseKeys.Left)
            {
                byte newColor = (byte)(pos.Y / 18);

                if (newColor >= Logics.WireColorMapping.Count)
                {
                    if (Logics.WireColorMapping.Count >= 32) return;
                    Locked = true;
                    PositionRecalculateRequired = true;

                    ColorSelector.Instance.ShowDialog(null, (cancel, color) =>
                    {
                        if (!cancel)
                            Logics.WireColorMapping.Add((Color)color);
                        Locked = false;
                        PositionRecalculateRequired = true;
                    }, null, Logics.Wire, Logics.CalculateWireSpriteOffset(true,true,true,true));
                }
                else
                {
                    Logics.SelectedWire ^= (1 << newColor);
                    Logics.SelectedTileId = null;
                    Logics.SelectedTilePreview = null;
                    if (Logics.SelectedToolId != -1)
                    {
                        if (!Logics.Tools[Logics.SelectedToolId].AllowWireSelection)
                        {
                            Logics.Tools[Logics.SelectedToolId].IsSelected = false;
                            Logics.SelectedToolId = -1;
                        }
                        else Logics.Tools[Logics.SelectedToolId].WireColorChanged();
                    }

                    Logics.PastePreview = null;
                }
            }

            if (@event == EventType.Presssed && key == MouseKeys.Right && !Locked)
            {
                byte id = (byte)(pos.Y / 18);

                if (id < Logics.WireColorMapping.Count)
                {

                    Locked = true;
                    PositionRecalculateRequired = true;

                    ColorSelector.Instance.ShowDialog(Logics.WireColorMapping[id], (cancel, color) =>
                    {
                        Logics.WireColorMapping[id] = (Color)color;
                        Locked = false;
                        PositionRecalculateRequired = true;

                    },
                    (c) => Logics.WireColorMapping[id] = c, Logics.Wire, Logics.CalculateWireSpriteOffset(true, true, true, true));
                }
            }
        }
        protected internal override void KeyStateUpdate(Keys key, EventType @event)
        {
            base.KeyStateUpdate(key, @event);

            if (Hover && @event == EventType.Presssed && key == Keys.Delete && !Locked)
            {
                byte id = (byte)(MousePosition.Y / 18);

                if (id < Logics.WireColorMapping.Count)
                {
                    Logics.WireColorMapping.RemoveAt(id);
                    PositionRecalculateRequired = true;
                }
            }
        }
    }
}
