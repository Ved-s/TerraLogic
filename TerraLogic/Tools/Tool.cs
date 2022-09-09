using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System.Diagnostics;
using TerraLogic.GuiElements;

namespace TerraLogic.Tools
{
    public abstract class Tool
    {
        public abstract string Id { get; }
        public abstract string DisplayName { get; }

        public virtual bool DrawMouseIcon => true;

        public virtual bool AllowWireSelection => false;

        public bool IsSelected
        {
            get => isSelected;
            internal set
            {
                isSelected = value;
                if (value) Selected();
                else Deselected();
            }
        }

        public Texture2D Texture;
        private bool isSelected;

        public virtual bool ShowWires => false;

        public abstract void MouseKeyUpdate(MouseKeys key, EventType @event, Point pos);
        public virtual void KeyUpdate(Keys key, EventType @event, KeyboardState state) { }

        public virtual void Selected() { }
        public virtual void Deselected() { }

        public virtual void WireColorChanged() { }

        public virtual void Update() { }
        public virtual void Draw(bool selected) { }
    }
}
