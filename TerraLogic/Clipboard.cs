using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace TerraLogic
{
    public static class ClipboardUtils
    {
        private static char[] MagicHeader = "TL2C".ToCharArray();

        public static string Text 
        {
            get
            {
                string text = null;
                Thread t = new Thread(() =>
                {
                    if (Clipboard.ContainsText()) 
                        text = Clipboard.GetText();
                });
                t.SetApartmentState(ApartmentState.STA);
                t.Start();
                t.Join();
                return text;
            }
            set
            {
                Thread t = new Thread(() =>
                {
                    Clipboard.SetText(value);
                });
                t.SetApartmentState(ApartmentState.STA);
                t.Start();
            }
        }

        public static World World 
        {
            get 
            {
                char[] text = Text.ToCharArray();
                if (text.SequenceStartsWith(MagicHeader)) 
                {
                    MemoryStream stream = new(Convert.FromBase64CharArray(text, 4, text.Length - 4));
                    World w = new World("pastedWorld", null, null, 0, 0, new());
                    using (BinaryReader reader = new(stream))
                    {
                        w.Visible = reader.ReadBoolean();
                        w.Padding = reader.ReadSingle();
                        w.WorldPos = new(reader.ReadInt32(), reader.ReadInt32(), reader.ReadInt32(), reader.ReadInt32());
                        w.Load(reader);
                    }
                    return w;
                }
                return null;
            }
            set 
            {
                MemoryStream stream = new();
                using (BinaryWriter writer = new(stream))
                {
                    writer.Write(value.Visible);
                    writer.Write(value.Padding);
                    writer.Write(value.WorldPos.X);
                    writer.Write(value.WorldPos.Y);
                    writer.Write(value.WorldPos.Width);
                    writer.Write(value.WorldPos.Height);
                    value.Save(writer);
                }

                Text = string.Join("", MagicHeader) + Convert.ToBase64String(stream.ToArray());
            }
        }
    }
}
