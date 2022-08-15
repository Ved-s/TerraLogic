using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TerraLogic.Gui;

namespace TerraLogic
{
    public static class BackgroundGridRenderer
    {
        public static Texture2D TileGrid;

        private static ChunkArray2D<byte> TileData = new(16, 0xFF);

        private static int[] GridTextureCount = new int[] { 9,3,3,1,1 };

        private static bool PreferNotFirst = true;

        private static int ZoomMult;

        public static void LoadContent(ContentManager content) 
        {
            TileGrid = content.Load<Texture2D>("GridTiles");
        }

        private static Rectangle WorldRect;
        private static Vector2 ScreenStart, TileSize;

        public static void Reset() 
        {
            TileData.Clear();
        }

        public static void Update()
        {
            CalcucatePosition();

            List<Point> genPos = new();

            for (int y = WorldRect.Y; y < WorldRect.Bottom; y++)
            {
                for (int x = WorldRect.X; x < WorldRect.Right; x++)
                {
                    if (!HasTile(x, y))
                    {
                        genPos.Add(new Point(x, y));
                    }
                }
            }

            int generated = 0;

            while (genPos.Count > 0)
            {
                int i = Random.Shared.Next(genPos.Count);
                Point point = genPos[i];
                if (!HasTile(point.X, point.Y))
                {
                    GenerateTile(point.X, point.Y);
                    generated++;
                }
                genPos.RemoveAt(i);
            }
        }

        private static void CalcucatePosition()
        {
            Rectangle rect = Logics.Instance.Bounds;
            ScreenStart = Vector2.Zero;

            ZoomMult = 1;
            int zx = (int)(1 / PanNZoom.Zoom);

            while ((ZoomMult << 1) < zx) ZoomMult <<= 1;

            float grid = 16 * PanNZoom.Zoom * ZoomMult;

            ScreenStart.X = (PanNZoom.ScreenPosition.X - grid) % grid;
            ScreenStart.Y = (PanNZoom.ScreenPosition.Y - grid) % grid;

            rect.Width = (int)(rect.Width / (PanNZoom.Zoom * ZoomMult) + grid * 2);
            rect.Height = (int)(rect.Height / (PanNZoom.Zoom * ZoomMult) + grid * 2);

            WorldRect.Width = (int)((rect.Width - ScreenStart.X) / 16);
            WorldRect.Height = (int)((rect.Height - ScreenStart.Y) / 16);

            Vector2 tWorldPos = PanNZoom.Position / (16 * ZoomMult);

            WorldRect.X = (int)tWorldPos.X;
            WorldRect.Y = (int)tWorldPos.Y;

            TileSize = new(grid);
        }

        public static void Draw()
        {
            CalcucatePosition();
            Vector2 tilePos = ScreenStart;

            TerraLogic.SpriteBatch.Begin(SpriteSortMode.Deferred, null, PanNZoom.Zoom > 1 ? SamplerState.PointWrap : SamplerState.LinearWrap, null, null);

            for (int y = WorldRect.Y; y < WorldRect.Bottom; y++)
            {
                tilePos.X = ScreenStart.X;
                for (int x = WorldRect.X; x < WorldRect.Right; x++)
                {
                    byte v = TileData[x, y];

                    if (v != 0xFF)
                    { 
                        Point texPos = new((v & 0xF0) >> 4, v & 0xF);

                        Rectangle source = new Rectangle(texPos.X * 16, texPos.Y * 16, 16, 16);
                        TerraLogic.SpriteBatch.Draw(TileGrid, tilePos, source, Color.White, 0f, Vector2.Zero, PanNZoom.Zoom * ZoomMult, SpriteEffects.None, 1f);
                    }
                    tilePos.X += TileSize.X;
                }
                tilePos.Y += TileSize.Y;
            }

            TerraLogic.SpriteBatch.End();
        }

        private static void GenerateTile(int x, int y)
        {
            int maxTileSize = GridTextureCount.Length - 1;

            bool generating = true;

            int maxSize = maxTileSize;
            int size = 0;

            while (generating)
            {
                maxSize = maxTileSize;
                bool @break = false;

                for (int i = 0; i <= maxTileSize && !@break; i++)
                    for (int j = 0; j <= maxTileSize && !@break; j++)
                    {
                        if (i == 0 && j == 0) continue;

                        if (HasTile(x + i, y + j))
                        {
                            maxSize = Math.Max(Math.Min(i - 1, j - 1), 0);
                            @break = true;
                        }
                    }

                size = Random.Shared.Next(maxSize + 1);
                if (size > 0 || !PreferNotFirst || maxSize == 0 || Random.Shared.Next(10) == 0) 
                {
                    generating = false;
                }
            }
            Point texpos = GetTextureOffset(size);

            for (int i = 0; i <= size; i++)
                for (int j = 0; j <= size; j++)
                {
                    byte v = (byte)((((texpos.X + i) & 0xF) << 4) | ((texpos.Y + j) & 0xf));
                    if (HasTile(i + x, j + y)) Debugger.Break();

                    TileData[i + x, j + y] = v;
                }
        }

        private static bool HasTile(int x, int y) => TileData[x, y] != 0xFF;

        private static Point GetTextureOffset(int size) 
        {
            if (GridTextureCount.Length <= size) return Point.Zero;

            Point p = Point.Zero;
            for (int i = 1; i <= size; i++) 
                p.X += i;

            int max = GridTextureCount[size];

            if (size == 0 && Random.Shared.Next(10000) == 0)
            {
                p.X = 15;
                max = 1;
            }
            p.Y = (Random.Shared.Next() % max) * (size + 1);

            return p;
        }

        //private static int GenerateNumber(int x, int y, int seed)
        //    => Math.Abs(HashCode.Combine(x, y, seed, ZoomMult)) % 2;

        private record struct GridTile(int Size, int Variant);
    }
}
