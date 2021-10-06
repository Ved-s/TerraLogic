using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using TerraLogic.GuiElements;
using TerraLogic.Tiles;

namespace TerraLogic.Gui
{
    internal class TileSelector : UIElement
    {
        public TileSelector(string name) : base(name)
        {
            BackColor = new Color(32, 32, 32);
        }

        public override Pos Height => 32;
        public override Pos Width => Logics.TilePreviews.Count * 32;

        string CurrentlyHoveredId = null;
        Tile CurrentlyHoveredTile = null;
        Rectangle CurrentlyHoveredRect;

        public override void Draw(SpriteBatch spriteBatch)
        {
            DrawBackground(spriteBatch);

            int xpos = 0;

            HoverText = null;
            CurrentlyHoveredId = null;
            CurrentlyHoveredTile = null;


            foreach (KeyValuePair<string, Tile> kvp in Logics.TilePreviews)
            {
                if (Hover && MousePosition.X >= xpos && MousePosition.X < xpos + 32)
                {
                    CurrentlyHoveredId = kvp.Key;
                    CurrentlyHoveredTile = kvp.Value;
                    HoverText = (kvp.Key == "") ? "Remove tile" : kvp.Value.DisplayName;
                    spriteBatch.Draw(TerraLogic.Pixel, CurrentlyHoveredRect = new Rectangle(Bounds.X + xpos, Bounds.Y, 32, 32), new Color(64, 64, 64));
                }
                else if (Logics.SelectedTileId == kvp.Key) spriteBatch.Draw(TerraLogic.Pixel, new Rectangle(Bounds.X + xpos, Bounds.Y, 32, 32), new Color(48, 48, 48));

                int sizeDiv = kvp.Value is null ? 1 : Math.Max(kvp.Value.Size.X, kvp.Value.Size.Y);

                Rectangle rect = new Rectangle(Bounds.X + xpos + 8, Bounds.Y + 8, (16 * (kvp.Value?.Size.X ?? 1)) / sizeDiv, (16 * (kvp.Value?.Size.Y ?? 1)) / sizeDiv);

                kvp.Value.Draw(rect, true);

                xpos += 32;
            }
        }

        protected internal override void MouseKeyStateUpdate(MouseKeys key, EventType @event, Point pos)
        {
            if (key == MouseKeys.Left && @event == EventType.Presssed && CurrentlyHoveredId != null)
            {
                if (Logics.SelectedToolId != -1) Logics.Tools[Logics.SelectedToolId].Deselected();
                Logics.SelectedToolId = -1;
                Logics.SelectedTileId = CurrentlyHoveredId;
                Logics.SelectedTilePreview = CurrentlyHoveredTile;
                Logics.SelectedWire = 0;
                Logics.PastePreview = null;
            }
            if (key == MouseKeys.Right && @event != EventType.Released && CurrentlyHoveredTile != null)
            {
                CurrentlyHoveredTile.RightClick(@event == EventType.Hold, true);
            }
        }

    }

    internal class ToolSelector : UIElement
    {
        public ToolSelector(string name) : base(name)
        {
            BackColor = new Color(32, 32, 32);
        }

        public override Pos Height => 32;
        public override Pos Width => Logics.Tools.Count * 32;

        int CurrentlyHoveredId = -1;

        public override void Draw(SpriteBatch spriteBatch)
        {
            DrawBackground(spriteBatch);

            int xpos = 0;

            HoverText = null;
            CurrentlyHoveredId = -1;

            for (int i = 0; i < Logics.Tools.Count; i++)
            {
                Tools.Tool tool = Logics.Tools[i];
                if (Hover && MousePosition.X >= xpos && MousePosition.X < xpos + 32)
                {
                    CurrentlyHoveredId = i;

                    HoverText = tool.DisplayName;
                    spriteBatch.Draw(TerraLogic.Pixel, new Rectangle(Bounds.X + xpos, Bounds.Y, 32, 32), new Color(64, 64, 64));
                }
                else if (Logics.SelectedToolId == i) spriteBatch.Draw(TerraLogic.Pixel, new Rectangle(Bounds.X + xpos, Bounds.Y, 32, 32), new Color(48, 48, 48));

                Rectangle rect = new Rectangle(Bounds.X + xpos + 8, Bounds.Y + 8, 16, 16);

                spriteBatch.Draw(tool.Texture, rect, Color.White);

                xpos += 32;
            }
        }

        protected internal override void MouseKeyStateUpdate(MouseKeys key, EventType @event, Point pos)
        {
            if (key == MouseKeys.Left && @event == EventType.Presssed && CurrentlyHoveredId != -1)
            {
                if (Logics.SelectedToolId != CurrentlyHoveredId) 
                {
                    if (Logics.SelectedToolId != -1) Logics.Tools[Logics.SelectedToolId].Deselected();
                    Logics.SelectedToolId = CurrentlyHoveredId;
                    Logics.SelectedTileId = null;
                    Logics.SelectedTilePreview = null;
                    Logics.SelectedWire = 0;
                    Logics.PastePreview = null;
                    if (CurrentlyHoveredId != -1) Logics.Tools[CurrentlyHoveredId].Selected();
                }
            }
        }

    }
}
