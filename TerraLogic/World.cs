using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using TerraLogic.Gui;
using TerraLogic.Structures;
using TerraLogic.Tiles;

namespace TerraLogic
{
    public class World
    {
        public ChunkArray2D Wires { get; private set; }
        public ChunkArray2D<Tile?> Tiles { get; private set; }

        public List<WireDebug> WireDebug { get; } = new List<WireDebug>();

        internal static ChunkArray2D<long> WireUpdateArray = new ChunkArray2D<long>(8);

        public int Width { get; private set; } = int.MaxValue;
        public int Height { get; private set; } = int.MaxValue;

        public object? Owner { get; set; }
        public string WorldId { get; set; }

        public Rectangle WorldPos { get; set; }

        public float WorldZoom { get; set; } = 1f;

        public Rect WorldScreenRect { get; set; }
        public Rect WorldBackgroundRect { get; set; }

        public Rect VisibleScreenRect { get; set; }
        public Rectangle VisibleWorldRect { get; set; }

        public bool Visible { get; set; } = true;

        public static BlendState CurrentDrawBlendState;
        public static bool UseDeflate = true;

        private List<Tile> TopMostTiles = new();

        public float Padding { get; set; } = 0;

        public Color BackgroundColor { get; set; } = Color.Gray;
        public Color BackgroundOOBColor { get; set; } = new(9, 46, 51);

        public World? Parent { get; internal set; }

        public World(string id, object? owner, World? parent, int width, int height, Rectangle worldRect)
        {
            WorldId = id;
            Owner = owner;
            Parent = parent;
            Width = width;
            Height = height;
            WorldPos = worldRect;

            int chunkSize = (int)Math.Max(width, height);
            if (chunkSize > 16 || chunkSize <= 0)
                chunkSize = 16;
            Wires = new(chunkSize);
            Tiles = new(chunkSize);
        }

