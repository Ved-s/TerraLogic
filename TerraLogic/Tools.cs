using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using TerraLogic.GuiElements;

namespace TerraLogic.Tools
{
    public abstract class Tool
    {
        public abstract string Id { get; }
        public abstract string DisplayName { get; }
        public Texture2D Texture;

        public virtual bool ShowWires => false;

        public abstract void MouseKeyUpdate(MouseKeys key, EventType @event, Point worldpos);
        public virtual void KeyUpdate(Keys key, EventType @event, KeyboardState state) { }

        public virtual void Selected() { }
        public virtual void Deselected() { }

        public virtual void Update() { }
        public virtual void Draw(SpriteBatch spriteBatch, bool selected) { }
    }

    public class Remove : Tool
    {
        public override string Id => "remove";
        public override string DisplayName => "Remove tile";

        public override void MouseKeyUpdate(MouseKeys key, EventType @event, Point worldpos)
        {
            if (key == MouseKeys.Left && @event != EventType.Released) Gui.Logics.SetTile(worldpos, null);
        }
    }

    public class Select : Tool
    {
        public static Select Instance { get; private set; }

        public Select() { Instance = this; }

        public override string Id => "select";
        public override string DisplayName => "Select area";
        public override bool ShowWires => true;

        public Rectangle Selection = new Rectangle();
        public bool NoSelection { get => Selection.Width == 0 || Selection.Height == 0; }

        bool Dragging;
        Point DragPos;

        public override void MouseKeyUpdate(MouseKeys key, EventType @event, Point worldpos)
        {
            if (key == MouseKeys.Left) 
            {
                if (!Dragging && @event == EventType.Presssed)
                {
                    Dragging = true;
                    DragPos = worldpos;
                }
                else if (Dragging && @event == EventType.Hold)
                {
                    Selection = Util.RectFrom2Points(worldpos, DragPos);
                }
                else if (Dragging && @event == EventType.Released) 
                {
                    Dragging = false;
                }
            }
            if (key == MouseKeys.Right) { Dragging = false;  Selection = new Rectangle(); }
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
                            Gui.Logics.SetTile(x, y, null);
                            Gui.Logics.WireArray[x, y] = 0;
                        }
                }
                if (state.IsKeyDown(Keys.LeftControl))
                {
                    if (key == Keys.C)
                    {
                        Gui.Logics.CopyToClipboard(Selection);
                    }
                    else if (key == Keys.X)
                    {
                        Gui.Logics.CopyToClipboard(Selection);
                        for (int y = Selection.Y; y < Selection.Bottom; y++)
                            for (int x = Selection.X; x < Selection.Right; x++)
                            {
                                Gui.Logics.SetTile(x, y, null);
                                Gui.Logics.WireArray[x, y] = 0;
                            }
                    }
                }
            }
        }

        public override void Deselected()
        {
            if (Dragging) Dragging = false;
        }

        public override void Draw(SpriteBatch spriteBatch, bool selected)
        {
            if (NoSelection) return;
            
            Rectangle rect = PanNZoom.WorldToScreen(Selection.Mul(16));

            string str = $"{Selection.Width}x{Selection.Height}";
            if (!Dragging)
            {
                if (selected) str += $"\nPress DEL to remove tiles";
                else if (Gui.Logics.SelectedTilePreview != null) str += $"\nClick in the area to fill it";
            }

            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
            spriteBatch.DrawStringShadedCentered(TerraLogic.Consolas10, str, rect, Color.White, Color.Black);
            spriteBatch.End();

            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive);
            Graphics.FillRectangle(spriteBatch, rect, Color.CornflowerBlue.Div(4, true));
            Graphics.DrawRectangle(spriteBatch, rect, Color.CornflowerBlue);
            spriteBatch.End();
        }
    }

}
