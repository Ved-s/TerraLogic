﻿using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using TerraLogic.GuiElements;
using TerraLogic.Tiles;

namespace TerraLogic.Gui
{
    internal class TileSelector : UIElement
    {
        public TileSelector(string name) : base(name)
        {
            BackColor = new Color(32, 32, 32);
            Width = LoadedTileGroups.Count * 32;
        }

        public override Pos Height => 32;

        static List<string[]> TileGroups = new List<string[]>()
        {
            new string[] { "lever", "switch" },
            new string[] { "logicLamp*" },
            new string[] { "timer*" },
            new string[] { "gate*" },
            new string[] { "compact*" }
        };
        static List<Dictionary<string, Tile>> LoadedTileGroups = new List<Dictionary<string, Tile>>();
        static List<string> GroupIcon = new List<string>();
        int UnfoldedGroup = -1;

        string CurrentlyHoveredId = null;
        Tile CurrentlyHoveredTile = null;
        Rectangle CurrentlyHoveredRect;

        static TileSelector()
        {
            List<string> tilesToLoad = new List<string>(Logics.TilePreviews.Keys);

            foreach (string[] group in TileGroups)
            {
                HashSet<string> matchedTiles = new HashSet<string>();

                foreach (string tilemask in group)
                    foreach (string tile in Logics.TilePreviews.Keys)
                        if (CheckWildcard(tile, tilemask))
                        {
                            matchedTiles.Add(tile);
                            tilesToLoad.Remove(tile);
                        }

                Dictionary<string, Tile> loadedGroup = new Dictionary<string, Tile>();
                foreach (string tile in matchedTiles)
                    loadedGroup.Add(tile, Logics.TilePreviews[tile]);

                LoadedTileGroups.Add(loadedGroup);
                GroupIcon.Add(loadedGroup.Keys.First());
            }

            foreach (string tile in tilesToLoad)
            {
                LoadedTileGroups.Add(new Dictionary<string, Tile>() { { tile, Logics.TilePreviews[tile] } });
                GroupIcon.Add(tile);
            }
        }

        public override void Update()
        {
            base.Update();
            if (Logics.SelectedTileId is null && UnfoldedGroup != -1)
            {
                UnfoldedGroup = -1;
                Width = LoadedTileGroups.Count * 32;
            }
        }

        public override void Draw()
        {
            DrawBackground();

            int xpos = 0;

            HoverText = null;
            CurrentlyHoveredId = null;
            CurrentlyHoveredTile = null;

            Graphics.DrawRectangle(Bounds, new Color(48,48,48));

            for (int i = 0; i < LoadedTileGroups.Count; i++)
            {
                if (i != UnfoldedGroup)
                {
                    string tile = GroupIcon[i];
                    Dictionary<string, Tile> group = LoadedTileGroups[i];

                    string? groupHint = group.Count > 1 ? "In group:" : null;

                    if (group.Count > 1)
                        foreach (KeyValuePair<string, Tile> t in group)
                            if (tile != t.Key)
                            {
                                groupHint += "\n  " + t.Value.DisplayName;
                            }

                    DrawTile(xpos, tile, group[tile], false, groupHint);
                    if (group.Count > 1) 
                        TerraLogic.SpriteBatch.Draw(TerraLogic.MoreTex, new Rectangle(Bounds.X + xpos + 16, Bounds.Y + 16, 10, 10), Color.White);
                    xpos += 32;
                }
                else
                {
                    Dictionary<string, Tile> group = LoadedTileGroups[i];

                    foreach (KeyValuePair<string, Tile> tile in group)
                    {
                        DrawTile(xpos, tile.Key, tile.Value, true);
                        xpos += 32;
                    }
                    if (group.Count > 1)
                        Graphics.DrawRectangle(
                            new Rectangle(xpos - (group.Count * 32), 0, group.Count * 32, 32)
                            .WithOffset(Bounds.Location), new Color(72, 72, 72));
                }
            }
        }

        private void DrawTile(int xpos, string name, Tile tile, bool highlight, string addText = null)
        {
            if (highlight)
            {
                TerraLogic.SpriteBatch.Draw(TerraLogic.Pixel, new Rectangle(Bounds.X + xpos, Bounds.Y, 32, 32), new Color(40, 40, 40));
            }
            if (Hover && MousePosition.X >= xpos && MousePosition.X < xpos + 32)
            {
                CurrentlyHoveredId = name;
                CurrentlyHoveredTile = tile;
                HoverText = tile.DisplayName;
                if (Root.CurrentKeys.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.LeftShift))
                {
                    string data = tile.GetData();
                    if (data is not null) data = ":" + data;
                    HoverText += $"\n[{tile.Id}{data}]";
                }
                if (addText is not null) HoverText += "\n" + addText;
                TerraLogic.SpriteBatch.Draw(TerraLogic.Pixel, CurrentlyHoveredRect = new Rectangle(Bounds.X + xpos, Bounds.Y, 32, 32), new Color(64, 64, 64));
            }
            else if (Logics.SelectedTileId == name) 
                TerraLogic.SpriteBatch.Draw(TerraLogic.Pixel, new Rectangle(Bounds.X + xpos, Bounds.Y, 32, 32), new Color(48, 48, 48));

