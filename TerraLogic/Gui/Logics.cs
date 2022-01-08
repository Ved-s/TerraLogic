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
        internal static ChunkArray2D<long> WireUpdateArray = new ChunkArray2D<long>(8);

        internal static List<Tools.Tool> Tools = new List<Tools.Tool>();

        public static Logics Instance;
        public static Stopwatch WireUpdateWatch = new Stopwatch();

        static readonly Random Rnd = new Random();

        public Logics(string name) : base(name) { Instance = this; }
        static Logics()
        {
            foreach (Type t in typeof(Logics).Assembly.GetTypes())
            {
                if (t.IsAbstract) continue;
                if (t.BaseType == typeof(Tile) || t.BaseType.BaseType == typeof(Tile))
                {
                    Tile tile = Activator.CreateInstance(t) as Tile;
                    TileMap.Add(tile.Id, tile);

                    if (tile.PreviewVariants is null) TilePreviews.Add(tile.Id, tile.CreateTile(null, true));
                    else foreach (string previewId in tile.PreviewVariants) TilePreviews.Add(tile.Id + ":" + previewId, tile.CreateTile(previewId, true));
                }
                else if (t.BaseType == typeof(Tools.Tool))
                {
                    Tools.Add(Activator.CreateInstance(t) as Tools.Tool);
                }
            }
        }

        internal static ChunkArray2D WireArray = new ChunkArray2D(16);
        internal static ChunkArray2D<Tile> TileArray = new ChunkArray2D<Tile>(16);

        internal static string SelectedTileId = null;
        internal static Tile SelectedTilePreview = null;
        internal static int SelectedToolId = -1;
        internal static int SelectedWire = 0;
        internal static PastePreview PastePreview = null;

        internal static List<WireDebug> WireDebug = new List<WireDebug>();
        internal static bool WireDebugActive = false;

        internal static List<Color> WireColorMapping = new List<Color>()
        {
            Color.Red,
            Color.Green,
            Color.Blue,
            Color.Yellow
        };

        internal static Texture2D Wire, WireCross, WireTL, WireTR;

        private int WheelZoom = 0;

        public static Point TileSize = new Point(16, 16);

        public static Rectangle ViewBounds;

        public static bool UpdatePaused, UpdateTick;

        public static HashSet<LogicGate> GatesToUpdate = new HashSet<LogicGate>();

        internal static void LoadTileContent(ContentManager content)
        {
            Wire = content.Load<Texture2D>("Wires/WireDefault");
            WireCross = content.Load<Texture2D>("Wires/WireJunctionCross");
            WireTL = content.Load<Texture2D>("Wires/WireJunctionTL");
            WireTR = content.Load<Texture2D>("Wires/WireJunctionTR");

            foreach (Tools.Tool t in Tools)
            {
                t.Texture = content.Load<Texture2D>($"Tools/{t.Id}");
            }

            foreach (Tile t in TileMap.Values) t.LoadContent(content);
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
            if (!File.Exists(file)) return;

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

        internal static void CopyToClipboard(Rectangle selection)
        {
            StringBuilder tileBuilder = new StringBuilder();
            for (int y = selection.Y; y < selection.Bottom; y++)
                for (int x = selection.X; x < selection.Right; x++)
                {
                    Tile t = TileArray[x, y];
                    if (t is null) continue;
                    if (x != t.Pos.X || y != t.Pos.Y) continue;

                    tileBuilder.Append(x - selection.X);
                    tileBuilder.Append(',');
                    tileBuilder.Append(y - selection.Y);
                    tileBuilder.Append(':');
                    string data = t.GetData();
                    if (data != null)
                        tileBuilder.Append(t.Id + ":" + data);
                    else
                        tileBuilder.Append(t.Id);

                    tileBuilder.Append(';');
                }

            ClipboardUtils.Text = WireArray.ToPartialDataString(selection) + tileBuilder.ToString();
        }
        internal static void LoadFromClipboard()
        {
            string[] data = ClipboardUtils.Text.Split(new char[] { ';' }, 2);
            if (data.Length != 2) return;

            data[0] = data[0] + ';';

            Match header = ChunkArray2D.ChunkRegex.Match(data[0]);
            if (!header.Success) return;

            Point size = new Point(int.Parse(header.Groups[1].Value), int.Parse(header.Groups[2].Value));
            Tile[,] tiles = new Tile[size.X, size.Y];
            foreach (Match tile in ChunkRegex.Matches(data[1]))
            {
                int x = int.Parse(tile.Groups[1].Value);
                int y = int.Parse(tile.Groups[2].Value);
                string[] tileData = tile.Groups[3].Value.Split(new char[] { ':' }, 2);

                if (TileMap.TryGetValue(tileData[0], out Tile newTile))
                {
                    tiles[x, y] = newTile.CreateTile(tileData.Length == 1 ? null : tileData[1], false);
                }
            }

            if (SelectedToolId != -1) Tools[SelectedToolId].IsSelected = false;
            SelectedToolId = -1;
            SelectedTileId = null;
            SelectedTilePreview = null;
            SelectedWire = 0;
            global::TerraLogic.Tools.Select.Instance.Selection = new Rectangle();
            PastePreview = new PastePreview() { WireData = data[0], Size = size, Tiles = tiles };
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            TerraLogic.SpriteBatch.End();
            DrawGrid();
            DrawTiles();
            if (SelectedWire > 0
                || SelectedTileId != null
                || (SelectedToolId > -1 && Tools[SelectedToolId].ShowWires))
                DrawWires(WireArray, BlendState.AlphaBlend);
            DrawTilePreview();

            for (int i = 0; i < Tools.Count; i++)
                Tools[i].Draw(spriteBatch, SelectedToolId == i);


            DrawWireDebug();

            TerraLogic.SpriteBatch.Begin();

        }

        public override void Update()
        {
            WireUpdateWatch.Reset();
            base.Update();
            if (SelectedToolId > -1) Tools[SelectedToolId].Update();

            bool currentPausedUpdateTick = UpdatePaused && UpdateTick;
            if (currentPausedUpdateTick)
                UpdateTick = false;

            if (currentPausedUpdateTick || !WireDebugActive)
                WireDebug.Clear();

            if (!UpdatePaused || currentPausedUpdateTick && GatesToUpdate.Count == 0)
            {
                foreach (ChunkArray2D<Tile>.ChunkItem tile in TileArray)
                {
                    if (tile.Item is null || (!tile.Item.NeedsUpdate && !tile.Item.NeedsContinuousUpdate)
                    || tile.Item.Pos.X != tile.X || tile.Item.Pos.Y != tile.Y) continue;
                    tile.Item.Update();
                    if (!tile.Item.NeedsContinuousUpdate) tile.Item.NeedsUpdate = false;
                }
            }

            if (UpdatePaused && currentPausedUpdateTick && GatesToUpdate.Count > 0)
                UpdateGates();
            else if (!UpdatePaused)
                while (GatesToUpdate.Count > 0)
                    UpdateGates();


            WireDebug.RemoveAll(wd => wd.Fade <= 0);

            if (!UpdatePaused)
                foreach (WireDebug wd in WireDebug)
                {
                    wd.Fade--;
                }

            ViewBounds.X = (int)(PanNZoom.Position.X / TileSize.X);
            ViewBounds.Y = (int)(PanNZoom.Position.Y / TileSize.Y);
            ViewBounds.Width = (int)(TerraLogic.Instance.Window.ClientBounds.Width / (PanNZoom.Zoom * TileSize.X)) + 1;
            ViewBounds.Height = (int)(TerraLogic.Instance.Window.ClientBounds.Height / (PanNZoom.Zoom * TileSize.Y)) + 1;
        }

        private static void UpdateGates()
        {
            LogicGate[] gates = GatesToUpdate.ToArray();
            GatesToUpdate.Clear();

            foreach (LogicGate g in gates)
                g.UpdateState();

            foreach (LogicGate g in gates)
                g.Update();
        }

        protected internal override void MouseKeyStateUpdate(MouseKeys key, EventType @event, Point pos)
        {
            Point worldpos = (PanNZoom.ScreenToWorld(pos) / TileSize.ToVector2()).ToPoint();

            if (SelectedToolId > -1) Tools[SelectedToolId].MouseKeyUpdate(key, @event, worldpos);

            if (key == MouseKeys.Left)
            {
                if (@event == EventType.Hold)
                {
                    if (SelectedTileId != null)
                    {
                        string data = SelectedTilePreview.GetData();
                        SetTile(worldpos, SelectedTilePreview.Id + (data is null ? "" : ":" + data));
                    }
                    else if (SelectedWire > 0 && SelectedToolId == -1)
                        SetWires(worldpos.X, worldpos.Y, SelectedWire, true);
                }
                if (@event == EventType.Presssed)
                {
                    Rectangle selection = global::TerraLogic.Tools.Select.Instance.Selection;
                    if (selection.Contains(worldpos))
                    {
                        if (SelectedTileId != null)
                        {
                            string data = SelectedTilePreview.GetData();
                            data = SelectedTilePreview.Id + (data is null ? "" : ":" + data);

                            for (int y = selection.Y; y < selection.Bottom; y++)
                                for (int x = selection.X; x < selection.Right; x++)
                                    SetTile(x, y, data);
                        }
                        else if (SelectedWire > 0)
                        {
                            for (int y = selection.Y; y < selection.Bottom; y++)
                                for (int x = selection.X; x < selection.Right; x++)
                                    SetWires(x, y, SelectedWire, true);
                        }
                    }

                    if (PastePreview is not null)
                    {
                        WireArray.LoadPartialDataString(PastePreview.WireData, worldpos, true);
                        for (int y = 0; y < PastePreview.Size.Y; y++)
                            for (int x = 0; x < PastePreview.Size.X; x++)
                                if (PastePreview.Tiles[x, y] is not null)
                                {
                                    string data = PastePreview.Tiles[x, y].GetData();
                                    SetTile(worldpos.X + x, worldpos.Y + y, PastePreview.Tiles[x, y].Id + (data is null ? "" : ":" + data), true);
                                }
                    }
                }
            }
            if (key == MouseKeys.Right)
            {
                if (@event == EventType.Presssed)
                {
                    if (SelectedToolId != -1) Tools[SelectedToolId].IsSelected = false;
                    SelectedTileId = null;
                    SelectedTilePreview = null;
                    SelectedToolId = -1;
                    PastePreview = null;
                }
                if (@event == EventType.Presssed || @event == EventType.Hold)
                {

                    if (SelectedWire == 0)
                    {
                        Tile t = TileArray[worldpos.X, worldpos.Y];
                        if (t != null)
                        {
                            t.RightClick(@event == EventType.Hold, false);
                        }
                    }
                }
                if (@event == EventType.Hold)
                {

                    if (SelectedWire > 0 && SelectedToolId == -1)
                        SetWires(worldpos.X, worldpos.Y, SelectedWire, false);

                }
            }
            if (key == MouseKeys.Middle) PanNZoom.UpdateDragging(@event == EventType.Hold, pos);
        }
        protected internal override void KeyStateUpdate(Keys key, EventType @event)
        {
            if (Hover)
            {
                if (@event == EventType.Presssed)
                {
                    switch (key)
                    {
                        case Keys.D1: SelectedWire ^= 1; break;
                        case Keys.D2: SelectedWire ^= 2; break;
                        case Keys.D3: SelectedWire ^= 4; break;
                        case Keys.D4: SelectedWire ^= 8; break;
                        case Keys.D5: SelectedWire ^= 16; break;
                        case Keys.D6: SelectedWire ^= 32; break;
                        case Keys.D7: SelectedWire ^= 64; break;
                        case Keys.D8: SelectedWire ^= 128; break;
                        case Keys.D9: SelectedWire ^= 256; break;
                        case Keys.Escape:
                            if (SelectedToolId != -1) Tools[SelectedToolId].IsSelected = false;
                            SelectedToolId = -1;

                            SelectedTileId = null;
                            SelectedWire = 0;
                            SelectedTilePreview = null;
                            PastePreview = null;
                            global::TerraLogic.Tools.Select.Instance.Selection = new Rectangle();
                            break;
                    }
                    if (key == Keys.V && Root.CurrentKeys.IsKeyDown(Keys.LeftControl)) LoadFromClipboard();
                }

                if (@event == EventType.Hold)
                {

                    Point worldpos = (PanNZoom.ScreenToWorld(MousePosition) / new Vector2(16, 16)).ToPoint();

                    if (key == Keys.T) SetTile(worldpos, "test");

                }

                if (SelectedToolId > -1) Tools[SelectedToolId].KeyUpdate(key, @event, Root.CurrentKeys);
            }
        }
        protected internal override void MouseWheelStateUpdate(int change, Point pos)
        {
            WheelZoom -= change;

            float zoom = WheelZoom < 0 ? -1 / (0.2f * WheelZoom - 1) : 0.2f * WheelZoom + 1;


            PanNZoom.SetZoom(zoom, pos);
        }

        private static void DrawWireDebug()
        {
            if (!WireDebugActive) return;

            TerraLogic.SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);

            Vector2 end = PanNZoom.ScreenToWorld(new Point(Instance.Bounds.Right, Instance.Bounds.Bottom)) / new Vector2(16, 16);
            Rectangle drawArea = new Rectangle((int)(PanNZoom.Position.X / 16), (int)(PanNZoom.Position.Y / 16),
                (int)end.X + 1, (int)end.Y + 1);

            foreach (WireDebug wd in WireDebug)
            {
                Point originCenter = wd.Signal.Origin.Multiply(TileSize);
                originCenter.X += TileSize.X / 2;
                originCenter.Y += TileSize.Y / 2;

                if (drawArea.Contains(wd.Signal.Origin) || drawArea.Contains(wd.Signal.Pos))
                {
                    Color c = GetWireColor(wd.Signal.Wire);
                    c *= (wd.Fade / 300f);

                    Graphics.DrawRectangle(TerraLogic.SpriteBatch, PanNZoom.WorldToScreen(new Rectangle(wd.Signal.Origin.X * TileSize.X, wd.Signal.Origin.Y * TileSize.Y, TileSize.X, TileSize.Y)), c);
                    Graphics.DrawLineWithText(TerraLogic.SpriteBatch,
                        PanNZoom.WorldToScreen(originCenter.ToVector2()),
                        PanNZoom.WorldToScreen(new Vector2((wd.Signal.X * TileSize.X) + (TileSize.X / 2), (wd.Signal.Y * TileSize.Y) + (TileSize.Y / 2))),
                        Instance.Font, wd.Count.ToString(), c);
                }
            }
            TerraLogic.SpriteBatch.End();

        }
        private static void DrawTilePreview()
        {
            if (!Instance.Hover) return;
            if (SelectedTilePreview is null && SelectedToolId == -1 && PastePreview is null) return;

            Point wp = (PanNZoom.ScreenToWorld(Instance.MousePosition) / TileSize.ToVector2()).ToPoint();

            TerraLogic.SpriteBatch.Begin(SpriteSortMode.Deferred, null, PanNZoom.Zoom > 1 ? SamplerState.PointWrap : SamplerState.LinearWrap, null, null);

            if (PastePreview is not null)
            {
                Rectangle rect = PanNZoom.WorldToScreen(new Rectangle(wp.X, wp.Y, PastePreview.Size.X, PastePreview.Size.Y).Mul(TileSize));
                Graphics.FillRectangle(TerraLogic.SpriteBatch, rect, Color.CornflowerBlue.Div(5, true));

                for (int y = 0; y < PastePreview.Size.Y; y++)
                    for (int x = 0; x < PastePreview.Size.X; x++)
                        if (PastePreview.Tiles[x, y] is not null)
                        {
                            PastePreview.Tiles[x, y].Draw(new Rectangle(wp.X + x, wp.Y + y, PastePreview.Tiles[x, y].Size.X, PastePreview.Tiles[x, y].Size.Y).Mul(TileSize));
                        }
            }

            else if (SelectedToolId != -1 && Tools[SelectedToolId].DrawMouseIcon)
                TerraLogic.SpriteBatch.Draw(Tools[SelectedToolId].Texture, PanNZoom.WorldToScreen(new Rectangle(wp.X * 16, wp.Y * 16, 16, 16)), Color.White);

            else if (SelectedTilePreview != null && CanSetTile(wp.X, wp.Y, SelectedTilePreview))
                SelectedTilePreview.Draw(new Rectangle(wp.X, wp.Y, SelectedTilePreview.Size.X, SelectedTilePreview.Size.Y).Mul(TileSize));
            TerraLogic.SpriteBatch.End();
        }
        public static void DrawWires(ChunkArray2D wires, BlendState blendState)
        {
            TerraLogic.SpriteBatch.Begin(SpriteSortMode.Deferred, blendState, PanNZoom.Zoom > 1 ? SamplerState.PointWrap : SamplerState.LinearWrap, null, null);

            int wireTop;
            int wireLeft;
            int wireBottom;
            int wireRight;

            Rectangle rect;

            foreach (ChunkArray2D.ChunkItem chunkWire in wires)
                if (ViewBounds.Contains(chunkWire.X, chunkWire.Y))
                {
                    int x = chunkWire.X;
                    int y = chunkWire.Y;

                    int wire = chunkWire.Item;
                    if (wire == 0) continue;

                    Texture2D wireSprite = Wire;
                    bool anySelectedWireDrawn = false;

                    if (TileArray[x, y] is JunctionBox box)
                        switch (box.Type)
                        {
                            case JunctionBox.JunctionType.Cross: wireSprite = WireCross; break;
                            case JunctionBox.JunctionType.TL: wireSprite = WireTL; break;
                            case JunctionBox.JunctionType.TR: wireSprite = WireTR; break;
                        }

                    wireTop = wires[x, y - 1];
                    wireLeft = wires[x - 1, y];
                    wireBottom = wires[x, y + 1];
                    wireRight = wires[x + 1, y];

                    rect = new Rectangle(x * TileSize.X, y * TileSize.Y, TileSize.X, TileSize.Y);

                    void DrawWire(byte id)
                    {
                        if (!GetWire(wire, id)) return;

                        Color c = WireColorMapping[id];



                        if (!GetWire(SelectedWire, id))
                        {
                            if (SelectedWire.Bits() == 1)
                                c *= 0.5f;
                            else c *= 0.25f;
                        }
                        else
                        {
                            if (SelectedWire.Bits() > 1 && anySelectedWireDrawn) c *= 0.5f;
                            anySelectedWireDrawn = true;
                        }

                        TerraLogic.SpriteBatch.Draw(wireSprite, PanNZoom.WorldToScreen(rect), CalculateWireSpriteOffset(GetWire(wireTop, id), GetWire(wireRight, id), GetWire(wireBottom, id), GetWire(wireLeft, id)), c, 0f, Vector2.Zero, SpriteEffects.None, 0);

                    }

                    for (byte id = 0; id < WireColorMapping.Count; id++)
                        if (!GetWire(SelectedWire, id)) DrawWire(id);

                    for (byte id = 0; id < WireColorMapping.Count; id++)
                        if (GetWire(SelectedWire, id)) DrawWire(id);

                }
            TerraLogic.SpriteBatch.End();
        }
        private static void DrawTiles()
        {
            TerraLogic.SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, PanNZoom.Zoom > 1 ? SamplerState.PointWrap : SamplerState.LinearWrap, null, null);
            foreach (ChunkArray2D<Tile>.ChunkItem t in TileArray)
            {
                if (t.Item is null || t.Item.Pos.X != t.X || t.Item.Pos.Y != t.Y) continue;
                t.Item.Draw(new Rectangle(t.X * 16, t.Y * 16, t.Item.Size.X * 16, t.Item.Size.Y * 16));
            }
            TerraLogic.SpriteBatch.End();
        }
        private static void DrawGrid()
        {
            Rectangle rect = Instance.Bounds;
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

            if (top) pos.X += TileSize.X;
            if (right) pos.X += TileSize.X * 2;
            if (bottom) pos.Y += TileSize.Y;
            if (left) pos.Y += TileSize.Y * 2;

            return new Rectangle((int)pos.X, (int)pos.Y, (int)TileSize.X, (int)TileSize.Y);
        }

        public static void SignalWire(Point pos, int wire)
        {
            SignalWire(new Rectangle(pos.X, pos.Y, 1, 1), wire);
        }
        public static void SignalWire(Rectangle rect, int wire)
        {
            WireUpdateWatch.Start();

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
                    Tile t = TileArray[w.X, w.Y];
                    if (t != null)
                    {
                        t.WireSignal(w.Wire, w.Origin);
                        AddDebug(w);
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
                                GatesToUpdate.Add(lg);
                            }
                        }
                    }
                }
            }

            WireUpdateWatch.Stop();

        }
        public static WireSignal[] TrackWire(Point pos, int wire) => TrackWire(new Point[] { pos }, wire);
        public static WireSignal[] TrackWire(Point[] points, int wire)
        {
            int trackId = Rnd.Next();

            Stack<WireSignal> WiresToTrack = new Stack<WireSignal>();
            List<WireSignal> TrackedPoints = new List<WireSignal>();

            foreach (Point p in points)
            {
                int thiswire = WireArray[p.X, p.Y] & wire;
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
                    int nextWire = WireArray[pos.X, pos.Y] & w.Wire;
                    while (nextWire > 0 && TileArray[pos.X, pos.Y] is JunctionBox box)
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

                        nextWire = WireArray[pos.X, pos.Y] & nextWire;
                    }
                    if (nextWire > 0) WiresToTrack.Push(w.NewPos(pos, nextWire));
                }

                TrackWire(new Point(w.X, w.Y - 1), Side.Down);
                TrackWire(new Point(w.X + 1, w.Y), Side.Left);
                TrackWire(new Point(w.X, w.Y + 1), Side.Up);
                TrackWire(new Point(w.X - 1, w.Y), Side.Right);
                //Point pos = new Point(w.X, w.Y - 1); // top
                //int nextWire = WireArray[pos.X, pos.Y] & w.Wire;
                //bool redirected = false;
                //
                //if (nextWire > 0)
                //{
                //    if (TileArray[pos.X, pos.Y] is JunctionBox topBox)
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
                //    nextWire = WireArray[pos.X, pos.Y] & w.Wire;
                //    WiresToTrack.Push(w.NewPos(pos, nextWire));
                //}
                //
                //
                //pos = new Point(w.X + 1, w.Y); // right
                //nextWire = WireArray[pos.X, pos.Y] & w.Wire;
                //
                //if (nextWire > 0)
                //{
                //    if (TileArray[pos.X, pos.Y] is JunctionBox rightBox)
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
                //    nextWire = WireArray[pos.X, pos.Y] & nextWire;
                //    WiresToTrack.Push(w.NewPos(pos, nextWire));
                //}
                //
                //
                //pos = new Point(w.X, w.Y + 1); // bottom
                //nextWire = WireArray[pos.X, pos.Y] & w.Wire;
                //if (nextWire > 0)
                //{
                //    if (TileArray[pos.X, pos.Y] is JunctionBox bottomBox)
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
                //    nextWire = WireArray[pos.X, pos.Y] & nextWire;
                //    WiresToTrack.Push(w.NewPos(pos, nextWire));
                //}
                //
                //pos = new Point(w.X - 1, w.Y); // left
                //nextWire = WireArray[pos.X, pos.Y] & w.Wire;
                //if (nextWire > 0)
                //{
                //    if (TileArray[pos.X, pos.Y] is JunctionBox leftBox)
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
                //    nextWire = WireArray[pos.X, pos.Y] & nextWire;
                //    WiresToTrack.Push(w.NewPos(pos, nextWire));
                //}
            }

            return TrackedPoints.ToArray();
        }

        internal static void AddDebug(WireSignal signal)
        {
            if (!WireDebugActive) return;

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

        internal static void SetWire(int x, int y, byte id, bool state)
        {
            int current = WireArray[x, y];
            int mask = ~(1 << id);
            int newstate = current & mask;
            if (state) newstate |= ~mask;
            if (newstate == current) return;
            WireArray[x, y] = newstate;
        }
        internal static void SetWires(int x, int y, int wires, bool state)
        {
            int current = WireArray[x, y];
            int newstate = current & ~wires;
            if (state) newstate |= wires;
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

        internal static Color GetWireColor(int wire)
        {
            if (wire == 0) return Color.Transparent;

            int count = 0;
            int r = 0, g = 0, b = 0;

            for (int i = 0; i < 32; i++)
            {
                if ((wire & 1) == 1)
                {
                    Color c = WireColorMapping[i];

                    r += c.R;
                    g += c.G;
                    b += c.B;
                    count++;
                }
                wire >>= 1;
            }

            return new Color(r / count, g / count, b / count);
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

    public struct WireSignal
    {
        public Point Origin;
        public Point Pos => new(X, Y); 
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
    class WireDebug
    {
        public WireSignal Signal;
        public int Count;
        public int Fade;

        public WireDebug(WireSignal signal)
        {
            Signal = signal;
            Count = 1;
            Fade = 300;
        }

        public void Add()
        {
            Count++;
            Fade = 300;
        }
    }
    class PastePreview
    {
        internal string WireData;
        internal Point Size;
        internal Tile[,] Tiles;
    }

    enum Side { Up, Right, Down, Left }


}
