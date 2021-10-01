using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using TerraLogic.GuiElements;
using TerraLogic.Tiles;

namespace TerraLogic.Gui
{
    public class Logics : UIElement
    {
        static Regex ChunkRegex = new Regex("(\\d+),(\\d+):([^;]*);");

        internal static Dictionary<string, Tile> TileMap = new Dictionary<string, Tile>();
        internal static Dictionary<string, Tile> TilePreviews = new Dictionary<string, Tile>();
        internal static ChunkArray2D WireUpdateArray = new ChunkArray2D(8);

        internal static string[] Tools = new string[] { "remove" };
        internal static Texture2D[] ToolTextures = new Texture2D[Tools.Length];
        internal static Dictionary<string, string> ToolNames = new Dictionary<string, string> { { "remove", "Remove tile" } };

        public static Logics Instance;


        static Random Rnd = new Random();

        public Logics(string name) : base(name) { Instance = this; }
        static Logics()
        {
            foreach (Type t in typeof(Logics).Assembly.GetTypes())
            {
                if (t.IsAbstract) continue;
                if (t.BaseType == typeof(Tile) || t.BaseType == typeof(LogicGate))
                {
                    Tile tile = Activator.CreateInstance(t) as Tile;
                    TileMap.Add(tile.Id, tile);

                    if (tile.PreviewVariants is null) TilePreviews.Add(tile.Id, tile.CreateTile(null, true));
                    else foreach (string previewId in tile.PreviewVariants) TilePreviews.Add(tile.Id + ":" + previewId, tile.CreateTile(previewId, true));
                }
            }
        }

        internal static ChunkArray2D WireArray = new ChunkArray2D(16);
        internal static ChunkArray2D<Tile> TileArray = new ChunkArray2D<Tile>(16);

        internal static string SelectedTileId = null;
        internal static Tile SelectedTilePreview = null;

        internal static int SelectedToolId = -1;

        internal static byte SelectedWireColor = 0;
        internal static List<Color> WireColorMapping = new List<Color>()
        {
            Color.Red,
            Color.Green,
            Color.Blue,
            Color.Yellow
        };

        internal static Texture2D Wire, WireCross, WireTL, WireTR;

        private int WheelZoom = 0;

        public static Vector2 TileVec = new Vector2(16, 16);

        static Stack<WireSignal> WiresToSignal = new Stack<WireSignal>();

        internal static void LoadTileContent(ContentManager content)
        {
            Wire = content.Load<Texture2D>("Wires/WireDefault");
            WireCross = content.Load<Texture2D>("Wires/WireJunctionCross");
            WireTL = content.Load<Texture2D>("Wires/WireJunctionTL");
            WireTR = content.Load<Texture2D>("Wires/WireJunctionTR");

            for (int i = 0; i < Tools.Length; i++) 
            {
                ToolTextures[i] = content.Load<Texture2D>($"Tools/{Tools[i]}");
            }

            foreach (Tile t in TileMap.Values) t.LoadContent(content);
        }
        public override void Draw(SpriteBatch spriteBatch)
        {
            TerraLogic.SpriteBatch.End();
            DrawGrid();
            DrawTiles();
            if (SelectedWireColor < WireColorMapping.Count || SelectedTileId != null) DrawWires();
            DrawTilePreview();
            TerraLogic.SpriteBatch.Begin();
        }
        public override void Update()
        {
            base.Update();
            for (int y = 0; y < TileArray.Height; y++)
                for (int x = 0; x < TileArray.Width; x++)
                {
                    Tile t = TileArray[x, y];
                    if (t is null || (!t.NeedsUpdate && !t.NeedsContinuousUpdate) || t.Pos.X != x || t.Pos.Y != y) continue;
                    t.Update();
                    if (!t.NeedsContinuousUpdate) t.NeedsUpdate = false;
                }
        }