        public void SignalWire(Point pos, int wire)
        {
            SignalWire(new Rectangle(pos.X, pos.Y, 1, 1), wire);
        }
        public void SignalWire(Rectangle rect, int wire)
        {
            Logics.WireUpdateWatch.Start();

            List<Point> points = new List<Point>();

            for (int y = rect.Y; y < rect.Bottom; y++)
                for (int x = rect.X; x < rect.Right; x++)
                {
                    points.Add(new Point(x, y));
                }

            foreach (WireSignal w in TrackWire(points.ToArray(), wire))
            {
                if (!w.IsAtOrigin)
                {
                    Tile t = Tiles[w.X, w.Y];
                    if (t is not null)
                    {
                        Point inPos = new Point(w.X - t.Pos.X, w.Y - t.Pos.Y);
                        t.WireSignal(w.Wire, w.Origin, inPos);
                        AddDebug(w);
                    }
                }
            }

            Logics.WireUpdateWatch.Stop();

        }
        public WireSignal[] TrackWire(Point pos, int wire) => TrackWire(new Point[] { pos }, wire);
        public WireSignal[] TrackWire(Point[] points, int wire)
        {
            int trackId = Random.Shared.Next();

            Stack<WireSignal> WiresToTrack = new Stack<WireSignal>();
            List<WireSignal> TrackedPoints = new List<WireSignal>();

            foreach (Point p in points)
            {
                int thiswire = Wires[p.X, p.Y] & wire;
                if (thiswire == 0) continue;
                WiresToTrack.Push(new WireSignal(p.X, p.Y, thiswire));
            }


            while (WiresToTrack.Count > 0)
            {
                WireSignal w = WiresToTrack.Pop();

                long fullUpdate = WireUpdateArray[w.X, w.Y];

                if (fullUpdate >> 32 == trackId)
                {
                    w.Wire = (int)((fullUpdate & 0xffffffff) ^ w.Wire) & w.Wire;
                    if (w.Wire == 0) continue;
                    WireUpdateArray[w.X, w.Y] = fullUpdate | (uint)w.Wire;
                }
                else WireUpdateArray[w.X, w.Y] = ((long)trackId << 32) | (uint)w.Wire;
                TrackedPoints.Add(w);

                void TrackWire(Point pos, Side sideIn)
                {
                    int nextWire = Wires[pos.X, pos.Y] & w.Wire;
                    while (nextWire > 0 && Tiles[pos.X, pos.Y] is JunctionBox box)
                    {
                        switch (box.Type)
                        {
                            case JunctionBox.JunctionType.Cross:
                                switch (sideIn)
                                {
                                    case Side.Up: pos.Y++; break;
                                    case Side.Right: pos.X--; break;
                                    case Side.Down: pos.Y--; break;
                                    case Side.Left: pos.X++; break;
                                }
                                break;
                            case JunctionBox.JunctionType.TL:
                                switch (sideIn)
                                {
                                    case Side.Up: pos.X--; sideIn = Side.Right; break;
                                    case Side.Right: pos.Y++; sideIn = Side.Up; break;
                                    case Side.Down: pos.X++; sideIn = Side.Left; break;
                                    case Side.Left: pos.Y--; sideIn = Side.Down; break;
                                }
                                break;
                            case JunctionBox.JunctionType.TR:
                                switch (sideIn)
                                {
                                    case Side.Up: pos.X++; sideIn = Side.Left; break;
                                    case Side.Right: pos.Y--; sideIn = Side.Down; break;
                                    case Side.Down: pos.X--; sideIn = Side.Right; break;
                                    case Side.Left: pos.Y++; sideIn = Side.Up; break;
                                }
                                break;
                        }

                        nextWire = Wires[pos.X, pos.Y] & nextWire;
                    }
                    if (nextWire > 0) WiresToTrack.Push(w.NewPos(pos, nextWire));
                }

                TrackWire(new Point(w.X, w.Y - 1), Side.Down);
                TrackWire(new Point(w.X + 1, w.Y), Side.Left);
                TrackWire(new Point(w.X, w.Y + 1), Side.Up);
                TrackWire(new Point(w.X - 1, w.Y), Side.Right);
                //Point pos = new Point(w.X, w.Y - 1); // top
                //int nextWire = Wires[pos.X, pos.Y] & w.Wire;
                //bool redirected = false;
                //
                //if (nextWire > 0)
                //{
                //    if (Tiles[pos.X, pos.Y] is JunctionBox topBox)
                //    {
                //        redirected = true;
                //        switch (topBox.Type)
                //        {
                //            case JunctionBox.JunctionType.Cross: pos.Y--; break;
                //            case JunctionBox.JunctionType.TL: pos.X++; break;
                //            case JunctionBox.JunctionType.TR: pos.X--; break;
                //        }
                //    }
                //
                //    nextWire = Wires[pos.X, pos.Y] & w.Wire;
                //    WiresToTrack.Push(w.NewPos(pos, nextWire));
                //}
                //
                //
                //pos = new Point(w.X + 1, w.Y); // right
                //nextWire = Wires[pos.X, pos.Y] & w.Wire;
                //
                //if (nextWire > 0)
                //{
                //    if (Tiles[pos.X, pos.Y] is JunctionBox rightBox)
                //    {
                //        redirected = true;
                //        switch (rightBox.Type)
                //        {
                //            case JunctionBox.JunctionType.Cross: pos.X++; break;
                //            case JunctionBox.JunctionType.TL: pos.Y--; break;
                //            case JunctionBox.JunctionType.TR: pos.Y++; break;
                //        }
                //    }
                //
                //    nextWire = Wires[pos.X, pos.Y] & nextWire;
                //    WiresToTrack.Push(w.NewPos(pos, nextWire));
                //}
                //
                //
                //pos = new Point(w.X, w.Y + 1); // bottom
                //nextWire = Wires[pos.X, pos.Y] & w.Wire;
                //if (nextWire > 0)
                //{
                //    if (Tiles[pos.X, pos.Y] is JunctionBox bottomBox)
                //    {
                //        redirected = true;
                //        switch (bottomBox.Type)
                //        {
                //            case JunctionBox.JunctionType.Cross: pos.Y++; break;
                //            case JunctionBox.JunctionType.TL: pos.X--; break;
                //            case JunctionBox.JunctionType.TR: pos.X++; break;
                //        }
                //    }
                //
                //    nextWire = Wires[pos.X, pos.Y] & nextWire;
                //    WiresToTrack.Push(w.NewPos(pos, nextWire));
                //}
                //
                //pos = new Point(w.X - 1, w.Y); // left
                //nextWire = Wires[pos.X, pos.Y] & w.Wire;
                //if (nextWire > 0)
                //{
                //    if (Tiles[pos.X, pos.Y] is JunctionBox leftBox)
                //    {
                //        redirected = true;
                //        switch (leftBox.Type)
                //        {
                //            case JunctionBox.JunctionType.Cross: pos.X--; break;
                //            case JunctionBox.JunctionType.TL: pos.Y++; break;
                //            case JunctionBox.JunctionType.TR: pos.Y--; break;
                //        }
                //    }
                //
                //    nextWire = Wires[pos.X, pos.Y] & nextWire;
                //    WiresToTrack.Push(w.NewPos(pos, nextWire));
                //}
            }

            return TrackedPoints.ToArray();
        }

        internal void AddDebug(WireSignal signal)
        {
            if (!Logics.WireDebugActive) return;

            foreach (WireDebug debug in WireDebug)
                if (debug.Signal.Origin == signal.Origin
                    && debug.Signal.X == signal.X
                    && debug.Signal.Y == signal.Y)
                {
                    debug.Signal.Wire |= signal.Wire;
                    debug.Add();
                    return;
                }
            WireDebug.Add(new WireDebug(signal));
        }

