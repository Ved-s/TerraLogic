using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;

namespace TerraLogic
{
    static class DeflateUtils
    {
        public static byte[] Compress(Array data, int lengthMultiplier) 
        {
            byte[] rawData = new byte[data.Length * lengthMultiplier];
            Buffer.BlockCopy(data, 0, rawData, 0, rawData.Length);

            using (MemoryStream stream = new MemoryStream())
            {
                DeflateStream deflate = new DeflateStream(stream, CompressionMode.Compress, true);
                deflate.Write(rawData, 0, rawData.Length);
                deflate.Close();
                return stream.ToArray();
            }
        }

        public static void Decompress(byte[] data, Array outArray, int lengthMultiplier) 
        {
            MemoryStream compressedChunk = new MemoryStream(data);
            MemoryStream chunk = new MemoryStream(outArray.Length * lengthMultiplier);
            DeflateStream deflate = new DeflateStream(compressedChunk, CompressionMode.Decompress);
            deflate.CopyTo(chunk);
            deflate.Close();
            compressedChunk.Close();
            Buffer.BlockCopy(chunk.ToArray(), 0, outArray, 0, outArray.Length * lengthMultiplier);
        }
    }
}
