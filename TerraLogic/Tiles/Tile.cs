using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using TerraLogic.Structures;

namespace TerraLogic.Tiles
{
    public abstract class Tile
    {
        public virtual Point Pos { get; internal protected set; }
        public virtual Point Size { get => new Point(1, 1); }

        public virtual bool ShowPreview => true;
        public virtual bool DrawTopMost => false;
        public virtual string DisplayName { get => Id; }

        public virtual bool CanRemove => true;

        public bool NeedsUpdate { get; set; }
        public virtual bool NeedsContinuousUpdate { get; set; }

        internal protected bool Created { get; internal set; } = false;

        public abstract string Id { get; }
        public virtual string[] PreviewDataVariants { get => null; }

        public virtual World World { get; internal protected set; }

        public abstract void Draw(Transform transform);
        public virtual void Update() { }

        public virtual void WireSignal(int wire, Point from, Point inputPosition) { }
        public virtual void BeforeDestroy() { }
        public virtual void PlacedInWorld() { }

        public virtual void RightClick(bool held, bool preview) { }

        internal static Stack<Tile> CurrentWireUpdateStack = new Stack<Tile>();

        protected void SendSignal(int wire = -1)
        {
            if (!Created) return;

            CurrentWireUpdateStack.Push(this);
            World.SignalWire(new Rectangle(Pos.X, Pos.Y, Size.X, Size.Y), wire);
            CurrentWireUpdateStack.Pop();
        }
        protected void SendSignal(Point relativePos, int wire = -1)
        {
            if (!Created) return;

            CurrentWireUpdateStack.Push(this);
            World.SignalWire(Pos + relativePos, wire);
            CurrentWireUpdateStack.Pop();
        }

        public virtual void LoadContent(ContentManager content) { }

        public abstract Tile Copy();

        public abstract Tile CreateTile(string? data, bool preview);
        internal virtual string? GetData() => null;

        public virtual void Save(BinaryWriter writer) { }
        public virtual void Load(BinaryReader reader) { }

        public override string ToString()
        {
            return $"{Id}:{GetData()} {Pos.X},{Pos.Y}";
        }
    }
}