        public void SetWire(int x, int y, byte id, bool state)
        {
            if (!CheckInBounds(x, y)) return;

            int current = Wires[x, y];
            int mask = ~(1 << id);
            int newstate = current & mask;
            if (state) newstate |= ~mask;
            if (newstate == current) return;
            Wires[x, y] = newstate;
        }
        public void SetWires(int x, int y, int wires, bool state)
        {
            if (!CheckInBounds(x, y)) return;

            int current = Wires[x, y];
            int newstate = current & ~wires;
            if (state) newstate |= wires;
            if (newstate == current) return;
            Wires[x, y] = newstate;
        }
        public bool GetWire(int x, int y, byte id)
        {
            if (!CheckInBounds(x, y)) return false;
            return ((Wires[x, y] >> id) & 1) != 0;
        }
        public static bool GetWire(int wire, byte id)
        {
            return ((wire >> id) & 1) != 0;
        }

        public void SetTile(Point pos, string tileid = null, Tile reference = null, bool noUpdates = false, bool ignoreIndestructible = false)
            => SetTile(pos.X, pos.Y, tileid, reference, noUpdates, ignoreIndestructible);
        public void SetTile(int x, int y, string? tileid = null, Tile? reference = null, bool noUpdates = false, bool ignoreIndestructible = false)
        {
            if (!CheckInBounds(x, y)) return;

            if (tileid == "") tileid = null;
            Tile t = Tiles[x, y];

            if (t is not null && !t.CanRemove && !ignoreIndestructible) return;

            if (tileid is null)
            {
                if (t is null) return;
                t.BeforeDestroy();
                for (int ty = t.Pos.Y; ty < t.Size.Y + t.Pos.Y; ty++)
                    for (int tx = t.Pos.X; tx < t.Size.X + t.Pos.X; tx++)
                        Tiles[tx, ty] = null;
                return;
            }

            string[] data = tileid.Split(new char[] { ':' }, 2);

            if (reference is null)
                Logics.TileMap.TryGetValue(data[0], out reference);

            if (reference is not null)
                if (CanSetTile(x, y, reference))
                {
                    t = reference.CreateTile(data.Length == 1 ? null : data[1], false);
                    t.Created = true;
                    t.Pos = new Point(x, y);
                    t.World = this;

                    for (int ty = y; ty < t.Size.Y + y; ty++)
                        for (int tx = x; tx < t.Size.X + x; tx++)
                            Tiles[tx, ty] = t;

                    if (!noUpdates) t.PlacedInWorld();
                }
        }
        public void SetTile(Point pos, Tile? tile = null, bool noUpdates = false, bool ignoreIndestructible = false)
            => SetTile(pos.X, pos.Y, tile, noUpdates, ignoreIndestructible);
        public void SetTile(int x, int y, Tile? tile = null, bool noUpdates = false, bool ignoreIndestructible = false)
        {
            if (!CheckInBounds(x, y)) return;

            Tile? t = Tiles[x, y];

            if (t is not null && !t.CanRemove && !ignoreIndestructible) return;

            if (tile is null)
            {
                if (t is null) return;
                t.BeforeDestroy();
                for (int ty = t.Pos.Y; ty < t.Size.Y + t.Pos.Y; ty++)
                    for (int tx = t.Pos.X; tx < t.Size.X + t.Pos.X; tx++)
                        Tiles[tx, ty] = null;
                return;
            }
            if (CanSetTile(x, y, tile))
            {
                tile.Created = true;
                tile.Pos = new Point(x, y);
                tile.World = this;

                for (int ty = y; ty < tile.Size.Y + y; ty++)
                    for (int tx = x; tx < tile.Size.X + x; tx++)
                        Tiles[tx, ty] = tile;

                if (!noUpdates) tile.PlacedInWorld();
            }
        }
        public bool CanSetTile(int x, int y, Tile t)
        {
            if (!CheckInBounds(x, y)) return false;

            for (int ty = y; ty < t.Size.Y + y; ty++)
                for (int tx = x; tx < t.Size.X + x; tx++)
                {
                    if (Tiles[tx, ty] is not null || !CheckInBounds(tx, ty)) return false;
                }
            return true;

        }

        public bool CheckInBounds(int x, int y)
        {
            if (x < 0 || y < 0) return false;
            if (x >= Width || y >= Height) return false;
            return true;
        }

        public void RightClick(int x, int y, bool held)
        {
            if (!CheckInBounds(x, y)) return;

            Tile? t = Tiles[x, y];
            if (t is not null)
            {
                t.RightClick(held, false);
            }
        }