        private void DrawTilePreview()
        {
            if (!Hover) return;

            if (SelectedTilePreview is null && SelectedToolId == -1) return;

            Point wp = (PanNZoom.ScreenToWorld(MousePosition) / TileVec).ToPoint();
            if (SelectedTilePreview != null && !CanSetTile(wp.X, wp.Y, SelectedTilePreview)) return;


            TerraLogic.SpriteBatch.Begin(SpriteSortMode.Deferred, null, PanNZoom.Zoom > 1 ? SamplerState.PointWrap : SamplerState.LinearWrap, null, null);
            if (SelectedToolId != -1)
                    TerraLogic.SpriteBatch.Draw(ToolTextures[SelectedToolId], PanNZoom.WorldToScreen(new Rectangle(wp.X * 16, wp.Y * 16, 16, 16)), Color.White);

            else if (SelectedTilePreview != null)
                SelectedTilePreview.Draw(new Rectangle(wp.X * 16, wp.Y * 16, SelectedTilePreview.Size.X * 16, SelectedTilePreview.Size.Y * 16));
            TerraLogic.SpriteBatch.End();

        }

        protected internal override void MouseKeyStateUpdate(MouseKeys key, EventType @event, Point pos)
        {
            Point worldpos = (PanNZoom.ScreenToWorld(pos) / TileVec).ToPoint();
            if (@event == EventType.Hold)
            {
                if (key == MouseKeys.Left)
                {
                    if (SelectedTileId != null)
                        SetTile(worldpos, SelectedTileId);
                    else if (SelectedWireColor < WireColorMapping.Count)
                        SetWire(worldpos.X, worldpos.Y, SelectedWireColor, true);
                    else if (SelectedToolId > -1) switch (Tools[SelectedToolId]) 
                        {
                            case "remove": SetTile(worldpos, null); break;
                        }

                }
                if (key == MouseKeys.Right)
                {
                    if (SelectedWireColor < WireColorMapping.Count)
                        SetWire(worldpos.X, worldpos.Y, SelectedWireColor, false);
                    SelectedTileId = null;
                    SelectedTilePreview = null;
                    SelectedToolId = -1;
                }
            }

            if (@event == EventType.Presssed || @event == EventType.Hold)
            {

                if (key == MouseKeys.Right && SelectedWireColor >= WireColorMapping.Count)
                {
                    Tile t = TileArray[worldpos.X, worldpos.Y];
                    if (t != null)
                    {
                        t.RightClick(@event == EventType.Hold);
                    }
                }
            }

            if (key == MouseKeys.Middle) PanNZoom.UpdateDragging(@event == EventType.Hold, pos);
        }

        internal static void SaveToFile(string filename)
        {
            string wires = WireArray.ToDataString();

            StringBuilder tileBuilder = new StringBuilder();
            for (int y = 0; y < TileArray.Height; y++)
                for (int x = 0; x < TileArray.Width; x++)
                {
                    Tile t = TileArray[x, y];
                    if (t is null) continue;
                    if (x != t.Pos.X || y != t.Pos.Y) continue;

                    tileBuilder.Append(x);
                    tileBuilder.Append(',');
                    tileBuilder.Append(y);
                    tileBuilder.Append(':');
                    string data = t.GetData();
                    if (data != null)
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
                file.Write(string.Join(",", WireColorMapping.Select(c => c.PackedValue.ToString())));
            }
        }

        internal static void LoadFromFile(string file)
        {
            string[] lines = File.ReadAllLines(file);

            if (lines.Length < 1) return;
            if (!WireArray.LoadDataString(lines[0])) return;

            TileArray = new ChunkArray2D<Tile>(16);
            if (lines.Length < 2) return;
            foreach (Match tile in ChunkRegex.Matches(lines[1]))
            {
                Point pos = new Point(int.Parse(tile.Groups[1].Value), int.Parse(tile.Groups[2].Value));
                SetTile(pos, tile.Groups[3].Value, true);
            }
            if (lines.Length < 3) return;
            WireColorMapping.Clear();
            foreach (string c in lines[2].Split(',')) WireColorMapping.Add(new Color() { PackedValue = uint.Parse(c) });
        }

