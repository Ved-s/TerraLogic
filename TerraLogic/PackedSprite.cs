using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TerraLogic
{
    public static class PackedSprite
    {
        static public Point[] CalculatedPositions = new Point[256];

        static HashSet<byte> UniqueTiles = new HashSet<byte>();
        static List<byte> UniqueTileArray = new List<byte>();

        const int SpriteWidth = 8;

        static PackedSprite() 
        {
            for (int b = 0; b < 256; b++)
            {
                byte tile = CalcTile((byte)b);
                if (!UniqueTiles.Contains(tile))
                {
                    UniqueTiles.Add(tile);
                    UniqueTileArray.Add(tile);
                }
            }

            for (int b = 0; b < 256; b++) 
            {
                int index = UniqueTileArray.IndexOf(CalcTile((byte)b));
                CalculatedPositions[b].X = index % SpriteWidth;
                CalculatedPositions[b].Y = index / SpriteWidth;
            }
        }

        static byte CalcTile(byte tile)
        {
            bool n1 = (tile & 0b00000111) == 0b00000111;
            bool n2 = (tile & 0b00011100) == 0b00011100;
            bool n3 = (tile & 0b01110000) == 0b01110000;
            bool n4 = (tile & 0b11000001) == 0b11000001;

            tile = (byte)((tile & 0b11111101) | (n1 ? 0b00000010 : 0));
            tile = (byte)((tile & 0b11110111) | (n2 ? 0b00001000 : 0));
            tile = (byte)((tile & 0b11011111) | (n3 ? 0b00100000 : 0));
            tile = (byte)((tile & 0b01111111) | (n4 ? 0b10000000 : 0));

            return tile;
        }
    }
}