        public void Draw(BlendState blendState)
        {
            CurrentDrawBlendState = blendState;
            if (!Visible) return;
            CalculatePos();

            if (Parent is not null)
            {
                TerraLogic.SpriteBatch.Begin();
                Graphics.FillRectangle((Rectangle)WorldBackgroundRect, BackgroundOOBColor);
                Graphics.FillRectangle((Rectangle)WorldScreenRect, BackgroundColor);
                TerraLogic.SpriteBatch.End();
            }

            DrawTiles(blendState);
            if (Logics.PastePreview is not null
                || Logics.SelectedWire > 0
                || Logics.SelectedTileId is not null
                || (Logics.SelectedToolId > -1 && Logics.Tools[Logics.SelectedToolId].ShowWires))
                DrawWires(Wires, blendState);

            DrawWireDebug();
            DrawTopMostTiles(blendState);
            DrawTilePreview();

        }
        public void Update()
        {
            CalculatePos();

            if (Visible)
                if (Logics.PastePreview != this && WorldScreenRect.Contains(Logics.Instance.MousePosition.ToVector2()))
                    Logics.HoverWorld = this;

            bool currentPausedUpdateTick = Logics.UpdatePaused && Logics.UpdateTick;

            if (currentPausedUpdateTick || !Logics.WireDebugActive)
                WireDebug.Clear();

            if (!Logics.UpdatePaused || currentPausedUpdateTick)
            {
                foreach (ChunkArray2D<Tile>.ChunkItem tile in Tiles)
                {
                    if (tile.Item is null || (!tile.Item.NeedsUpdate && !tile.Item.NeedsContinuousUpdate)
                    || tile.Item.Pos.X != tile.X || tile.Item.Pos.Y != tile.Y) continue;
                    tile.Item.Update();
                    if (!tile.Item.NeedsContinuousUpdate) tile.Item.NeedsUpdate = false;
                }
            }


            WireDebug.RemoveAll(wd => wd.Fade <= 0);

            if (!Logics.UpdatePaused)
                foreach (WireDebug wd in WireDebug)
                {
                    wd.Fade--;
                }
        }

        private void CalculatePos()
        {
            if (Parent is null)
            {
                WorldZoom = PanNZoom.Zoom;
                VisibleWorldRect = Logics.ViewBounds;
                VisibleScreenRect = new(0, 0, TerraLogic.Instance.Window.ClientBounds.Width, TerraLogic.Instance.Window.ClientBounds.Height);
                WorldScreenRect = new(PanNZoom.ScreenPosition, new(int.MaxValue, int.MaxValue));
                WorldBackgroundRect = WorldScreenRect;
            }
            else
            {
                float zoomH = (float)WorldPos.Width / Width;
                float zoomV = (float)WorldPos.Height / Height;

                WorldZoom = Parent.WorldZoom * Math.Min(zoomV, zoomH);

                Rect worldScreen = new();
                Rect worldBack = new();

                Rect fixedWorldPos = WorldPos;

                fixedWorldPos.Location += new Vector2(Padding);
                fixedWorldPos.Size -= new Vector2(Padding * 2);

                Rect paddedWorldPos = fixedWorldPos;

                if (zoomV != zoomH)
                {
                    Vector2 fixedZoom = new Vector2(fixedWorldPos.Width / Width, fixedWorldPos.Height / Height);

                    if (zoomV > zoomH)
                    {
                        fixedWorldPos.Height *= fixedZoom.X / fixedZoom.Y;
                        fixedWorldPos.Y += (paddedWorldPos.Height - fixedWorldPos.Height) / 2;
                    }
                    else
                    {
                        fixedWorldPos.Width *= fixedZoom.Y / fixedZoom.X;
                        fixedWorldPos.X += (paddedWorldPos.Width - fixedWorldPos.Width) / 2;
                    }
                }

                worldScreen.Location = Logics.TileSize.ToVector2() * Parent.WorldZoom * fixedWorldPos.Location;
                worldScreen.Location += Parent.WorldScreenRect.Location;
                worldScreen.Size = Parent.WorldZoom * Logics.TileSize.ToVector2() * fixedWorldPos.Size;

                worldBack.Location = Logics.TileSize.ToVector2() * Parent.WorldZoom * paddedWorldPos.Location;
                worldBack.Location += Parent.WorldScreenRect.Location;
                worldBack.Size = Parent.WorldZoom * Logics.TileSize.ToVector2() * paddedWorldPos.Size;

                int maxSide = Math.Max(Width, Height);
                float maxRatio = Math.Max(Width / WorldPos.Width, Height / WorldPos.Height);

                WorldZoom *= 1f - (Padding * (maxRatio * 2) / maxSide);

                WorldScreenRect = worldScreen;
                WorldBackgroundRect = worldBack;

                Rect fullScreen = new(0, 0, TerraLogic.Instance.Window.ClientBounds.Width, TerraLogic.Instance.Window.ClientBounds.Height);
                VisibleScreenRect = fullScreen.Intersection(WorldScreenRect);
                Rect worldRect = ScreenToWorld(VisibleScreenRect);
                VisibleWorldRect = new Rectangle()
                {
                    X = (int)worldRect.X / Logics.TileSize.X,
                    Y = (int)worldRect.Y / Logics.TileSize.Y,
                    Width = (int)Math.Ceiling(worldRect.Width / Logics.TileSize.X),
                    Height = (int)Math.Ceiling(worldRect.Height / Logics.TileSize.Y)
                };
            }
        }

