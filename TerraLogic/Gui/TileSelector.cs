using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

        public override void Draw(SpriteBatch spriteBatch)
        {
            DrawBackground(spriteBatch);

            int xpos = 0;

            HoverText = null;
            CurrentlyHoveredId = null;

            foreach (KeyValuePair<string, Tile> kvp in Logics.TilePreviews) 
            {
                if (Hover && MousePosition.X >= xpos && MousePosition.X < xpos + 32)
                {
                    CurrentlyHoveredId = kvp.Key;
                    CurrentlyHoveredTile = kvp.Value;
                    HoverText = (kvp.Key == "")? "Remove tile" : kvp.Value.DisplayName;
                    spriteBatch.Draw(TerraLogic.Pixel, new Rectangle(Bounds.X + xpos, Bounds.Y, 32, 32), new Color(64, 64, 64));
                }
                else if (Logics.SelectedTileId == kvp.Key) spriteBatch.Draw(TerraLogic.Pixel, new Rectangle(Bounds.X + xpos, Bounds.Y, 32, 32), new Color(48, 48, 48));

                int sizeDiv = kvp.Value is null ? 1 : Math.Max(kvp.Value.Size.X, kvp.Value.Size.Y);

                

                Rectangle rect = new Rectangle(Bounds.X + xpos + 8, Bounds.Y + 8, (16 * (kvp.Value?.Size.X ?? 1)) / sizeDiv, (16 * (kvp.Value?.Size.Y ?? 1)) / sizeDiv);

                if (kvp.Key != "") kvp.Value.Draw(rect, true);
                else spriteBatch.Draw(TerraLogic.RedCross, rect, Color.White); 

                xpos += 32;
            }
        }

        protected internal override void MouseKeyStateUpdate(MouseKeys key, EventType @event, Point pos)
        {
            if (key == MouseKeys.Left && @event == EventType.Presssed && CurrentlyHoveredId != null) 
            {
                Logics.SelectedTileId = CurrentlyHoveredId;
                Logics.SelectedTilePreview = CurrentlyHoveredTile;
                Logics.SelectedWireColor = 255;
            }
        }

    }
}
