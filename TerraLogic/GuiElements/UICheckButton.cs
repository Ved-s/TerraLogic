using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TerraLogic.GuiElements
{
    internal class UICheckButton : UIButton
    {

        public UICheckButton(string name = null) : base(name) 
        {

        }

        public override Colors Colors { get => Checked ? base.Colors.Swapped() : base.Colors; set => base.Colors = value; }
        public override Colors HoverColors { get => Checked ? base.HoverColors.Swapped() : base.Colors; set => base.HoverColors = value; }

        public virtual bool Checked { get; set; }

        protected internal override void MouseKeyStateUpdate(MouseKeys key, EventType @event, Point pos)
        {
            if (key == MouseKeys.Left && @event == EventType.Presssed) Checked = !Checked;
            base.MouseKeyStateUpdate(key, @event, pos);
        }
    }
}
