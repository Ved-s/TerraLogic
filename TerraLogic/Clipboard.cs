using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace TerraLogic
{
    public static class ClipboardUtils
    {
        public static string Text 
        {
            get
            {
                string text = null;
                Thread t = new Thread(() =>
                {
                    if (Clipboard.ContainsText()) text = Clipboard.GetText();
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
    }
}