            float scale = 1f/Math.Max(tile.Size.X, tile.Size.Y);

            Vector2 pos = new(Bounds.X + xpos + 8, Bounds.Y + 8);

            if (tile.Size.X != tile.Size.Y)
            {
                if (tile.Size.X > tile.Size.Y) 
                    pos.Y += (tile.Size.X - tile.Size.Y) / 2f * Logics.TileSize.Y * scale;
                else
                    pos.X += (tile.Size.Y - tile.Size.X) / 2f * Logics.TileSize.X * scale;
            }

            tile.Draw(new(pos, scale));
        }

        protected internal override void MouseKeyStateUpdate(MouseKeys key, EventType @event, Point pos)
        {
            if (key == MouseKeys.Left && @event == EventType.Presssed && CurrentlyHoveredId is not null)
            {
                if (Logics.SelectedToolId != -1) Logics.Tools[Logics.SelectedToolId].IsSelected = false;
                Logics.SelectedToolId = -1;
                Logics.SelectedTileId = CurrentlyHoveredId;
                Logics.SelectedTilePreview = CurrentlyHoveredTile;
                Logics.SelectedWire = 0;
                Logics.PastePreview = null;

                if (UnfoldedGroup != -1 && LoadedTileGroups[UnfoldedGroup].ContainsKey(Logics.SelectedTileId))
                {
                    GroupIcon[UnfoldedGroup] = Logics.SelectedTileId;
                }
                else for (int i = 0; i < LoadedTileGroups.Count; i++)
                    {
                        if (GroupIcon[i] == Logics.SelectedTileId) { UnfoldedGroup = i; break; }
                    }
                Width = (LoadedTileGroups.Count + (UnfoldedGroup == -1 ? 0 : LoadedTileGroups[UnfoldedGroup].Count - 1)) * 32;

            }
            if (key == MouseKeys.Right && @event != EventType.Released && CurrentlyHoveredTile is not null)
            {
                CurrentlyHoveredTile.RightClick(@event == EventType.Hold, true);
            }
        }

        private static bool CheckWildcard(string input, string mask)
        {
            if (!mask.Contains('*')) return mask == input;
            return Regex.Match(input, "^" + Regex.Escape(mask).Replace("\\*", ".*") + "$").Success;
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

        public override void Draw()
        {
            DrawBackground();

            Graphics.DrawRectangle(Bounds, new Color(48, 48, 48));
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
                    TerraLogic.SpriteBatch.Draw(TerraLogic.Pixel, new Rectangle(Bounds.X + xpos, Bounds.Y, 32, 32), new Color(64, 64, 64));
                }
                else if (Logics.SelectedToolId == i) 
                    TerraLogic.SpriteBatch.Draw(TerraLogic.Pixel, new Rectangle(Bounds.X + xpos, Bounds.Y, 32, 32), new Color(48, 48, 48));

                Rectangle rect = new Rectangle(Bounds.X + xpos + 8, Bounds.Y + 8, 16, 16);

                TerraLogic.SpriteBatch.Draw(tool.Texture, rect, Color.White);

                xpos += 32;
            }
        }

        protected internal override void MouseKeyStateUpdate(MouseKeys key, EventType @event, Point pos)
        {
            if (key == MouseKeys.Left && @event == EventType.Presssed && CurrentlyHoveredId != -1)
            {
                if (Logics.SelectedToolId != CurrentlyHoveredId)
                {
                    if (Logics.SelectedToolId != -1) Logics.Tools[Logics.SelectedToolId].IsSelected = false;
                    Logics.SelectedToolId = CurrentlyHoveredId;
                    Logics.SelectedTileId = null;
                    Logics.SelectedTilePreview = null;
                    Logics.PastePreview = null;
                    if (CurrentlyHoveredId != -1)
                    {
                        Logics.Tools[CurrentlyHoveredId].IsSelected = true;
                        if (!Logics.Tools[Logics.SelectedToolId].AllowWireSelection)
                            Logics.SelectedWire = 0;
                    }
                }
            }
        }

    }
}
