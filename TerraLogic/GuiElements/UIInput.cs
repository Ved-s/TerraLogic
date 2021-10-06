using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TerraLogic.GuiElements
{
    class UIInput : UIElement
    {
        int CursorPos = 0;

        int HoldTime = 0;
        float HoldInterval = 60;

        public bool ReadOnly = false;

        public override string Text 
        { 
            get => TextBuilder.ToString() + PostText;
            set { TextBuilder.Clear(); TextBuilder.Append(value); OnTextChanged?.Invoke(this); } 
        }

        public Action<UIInput> OnTextChanged;
        public Action<UIInput> OnEnter;

        public StringBuilder TextBuilder = new StringBuilder();

        public string PostText = "";
        public Color PostTextColor = new Color(128,128,128);

        public UIInput(string name) : base(name) 
        {
            
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            DrawBackground(spriteBatch);

            if (CursorPos > TextBuilder.Length) CursorPos = TextBuilder.Length;
            if (CursorPos < 0) CursorPos = 0;

            string text = TextBuilder.ToString();
            int width = (int)Font.MeasureString(text).X;

            spriteBatch.DrawString(Font, text, Bounds.Location.ToVector2(), TextColor);
            spriteBatch.DrawString(Font, PostText, new Vector2(Bounds.X + width, Bounds.Y), PostTextColor);

            if (!ReadOnly)
            {
                int curPos = (int)Font.MeasureString(TextBuilder.ToString(0, CursorPos)).X;
                spriteBatch.Draw(TerraLogic.Pixel, new Rectangle(curPos + Bounds.X, Bounds.Y, 1, Bounds.Height), Color.White);
            }

            
        }

        protected internal override void MouseKeyStateUpdate(MouseKeys key, EventType @event, Point pos)
        {
            if (key == MouseKeys.Left && @event == EventType.Presssed) 
            {
                CursorPos = 0;
                int textWidth = 0;

                while (textWidth < pos.X && CursorPos < TextBuilder.Length) 
                {
                    CursorPos++;
                    textWidth = (int)Font.MeasureString(TextBuilder.ToString(0, CursorPos)).X;
                }
                if (CursorPos > 0 && pos.X < textWidth) CursorPos--;
            }
        }

        protected internal override void KeyStateUpdate(Keys key, EventType @event)
        {
            if (!Hover) return;

            bool hold = false;
            if (@event == EventType.Hold)
            {
                HoldTime++;

                if (HoldTime > HoldInterval) 
                {
                    hold = true; 
                    HoldTime = 0;
                    if (HoldInterval > 2) HoldInterval = HoldInterval * 0.75f;
                }
            }
            else { HoldTime = 0; HoldInterval = 30; }

            if (key == Keys.Enter && !ReadOnly && @event == EventType.Presssed) OnEnter?.Invoke(this);

            if (!ReadOnly && (@event == EventType.Presssed || (@event == EventType.Hold && hold))) 
            {
                switch (key)
                {
                    case Keys.Right:
                        if (CursorPos < TextBuilder.Length) CursorPos++; return;
                    case Keys.Left:
                        if (CursorPos > 0) CursorPos--; return;
                    case Keys.Back:
                        if (CursorPos > 0) 
                        {
                            CursorPos--;
                            TextBuilder.Remove(CursorPos, 1);
                            OnTextChanged?.Invoke(this);
                        }
                        return;
                    case Keys.Delete:
                        if (CursorPos > 0)
                        {
                            TextBuilder.Remove(CursorPos, 1);
                            CursorPos = Math.Min(CursorPos, TextBuilder.Length);
                            OnTextChanged?.Invoke(this);
                        }
                        return;
                }

                char c = Util.KeyToChar(key, Root.CurrentKeys.IsKeyDown(Keys.LeftShift));

                if (c != 0) 
                {
                    TextBuilder.Insert(CursorPos, c);
                    CursorPos++;
                    OnTextChanged?.Invoke(this);
                }

            }
        }
    }
}