        internal void DrawWires(ChunkArray2D wires, BlendState blendState)
        {
            TerraLogic.SpriteBatch.Begin(SpriteSortMode.Deferred, blendState, WorldZoom > 1 ? SamplerState.PointClamp : SamplerState.LinearClamp, null, null);

            int wireTop;
            int wireLeft;
            int wireBottom;
            int wireRight;

            Vector2 pos;

            for (int y = VisibleWorldRect.Top; y <= Math.Min(VisibleWorldRect.Bottom, wires.Height); y++)
                for (int x = VisibleWorldRect.Left; x <= Math.Min(VisibleWorldRect.Right, wires.Width); x++)
                {
                    int wire = wires[x, y];
                    if (wire == 0) continue;

                    Texture2D wireSprite = Logics.Wire;
                    bool anySelectedWireDrawn = false;

                    if (Tiles[x, y] is JunctionBox box)
                        switch (box.Type)
                        {
                            case JunctionBox.JunctionType.Cross: wireSprite = Logics.WireCross; break;
                            case JunctionBox.JunctionType.TL: wireSprite = Logics.WireTL; break;
                            case JunctionBox.JunctionType.TR: wireSprite = Logics.WireTR; break;
                        }

                    wireTop = wires[x, y - 1];
                    wireLeft = wires[x - 1, y];
                    wireBottom = wires[x, y + 1];
                    wireRight = wires[x + 1, y];

                    pos = new(x * Logics.TileSize.X, y * Logics.TileSize.Y);
                    pos = WorldToScreen(pos);

                    void DrawWire(byte id)
                    {
                        if (!GetWire(wire, id)) return;

                        Color c = Logics.WireColorMapping[id];

                        if (!GetWire(Logics.SelectedWire, id))
                        {
                            c *= 0.3f;
                        }
                        else
                        {
                            if (Logics.SelectedWire.Bits() > 1 && anySelectedWireDrawn) c *= 0.5f;
                            anySelectedWireDrawn = true;
                        }

                        Rectangle source = Logics.CalculateWireSpriteOffset(GetWire(wireTop, id), GetWire(wireRight, id), GetWire(wireBottom, id), GetWire(wireLeft, id));

                        TerraLogic.SpriteBatch.Draw(wireSprite, pos, source, c, 0f, Vector2.Zero, WorldZoom, SpriteEffects.None, 0);
                    }

                    for (byte id = 0; id < Logics.WireColorMapping.Count; id++)
                        if (!GetWire(Logics.SelectedWire, id)) DrawWire(id);

                    for (byte id = 0; id < Logics.WireColorMapping.Count; id++)
                        if (GetWire(Logics.SelectedWire, id)) DrawWire(id);

                }
            TerraLogic.SpriteBatch.End();
        }
        private void DrawTiles(BlendState blendState)
        {
            TerraLogic.SpriteBatch.Begin(SpriteSortMode.Deferred, blendState, WorldZoom > 1 ? SamplerState.PointWrap : SamplerState.LinearWrap, null, null);

            for (int y = VisibleWorldRect.Top; y <= Math.Min(VisibleWorldRect.Bottom, Tiles.Height); y++)
                for (int x = VisibleWorldRect.Left; x <= Math.Min(VisibleWorldRect.Right, Tiles.Width); x++)
                {
                    Tile tile = Tiles[x, y];

                    if (tile is null) continue;

                    if (tile.Pos.X != x || tile.Pos.Y != y)
                    {
                        int maxTop = Math.Max(VisibleWorldRect.Top, tile.Pos.Y);
                        int maxLeft = Math.Max(VisibleWorldRect.Left, tile.Pos.X);

                        if (x != maxLeft || y != maxTop) continue;
                    }

                    if (tile.DrawTopMost)
                    {
                        TopMostTiles.Add(tile);
                        continue;
                    }

                    DrawTile(tile);
                }
            TerraLogic.SpriteBatch.End();
        }
        private void DrawWireDebug()
        {
            if (!Logics.WireDebugActive) return;

            TerraLogic.SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);

