using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TerraLogic.GuiElements
{
    class UIList : UIElement
    {
        public List<object> Items = new List<object>();
        public int SelectedIndex = -1;

        public int Scroll = 0;

        public Color HoverColor = new Color(32, 32, 32);
        public Color SelectionColor = new Color(64,64,64);

        public delegate void ItemClickDelegate(UIElement caller, int index, object item, bool doubleClick);

        public ItemClickDelegate? ItemClick;

        private int ItemHeight;
        private int LastClickIndex;
        private DateTime LastClickTime;

        public UIList(string? name) : base(name) 
        {

        }

        public override void Update()
        {
            base.Update();
            if (Parent is null) return;

            ItemHeight = (int)Font.MeasureString("Abc").Y;
            int heightScroll = Bounds.Height / ItemHeight;
            int maxScroll = Math.Max(Items.Count - heightScroll, 0);
            if (Scroll > maxScroll) Scroll = maxScroll;
            if (Scroll < 0) Scroll = 0;

            if (SelectedIndex >= Items.Count) SelectedIndex = Items.Count - 1;
            if (SelectedIndex < -1) SelectedIndex = -1;
        }

        protected internal override void MouseKeyStateUpdate(MouseKeys key, EventType @event, Point pos)
        {
            if (key == MouseKeys.Left && @event == EventType.Presssed) 
            {
                SelectedIndex = (pos.Y / ItemHeight) + Scroll;
                if (SelectedIndex >= Items.Count) SelectedIndex = -1;

                if (SelectedIndex != -1) 
                {
                    if (LastClickIndex == SelectedIndex && (DateTime.Now - LastClickTime).TotalMilliseconds < 500)
                    {
                        ItemClick?.Invoke(this, SelectedIndex, Items[SelectedIndex], true);
                    }
                    else ItemClick?.Invoke(this, SelectedIndex, Items[SelectedIndex], false);
                }
                LastClickTime = DateTime.Now;
                LastClickIndex = SelectedIndex;
            }
        }

        protected internal override void KeyStateUpdate(Keys key, EventType @event)
        {
            if (@event == EventType.Presssed && Hover)
            {
                int heightScroll = Bounds.Height / ItemHeight;

                if (key == Keys.Enter && SelectedIndex > -1)
                    ItemClick?.Invoke(this, SelectedIndex, Items[SelectedIndex], true);

                if (key == Keys.Down) 
                {
                    SelectedIndex++; 
                    SelectedIndex %= Items.Count; 
                    ItemClick?.Invoke(this, SelectedIndex, Items[SelectedIndex], false);

                    
                    if (Scroll > SelectedIndex) Scroll = SelectedIndex;
                    if (Scroll + heightScroll < SelectedIndex) Scroll = SelectedIndex + heightScroll;
                    
                }

                if (key == Keys.Up) 
                { 
                    SelectedIndex--; 
                    if (SelectedIndex < 0) SelectedIndex = Items.Count - 1; 
                    ItemClick?.Invoke(this, SelectedIndex, Items[SelectedIndex], false);

                    if (Scroll > SelectedIndex) Scroll = SelectedIndex;
                    if (Scroll + heightScroll < SelectedIndex) Scroll = SelectedIndex + heightScroll;
                }
            }
        }

        protected internal override void MouseWheelStateUpdate(int change, Point pos)
        {
            Scroll += change;
            int heightScroll = Bounds.Height / ItemHeight;
            int maxScroll = Math.Max(Items.Count - heightScroll, 0);
            if (Scroll > maxScroll) Scroll = maxScroll;
            if (Scroll < 0) Scroll = 0;
        }

        public override void Draw()
        {
            if (Parent is null || PositionRecalculateRequired) return;

            int heightScroll = Bounds.Height / ItemHeight;

            DrawBackground();
            int ypos = 0;

            for (int i = Scroll; i < Math.Min(Items.Count, Scroll + heightScroll); i++)
            {
                Rectangle rect = new Rectangle(Bounds.X, Bounds.Y + ypos, Bounds.Width, ItemHeight);

                if (rect.Contains(Root!.MousePosition)) Graphics.FillRectangle(rect, HoverColor);
                if (i == SelectedIndex) Graphics.FillRectangle(rect, SelectionColor);
                try
                {
                    TerraLogic.SpriteBatch.DrawString(Font, Items[i].ToString(), new Vector2(Bounds.X, Bounds.Y + ypos), TextColor);
                }
                catch { TerraLogic.SpriteBatch.DrawString(Font, "--Render error--", new Vector2(Bounds.X, Bounds.Y + ypos), Color.Red); }
                ypos += ItemHeight;
            }
            
        }
    }
}
