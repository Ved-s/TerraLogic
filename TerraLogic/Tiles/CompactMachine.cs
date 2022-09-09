using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.IO;
using TerraLogic.Gui;
using TerraLogic.Structures;

namespace TerraLogic.Tiles
{
    class CompactMachine : Tile
    {
        public override string Id => "compact";

        public World? CompactWorld
        {
            get => InternalWorld;
            set
            {
                InternalWorld = value;
                if (InternalWorld is not null)
                    InternalWorld.Owner = this;
            }
        }

        public override bool NeedsContinuousUpdate => true;
        public override bool DrawTopMost => CompactWorld?.Visible ?? false;
        public override string DisplayName => $"Compact Machine {InternalSize.X}x{InternalSize.Y}\nShift+Right Click to change size";

        public override string[] PreviewDataVariants => new string[]
        {
            "7x7:1x1"
        };

        public override Point Size => ExternalSize;

        internal bool RespectWire = true;
        internal Point InternalSize = new(9);
        internal Point ExternalSize = new(3);

        private Point InterfacePos;

        private static Texture2D Sprite = null!;
        private World? InternalWorld;

        public override Point Pos
        {
            get => base.Pos;
            protected internal set
            {
                base.Pos = value;
                if (CompactWorld is not null)
                    CompactWorld.WorldPos = new(value, CompactWorld.WorldPos.Size);
            }
        }
        public override World World
        {
            get => base.World;
            protected internal set
            {
                base.World = value;
                if (CompactWorld is not null)
                {
                    CompactWorld.Parent = value;

                    int level = 0;
                    World? p = World;
                    while (p is not null)
                    {
                        level++;
                        p = p.Parent;
                    }

                    byte color = (byte)(64 / level + 64);
                    CompactWorld.BackgroundColor = new(color, color, color);
                }
            }
        }

        public override void LoadContent(ContentManager content)
        {
            Sprite = content.Load<Texture2D>("Tiles/CompactMachine");
        }

        public override void Draw(Transform transform)
        {
            if (CompactWorld is not null)
                CompactWorld.Visible = World.WorldZoom > 4f / Math.Max(ExternalSize.X, ExternalSize.Y);

            if (World is not null)
            {
                TerraLogic.SpriteBatch.End();
                TerraLogic.SpriteBatch.Begin(SpriteSortMode.Deferred, World.CurrentDrawBlendState, World.WorldZoom > 1f / Math.Max(ExternalSize.X, ExternalSize.Y) ? SamplerState.PointWrap : SamplerState.LinearWrap);
            }

            DrawBordered(transform, Size, false, !RespectWire, World!);
            if (World is not null)
            {
                TerraLogic.SpriteBatch.End();
                TerraLogic.SpriteBatch.Begin(SpriteSortMode.Deferred, World.CurrentDrawBlendState, World.WorldZoom > 1 ? SamplerState.PointWrap : SamplerState.LinearWrap);
            }
            if (CompactWorld is not null)
            {
                TerraLogic.SpriteBatch.End();
                CompactWorld.Draw(World.CurrentDrawBlendState);
                TerraLogic.SpriteBatch.Begin(SpriteSortMode.Deferred, World.CurrentDrawBlendState, World!.WorldZoom > 1 ? SamplerState.PointWrap : SamplerState.LinearWrap);
            }
        }

        internal override string GetData()
        {
            return $"{(RespectWire ? "" : "-")}{InternalSize.X}x{InternalSize.Y}:{ExternalSize.X}x{ExternalSize.Y}";
        }

        public override Tile Copy()
        {
            return new CompactMachine()
            {
                InternalSize = InternalSize,
                ExternalSize = ExternalSize,
                InterfacePos = InterfacePos,
                RespectWire = RespectWire,
                CompactWorld = CompactWorld!.CopyExact(),
            };
        }