            foreach (WireDebug wd in WireDebug)
            {
                Point originCenter = wd.Signal.Origin.Multiply(Logics.TileSize);
                originCenter.X += Logics.TileSize.X / 2;
                originCenter.Y += Logics.TileSize.Y / 2;

                if (VisibleWorldRect.Contains(wd.Signal.Origin) || VisibleWorldRect.Contains(wd.Signal.Pos))
                {
                    Color c = Logics.GetWireColor(wd.Signal.Wire);
                    c *= (wd.Fade / 300f);

                    Graphics.DrawRectangle(WorldToScreen(new Rectangle(wd.Signal.Origin.X * Logics.TileSize.X, wd.Signal.Origin.Y * Logics.TileSize.Y, Logics.TileSize.X, Logics.TileSize.Y)), c);
                    Graphics.DrawLineWithText(
                        WorldToScreen(originCenter.ToVector2()).ToPoint(),
                        WorldToScreen(new Vector2((wd.Signal.X * Logics.TileSize.X) + (Logics.TileSize.X / 2), (wd.Signal.Y * Logics.TileSize.Y) + (Logics.TileSize.Y / 2))).ToPoint(),
                        Logics.Instance.Font, wd.Count.ToString(), c);
                }
            }
            TerraLogic.SpriteBatch.End();

        }
        private void DrawTilePreview()
        {
            if (!Logics.Instance.Hover || Logics.HoverWorld != this) return;
            if (Logics.SelectedTilePreview is null && Logics.SelectedToolId == -1 && Logics.PastePreview is null) return;

            Point wp = (ScreenToWorld(Logics.Instance.MousePosition) / Logics.TileSize.ToVector2()).ToPoint();

            TerraLogic.SpriteBatch.Begin(SpriteSortMode.Deferred, null, PanNZoom.Zoom > 1 ? SamplerState.PointWrap : SamplerState.LinearWrap, null, null);

            if (Logics.PastePreview is not null)
            {
                Rectangle rect = WorldToScreen(new Rectangle(wp.X, wp.Y, Logics.PastePreview.Width, Logics.PastePreview.Height).Mul(Logics.TileSize));
                //Graphics.FillRectangle(TerraLogic.SpriteBatch, rect, Color.CornflowerBlue.Div(5, true));

                Logics.PastePreview.Parent = Logics.HoverWorld;
                Logics.PastePreview.WorldPos = new(wp, Logics.PastePreview.WorldPos.Size);

                TerraLogic.SpriteBatch.End();
                Logics.PastePreview.Draw(BlendState.Additive);
                TerraLogic.SpriteBatch.Begin(SpriteSortMode.Deferred, null, PanNZoom.Zoom > 1 ? SamplerState.PointWrap : SamplerState.LinearWrap, null, null);

            }

            if (Logics.SelectedToolId != -1 && Logics.Tools[Logics.SelectedToolId].DrawMouseIcon)
                TerraLogic.SpriteBatch.Draw(
                    Logics.Tools[Logics.SelectedToolId].Texture,
                    WorldToScreen(new Rectangle(wp.X * 16, wp.Y * 16, 16, 16)),
                    Color.White);

            else if (Logics.SelectedTilePreview is not null && CanSetTile(wp.X, wp.Y, Logics.SelectedTilePreview))
            {
                Logics.SelectedTilePreview.Draw(new(TilesToScreen(new(wp.X, wp.Y)), WorldZoom));
            }
            TerraLogic.SpriteBatch.End();
        }
        private void DrawTopMostTiles(BlendState blendState)
        {
            if (TopMostTiles.Count == 0) return;

            TerraLogic.SpriteBatch.Begin(SpriteSortMode.Deferred, blendState, WorldZoom > 1 ? SamplerState.PointWrap : SamplerState.LinearWrap, null, null);
            foreach (Tile t in TopMostTiles)
                DrawTile(t);
            TopMostTiles.Clear();
            TerraLogic.SpriteBatch.End();
        }

        private void DrawTile(Tile t)
        {
            Transform transform = new(TilesToScreen(t.Pos), WorldZoom);
            t.Draw(transform);
        }

        public Vector2 ScreenToWorld(Point screenPos)
        {
            Vector2 v = new Vector2();
            v.X = (screenPos.X - WorldScreenRect.X) / WorldZoom;
            v.Y = (screenPos.Y - WorldScreenRect.Y) / WorldZoom;
            return v;
        }
        public Vector2 WorldToScreen(Vector2 worldPos)
        {
            return worldPos * WorldZoom + WorldScreenRect.Location;
        }

        public Rectangle ScreenToWorld(Rectangle screenPos)
        {
            screenPos.X = (int)((screenPos.X - WorldScreenRect.X) / WorldZoom);
            screenPos.Y = (int)((screenPos.Y - WorldScreenRect.Y) / WorldZoom);
            screenPos.Width = (int)(screenPos.Width / WorldZoom);
            screenPos.Height = (int)(screenPos.Height / WorldZoom);
            return screenPos;
        }
        public Rect ScreenToWorld(Rect screenRect)
        {
            screenRect.X = (screenRect.X - WorldScreenRect.X) / WorldZoom;
            screenRect.Y = (screenRect.Y - WorldScreenRect.Y) / WorldZoom;
            screenRect.Width = screenRect.Width / WorldZoom;
            screenRect.Height = screenRect.Height / WorldZoom;
            return screenRect;
        }
        public Rectangle WorldToScreen(Rectangle worldRect)
        {
            worldRect.Location = (worldRect.Location.ToVector2() * WorldZoom).ToPoint();
            worldRect.Location += WorldScreenRect.Location.ToPoint();

            worldRect.Width = (int)(worldRect.Width * WorldZoom);
            worldRect.Height = (int)(worldRect.Height * WorldZoom);

            return worldRect;
        }

        public Vector2 ScreenToTiles(Point screenPos)
        {
            Vector2 v = new();
            v.X = ((screenPos.X - WorldScreenRect.X) / WorldZoom) / Logics.TileSize.X;
            v.Y = ((screenPos.Y - WorldScreenRect.Y) / WorldZoom) / Logics.TileSize.Y;
            return v;
        }
        public Vector2 TilesToScreen(Point tilePos)
        {
            return WorldToScreen(tilePos.Multiply(Logics.TileSize).ToVector2());
        }

