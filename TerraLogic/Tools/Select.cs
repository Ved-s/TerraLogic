using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using TerraLogic.GuiElements;
using static TerraLogic.Graphics;

namespace TerraLogic.Tools
{
    public class Select : Tool
    {
        public static Select? Instance { get; private set; }

        public Select() { Instance = this; }

        public override string Id => "select";
        public override string DisplayName => "Select area";
        public override bool ShowWires => true;
        public override bool DrawMouseIcon => false;

        public World? SelectionWorld;
        public Rectangle Selection = new Rectangle();
        public bool NoSelection { get => SelectionWorld is null || Selection.Width == 0 || Selection.Height == 0; }

        bool Dragging;
        Point DragPos;

        public override void MouseKeyUpdate(MouseKeys key, EventType @event, Point pos)
        {
            if (key == MouseKeys.Left)
            {
                if (!Dragging && @event == EventType.Presssed)
                {
                    SelectionWorld = Gui.Logics.HoverWorld;
                    Dragging = true;
                    Vector2 worldpos = SelectionWorld.ScreenToTiles(pos);
                    worldpos.Round();
                    DragPos = worldpos.ToPoint();

                    if (DragPos.X < 0) DragPos.X = 0;
                    if (DragPos.Y < 0) DragPos.Y = 0;
                    if (DragPos.X > SelectionWorld.Width) DragPos.X = SelectionWorld.Width - 1;
                    if (DragPos.Y > SelectionWorld.Height) DragPos.Y = SelectionWorld.Height - 1;
                }
                else if (Dragging && @event == EventType.Hold)
                {
                    Vector2 worldPosVec = SelectionWorld!.ScreenToTiles(pos);
                    worldPosVec.Round();
                    Point worldpos = worldPosVec.ToPoint();

                    if (worldpos.X < 0) worldpos.X = 0;
                    if (worldpos.Y < 0) worldpos.Y = 0;
                    if (worldpos.X > SelectionWorld.Width) worldpos.X = SelectionWorld.Width - 1;
                    if (worldpos.Y > SelectionWorld.Height) worldpos.Y = SelectionWorld.Height - 1;

                    Selection = Util.RectFrom2Points(worldpos, DragPos);
                }
                else if (Dragging && @event == EventType.Released)
                {
                    Dragging = false;
                }
            }
            if (key == MouseKeys.Right) { Dragging = false; Selection = new Rectangle(); }
        }
        public override void KeyUpdate(Keys key, EventType @event, KeyboardState state)
        {
            if (@event == EventType.Presssed && !Dragging && !NoSelection)
            {
                if (key == Keys.Delete)
                {
                    for (int y = Selection.Y; y < Selection.Bottom; y++)
                        for (int x = Selection.X; x < Selection.Right; x++)
                        {
                            SelectionWorld!.SetTile(x, y, "");
                            SelectionWorld.Wires[x, y] = 0;
                        }
                }
                if (state.IsKeyDown(Keys.LeftControl))
                {
                    if (key == Keys.C)
                    {
                        Gui.Logics.PastePreview = SelectionWorld!.Copy(Selection);
                        Gui.Logics.PastePreview.BackgroundColor = Color.CornflowerBlue * 0.2f;
                        ClipboardUtils.World = Gui.Logics.PastePreview;
                    }
                    else if (key == Keys.X)
                    {
                        Gui.Logics.PastePreview = SelectionWorld!.Copy(Selection);
                        Gui.Logics.PastePreview.BackgroundColor = Color.CornflowerBlue * 0.2f;
                        ClipboardUtils.World = Gui.Logics.PastePreview;
                        for (int y = Selection.Y; y < Selection.Bottom; y++)
                            for (int x = Selection.X; x < Selection.Right; x++)
                            {
                                SelectionWorld.SetTile(x, y, "");
                                SelectionWorld.Wires[x, y] = 0;
                            }
                    }
                }
            }
        }

        public override void Deselected()
        {
            if (Dragging) Dragging = false;
        }

        public override void Draw(bool selected)
        {
            if (NoSelection) return;

            Rectangle rect = SelectionWorld!.WorldToScreen(Selection.Mul(16));

            string str = $"{Selection.Width}x{Selection.Height}";
            if (!Dragging)
            {
                if (selected) str += $"\nPress DEL to remove tiles";
                else if (Gui.Logics.SelectedTilePreview is not null) str += $"\nClick in the area to fill it";
            }

            TerraLogic.SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
            TerraLogic.SpriteBatch.DrawStringShadedCentered(TerraLogic.Consolas10, str, rect, Color.White, Color.Black);
            TerraLogic.SpriteBatch.End();

            TerraLogic.SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive);
            FillRectangle(rect, Color.CornflowerBlue.Div(4, true));
            DrawRectangle(rect, Color.CornflowerBlue);
            TerraLogic.SpriteBatch.End();
        }
    }

}
