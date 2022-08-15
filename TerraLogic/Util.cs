using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace TerraLogic
{
    static class Util
    {
        static NumberFormatInfo dotnfi = new NumberFormatInfo() { NumberDecimalSeparator = "." };

        readonly static string[] sizes = new string[] { "b", "kb", "mb", "gb" };
        public static string MakeSize(ulong s)
        {
            for (int i = 0; i < sizes.Length; i++)
            {
                if (s < Math.Pow(1024, i + 1))
                    return $"{(s / Math.Pow(1024, i)).ToString((i == 0) ? "" : "f2", dotnfi)} {sizes[i]}";
            }
            return $"{(s / Math.Pow(1024, sizes.Length - 1)).ToString("f2", dotnfi)} {sizes[sizes.Length - 1]}";
        }


        // https://www.reddit.com/70j259
        public static char KeyToChar(Keys Key, bool Shift = false)
        {
            /* It's the space key. */
            if (Key == Keys.Space)
            {
                return ' ';
            }
            else
            {
                string String = Key.ToString();

                /* It's a letter. */
                if (String.Length == 1)
                {
                    Char Character = Char.Parse(String);
                    byte Byte = Convert.ToByte(Character);

                    if (
                        (Byte >= 65 && Byte <= 90) ||
                        (Byte >= 97 && Byte <= 122)
                        )
                    {
                        return (!Shift ? Character.ToString().ToLower() : Character.ToString())[0];
                    }
                }

                /* 
                 * 
                 * The only issue is, if it's a symbol, how do I know which one to take if the user isn't using United States international?
                 * Anyways, thank you, for saving my time
                 * down here:
                 */

                #region Credits :  http://roy-t.nl/2010/02/11/code-snippet-converting-keyboard-input-to-text-in-xna.html for saving my time.
                switch (Key)
                {
                    case Keys.D0:
                        if (Shift) { return ')'; } else { return '0'; }
                    case Keys.D1:
                        if (Shift) { return '!'; } else { return '1'; }
                    case Keys.D2:
                        if (Shift) { return '@'; } else { return '2'; }
                    case Keys.D3:
                        if (Shift) { return '#'; } else { return '3'; }
                    case Keys.D4:
                        if (Shift) { return '$'; } else { return '4'; }
                    case Keys.D5:
                        if (Shift) { return '%'; } else { return '5'; }
                    case Keys.D6:
                        if (Shift) { return '^'; } else { return '6'; }
                    case Keys.D7:
                        if (Shift) { return '&'; } else { return '7'; }
                    case Keys.D8:
                        if (Shift) { return '*'; } else { return '8'; }
                    case Keys.D9:
                        if (Shift) { return '('; } else { return '9'; }

                    case Keys.NumPad0: return '0';
                    case Keys.NumPad1: return '1';
                    case Keys.NumPad2: return '2';
                    case Keys.NumPad3: return '3';
                    case Keys.NumPad4: return '4';
                    case Keys.NumPad5: return '5';
                    case Keys.NumPad6: return '6';
                    case Keys.NumPad7: return '7'; ;
                    case Keys.NumPad8: return '8';
                    case Keys.NumPad9: return '9';

                    case Keys.OemTilde:
                        if (Shift) { return '~'; } else { return '`'; }
                    case Keys.OemSemicolon:
                        if (Shift) { return ':'; } else { return ';'; }
                    case Keys.OemQuotes:
                        if (Shift) { return '"'; } else { return '\''; }
                    case Keys.OemQuestion:
                        if (Shift) { return '?'; } else { return '/'; }
                    case Keys.OemPlus:
                        if (Shift) { return '+'; } else { return '='; }
                    case Keys.OemPipe:
                        if (Shift) { return '|'; } else { return '\\'; }
                    case Keys.OemPeriod:
                        if (Shift) { return '>'; } else { return '.'; }
                    case Keys.OemOpenBrackets:
                        if (Shift) { return '{'; } else { return '['; }
                    case Keys.OemCloseBrackets:
                        if (Shift) { return '}'; } else { return ']'; }
                    case Keys.OemMinus:
                        if (Shift) { return '_'; } else { return '-'; }
                    case Keys.OemComma:
                        if (Shift) { return '<'; } else { return ','; }
                }
                #endregion

                return (Char)0;

            }

        }

        public static bool IsAllZerosOrNull(this int[,] array)
        {
            if (array is null) return true;
            for (int y = 0; y < array.GetLength(1); y++)
                for (int x = 0; x < array.GetLength(0); x++)
                    if (array[x, y] != 0) return false;
            return true;
        }

        public static int Constrain(int minIncl, int value, int maxIncl) 
        {
            if (value < minIncl) value = minIncl;
            else if (value > maxIncl) value = maxIncl;
            return value;
        }

        public static Rectangle RectFrom2Points(Point a, Point b) 
        {
            Point min = new Point(Math.Min(a.X, b.X), Math.Min(a.Y, b.Y));
            Point max = new Point(Math.Max(a.X, b.X), Math.Max(a.Y, b.Y));
            return new Rectangle(min.X, min.Y, max.X - min.X, max.Y - min.Y);
        }


        public static byte ZipBools(params bool[] bools) 
        {
            byte b = 0;
            foreach (bool bo in bools)
            {
                b = (byte)((b << 1) | (bo ? 1 : 0));
            }
            return b;
        }
        public static bool[] UnzipBools(byte b)
        {
            bool[] bools = new bool[8];
            for (int i = 0; i < 8; i++)
            {
                bools[i] = (b & 1) != 0;
                b = (byte)(b >> 1);
            }
            return bools;
        }
    }
}