        public World Copy(Rectangle rect)
        {
            rect = rect.Intersection(new(0, 0, Width, Height));

            World world = new World("copiedWorld", this, null, rect.Width, rect.Height, new(Point.Zero, rect.Size));

            for (int i = rect.Left; i < rect.Right; i++)
                for (int j = rect.Top; j < rect.Bottom; j++)
                {
                    int lx = i - rect.Left;
                    int ly = j - rect.Top;

                    world.Wires[lx, ly] = Wires[i, j];

                    Tile t = Tiles[i, j];

                    if (t is null) continue;

                    if (t.Pos.X != i || t.Pos.Y != j) continue;
                    t = t.Copy();
                    world.SetTile(lx, ly, t, true);

                }
            return world;
        }
        public World CopyExact()
        {
            World world = new World("exactWorld", Owner, Parent, Width, Height, WorldPos);

            for (int i = 0; i < Width; i++)
                for (int j = 0; j < Height; j++)
                {
                    world.Wires[i, j] = Wires[i, j];

                    Tile t = Tiles[i, j];

                    if (t is null) continue;

                    if (t.Pos.X != i || t.Pos.Y != j) continue;
                    t = t.Copy();
                    world.SetTile(i, j, t, true);

                }
            return world;
        }

        public void Paste(World w, Point pos)
        {
            Rectangle rect = new Rectangle(0, 0, Width, Height).Intersection(new(pos.X, pos.Y, w.Width, w.Height));
            for (int i = rect.Left; i < rect.Right; i++)
                for (int j = rect.Top; j < rect.Bottom; j++)
                {
                    int lx = i - rect.Left;
                    int ly = j - rect.Top;

                    Wires[i, j] = w.Wires[lx, ly];

                    Tile t = w.Tiles[lx, ly];

                    if (t is null) continue;

                    if (t.Pos.X != lx || t.Pos.Y != ly) continue;
                    t = t.Copy();
                    SetTile(i, j, t, true);

                }
        }

        public void Load(BinaryReader reader)
        {
            byte format = reader.ReadByte();
            if (format == 0) LoadFormat0(reader);
            else return;
        }
        private void LoadFormat0(BinaryReader reader)
        {
            Wires.Clear();
            Tiles.Clear();

            string[] tiletypes = new string[reader.ReadUInt16()];
            for (int i = 0; i < tiletypes.Length; i++)
                tiletypes[i] = reader.ReadString();


            Width = reader.ReadInt32();
            Height = reader.ReadInt32();

            int tilesw = reader.ReadInt32();
            int tilesh = reader.ReadInt32();

            int wiresw = reader.ReadInt32();
            int wiresh = reader.ReadInt32();

            MemoryStream memoryStream = new();

            int compressedLength = reader.ReadInt32();
            int length = reader.ReadInt32();

            if (compressedLength == -1)
            {
                reader.BaseStream.CopyExact(memoryStream, length);
            }
            else
                using (MemoryStream buf = new())
                {
                    reader.BaseStream.CopyExact(buf, length);
                    buf.Position = 0;
                    using (DeflateStream deflate = new DeflateStream(buf, CompressionMode.Decompress, true))
                    {
                        deflate.CopyExact(memoryStream, compressedLength);
                    }
                }

            memoryStream.Position = 0;

            using (BinaryReader dread = new(memoryStream))
            {
                for (int i = 0; i < wiresw; i++)
                    for (int j = 0; j < wiresh; j++)
                        Wires[i, j] = dread.ReadInt32();

                for (int i = 0; i < tilesw; i++)
                    for (int j = 0; j < tilesh; j++)
                    {
                        ushort tiletype = dread.ReadUInt16();
                        if (tiletype == 0xffff) continue;

                        Tile tile = (Tile)Activator.CreateInstance(Logics.TileMap[tiletypes[tiletype]].GetType());
                        tile.World = this;

                        ushort expectedLength = dread.ReadUInt16();
                        long startpos = memoryStream.Position;
                        tile.Load(dread);
                        long diff = memoryStream.Position - startpos;

                        if (diff != expectedLength)
                        {
                            if (diff > expectedLength)
                                Debug.WriteLine($"Data overread by tile {tiletypes[tiletype]} at {i}, {j}: got {expectedLength} bytes, read {diff}");
                            else
                                Debug.WriteLine($"Data underread by tile {tiletypes[tiletype]} at {i}, {j}: got {expectedLength} bytes, read {diff}");

                            dread.BaseStream.Seek(startpos + expectedLength, SeekOrigin.Begin);
                        }
                        SetTile(i, j, tile, true);
                    }
            }
        }