        public override Tile CreateTile(string? data, bool preview)
        {
            Point @internal = InternalSize;
            Point @external = ExternalSize;

            bool respectWire = data.IsNullEmptyOrWhitespace() || data[0] != '-';
            if (!respectWire) data = data![1..];

            if (!data.IsNullEmptyOrWhitespace())
            {
                string[] split = data.Split(new char[] { ':' }, 2);

                if (split.Length == 2)
                {
                    string[] inner = split[0].Split(new char[] { 'x' });

                    if (inner.Length == 2)
                    {
                        if (!int.TryParse(inner[0], out @internal.X))
                            @internal.X = InternalSize.X;
                        if (!int.TryParse(inner[1], out @internal.Y))
                            @internal.Y = InternalSize.Y;
                    }

                    string[] outer = split[1].Split(new char[] { 'x' });

                    if (outer.Length == 2)
                    {
                        if (!int.TryParse(outer[0], out @external.X))
                            @external.X = ExternalSize.X;
                        if (!int.TryParse(outer[1], out @external.Y))
                            @external.Y = ExternalSize.Y;
                    }
                }
            }

            CompactMachine compactMachine = new CompactMachine();
            compactMachine.InternalSize = @internal;
            compactMachine.ExternalSize = @external;
            compactMachine.RespectWire = respectWire;

            if (preview) return compactMachine;

            compactMachine.CompactWorld = new("compactWorld", compactMachine, World, @internal.X, @internal.Y, new(0, 0, external.X, external.Y));
            compactMachine.CompactWorld.Padding = 1 / 16f;

            Point ipos = (@internal - external) / new Point(2);

            compactMachine.InterfacePos = ipos;
            compactMachine.CompactWorld.SetTile(ipos, new CompactInterface(external));
            return compactMachine;
        }

        public override void WireSignal(int wire, Point from, Point inputPosition)
        {
            if (new Rectangle(Pos, Size).Contains(from)) return;

            if (CompactWorld!.Tiles[InterfacePos.X, InterfacePos.Y] is CompactInterface @interface)
            {
                if (!RespectWire)
                    wire = -1;
                @interface.Signal(wire, inputPosition);
            }
        }

        internal void Signal(int wire, Point pos)
        {
            if (!RespectWire)
                wire = -1;

            SendSignal(pos, wire);
        }

        public override void RightClick(bool held, bool preview)
        {
            if (!held && preview && TerraLogic.Root.CurrentKeys.IsKeyDown(Keys.LeftShift))
            {
                CompactSizeSelector.Instance?.ShowDialog(this);
            }
        }

        public override void Update()
        {
            if (CompactWorld is not null)
                CompactWorld.Update();
        }

        public override void Save(BinaryWriter writer)
        {
            writer.Write(RespectWire);
            writer.Write(InternalSize.X);
            writer.Write(InternalSize.Y);
            writer.Write(ExternalSize.X);
            writer.Write(ExternalSize.Y);
            writer.Write(InterfacePos.X);
            writer.Write(InterfacePos.Y);
            CompactWorld!.Save(writer);
        }

        public override void Load(BinaryReader reader)
        {
            RespectWire = reader.ReadBoolean();
            InternalSize = new(reader.ReadInt32(), reader.ReadInt32());
            ExternalSize = new(reader.ReadInt32(), reader.ReadInt32());
            InterfacePos = new(reader.ReadInt32(), reader.ReadInt32());
            CompactWorld = new("compactWorld", this, World, InternalSize.X, InternalSize.Y, new(0, 0, ExternalSize.X, ExternalSize.Y));
            CompactWorld.Padding = 1 / 16f;
            CompactWorld.Load(reader);
        }