        protected internal override void KeyStateUpdate(Keys key, EventType @event)
        {
            if (Hover)
            {
                if (@event == EventType.Presssed)
                {
                    switch (key)
                    {
                        case Keys.D1: SelectedWireColor = 0; break;
                        case Keys.D2: SelectedWireColor = 1; break;
                        case Keys.D3: SelectedWireColor = 2; break;
                        case Keys.D4: SelectedWireColor = 3; break;
                        case Keys.D5: SelectedWireColor = 4; break;
                        case Keys.D6: SelectedWireColor = 5; break;
                        case Keys.D7: SelectedWireColor = 6; break;
                        case Keys.D8: SelectedWireColor = 7; break;
                        case Keys.D9: SelectedWireColor = 8; break;
                        case Keys.Escape: SelectedTileId = null; SelectedWireColor = 255; SelectedTilePreview = null; break;
                    }
                }

                if (@event == EventType.Hold)
                {

                    Point worldpos = (PanNZoom.ScreenToWorld(MousePosition) / new Vector2(16, 16)).ToPoint();

                    if (key == Keys.T) SetTile(worldpos, "test");

                }
            }
        }
        protected internal override void MouseWheelStateUpdate(int change, Point pos)
        {
            WheelZoom -= change;

            float zoom = WheelZoom < 0 ? -1 / (0.2f * WheelZoom - 1) : 0.2f * WheelZoom + 1;


            PanNZoom.SetZoom(zoom, pos);
        }

        private void DrawWires()
        {
            TerraLogic.SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive, PanNZoom.Zoom > 1 ? SamplerState.PointWrap : SamplerState.LinearWrap, null, null);


            Vector2 end = (PanNZoom.ScreenToWorld(new Point(Bounds.Right, Bounds.Bottom).Add(new Point(16, 16))) / TileVec);

            for (int y = (int)(PanNZoom.Position.Y / 16); y < Math.Min(WireArray.Height, (int)end.Y); y++)
                for (int x = (int)(PanNZoom.Position.X / 16); x < Math.Min(WireArray.Width, (int)end.X); x++)
                {
                    int wire = WireArray[x, y];

                    if (wire == 0) continue;

                    Texture2D wireSprite = Wire;

                    if (TileArray[x, y] is JunctionBox box)
                        switch (box.Type)
                        {
                            case JunctionBox.JunctionType.Cross: wireSprite = WireCross; break;
                            case JunctionBox.JunctionType.TL: wireSprite = WireTL; break;
                            case JunctionBox.JunctionType.TR: wireSprite = WireTR; break;
                        }

                    int wireTop = WireArray[x, y - 1];
                    int wireLeft = WireArray[x - 1, y];
                    int wireBottom = WireArray[x, y + 1];
                    int wireRight = WireArray[x + 1, y];

                    RectangleF rect = new RectangleF(x * TileVec.X, y * TileVec.Y, TileVec.X, TileVec.Y);

                    bool blackDrawn = false;

                    bool anyTop = false, anyRight = false, anyBottom = false, anyLeft = false;
                    bool[] top = new bool[WireColorMapping.Count];
                    bool[] right = new bool[WireColorMapping.Count];
                    bool[] bottom = new bool[WireColorMapping.Count];
                    bool[] left = new bool[WireColorMapping.Count];

                    void CalcWire(byte id)
                    {
                        if (!GetWire(wire, id)) return;

                        top[id] = y > 0 && GetWire(wireTop, id);
                        left[id] = x > 0 && GetWire(wireLeft, id);
                        bottom[id] = y < WireArray.Height - 1 && GetWire(wireBottom, id);
                        right[id] = x < WireArray.Width - 1 && GetWire(wireRight, id);

                        anyTop |= top[id];
                        anyRight |= right[id];
                        anyBottom |= bottom[id];
                        anyLeft |= left[id];
                    }

                    void DrawWire(byte id, bool @override)
                    {
                        if (!GetWire(wire, id)) return;

                        Color c = WireColorMapping[id];
                        if (SelectedWireColor != id)
                        {
                            c.R /= 2;
                            c.G /= 2;
                            c.B /= 2;
                        }

                        if (!blackDrawn)
                        {
                            TerraLogic.SpriteBatch.End();
                            TerraLogic.SpriteBatch.Begin(SpriteSortMode.Deferred, null, PanNZoom.Zoom > 1 ? SamplerState.PointWrap : SamplerState.LinearWrap, null, null);
                            TerraLogic.SpriteBatch.Draw(wireSprite, PanNZoom.WorldToScreen(rect).PixelStretch(), CalculateWireSpriteOffset(anyTop, anyRight, anyBottom, anyLeft), Color.Black, 0f, Vector2.Zero, SpriteEffects.None, 0);
                            TerraLogic.SpriteBatch.End();
                            TerraLogic.SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive, PanNZoom.Zoom > 1 ? SamplerState.PointWrap : SamplerState.LinearWrap, null, null);
                        }
                        blackDrawn = true;

                        if (@override)
                        {
                            TerraLogic.SpriteBatch.End();
                            TerraLogic.SpriteBatch.Begin(SpriteSortMode.Deferred, null, PanNZoom.Zoom > 1 ? SamplerState.PointWrap : SamplerState.LinearWrap, null, null);
                            TerraLogic.SpriteBatch.Draw(wireSprite, PanNZoom.WorldToScreen(rect).PixelStretch(), CalculateWireSpriteOffset(top[id], right[id], bottom[id], left[id]), Color.Black, 0f, Vector2.Zero, SpriteEffects.None, 0);
                            TerraLogic.SpriteBatch.End();
                            TerraLogic.SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive, PanNZoom.Zoom > 1 ? SamplerState.PointWrap : SamplerState.LinearWrap, null, null);
                        }

