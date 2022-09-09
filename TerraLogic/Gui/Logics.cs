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
using TerraLogic.GuiElements;
using TerraLogic.Tiles;

namespace TerraLogic.Gui
{
    public class Logics : UIElement
    {

        internal static Dictionary<string, Tile> TileMap = new Dictionary<string, Tile>();
        internal static Dictionary<string, Tile> TilePreviews = new Dictionary<string, Tile>();

        internal static List<Tools.Tool> Tools = new List<Tools.Tool>();

        public static Logics Instance;
        public static Stopwatch WireUpdateWatch = new Stopwatch();

        private static byte[] MagicHeader = Encoding.ASCII.GetBytes("TL2H");

        public Logics(string name) : base(name)
        {
            Instance = this;
        }

        static Logics()
        {
            foreach (Type t in typeof(Logics).Assembly.GetTypes())
            {
                if (t.IsAbstract) continue;
                if (t.BaseType == typeof(Tile) || t.BaseType.BaseType == typeof(Tile))
                {
                    Tile tile = Activator.CreateInstance(t) as Tile;
                    TileMap.Add(tile.Id, tile);

                    if (tile.ShowPreview)
                    {
                        if (tile.PreviewDataVariants is null) TilePreviews.Add(tile.Id, tile.CreateTile(null, true));
                        else foreach (string previewId in tile.PreviewDataVariants) TilePreviews.Add(tile.Id + ":" + previewId, tile.CreateTile(previewId, true));
                    }
                }
                else if (t.BaseType == typeof(Tools.Tool))
                {
                    Tools.Add(Activator.CreateInstance(t) as Tools.Tool);
                }
            }
        }

        public static World World { get; } = new("main", null, null, int.MaxValue, int.MaxValue, new(0, 0, int.MaxValue, int.MaxValue));
        public static World HoverWorld { get; set; }
        public static World ActiveWorld { get; private set; }

        public static World? PastePreview { get; set; }

        internal static string SelectedTileId = null;
        internal static Tile SelectedTilePreview = null;
        internal static int SelectedToolId = -1;
        internal static int SelectedWire = 0;
        

        internal static bool WireDebugActive = false;

        internal static List<Color> WireColorMapping = new List<Color>()
        {
            Color.Red,
            Color.Green,
            Color.Blue,
            Color.Yellow
        };

        internal static Texture2D Wire, WireCross, WireTL, WireTR;

        private float WheelZoom = 0;

        public static Point TileSize = new Point(16, 16);

        public static Rectangle ViewBounds;

        public static bool UpdatePaused, UpdateTick;
        public static int GridDrawType = 1;

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

        public static void LoadFile(string filename)
        {
            if (!File.Exists(filename)) return;

            Stopwatch watch = Stopwatch.StartNew();

            using (FileStream fs = File.OpenRead(filename))
            using (BinaryReader sr = new(fs))
            {
                if (!sr.ReadBytes(MagicHeader.Length).SequenceEqual(MagicHeader))
                {
                    sr.Close();
                    World.LoadFromOldFile(filename);
                    return;
                }
                byte format = sr.ReadByte();
                if (format == 0) LoadFormat0(sr);
                else return;
            }

            watch.Stop();
            Debug.WriteLine($"Loaded file in {watch.Elapsed.TotalMilliseconds:0.00}ms");
        }
        private static void LoadFormat0(BinaryReader sr)
        {
            WireColorMapping.Clear();

            byte colorCount = sr.ReadByte();
            for (int i = 0; i < colorCount; i++)
                WireColorMapping.Add(new Color() { PackedValue = sr.ReadUInt32() });

            World.Load(sr);
        }

        public static void SaveFile(string filename)
        {
            Stopwatch watch = Stopwatch.StartNew();

            using (FileStream fs = File.Create(filename))
            using (BinaryWriter sr = new(fs))
            {
                sr.Write(MagicHeader);
                sr.Write((byte)0);

                sr.Write((byte)WireColorMapping.Count);
                foreach (Color c in WireColorMapping)
                    sr.Write(c.PackedValue);

                World.Save(sr);
            }

            watch.Stop();
            Debug.WriteLine($"Saved file in {watch.Elapsed.TotalMilliseconds:0.00}ms");
        }

        public override void Draw()
        {
            TerraLogic.SpriteBatch.End();
            DrawGrid();

            World.Draw(BlendState.AlphaBlend);

            for (int i = 0; i < Tools.Count; i++)
                Tools[i].Draw(SelectedToolId == i);

            TerraLogic.SpriteBatch.Begin();
        }

        public override void Update()
        {
            World.Update();
            PastePreview?.Update();

            WireUpdateWatch.Reset();
            base.Update();
            if (SelectedToolId > -1) Tools[SelectedToolId].Update();

            bool currentPausedUpdateTick = UpdatePaused && UpdateTick;
            if (currentPausedUpdateTick)
                UpdateTick = false;

            if (GridDrawType == 2)
                BackgroundGridRenderer.Update();

            ViewBounds.X = (int)(PanNZoom.Position.X / TileSize.X);
            ViewBounds.Y = (int)(PanNZoom.Position.Y / TileSize.Y);
            ViewBounds.Width = (int)(TerraLogic.Instance.Window.ClientBounds.Width / (PanNZoom.Zoom * TileSize.X)) + 1;
            ViewBounds.Height = (int)(TerraLogic.Instance.Window.ClientBounds.Height / (PanNZoom.Zoom * TileSize.Y)) + 1;
        }

