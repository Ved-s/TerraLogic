using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace TerraLogic
{
    public class ChunkArray2D
    {
        static Regex ChunkRegex = new Regex("(\\d+),(\\d+):([^;]*);");
        static Regex HeaderRegex = new Regex("^(\\d+);");

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
            byte[] chunk = new byte[ChunkSize * ChunkSize * 4];
            byte[] compressedChunk;

            builder.Append(ChunkSize);
            builder.Append(';');

            for (int y = 0; y < ChunkMap.GetLength(1); y++)
                for (int x = 0; x < ChunkMap.GetLength(0); x++)
                    if (!ChunkMap[x, y].IsAllZerosOrNull())
                    {
                        Buffer.BlockCopy(ChunkMap[x, y], 0, chunk, 0, chunk.Length);

                        MemoryStream stream = new MemoryStream();
                        DeflateStream deflate = new DeflateStream(stream, CompressionMode.Compress, true);
                        deflate.Write(chunk, 0, chunk.Length);
                        deflate.Close();
                        compressedChunk = stream.ToArray();
                        stream.Close();
                        
                        builder.Append(x);
                        builder.Append(',');
                        builder.Append(y);
                        builder.Append(':');
                        builder.Append(Convert.ToBase64String(compressedChunk));
                        builder.Append(';');
                    }
            return builder.ToString();
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
                MemoryStream compressedChunk = new MemoryStream(Convert.FromBase64String(kvp.Value));
                MemoryStream chunk = new MemoryStream(ChunkSize * ChunkSize * 4);
                DeflateStream deflate = new DeflateStream(compressedChunk, CompressionMode.Decompress);
                deflate.CopyTo(chunk);
                deflate.Close();
                compressedChunk.Close();

                ChunkMap[kvp.Key.X, kvp.Key.Y] = new int[ChunkSize, ChunkSize];

                Buffer.BlockCopy(chunk.ToArray(), 0, ChunkMap[kvp.Key.X, kvp.Key.Y], 0, ChunkSize * ChunkSize * 4);
            }

            return true;

        }
    }

    public class ChunkArray2D<T>
    {
        T[,][,] ChunkMap;
        int ChunkSize;

        public int Width { get => ChunkMap.GetLength(0) * ChunkSize; }
        public int Height { get => ChunkMap.GetLength(1) * ChunkSize; }

        public ChunkArray2D(int chunkSize)
        {
            ChunkSize = chunkSize;
            ChunkMap = new T[0, 0][,];
        }

        public T QuickUnsafeGet(int x, int y)
        {
            T[,] ch = ChunkMap[x / ChunkSize, y / ChunkSize];
            if (ch is null) return default;
            else return ch[x % ChunkSize, y % ChunkSize];
        }

        public T this[int x, int y]
        {
            get
            {
                if (x < 0 || y < 0) return default;
                int chunkX = x / ChunkSize;
                int chunkY = y / ChunkSize;

                if (chunkY >= ChunkMap.GetLength(1)
                    || chunkX >= ChunkMap.GetLength(0)
                    || ChunkMap[chunkX, chunkY] is null) return default;

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
                    ChunkMap[chunkX, chunkY] = new T[ChunkSize, ChunkSize];

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
    }
}