        public void Save(BinaryWriter writer)
        {
            writer.Write((byte)0);

            int typeIndex = 0;
            Dictionary<string, int> typeMap = new();

            foreach (ChunkArray2D<Tile>.ChunkItem tile in Tiles)
            {
                string id = tile.Item.Id;

                if (typeMap.ContainsKey(id)) continue;

                typeMap.Add(id, typeIndex);
                typeIndex++;
            }

            writer.Write((ushort)typeMap.Count);
            foreach (string id in typeMap.Keys)
                writer.Write(id);

            writer.Write(Width);
            writer.Write(Height);

            writer.Write(Tiles.Width);
            writer.Write(Tiles.Height);

            writer.Write(Wires.Width);
            writer.Write(Wires.Height);

            MemoryStream memoryStream = new();

            using (BinaryWriter dwrite = new(memoryStream))
            {
                for (int i = 0; i < Wires.Width; i++)
                    for (int j = 0; j < Wires.Height; j++)
                        dwrite.Write(Wires[i, j]);

                for (int i = 0; i < Tiles.Width; i++)
                    for (int j = 0; j < Tiles.Height; j++)
                    {
                        Tile tile = Tiles[i, j];
                        if (tile is null || tile.Pos.X != i || tile.Pos.Y != j)
                            dwrite.Write((ushort)0xffff);
                        else
                        {
                            dwrite.Write((ushort)typeMap[tile.Id]);
                            long lenpos = dwrite.BaseStream.Position;
                            dwrite.Write((ushort)0);
                            long datapos = dwrite.BaseStream.Position;
                            tile.Save(dwrite);
                            long diff = dwrite.BaseStream.Position - datapos;
                            datapos = dwrite.BaseStream.Position;

                            //Debug.WriteLine($"Data saved: tile {tile.Id} at {i}, {j}: written {diff} bytes");

                            dwrite.BaseStream.Seek(lenpos, SeekOrigin.Begin);
                            dwrite.Write((ushort)diff);
                            dwrite.BaseStream.Seek(datapos, SeekOrigin.Begin);
                        }
                    }

                memoryStream.Position = 0;

                if (UseDeflate)
                {
                    writer.Write((int)memoryStream.Length);
                    long clenpos = writer.BaseStream.Position;
                    writer.Write(0);
                    long cdatapos = writer.BaseStream.Position;

                    using (DeflateStream deflate = new DeflateStream(writer.BaseStream, CompressionMode.Compress, true))
                        memoryStream.CopyTo(deflate);

                    long cdiff = writer.BaseStream.Position - cdatapos;
                    cdatapos = writer.BaseStream.Position;

                    writer.BaseStream.Seek(clenpos, SeekOrigin.Begin);
                    writer.Write((int)cdiff);
                    writer.BaseStream.Seek(cdatapos, SeekOrigin.Begin);

                    Debug.WriteLine($"Data saved: world: written {cdiff} bytes");
                }
                else
                {
                    writer.Write(-1);
                    writer.Write((int)memoryStream.Length);
                    memoryStream.CopyTo(writer.BaseStream);

                    Debug.WriteLine($"Data saved: world: written {memoryStream.Length} bytes (uncompressed)");
                }


            }
        }

        internal void SaveToOldFile(string filename)
        {
            string wires = Wires.ToDataString();

            StringBuilder tileBuilder = new StringBuilder();
            for (int y = 0; y < Tiles.Height; y++)
                for (int x = 0; x < Tiles.Width; x++)
                {
                    Tile t = Tiles[x, y];
                    if (t is null) continue;
                    if (x != t.Pos.X || y != t.Pos.Y) continue;

                    tileBuilder.Append(x);
                    tileBuilder.Append(',');
                    tileBuilder.Append(y);
                    tileBuilder.Append(':');
                    string data = t.GetData();
                    if (data is not null)
                        tileBuilder.Append(t.Id + ":" + data);
                    else
                        tileBuilder.Append(t.Id);

                    tileBuilder.Append(';');
                }


            using (StreamWriter file = File.CreateText(filename))
            {
                file.Write(wires);
                file.Write("\n");
                file.Write(tileBuilder.ToString());
                file.Write("\n");
                file.Write(string.Join(",", Logics.WireColorMapping.Select(c => c.PackedValue.ToString())));
            }
        }
        internal void LoadFromOldFile(string file)
        {
            if (!File.Exists(file)) return;

            string[] lines = File.ReadAllLines(file);

            if (lines.Length < 1) return;
            if (!Wires.LoadDataString(lines[0])) return;

            Tiles = new ChunkArray2D<Tile>(16);
            if (lines.Length < 2) return;

            Regex ChunkRegex = new Regex("(\\d+),(\\d+):([^;]*);");

            foreach (Match tile in ChunkRegex.Matches(lines[1]))
            {
                Point pos = new Point(int.Parse(tile.Groups[1].Value), int.Parse(tile.Groups[2].Value));
                SetTile(pos, tile.Groups[3].Value, null, true);
            }
            if (lines.Length < 3) return;
            Logics.WireColorMapping.Clear();
            foreach (string c in lines[2].Split(','))
                Logics.WireColorMapping.Add(new Color() { PackedValue = uint.Parse(c) });
        }

        public override string ToString()
        {
            return $"{WorldId} @ {WorldPos.X} {WorldPos.Y}" + (Owner is null ? "" : $", owned by {{{Owner}}}");
        }
    }
}