        public static void DrawBordered(Transform transform, Point size, bool @interface, bool yellow, World? world)
        {
            int startY = @interface ? Logics.TileSize.Y : 0;

            size *= Logics.TileSize;
            Point bend = size - new Point(5);
            Point bsize = bend - new Point(5);

            TerraLogic.SpriteBatch.End();
            TerraLogic.SpriteBatch.Begin(transformMatrix: transform.ToMatrix(),
                samplerState: world?.WorldZoom > 1 ? SamplerState.PointClamp : SamplerState.LinearClamp);

            TerraLogic.SpriteBatch.Draw(Sprite, new Rectangle(0, 0, 5, 5), new Rectangle(0, startY, 5, 5), Color.White);
            TerraLogic.SpriteBatch.Draw(Sprite, new Rectangle(bend.X, 0, 5, 5), new Rectangle(11, startY, 5, 5), Color.White);
            TerraLogic.SpriteBatch.Draw(Sprite, new Rectangle(0, bend.Y, 5, 5), new Rectangle(0, startY + 11, 5, 5), Color.White);
            TerraLogic.SpriteBatch.Draw(Sprite, new Rectangle(bend.X, bend.Y, 5, 5), new Rectangle(11, startY + 11, 5, 5), Color.White);

            TerraLogic.SpriteBatch.Draw(Sprite, new Rectangle(5, 0, bsize.X, 5), new Rectangle(5, startY, 6, 5), Color.White);
            TerraLogic.SpriteBatch.Draw(Sprite, new Rectangle(0, 5, 5, bsize.Y), new Rectangle(0, startY + 5, 5, 6), Color.White);
            TerraLogic.SpriteBatch.Draw(Sprite, new Rectangle(5, bend.Y, bsize.X, 5), new Rectangle(5, startY + 11, 6, 5), Color.White);
            TerraLogic.SpriteBatch.Draw(Sprite, new Rectangle(bend.X, 5, 5, bsize.Y), new Rectangle(11, startY + 5, 5, 6), Color.White);

            TerraLogic.SpriteBatch.Draw(Sprite, new Rectangle(5, 5, bsize.X, bsize.Y), new Rectangle(5, startY + 5, 6, 6), Color.White);

            if (yellow)
                startY += 6;

            TerraLogic.SpriteBatch.Draw(Sprite, new Rectangle((size.X - 6) / 2, (size.Y - 6) / 2, 6, 6), new Rectangle(16, startY, 6, 6), Color.White);

            TerraLogic.SpriteBatch.End();
            TerraLogic.SpriteBatch.Begin(samplerState: world?.WorldZoom > 1 ? SamplerState.PointClamp : SamplerState.LinearClamp);
        }

        class CompactInterface : Tile
        {
            public override string Id => "interface";

            public override bool CanRemove
            {
                get
                {
                    if (World.Owner is not CompactMachine compact || compact.CompactWorld != World)
                        return true;

                    return compact.InterfacePos != Pos;
                }
            }

            public override bool ShowPreview => false;


            public override void Draw(Transform transform)
            {
                TerraLogic.SpriteBatch.End();
                TerraLogic.SpriteBatch.Begin(blendState: World.CurrentDrawBlendState, samplerState: SamplerState.PointWrap);

                DrawBordered(transform, Size, true, !RespectWire, World!);

                TerraLogic.SpriteBatch.End();
                TerraLogic.SpriteBatch.Begin(blendState: World.CurrentDrawBlendState, samplerState: World.WorldZoom > 1 ? SamplerState.PointWrap : SamplerState.LinearWrap);
            }

            public override Point Size => InternalSize;

            private bool RespectWire => World.Owner is not CompactMachine compact || compact.RespectWire;

            private Point InternalSize;

            public CompactInterface()
            {
                InternalSize = new(1);
            }

            public CompactInterface(Point size)
            {
                InternalSize = size;
            }

            public override Tile Copy()
            {
                return new CompactInterface(InternalSize);
            }

            public override Tile CreateTile(string? data, bool preview)
            {
                throw new NotImplementedException();
            }

            internal void Signal(int wire, Point pos)
            {
                SendSignal(pos, wire);
            }

            public override void WireSignal(int wire, Point from, Point inputPosition)
            {
                if (new Rectangle(Pos, Size).Contains(from)) return;

                if (World.Owner is CompactMachine compact)
                    compact.Signal(wire, inputPosition);
            }

            public override void Save(BinaryWriter writer)
            {
                writer.Write(InternalSize.X);
                writer.Write(InternalSize.Y);
            }

            public override void Load(BinaryReader reader)
            {
                InternalSize = new(reader.ReadInt32(), reader.ReadInt32());
            }
        }
    }
}