        protected internal override void MouseKeyStateUpdate(MouseKeys key, EventType @event, Point pos)
        {
            if (SelectedToolId > -1) Tools[SelectedToolId].MouseKeyUpdate(key, @event, pos);

            if (@event == EventType.Presssed)
                ActiveWorld = HoverWorld;
            else if (@event == EventType.Released)
                ActiveWorld = null;

            Point worldpos = ((ActiveWorld ?? HoverWorld).ScreenToWorld(pos) / TileSize.ToVector2()).ToPoint();

            if (key == MouseKeys.Left)
            {
                if (@event == EventType.Hold)
                {
                    if (SelectedTileId is not null)
                    {
                        string data = SelectedTilePreview.GetData();
                        ActiveWorld.SetTile(worldpos, SelectedTilePreview.Id + (data is null ? "" : ":" + data), SelectedTilePreview);
                    }
                    else if (SelectedWire > 0 && SelectedToolId == -1)
                        ActiveWorld.SetWires(worldpos.X, worldpos.Y, SelectedWire, true);
                }
                else if (@event == EventType.Presssed)
                {
                    Rectangle selection = global::TerraLogic.Tools.Select.Instance.Selection;
                    if (selection.Contains(worldpos))
                    {
                        if (SelectedTileId is not null)
                        {
                            string data = SelectedTilePreview.GetData();
                            data = SelectedTilePreview.Id + (data is null ? "" : ":" + data);

                            for (int y = selection.Y; y < selection.Bottom; y++)
                                for (int x = selection.X; x < selection.Right; x++)
                                    ActiveWorld.SetTile(x, y, data);
                        }
                        else if (SelectedWire > 0)
                        {
                            for (int y = selection.Y; y < selection.Bottom; y++)
                                for (int x = selection.X; x < selection.Right; x++)
                                    ActiveWorld.SetWires(x, y, SelectedWire, true);
                        }
                    }

                    if (PastePreview is not null)
                    {
                        ActiveWorld.Paste(PastePreview, worldpos);
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
                        ActiveWorld.RightClick(worldpos.X, worldpos.Y, @event == EventType.Hold);
                    }
                }
                if (@event == EventType.Hold)
                {

                    if (SelectedWire > 0 && SelectedToolId == -1)
                        ActiveWorld.SetWires(worldpos.X, worldpos.Y, SelectedWire, false);

                }
            }
            if (key == MouseKeys.Middle)
            {
                if (Root.CurrentKeys.IsKeyDown(Keys.LeftShift) && Root.CurrentKeys.IsKeyDown(Keys.LeftControl))
                {
                    PanNZoom.Position = Vector2.Zero;
                    PanNZoom.ScreenPosition = Vector2.Zero;
                }
                else PanNZoom.UpdateDragging(@event == EventType.Hold, pos);
            }
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
                        case Keys.F8:
                            if (GridDrawType == 2)
                                BackgroundGridRenderer.Reset();

                            GridDrawType++;
                            if (GridDrawType > 3)
                                GridDrawType = 0;

                            break;
                    }
                    if (key == Keys.V && Root.CurrentKeys.IsKeyDown(Keys.LeftControl))
                    {
                        PastePreview = ClipboardUtils.World;
                        if (PastePreview is not null)
                        {
                            PastePreview.BackgroundColor = Color.CornflowerBlue * 0.2f;
                            PastePreview.BackgroundOOBColor = Color.Transparent;
                        }
                    }
                }

                if (SelectedToolId > -1) Tools[SelectedToolId].KeyUpdate(key, @event, Root.CurrentKeys);
            }
        }
        protected internal override void MouseWheelStateUpdate(int change, Point pos)
        {
            if (Root.CurrentKeys.IsKeyDown(Keys.LeftShift) && Root.CurrentKeys.IsKeyDown(Keys.LeftControl))
            {
                WheelZoom = 1f;
            }
            else
            {
                float v = change / (HoverWorld.WorldZoom / PanNZoom.Zoom);
                WheelZoom -= v;
            }
            float zoom = WheelZoom < 0 ? -1 / (0.2f * WheelZoom - 1) : 0.2f * WheelZoom + 1;
            PanNZoom.SetZoom(zoom, pos);
        }

        private static void DrawGrid()
        {
            if (GridDrawType == 2)
            {
                BackgroundGridRenderer.Draw();
                return;
            }
            else if (GridDrawType == 0) return;

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


            Color c = new Color(64, 64, 64);
            if (GridDrawType == 3)
                c *= 0.2f;

            TerraLogic.SpriteBatch.Begin(SpriteSortMode.Deferred, null, PanNZoom.Zoom > 1 ? SamplerState.PointWrap : SamplerState.LinearWrap, null, null);
            TerraLogic.SpriteBatch.Draw(TerraLogic.GridTex, v2, rect, c, 0f, Vector2.Zero, PanNZoom.Zoom * zmul, SpriteEffects.None, 1f);
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
    public class WireDebug
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
    //class PastePreview
    //{
    //    internal string WireData;
    //    internal Point Size;
    //    internal Tile[,] Tiles;
    //}

    enum Side { Up, Right, Down, Left }


}
