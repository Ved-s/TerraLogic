using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace TerraLogic.GuiElements
{
    public abstract class UIElement : IEnumerable<UIElement>
    {
        public static Dictionary<string, UIElement> Elements = new Dictionary<string, UIElement>();

        List<UIModal> VisibleModals = new List<UIModal>();

        Stopwatch UpdateWatch = new Stopwatch();

        public UIElement(string name)
        {
            if (!string.IsNullOrEmpty(name) && !name.StartsWith(".")) Elements.Add(name, this);
            Name = name.TrimStart('.');
            Sub = new ElementCollection(this);
        }
        public void Add(UIElement element)
        {
            if (Sub is null) Sub = new ElementCollection(this);
            Sub.Add(element);
        }

        public virtual void Draw(SpriteBatch spriteBatch)
        {
            if (!Visible) return;

            foreach (UIElement e in Sub.Reverse<UIElement>())
            {
                if (e.Visible && e is not UIModal) e.Draw(spriteBatch);
            }

            foreach (UIModal modal in VisibleModals) Graphics.FillRectangle(spriteBatch, Bounds, modal.ModalBackground);
            foreach (UIModal modal in VisibleModals) modal.Draw(spriteBatch);

        }

        internal void DrawDebug(SpriteBatch spriteBatch, Color c)
        {
            Graphics.DrawRectangle(spriteBatch, Bounds, c);
            string data = $"{Parent?.Name}/{Name ?? "null"}\n{Bounds.X},{Bounds.Y} {Bounds.Width}x{Bounds.Height}\n{UpdateWatch.Elapsed.Milliseconds}ms update";
            Vector2 s = Font.MeasureString(data);
            Vector2 pos = Bounds.Location.ToVector2();
            pos.X += (Bounds.Width - s.X) / 2;
            pos.Y += (Bounds.Height - s.Y) / 2;

            if (parent != null)
            {
                if (pos.X < Parent.Bounds.X) pos.X = Parent.Bounds.X;
                if (pos.Y < Parent.Bounds.Y) pos.Y = Parent.Bounds.Y;

                if (pos.X + s.X > Parent.Bounds.Right) pos.X = Parent.Bounds.Right - s.X;
                if (pos.Y + s.Y > Parent.Bounds.Bottom) pos.Y = Parent.Bounds.Bottom - s.Y;
            }

            spriteBatch.DrawStringShaded(Font, data, pos, c, Color.Black);

            foreach (UIModal modal in VisibleModals) 
            {
                Graphics.DrawRectangle(spriteBatch, modal.Bounds, Color.Yellow);
                spriteBatch.DrawStringShadedCentered(modal.Font, "modal", new Rectangle(modal.Bounds.X, modal.Bounds.Y, modal.Bounds.Width, 1), Color.White, Color.Black);
            }

        }

        public virtual void Update()
        {
            UpdateWatch.Restart();
            OnUpdate?.Invoke(this);
            if (!Enabled || !Visible) return;
            if (PositionRecalculateRequired) Recalculate();
            VisibleModals.Clear();
            foreach (UIElement e in Sub)
            {
                if (e.Visible && e is UIModal modal) VisibleModals.Add(modal);
                if (PositionRecalculateRequired) e.PositionRecalculateRequired = true;
                if (!e.Enabled) continue;
                if (!e.Visible && !e.InvisibleUpdate) continue;
                e.Update();
            }
            PositionRecalculateRequired = false;
            UpdateWatch.Stop();
        }

        protected void Recalculate()
        {
            if (Parent is null) return;

            Bounds.Width = Width.Calculate(this);
            Bounds.Height = Height.Calculate(this);

            Bounds.X = X.Calculate(this) + Parent?.Bounds.X ?? 0;
            Bounds.Y = Y.Calculate(this) + Parent?.Bounds.Y ?? 0;
            //Debug.WriteLine($"Recalculated size for {GetType().Name} \"{Name}\": {Bounds.X},{Bounds.Y} {Bounds.Width}x{Bounds.Height}");

        }

        protected internal virtual void KeyStateUpdate(Keys key, EventType @event)
        {
            OnKeyUpdated?.Invoke(this, key, @event);
            foreach (UIElement e in Sub) e.KeyStateUpdate(key, @event);
        }
        protected internal virtual void MouseKeyStateUpdate(MouseKeys key, EventType @event, Point pos)
        {
            OnMouseUpdated?.Invoke(this, key, @event, pos);

            if (key == MouseKeys.Left && @event == EventType.Presssed)
            {
                OnClick?.Invoke(this);
                if (!string.IsNullOrEmpty(Name)) GlobalClick?.Invoke(Name);
            }
        }
        protected internal virtual void MouseWheelStateUpdate(int change, Point pos)
        {
            OnMouseWheeled?.Invoke(this, change, pos);
        }

        public void Loaded()
        {
            foreach (UIElement sub in Sub)
            {
                sub.Root = Root;
                sub.Loaded();
                sub.Parent = this;
            }

        }

        internal UIElement GetHover(Point pos)
        {
            foreach (UIElement me in VisibleModals)

                    if (me.Bounds.Contains(pos)) 
                    {
                        UIElement hover = me.GetHover(pos);
                        if (hover != null) return hover;
                    }
            if (VisibleModals.Count > 0) return this;

            foreach (UIElement e in Sub)
            {
                if (e.Bounds.Contains(pos) && e.Visible)
                {
                    if (e.Sub.Count == 0) return e;
                    else
                    {
                        UIElement hover = e.GetHover(pos);
                        if (hover != null) return hover;
                    }
                }
            }

            if (Bounds.Contains(pos) && !HoverTransparentBackground) return this;
            return null;
        }

        protected void DrawBackground(SpriteBatch spriteBatch)
        {
            if (BackColor != Color.Transparent)
            {
                Graphics.FillRectangle(spriteBatch, Bounds, BackColor);
            }
        }
        protected void DrawBackground(SpriteBatch spriteBatch, Color @override)
        {
            if (@override != Color.Transparent)
            {
                Graphics.FillRectangle(spriteBatch, Bounds, @override);
            }
        }

        public IEnumerator<UIElement> GetEnumerator()
        {
            return ((IEnumerable<UIElement>)Sub).GetEnumerator();
        }
        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)Sub).GetEnumerator();
        }

        public UIElement GetElement(string name)
        {
            if (name == ".") return this;
            if (name == "..") return Parent;

            if (name.StartsWith(".")) 
            {
                string localName = name.Substring(1);
                foreach (UIElement element in Sub) if (element.Name == localName) return element;

                UIElement e = this;
                while (name.StartsWith(".") && e != null) { name = name.Substring(1); e = e.Parent; }

                if (e is null) return null;
                foreach (UIElement element in e.Sub) if (element.Name == name) return element;
                return null;
            }

            foreach (UIElement element in Sub) if (element.Name == name) return element;

            return Elements[name];
        }

        public override string ToString()
        {
            return $"{Parent?.Name}/{Name ?? "null"} {Bounds.X},{Bounds.Y} {Bounds.Width}x{Bounds.Height}";
        }

        public bool Enabled = true, InvisibleUpdate = false, HoverTransparentBackground = false;
        public Rectangle Bounds;
        public ElementCollection Sub;
        private UIElement parent;
        private UIRoot root;
        private int? priority = null;

        public static Action<string> GlobalClick;


        public Action<UIElement> OnClick;
        public Action<UIElement> OnUpdate;
        public Action<UIElement, Keys, EventType> OnKeyUpdated;
        public Action<UIElement, MouseKeys, EventType, Point> OnMouseUpdated;
        public Action<UIElement, float, Point> OnMouseWheeled;

        public string Name;


        private string text = "";
        private Color backColor = Color.Transparent;
        private Color textColor = Color.White;
        private Pos x = 0, y = 0, w = 0, h = 0;
        protected bool PositionRecalculateRequired = true;

        private SpriteFont font = null;
        private bool visible = true;

        public bool Hover { get => Root.Hover != null && Root.Hover == this; }
        public virtual Pos X { get => x; set { x = value ?? 0; PositionRecalculateRequired = true; } }
        public virtual Pos Y { get => y; set { y = value ?? 0; PositionRecalculateRequired = true; } }
        public virtual Pos Width { get => w; set { w = value ?? 0; PositionRecalculateRequired = true; } }
        public virtual Pos Height { get => h; set { h = value ?? 0; PositionRecalculateRequired = true; } }
        public virtual SpriteFont Font { get => font ?? Parent?.Font; set => font = value; }
        public virtual string Text { get => text; set => text = value; }
        public virtual Color BackColor { get => backColor; set => backColor = value; }
        public virtual Color TextColor { get => textColor; set => textColor = value; }
        public virtual UIElement Parent
        {
            get => parent;
            set
            {
                PositionRecalculateRequired = true;
                parent = value;
                Root = value.Root;
            }
        }
        public int? Priority { get => priority; set { priority = value; Parent?.Sub.SortByPriority(); } }
        public Point MousePosition
        {
            get => Root?.MousePosition.Subtract(Bounds.Location) ?? default;
        }
        public bool Visible
        {
            get => visible;
            set
            {
                bool changed = value != visible;
                visible = value;
                Recalculate();
                if (changed && parent != null) Parent.PositionRecalculateRequired = true;
            }
        }
        public virtual string HoverText { get; set; } = null;
        public UIRoot Root 
        {
            get => root;
            set
            {
                root = value;
                foreach (UIElement e in Sub) e.Root = root;
            }
        }
    }



    public class ElementCollection : List<UIElement>
    {
        readonly UIElement Parent;
        public ElementCollection(UIElement Parent) : base()
        {
            this.Parent = Parent;
        }

        public new void AddRange(IEnumerable<UIElement> elements)
        {
            foreach (UIElement e in elements) Add(e);
        }

        public new void Add(UIElement element)
        {
            element.Parent = Parent;
            element.Root = Parent.Root;
            if (element.Priority is null) element.Priority = Count;
            base.Add(element);
            SortByPriority();
        }

        public void SortByPriority()
        {
            base.Sort((a, b) => ((int)a.Priority).CompareTo((int)b.Priority));
        }

        public new void Remove(UIElement element)
        {
            element.Parent = null;
            base.Remove(element);
        }
    }
}
