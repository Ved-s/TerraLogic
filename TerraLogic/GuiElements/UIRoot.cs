﻿using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TerraLogic.GuiElements
{
    public class UIRoot : UIElement
    {
        public KeyboardState CurrentKeys;
        private List<Keys> OldKeys = new List<Keys>();

        public MouseState CurrentMouseKeys;
        private List<MouseKeys> OldMouseKeys = new List<MouseKeys>();

        public int MouseWheel = 0;
        private int OldMouseWheel = 0;

        public new Point MousePosition;

        public new UIElement? Hover { get; private set; }
        public new UIElement? Active { get; private set; }

        public Action<UIElement?, UIElement?>? OnGlobalActiveChanged;

        internal bool Init = false;

        public UIRoot() : base("root")
        {
            Root = this;
        }

        protected internal override void Initialize()
        {
            Init = true;
            base.Initialize();
        }

        public override void Update()
        {
            if (!Init) Initialize();
            CurrentMouseKeys = Mouse.GetState();
            MousePosition = new Point(CurrentMouseKeys.X, CurrentMouseKeys.Y);

            if (Bounds.Width != TerraLogic.Instance.Window.ClientBounds.Width)   Width  = Bounds.Width = TerraLogic.Instance.Window.ClientBounds.Width;
            if (Bounds.Height != TerraLogic.Instance.Window.ClientBounds.Height) Height = Bounds.Height = TerraLogic.Instance.Window.ClientBounds.Height;

            base.Update();

            CurrentKeys = Keyboard.GetState();
            HashSet<Keys> changedKeys = new HashSet<Keys>(CurrentKeys.GetPressedKeys());
            changedKeys.UnionWith(OldKeys);
            foreach (Keys key in changedKeys)
            {
                int keyNow = (int)CurrentKeys[key];
                int keyBefore = OldKeys.Contains(key)? 1 : 0;
                EventType n = (EventType)(keyBefore << 1 | keyNow);
                if (TerraLogic.Instance.IsActive) this.KeyStateUpdate(key, n);
                if (n == EventType.Released) OldKeys.Remove(key);
                else if (n == EventType.Presssed) OldKeys.Add(key);
            }

            bool anyMouseKey = false;
            
            HashSet<MouseKeys> changedMouseKeys = new HashSet<MouseKeys>(OldMouseKeys);
            List<MouseKeys> mouseKeys = new List<MouseKeys>();
            if (CurrentMouseKeys.LeftButton == ButtonState.Pressed)   { mouseKeys.Add(MouseKeys.Left);     anyMouseKey = true;  }
            if (CurrentMouseKeys.RightButton == ButtonState.Pressed)  { mouseKeys.Add(MouseKeys.Right);    anyMouseKey = true;  }
            if (CurrentMouseKeys.MiddleButton == ButtonState.Pressed) { mouseKeys.Add(MouseKeys.Middle);   anyMouseKey = true;  }
            if (CurrentMouseKeys.XButton1 == ButtonState.Pressed)     { mouseKeys.Add(MouseKeys.XButton1); anyMouseKey = true;  }
            if (CurrentMouseKeys.XButton2 == ButtonState.Pressed)     { mouseKeys.Add(MouseKeys.XButton2); anyMouseKey = true;  }
            changedMouseKeys.UnionWith(mouseKeys);
            foreach (MouseKeys key in changedMouseKeys)
            {
                int keyNow = mouseKeys.Contains(key) ? 1 : 0;
                int keyBefore = OldMouseKeys.Contains(key) ? 1 : 0;
                EventType n = (EventType)(keyBefore << 1 | keyNow);
                if (TerraLogic.Instance.IsActive)
                {
                    Hover?.MouseKeyStateUpdate(key, n, MousePosition.Subtract(Hover.Bounds.Location));
                    if (key == MouseKeys.Left && n == EventType.Presssed)
                        SetActive(Hover);
                }
                if (n == EventType.Released) OldMouseKeys.Remove(key);
                else if (n == EventType.Presssed) OldMouseKeys.Add(key);
            }

            MouseWheel = CurrentMouseKeys.ScrollWheelValue / 120;
            if (OldMouseWheel != MouseWheel)
                if (TerraLogic.Instance.IsActive) Hover?.MouseWheelStateUpdate(OldMouseWheel - MouseWheel, MousePosition.Subtract(Hover.Bounds.Location));
            OldMouseWheel = MouseWheel;

            PositionRecalculateRequired = false;
            if (!anyMouseKey) Hover = TerraLogic.Instance.IsActive? GetHover(MousePosition) : null;
        }
        public override void Draw()
        {
            TerraLogic.SpriteBatch.Begin();

            base.Draw();

            if (Hover is not null && CurrentKeys.IsKeyDown(Keys.F3))
            {
                //if (Hover.Parent is not null) Hover.Parent.DrawDebug(spriteBatch, Color.Yellow);
                Hover.DrawDebug(Color.Red);
            }

            if (Hover is not null && Hover.HoverText is not null) 
            {
                Vector2 pos = MousePosition.ToVector2();

                float offX = Font.MeasureString(Hover.HoverText).X;
                if (pos.X > offX) pos.X -= offX;
                else pos.X += 10;

                TerraLogic.SpriteBatch.DrawString(Font, Hover.HoverText, pos + new Vector2(0, -1), Color.Black);
                TerraLogic.SpriteBatch.DrawString(Font, Hover.HoverText, pos + new Vector2(-1, 0), Color.Black);
                TerraLogic.SpriteBatch.DrawString(Font, Hover.HoverText, pos + new Vector2(0, 1),  Color.Black);
                TerraLogic.SpriteBatch.DrawString(Font, Hover.HoverText, pos + new Vector2(1, 0), Color.Black);
                TerraLogic.SpriteBatch.DrawString(Font, Hover.HoverText, pos + new Vector2(0, 0), Color.White);
            }
            TerraLogic.SpriteBatch.End();
        }

        public void SetActive(UIElement? element)
        {
            if (element is UIRoot) element = null;

            UIElement? previousActive = Active;
            Active = element;

            if (previousActive is not null) previousActive.OnActiveChanged();
            if (Active is not null) Active.OnActiveChanged();
            OnGlobalActiveChanged?.Invoke(previousActive, Active);
        }

    }
    public enum EventType 
    { 
        Presssed = 1, 
        Released = 2, 
        Hold = 3 
    }
    public enum MouseKeys
    {
        Left,
        Right,
        Middle,
        XButton1,
        XButton2
    }
}
