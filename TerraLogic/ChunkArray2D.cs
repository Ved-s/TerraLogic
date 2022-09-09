using Microsoft.Xna.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace TerraLogic
{
    public class ChunkArray2D : IEnumerable<ChunkArray2D.ChunkItem>
    {
        internal static Regex ChunkRegex = new Regex("(\\d+),(\\d+):([^;]*);");
        internal static Regex HeaderRegex = new Regex("^(\\d+);");

        int[,][,] ChunkMap;
        int ChunkSize;

        public int Width  { get => ChunkMap.GetLength(0) * ChunkSize; }
        public int Height { get => ChunkMap.GetLength(1) * ChunkSize; }

        public ChunkArray2D(int chunkSize) 
        {
            ChunkSize = chunkSize;
            ChunkMap = new int[0,0][,];
        }

        public int? QuickUnsafeGet(int x, int y) => ChunkMap[x / ChunkSize, y / ChunkSize]?[x % ChunkSize, y % ChunkSize];

        public int this[Point p] 
        {
            get => this[p.X, p.Y];
            set => this[p.X, p.Y] = value;
        }

        public int this[int x, int y] 
        {
            get 
            {
                if (x < 0 || y < 0) return 0;
                int chunkX = x / ChunkSize;
                int chunkY = y / ChunkSize;

                if (   chunkY >= ChunkMap.GetLength(1) 
                    || chunkX >= ChunkMap.GetLength(0)
                    || ChunkMap[chunkX, chunkY] is null) return 0;
                
                return ChunkMap[chunkX, chunkY][x % ChunkSize, y % ChunkSize];
            }

            set
            {
                if (x < 0 || y < 0) return;
                int chunkX = x / ChunkSize;
                int chunkY = y / ChunkSize;

                if (chunkY >= ChunkMap.GetLength(1) || chunkX >= ChunkMap.GetLength(0)) 
                    ChunkMap = ResizeArray(ChunkMap, Math.Max(ChunkMap.GetLength(0), chunkX + 1), Math.Max(ChunkMap.GetLength(1), chunkY + 1));

                if (ChunkMap[chunkX, chunkY] is null) 
                    ChunkMap[chunkX, chunkY] = new int[ChunkSize, ChunkSize];

                ChunkMap[chunkX, chunkY][x % ChunkSize, y % ChunkSize] = value;
            }
        }

        static int[,][,] ResizeArray(int[,][,] original, int rows, int cols)
        {
            var newArray = new int[rows, cols][,];
            int minRows = Math.Min(rows, original.GetLength(0));
            int minCols = Math.Min(cols, original.GetLength(1));
            for (int i = 0; i < minRows; i++)
                for (int j = 0; j < minCols; j++)
                    newArray[i, j] = original[i, j];
            original = null;
            GC.Collect();
            return newArray;
        }

        public string ToDataString() 
        {
            StringBuilder builder = new StringBuilder();

            builder.Append(ChunkSize);
            builder.Append(';');

            for (int y = 0; y < ChunkMap.GetLength(1); y++)
                for (int x = 0; x < ChunkMap.GetLength(0); x++)
                    if (!ChunkMap[x, y].IsAllZerosOrNull())
                    {
                        builder.Append(x);
                        builder.Append(',');
                        builder.Append(y);
                        builder.Append(':');
                        builder.Append(Convert.ToBase64String(DeflateUtils.Compress(ChunkMap[x,y], 4)));
                        builder.Append(';');
                    }
            return builder.ToString();
        }

        public string ToPartialDataString(Rectangle rect)
        {
            int[] bigChunk = new int[rect.Width * rect.Height];
            for (int y = 0; y < rect.Height; y++)
                for (int x = 0; x < rect.Width; x++) 
                {
                    bigChunk[y * rect.Width + x] = this[x + rect.X, y + rect.Y];
                }
            return $"{rect.Width},{rect.Height}:{Convert.ToBase64String(DeflateUtils.Compress(bigChunk, 4))};";
        }
        public bool LoadPartialDataString(string data, Point pos, bool merge)
        {
            Match chunk = ChunkRegex.Match(data);
            Point size = new Point(int.Parse(chunk.Groups[1].Value), int.Parse(chunk.Groups[2].Value));

            int[] chunkData = new int[size.X * size.Y];
            DeflateUtils.Decompress(Convert.FromBase64String(chunk.Groups[3].Value), chunkData, 4);

            for (int y = 0; y < size.Y; y++)
                for (int x = 0; x < size.X; x++)
                {
                    if (merge) this[pos.X + x, pos.Y + y] |= chunkData[y * size.X + x];
                    else this[pos.X + x, pos.Y + y] = chunkData[y * size.X + x];
                }

            return true;

        }

        public bool LoadDataString(string data) 
        {
            Dictionary<Point, string> chunks = new Dictionary<Point, string>();

            int newChunkSize = 0;
            Match header = HeaderRegex.Match(data);
            if (!header.Success) return false;
            newChunkSize = int.Parse(header.Groups[1].Value);

            int maxX = -1, maxY = -1;

            MatchCollection chunkGroups = ChunkRegex.Matches(data);

            foreach (Match chunk in chunkGroups) 
            {
                Point pos = new Point(int.Parse(chunk.Groups[1].Value), int.Parse(chunk.Groups[2].Value));
                chunks.Add(pos, chunk.Groups[3].Value);

                maxX = Math.Max(maxX, pos.X);
                maxY = Math.Max(maxY, pos.Y);
            }

            ChunkMap = new int[maxX+1, maxY+1][,];
            ChunkSize = newChunkSize;

            foreach (KeyValuePair<Point, string> kvp in chunks) 
            {
                ChunkMap[kvp.Key.X, kvp.Key.Y] = new int[ChunkSize, ChunkSize];
                DeflateUtils.Decompress(Convert.FromBase64String(kvp.Value), ChunkMap[kvp.Key.X, kvp.Key.Y], 4);
            }

            return true;

        }

        public void Clear() { ChunkMap = new int[0, 0][,]; }

        public IEnumerator<ChunkItem> GetEnumerator()
        {
            for (int y = 0; y < ChunkMap.GetLength(1); y++)
                for (int x = 0; x < ChunkMap.GetLength(0); x++)
                    if (ChunkMap[x, y] is not null)
                        for (int cy = 0; cy < ChunkSize; cy++)
                            for (int cx = 0; cx < ChunkSize; cx++)
                                yield return new ChunkItem()
                                {
                                    Item = ChunkMap[x, y][cx, cy],
                                    X = x * ChunkSize + cx,
                                    Y = y * ChunkSize + cy
                                };
            yield break;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            for (int y = 0; y < ChunkMap.GetLength(1); y++)
                for (int x = 0; x < ChunkMap.GetLength(0); x++)
                    if (ChunkMap[x, y] is not null)
                        for (int cy = 0; y < ChunkSize; cy++)
                            for (int cx = 0; x < ChunkSize; cx++)
                                yield return new ChunkItem()
                                {
                                    Item = ChunkMap[x, y][cx, cy],
                                    X = x * ChunkSize + cx,
                                    Y = y * ChunkSize + cy
                                };
            yield break;
        }

        public struct ChunkItem 
        {
            public int Item;
            public int X, Y;
        }
    }

    public class ChunkArray2D<T> : IEnumerable<ChunkArray2D<T>.ChunkItem>
    {
        T[,][,] ChunkMap;
        int ChunkSize;

        private T DefaultValue;

        public int Width { get => ChunkMap.GetLength(0) * ChunkSize; }
        public int Height { get => ChunkMap.GetLength(1) * ChunkSize; }

        public ChunkArray2D(int chunkSize, T @default = default)
        {
            ChunkSize = chunkSize;
            DefaultValue = @default;
            ChunkMap = new T[0, 0][,];
        }

        public T QuickUnsafeGet(int x, int y)
        {
            T[,] ch = ChunkMap[x / ChunkSize, y / ChunkSize];
            if (ch is null) return DefaultValue;
            else return ch[x % ChunkSize, y % ChunkSize];
        }

        public void QuickUnsafeSet(int x, int y, T value)
        {
            T[,] ch = ChunkMap[x / ChunkSize, y / ChunkSize];
            if (ch is null) return;
            ch[x % ChunkSize, y % ChunkSize] = value;
        }

        public T this[Point p]
        {
            get => this[p.X, p.Y];
            set => this[p.X, p.Y] = value;
        }

        public T this[int x, int y]
        {
            get
            {
                if (x < 0 || y < 0) return DefaultValue;
                int chunkX = x / ChunkSize;
                int chunkY = y / ChunkSize;

                if (chunkY >= ChunkMap.GetLength(1)
                    || chunkX >= ChunkMap.GetLength(0)
                    || ChunkMap[chunkX, chunkY] is null) return DefaultValue;

                return ChunkMap[chunkX, chunkY][x % ChunkSize, y % ChunkSize];
            }

            set
            {
                if (x < 0 || y < 0) return;
                int chunkX = x / ChunkSize;
                int chunkY = y / ChunkSize;

                if (chunkY >= ChunkMap.GetLength(1) || chunkX >= ChunkMap.GetLength(0))
                    ChunkMap = ResizeArray(ChunkMap, Math.Max(ChunkMap.GetLength(0), chunkX + 1), Math.Max(ChunkMap.GetLength(1), chunkY + 1));

                if (ChunkMap[chunkX, chunkY] is null)
                {
                    T[,] chunk = new T[ChunkSize, ChunkSize];
                    ChunkMap[chunkX, chunkY] = chunk;

                    for (int i = 0; i < ChunkSize; i++)
                        for (int j = 0; j < ChunkSize; j++)
                            chunk[i, j] = DefaultValue;
                }

                ChunkMap[chunkX, chunkY][x % ChunkSize, y % ChunkSize] = value;
            }
        }

        static T[,][,] ResizeArray(T[,][,] original, int rows, int cols)
        {
            var newArray = new T[rows, cols][,];
            int minRows = Math.Min(rows, original.GetLength(0));
            int minCols = Math.Min(cols, original.GetLength(1));
            for (int i = 0; i < minRows; i++)
                for (int j = 0; j < minCols; j++)
                    newArray[i, j] = original[i, j];
            original = null;
            GC.Collect();
            return newArray;
        }

        internal void Clear()
        {
            ChunkMap = new T[0, 0][,];
        }

        public IEnumerator<ChunkItem> GetEnumerator()
        {
            for (int y = 0; y < ChunkMap.GetLength(1); y++)
                for (int x = 0; x < ChunkMap.GetLength(0); x++)
                    if (ChunkMap[x, y] is not null)
                        for (int cy = 0; cy < ChunkSize; cy++)
                            for (int cx = 0; cx < ChunkSize; cx++)
                                if (ChunkMap[x, y][cx, cy] is not null)
                                    yield return new ChunkItem()
                                    {
                                        Item = ChunkMap[x, y][cx, cy],
                                        X = x * ChunkSize + cx,
                                        Y = y * ChunkSize + cy
                                    };
            yield break;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            for (int y = 0; y < ChunkMap.GetLength(1); y++)
                for (int x = 0; x < ChunkMap.GetLength(0); x++)
                    if (ChunkMap[x, y] is not null)
                        for (int cy = 0; cy < ChunkSize; cy++)
                            for (int cx = 0; cx < ChunkSize; cx++)
                                if (ChunkMap[x, y][cx, cy] is not null)
                                    yield return new ChunkItem()
                                    {
                                        Item = ChunkMap[x, y][cx, cy],
                                        X = x * ChunkSize + cx,
                                        Y = y * ChunkSize + cy
                                    };
            yield break;
        }

        public struct ChunkItem
        {
            public T Item;
            public int X, Y;
        }
    }
}
