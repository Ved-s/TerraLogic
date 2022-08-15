using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Diagnostics;
using TerraLogic.GuiElements;
using TerraLogic.Tiles;
using static TerraLogic.GuiElements.Graphics;

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
        public virtual void Draw(SpriteBatch spriteBatch, bool selected) { }
    }

    public class Remove : Tool
    {
        public override string Id => "remove";
        public override string DisplayName => "Remove tile";

        public override void MouseKeyUpdate(MouseKeys key, EventType @event, Point pos)
        {
            if (key == MouseKeys.Left && @event != EventType.Released)
                Gui.Logics.HoverWorld.SetTile(Gui.Logics.HoverWorld.ScreenToTiles(pos).ToPoint(), "");
        }
    }
    public class Select : Tool
    {
        public static Select Instance { get; private set; }

        public Select() { Instance = this; }

        public override string Id => "select";
        public override string DisplayName => "Select area";
        public override bool ShowWires => true;
        public override bool DrawMouseIcon => false;

        public World SelectionWorld;
        public Rectangle Selection = new Rectangle();
        public bool NoSelection { get => SelectionWorld is null || Selection.Width == 0 || Selection.Height == 0; }

        bool Dragging;
        Point DragPos;

        public override void MouseKeyUpdate(MouseKeys key, EventType @event, Point pos)
        {
            if (key == MouseKeys.Left)
            {
                if (!Dragging && @event == EventType.Presssed)
                {
                    SelectionWorld = Gui.Logics.HoverWorld;
                    Dragging = true;
                    DragPos = SelectionWorld.ScreenToTiles(pos).ToPoint();

                    if (DragPos.X < 0) DragPos.X = 0;
                    if (DragPos.Y < 0) DragPos.Y = 0;
                    if (DragPos.X > SelectionWorld.Width) DragPos.X = SelectionWorld.Width - 1;
                    if (DragPos.Y > SelectionWorld.Height) DragPos.Y = SelectionWorld.Height - 1;
                }
                else if (Dragging && @event == EventType.Hold)
                {
                    Point worldpos = SelectionWorld.ScreenToTiles(pos).ToPoint();

                    if (worldpos.X < 0) worldpos.X = 0;
                    if (worldpos.Y < 0) worldpos.Y = 0;
                    if (worldpos.X > SelectionWorld.Width) worldpos.X = SelectionWorld.Width - 1;
                    if (worldpos.Y > SelectionWorld.Height) worldpos.Y = SelectionWorld.Height - 1;

                    Selection = Util.RectFrom2Points(worldpos, DragPos);
                }
                else if (Dragging && @event == EventType.Released)
                {
                    Dragging = false;
                }
            }
            if (key == MouseKeys.Right) { Dragging = false; Selection = new Rectangle(); }
        }
        public override void KeyUpdate(Keys key, EventType @event, KeyboardState state)
        {
            if (@event == EventType.Presssed && !Dragging && !NoSelection)
            {
                if (key == Keys.Delete)
                {
                    for (int y = Selection.Y; y < Selection.Bottom; y++)
                        for (int x = Selection.X; x < Selection.Right; x++)
                        {
                            SelectionWorld.SetTile(x, y, "");
                            SelectionWorld.Wires[x, y] = 0;
                        }
                }
                if (state.IsKeyDown(Keys.LeftControl))
                {
                    if (key == Keys.C)
                    {
                        Gui.Logics.PastePreview = SelectionWorld.Copy(Selection);
                        Gui.Logics.PastePreview.BackgroundColor = Color.CornflowerBlue * 0.2f;
                        ClipboardUtils.World = Gui.Logics.PastePreview;
                    }
                    else if (key == Keys.X)
                    {
                        Gui.Logics.PastePreview = SelectionWorld.Copy(Selection);
                        Gui.Logics.PastePreview.BackgroundColor = Color.CornflowerBlue * 0.2f;
                        ClipboardUtils.World = Gui.Logics.PastePreview;
                        for (int y = Selection.Y; y < Selection.Bottom; y++)
                            for (int x = Selection.X; x < Selection.Right; x++)
                            {
                                SelectionWorld.SetTile(x, y, "");
                                SelectionWorld.Wires[x, y] = 0;
                            }
                    }
                }
            }
        }

        public override void Deselected()
        {
            if (Dragging) Dragging = false;
        }

        public override void Draw(SpriteBatch spriteBatch, bool selected)
        {
            if (NoSelection) return;

            Rectangle rect = SelectionWorld.WorldToScreen(Selection.Mul(16));

            string str = $"{Selection.Width}x{Selection.Height}";
            if (!Dragging)
            {
                if (selected) str += $"\nPress DEL to remove tiles";
                else if (Gui.Logics.SelectedTilePreview != null) str += $"\nClick in the area to fill it";
            }

            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
            spriteBatch.DrawStringShadedCentered(TerraLogic.Consolas10, str, rect, Color.White, Color.Black);
            spriteBatch.End();

            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive);
            FillRectangle(spriteBatch, rect, Color.CornflowerBlue.Div(4, true));
            DrawRectangle(spriteBatch, rect, Color.CornflowerBlue);
            spriteBatch.End();
        }
    }
    public class WirePath : Tool
    {
        public override string Id => "wirepath";
        public override string DisplayName => $"Wire pathfinder (Mode: {(RemoveMode ? "remove" : "place")})\n" + GetDescription();
        public override bool ShowWires => true;
        public override bool DrawMouseIcon => false;
        public override bool AllowWireSelection => true;

        World World, OtherWiresWorld;

        ChunkArray2D NewWires = new ChunkArray2D(4);
        ChunkArray2D<bool> TrackedWires = new ChunkArray2D<bool>(4);
        ChunkArray2D<bool> OtherWires = new ChunkArray2D<bool>(4);

        Point A, B;
        DragState Dragging;
        bool RemoveMode = false;

        bool AStarError = false;

        public override void MouseKeyUpdate(MouseKeys key, EventType @event, Point pos)
        {
            Point worldpos = Point.Zero;

            if (World is not null)
                worldpos = World.ScreenToTiles(pos).ToPoint().Constrain(new(0, 0, World.Width, World.Height));

            if (key == MouseKeys.Left)
            {
                if (RemoveMode)
                {
                    if (@event == EventType.Presssed)
                    {
                        if (Gui.Logics.HoverWorld != World)
                        {
                            NewWires.Clear();
                            World = Gui.Logics.HoverWorld;
                            worldpos = World.ScreenToTiles(pos).ToPoint();
                        }

                        if (!TerraLogic.Root.CurrentKeys.IsKeyDown(Keys.LeftShift)) 
                            NewWires.Clear();
                        foreach (Gui.WireSignal signal in World.TrackWire(worldpos, Gui.Logics.SelectedWire))
                            NewWires[signal.X, signal.Y] |= signal.Wire;
                    }
                }

                else switch (@event)
                    {
                        case EventType.Presssed when Dragging == DragState.None:
                            
                            if (A == default)
                            {
                                World = Gui.Logics.HoverWorld;
                                worldpos = World.ScreenToTiles(pos).ToPoint();
                                A = worldpos;
                                B = worldpos;
                                Dragging = DragState.B;
                                NewWires.Clear();

                                if (OtherWiresWorld != World)
                                    EnumOtherWires(World);
                            }
                            else if (A == worldpos)
                                Dragging = DragState.A;
                            else if (B == worldpos)
                                Dragging = DragState.B;
                            break;
                        case EventType.Released: Dragging = DragState.None; break;
                        case EventType.Hold:
                            if (Dragging == DragState.A && A != worldpos) { A = worldpos; AStar(); }
                            else if (Dragging == DragState.B && B != worldpos) { B = worldpos; AStar(); }
                            break;
                    }
            }
            else if (key == MouseKeys.Right) Deselected();
        }
        public override void KeyUpdate(Keys key, EventType @event, KeyboardState state)
        {
            if (@event == EventType.Presssed)
            {
                if (key == Keys.Enter)
                {
                    for (int y = 0; y < NewWires.Height; y++)
                        for (int x = 0; x < NewWires.Width; x++)
                        {
                            if (RemoveMode) World.Wires[x, y] &= ~NewWires[x, y];
                            else World.Wires[x, y] |= NewWires[x, y];
                        }
                    Deselected();
                    WireColorChanged();
                }
                if (key == Keys.M)
                { 
                    RemoveMode = !RemoveMode; 
                    NewWires.Clear();
                    if (!RemoveMode)
                        WireColorChanged();
                }
            }
        }

        public override void Draw(SpriteBatch spriteBatch, bool selected)
        {
            if (World is null) return;
            if (A == default && B == default && !RemoveMode) return;

            World.DrawWires(NewWires, BlendState.Additive);

            if (RemoveMode) return;

            Rectangle a = World.WorldToScreen(new Rectangle(A.X, A.Y, 1, 1).Mul(Gui.Logics.TileSize));
            Rectangle b = World.WorldToScreen(new Rectangle(B.X, B.Y, 1, 1).Mul(Gui.Logics.TileSize));

            spriteBatch.Begin();
            DrawRectangle(spriteBatch, a, Color.White);
            DrawRectangle(spriteBatch, b, Color.Black);
            DrawLine(spriteBatch,
                a.Location.Add(new Point(a.Width / 2, a.Height / 2)),
                b.Location.Add(new Point(b.Width / 2, b.Height / 2)), AStarError ? Color.Red : Color.White);
            spriteBatch.End();
        }

        public override void Deselected()
        {
            Dragging = DragState.None;
            A = B = default;
            World = null;

            NewWires.Clear();
            TrackedWires.Clear();
            OtherWires.Clear();
        }

        public override void Selected()
        {
            WireColorChanged();
        }

        public override void WireColorChanged()
        {
            if (OtherWiresWorld is not null)
                EnumOtherWires(OtherWiresWorld);
            if (A != default)
                AStar();
        }

        private void EnumOtherWires(World world) 
        {
            OtherWires.Clear();
            for (int y = 0; y < world.Wires.Height; y++)
                for (int x = 0; x < world.Wires.Width; x++)
                {
                    if ((world.Wires[x, y] & Gui.Logics.SelectedWire) != 0)
                    {
                        if (world.Tiles[x, y] is not Tiles.JunctionBox)
                        {
                            OtherWires[x, y] = true;
                            OtherWires[x, y - 1] = true;
                            OtherWires[x + 1, y] = true;
                            OtherWires[x, y + 1] = true;
                            OtherWires[x - 1, y] = true;
                        }
                    }
                }
            OtherWiresWorld = world;
        }

        private string GetDescription()
        {

            if (!IsSelected) return "";

            string line = $"Press M to change mode to {(RemoveMode ? "Place" : "Remove")}\n";
            if (Gui.Logics.SelectedWire == 0) line += "Select wire colors to plan";
            else 
            {
                if (RemoveMode)
                {
                    line += "Left-click on a wire to select it\nLeftShist+Left Click to select multiple groups";
                }
                else
                {
                    if (A == default) line += "Left-click and drag to set start and end points";
                    else line += "Hold LMB on start/end point and drag your mouse to change their positions\nPress Enter to finish planning";
                }
            }
            

            return line;
        }

        public void AStar()
        {
            NewWires.Clear();
            TrackedWires.Clear();
            AStarError = false;

            if (Gui.Logics.SelectedWire == 0) return;

            foreach (Gui.WireSignal signal in Gui.Logics.HoverWorld.TrackWire(Around4(A), Gui.Logics.SelectedWire)) TrackedWires[signal.X, signal.Y] = true;
            foreach (Gui.WireSignal signal in Gui.Logics.HoverWorld.TrackWire(Around4(B), Gui.Logics.SelectedWire)) TrackedWires[signal.X, signal.Y] = true;

            Point[] path = AStarPathfinding.AStar(A, B, TimeSpan.FromMilliseconds(100), IsPassable);
            if (path is null) { AStarError = true; return; }
            foreach (Point pos in path)
            {
                NewWires[pos.X, pos.Y] = Gui.Logics.SelectedWire;
            }
        }

        private bool IsPassable(AStarPathfinding.Side sideIn, Point pos, AStarPathfinding.Side sideOut)
        {
            if (pos.X < 0 || pos.Y < 0) return false;
            if (Gui.Logics.HoverWorld.Tiles[pos.X, pos.Y] is Tiles.JunctionBox box) return JunctionPassable(sideIn, sideOut, box);
            return (Gui.Logics.HoverWorld.Tiles[pos.X, pos.Y] is null && !OtherWires[pos.X, pos.Y])
                || pos == B || pos == A
                || TrackedWires[pos.X, pos.Y]
                || TrackedWires[pos.X, pos.Y + 1] || TrackedWires[pos.X, pos.Y - 1]
                || TrackedWires[pos.X + 1, pos.Y] || TrackedWires[pos.X - 1, pos.Y];
        }

        private bool JunctionPassable(AStarPathfinding.Side sideIn, AStarPathfinding.Side sideOut, JunctionBox box)
        {
            switch (box.Type) 
            {
                case JunctionBox.JunctionType.Cross: return sideIn == sideOut;
                case JunctionBox.JunctionType.TL:
                    switch (sideIn) 
                    {
                        case AStarPathfinding.Side.Up: return sideOut == AStarPathfinding.Side.Left;
                        case AStarPathfinding.Side.Left: return sideOut == AStarPathfinding.Side.Up;
                        case AStarPathfinding.Side.Down: return sideOut == AStarPathfinding.Side.Right;
                        case AStarPathfinding.Side.Right: return sideOut == AStarPathfinding.Side.Down;
                    }
                    return false;
                case JunctionBox.JunctionType.TR:
                    switch (sideIn)
                    {
                        case AStarPathfinding.Side.Up: return sideOut == AStarPathfinding.Side.Right;
                        case AStarPathfinding.Side.Right: return sideOut == AStarPathfinding.Side.Up;
                        case AStarPathfinding.Side.Down: return sideOut == AStarPathfinding.Side.Left;
                        case AStarPathfinding.Side.Left: return sideOut == AStarPathfinding.Side.Down;
                    }
                    return false;
            }
            return false;
        }

        private Point[] Around4(Point p) => new Point[]
        {
            p, p.Add(0, -1), p.Add(0, 1), p.Add(1, 0), p.Add(-1, 0)
        };

        enum DragState { None, A, B }
    }
    public class TriggerWire : Tool
    {
        public override string Id => "trigger";
        public override string DisplayName => "Trigger selected wires";

        public override bool AllowWireSelection => true;
        public override bool DrawMouseIcon => false;

        public override void MouseKeyUpdate(MouseKeys key, EventType @event, Point pos)
        {
            if (key == MouseKeys.Left && @event == EventType.Presssed)
                Gui.Logics.HoverWorld.SignalWire(Gui.Logics.HoverWorld.ScreenToTiles(pos).ToPoint(), Gui.Logics.SelectedWire);
        }
    }

}
