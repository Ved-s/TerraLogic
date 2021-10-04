using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TerraLogic.Tiles
{
    public abstract class Tile
    {
        public virtual Point Pos { get; internal protected set; }
        public virtual Point Size { get => new Point(1, 1); } 

        public virtual string DisplayName { get => Id; }

        public bool NeedsUpdate { get; set; }
        public virtual bool NeedsContinuousUpdate{ get; set; }

        internal protected bool Created { get; internal set; } = false;

        public abstract string Id { get; }
        public virtual string[] PreviewVariants { get => null; }

        public abstract void Draw(Rectangle rect, bool isScreenPos = false);
        public virtual void Update() { }

        public virtual void WireSignal(int wire, Point origin) { }
        public virtual void BeforeDestroy() { }
        public virtual void PlacedInWorld() { }

        public virtual void RightClick(bool held, bool preview) { }

        protected void SendSignal(int wire = -1) 
        {
            if (!Created) return;
            Gui.Logics.SendWireSignal(new Rectangle(Pos.X, Pos.Y, Size.X, Size.Y), wire);
        }

        public virtual void LoadContent(ContentManager content) { }

        internal abstract Tile CreateTile(string data, bool preview);
        internal virtual string GetData() => null;

        public override string ToString()
        {
            return $"{Id}:{GetData()} {Pos.X},{Pos.Y}";
        }
    }
}