                        TerraLogic.SpriteBatch.Draw(wireSprite, PanNZoom.WorldToScreen(rect).PixelStretch(), CalculateWireSpriteOffset(top[id], right[id], bottom[id], left[id]), c, 0f, Vector2.Zero, SpriteEffects.None, 0);

                    }

                    for (byte id = 0; id < WireColorMapping.Count; id++)
                        CalcWire(id);

                    for (byte id = 0; id < WireColorMapping.Count; id++)
                        if (id != SelectedWireColor) DrawWire(id, false);

                    if (SelectedWireColor < WireColorMapping.Count)
                    {
                        DrawWire(SelectedWireColor, true);
                    }

                }
            TerraLogic.SpriteBatch.End();
        }

        private void DrawTiles()
        {
            TerraLogic.SpriteBatch.Begin(SpriteSortMode.Deferred, null, PanNZoom.Zoom > 1 ? SamplerState.PointWrap : SamplerState.LinearWrap, null, null);

            Vector2 end = PanNZoom.ScreenToWorld(new Point(Bounds.Right, Bounds.Bottom)) / new Vector2(16, 16);

            for (int y = (int)(PanNZoom.Position.Y / 16); y < Math.Min(TileArray.Height, (int)end.Y); y++)
                for (int x = (int)(PanNZoom.Position.X / 16); x < Math.Min(TileArray.Width, (int)end.X); x++)
                {
                    Tile t = TileArray[x, y];
                    if (t is null || t.Pos.X != x || t.Pos.Y != y) continue;
                    t.Draw(new Rectangle(x * 16, y * 16, t.Size.X * 16, t.Size.Y * 16));
                }
            TerraLogic.SpriteBatch.End();
        }
        private void DrawGrid()
        {
            Rectangle rect = Bounds;
            Vector2 v2 = Vector2.Zero;

            int zmul = 1;
            int zx = (int)(1 / PanNZoom.Zoom);

            while ((zmul << 1) < zx) zmul <<= 1;

            float grid = 16 * PanNZoom.Zoom * zmul;

            v2.X = PanNZoom.ScreenPosition.X % grid - grid;
            v2.Y = PanNZoom.ScreenPosition.Y % grid - grid;

            rect.Width = (int)(rect.Width / (PanNZoom.Zoom * zmul) + grid * 2);
            rect.Height = (int)(rect.Height / (PanNZoom.Zoom * zmul) + grid * 2);


            TerraLogic.SpriteBatch.Begin(SpriteSortMode.Deferred, null, PanNZoom.Zoom > 1 ? SamplerState.PointWrap : SamplerState.LinearWrap, null, null);
            TerraLogic.SpriteBatch.Draw(TerraLogic.GridTex, v2, rect, new Color(64, 64, 64), 0f, Vector2.Zero, PanNZoom.Zoom * zmul, SpriteEffects.None, 1f);
            TerraLogic.SpriteBatch.End();

        }

        public static Rectangle CalculateWireSpriteOffset(bool top, bool right, bool bottom, bool left)
        {
            Vector2 pos = Vector2.Zero;

            if (top) pos.X += TileVec.X;
            if (right) pos.X += TileVec.X * 2;
            if (bottom) pos.Y += TileVec.Y;
            if (left) pos.Y += TileVec.Y * 2;

            return new Rectangle((int)pos.X, (int)pos.Y, (int)TileVec.X, (int)TileVec.Y);
        }

        public static void SendWireSignal(Point pos, int wire)
        {
            SendWireSignal(new Rectangle(pos.X, pos.Y, 1, 1), wire);
        }
        public static void SendWireSignal(Rectangle rect, int wire)
        {
            int updateId = Rnd.Next();

            HashSet<LogicGate> gatesToUpdate = new HashSet<LogicGate>();

            for (int y = rect.Y; y < rect.Bottom; y++)
                for (int x = rect.X; x < rect.Right; x++)
                {
                    WiresToSignal.Push(new WireSignal(x, y, WireArray[x, y] & wire));
                }

            while (WiresToSignal.Count > 0)
            {
                WireSignal w = WiresToSignal.Pop();
                if (WireUpdateArray[w.X, w.Y] == updateId || w.Wire == 0) continue;
                WireUpdateArray[w.X, w.Y] = updateId;

                if (!w.IsAtOrigin)
                {
                    Tile t = TileArray[w.X, w.Y];
                    if (t != null)
                    {
                        t.WireSignal(w.Wire, w.Origin);
                        if (t is LogicLamp) 
                        {
                            int scanpos = w.Y;
                            while (TileArray[w.X, scanpos] is LogicLamp) scanpos++;
                            if (TileArray[w.X, scanpos] is LogicGate lg)
                            {
                                if (w.IsOrigin(w.X, scanpos))
                                {
                                    Debug.WriteLine($"[{lg}] Puff!");
                                }
                                gatesToUpdate.Add(lg);
                            }
                        }
                    }
                }

                Point pos = new Point(w.X, w.Y - 1); // top
                if (TileArray[pos.X, pos.Y] is JunctionBox topBox)
                    switch (topBox.Type)
                    {
                        case JunctionBox.JunctionType.Cross: pos.Y--; break;
                        case JunctionBox.JunctionType.TL: pos.X++; break;
                        case JunctionBox.JunctionType.TR: pos.X--; break;
                    }

                int nextWire = WireArray[pos.X, pos.Y] & w.Wire;
                if (nextWire > 0) WiresToSignal.Push(w.NewPos(pos, nextWire));


                pos = new Point(w.X + 1, w.Y); // right
                if (TileArray[pos.X, pos.Y] is JunctionBox rightBox)
                    switch (rightBox.Type)
                    {
                        case JunctionBox.JunctionType.Cross: pos.X++; break;
                        case JunctionBox.JunctionType.TL: pos.Y--; break;
                        case JunctionBox.JunctionType.TR: pos.Y++; break;
                    }

                nextWire = WireArray[pos.X, pos.Y] & w.Wire;
                if (nextWire > 0) WiresToSignal.Push(w.NewPos(pos, nextWire));


                pos = new Point(w.X, w.Y + 1); // bottom
                if (TileArray[pos.X, pos.Y] is JunctionBox bottomBox)
                    switch (bottomBox.Type)
                    {
                        case JunctionBox.JunctionType.Cross: pos.Y++; break;
                        case JunctionBox.JunctionType.TL: pos.X--; break;
                        case JunctionBox.JunctionType.TR: pos.X++; break;
                    }

                nextWire = WireArray[pos.X, pos.Y] & w.Wire;
                if (nextWire > 0) WiresToSignal.Push(w.NewPos(pos, nextWire));


                pos = new Point(w.X - 1, w.Y); // left
                if (TileArray[pos.X, pos.Y] is JunctionBox leftBox)
                    switch (leftBox.Type)
                    {
                        case JunctionBox.JunctionType.Cross: pos.X--; break;
                        case JunctionBox.JunctionType.TL: pos.Y++; break;
                        case JunctionBox.JunctionType.TR: pos.Y--; break;
                    }

                nextWire = WireArray[pos.X, pos.Y] & w.Wire;
                if (nextWire > 0) WiresToSignal.Push(w.NewPos(pos, nextWire));
            }
            foreach (LogicGate lg in gatesToUpdate) lg.UpdateState();


        }

        internal static void SetWire(int x, int y, byte id, bool state)
        {
            int current = WireArray[x, y];
            int mask = ~(1 << id);
            int newstate = current & mask;
            if (state) newstate |= ~mask;
            if (newstate == current) return;
            WireArray[x, y] = newstate;
        }
        internal static bool GetWire(int x, int y, byte id)
        {
            return ((WireArray[x, y] >> id) & 1) != 0;
        }
        internal static bool GetWire(int wire, byte id)
        {
            return ((wire >> id) & 1) != 0;
        }

        internal static void SetTile(Point pos, string tileid = null, bool noUpdates = false) => SetTile(pos.X, pos.Y, tileid, noUpdates);
        internal static void SetTile(int x, int y, string tileid = null, bool noUpdates = false)
        {
            if (tileid == "") tileid = null;
            Tile t = TileArray[x, y];

            if (tileid is null)
            {
                if (t is null) return;
                t.BeforeDestroy();
                for (int ty = t.Pos.Y; ty < t.Size.Y + t.Pos.Y; ty++)
                    for (int tx = t.Pos.X; tx < t.Size.X + t.Pos.X; tx++)
                        TileArray[tx, ty] = null;
                return;
            }

            string[] data = tileid.Split(new char[] { ':' }, 2);

            if (TileMap.TryGetValue(data[0], out Tile tile))
                if (CanSetTile(x, y, tile))
                {
                    t = tile.CreateTile(data.Length == 1 ? null : data[1], false);
                    t.Created = true;
                    t.Pos = new Point(x, y);

                    for (int ty = y; ty < t.Size.Y + y; ty++)
                        for (int tx = x; tx < t.Size.X + x; tx++)
                            TileArray[tx, ty] = t;

                    if (!noUpdates) t.PlacedInWorld();
                }
        }
        internal static bool CanSetTile(int x, int y, Tile t)
        {
            for (int ty = y; ty < t.Size.Y + y; ty++)
                for (int tx = x; tx < t.Size.X + x; tx++)
                {
                    if (TileArray[tx, ty] != null) return false;
                }
            return true;

        }

    }

    struct WireSignal
    {
        public Point Origin;
        public int X, Y;
        public int Wire;

        public WireSignal(int x, int y, int wire)
        {
            Origin = new Point(x, y);
            this.X = x;
            this.Y = y;
            Wire = wire;
        }

        public WireSignal NewPos(Point pos, int wire)
        {
            return new WireSignal() { Origin = Origin, X = pos.X, Y = pos.Y, Wire = wire };
        }

        public bool IsOrigin(int x, int y)
        {
            return x == Origin.X && y == Origin.Y;
        }

        public bool IsAtOrigin { get => X == Origin.X && Y == Origin.Y; }
    }

}
