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

        public abstract void MouseKeyUpdate(MouseKeys key, EventType @event, Point worldpos);
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

        public override void MouseKeyUpdate(MouseKeys key, EventType @event, Point worldpos)
        {
            if (key == MouseKeys.Left && @event != EventType.Released) Gui.Logics.SetTile(worldpos, null);
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

        public Rectangle Selection = new Rectangle();
        public bool NoSelection { get => Selection.Width == 0 || Selection.Height == 0; }

        bool Dragging;
        Point DragPos;

        public override void MouseKeyUpdate(MouseKeys key, EventType @event, Point worldpos)
        {
            if (key == MouseKeys.Left)
            {
                if (!Dragging && @event == EventType.Presssed)
                {
                    Dragging = true;
                    DragPos = worldpos;
                }
                else if (Dragging && @event == EventType.Hold)
                {
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
                            Gui.Logics.SetTile(x, y, null);
                            Gui.Logics.WireArray[x, y] = 0;
                        }
                }
                if (state.IsKeyDown(Keys.LeftControl))
                {
                    if (key == Keys.C)
                    {
                        Gui.Logics.CopyToClipboard(Selection);
                    }
                    else if (key == Keys.X)
                    {
                        Gui.Logics.CopyToClipboard(Selection);
                        for (int y = Selection.Y; y < Selection.Bottom; y++)
                            for (int x = Selection.X; x < Selection.Right; x++)
                            {
                                Gui.Logics.SetTile(x, y, null);
                                Gui.Logics.WireArray[x, y] = 0;
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

            Rectangle rect = PanNZoom.WorldToScreen(Selection.Mul(16));

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
            Graphics.FillRectangle(spriteBatch, rect, Color.CornflowerBlue.Div(4, true));
            Graphics.DrawRectangle(spriteBatch, rect, Color.CornflowerBlue);
            spriteBatch.End();
        }
    }
    public class WirePath : Tool
    {
        public override string Id => "wirepath";
        public override string DisplayName => $"Wire pathfinder (Mode: {(removeMode ? "remove" : "place")})\n" + GetDescription();
        public override bool ShowWires => true;
        public override bool DrawMouseIcon => false;
        public override bool AllowWireSelection => true;

        ChunkArray2D newWires = new ChunkArray2D(4);
        ChunkArray2D<bool> trackedWires = new ChunkArray2D<bool>(4);
        ChunkArray2D<bool> otherWires = new ChunkArray2D<bool>(4);

        Point A, B;
        DragState Dragging;
        bool removeMode = false;

        bool aStarError = false;

        public override void MouseKeyUpdate(MouseKeys key, EventType @event, Point worldpos)
        {
            if (key == MouseKeys.Left)
            {
                if (removeMode)
                {
                    if (@event == EventType.Presssed) 
                    {
                        if (!TerraLogic.Root.CurrentKeys.IsKeyDown(Keys.LeftShift)) newWires.Clear();
                        foreach (Gui.WireSignal signal in Gui.Logics.TrackWire(worldpos, Gui.Logics.SelectedWire))
                            newWires[signal.X, signal.Y] |= signal.Wire;
                    }
                }

                else switch (@event)
                    {
                        case EventType.Presssed when Dragging == DragState.None:
                            if (A == default)
                            {
                                A = worldpos;
                                B = worldpos;
                                Dragging = DragState.B;
                                newWires.Clear();
                            }
                            else if (A == worldpos) Dragging = DragState.A;
                            else if (B == worldpos) Dragging = DragState.B;
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
                    for (int y = 0; y < newWires.Height; y++)
                        for (int x = 0; x < newWires.Width; x++)
                        {
                            if (removeMode) Gui.Logics.WireArray[x, y] &= ~newWires[x, y];
                            else Gui.Logics.WireArray[x, y] |= newWires[x, y];
                        }
                    Deselected();
                    WireColorChanged();
                }
                if (key == Keys.M) 
                { 
                    removeMode = !removeMode; 
                    newWires.Clear();
                    if (!removeMode) WireColorChanged();
                }
            }
        }

        public override void Draw(SpriteBatch spriteBatch, bool selected)
        {
            if (A == default && !removeMode) return;

            Gui.Logics.DrawWires(newWires, BlendState.Additive);

            if (removeMode) return;

            Rectangle a = PanNZoom.WorldToScreen(new Rectangle(A.X, A.Y, 1, 1).Mul(Gui.Logics.TileSize));
            Rectangle b = PanNZoom.WorldToScreen(new Rectangle(B.X, B.Y, 1, 1).Mul(Gui.Logics.TileSize));

            spriteBatch.Begin();
            DrawRectangle(spriteBatch, a, Color.White);
            DrawRectangle(spriteBatch, b, Color.White);
            DrawLine(spriteBatch,
                a.Location.Add(new Point(a.Width / 2, a.Height / 2)),
                b.Location.Add(new Point(b.Width / 2, b.Height / 2)), aStarError ? Color.Red : Color.White);
            spriteBatch.End();

        }

        public override void Deselected()
        {
            Dragging = DragState.None;
            A = default;

            newWires.Clear();
            trackedWires.Clear();
            otherWires.Clear();
        }

        public override void Selected()
        {
            WireColorChanged();
        }

        public override void WireColorChanged()
        {
            otherWires.Clear();
            for (int y = 0; y < Gui.Logics.WireArray.Height; y++)
                for (int x = 0; x < Gui.Logics.WireArray.Width; x++)
                {
                    if ((Gui.Logics.WireArray[x, y] & Gui.Logics.SelectedWire) != 0)
                    {
                        if (Gui.Logics.TileArray[x, y] is not Tiles.JunctionBox)
                        {
                            otherWires[x, y] = true;
                            otherWires[x, y - 1] = true;
                            otherWires[x + 1, y] = true;
                            otherWires[x, y + 1] = true;
                            otherWires[x - 1, y] = true;
                        }
                    }
                }
            if (A != default) AStar();
        }

        private string GetDescription()
        {

            if (!IsSelected) return "";

            string line = $"Press M to change mode to {(removeMode ? "Place" : "Remove")}\n";
            if (Gui.Logics.SelectedWire == 0) line += "Select wire colors to plan";
            else 
            {
                if (removeMode)
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
            newWires.Clear();
            trackedWires.Clear();
            aStarError = false;

            if (Gui.Logics.SelectedWire == 0) return;

            foreach (Gui.WireSignal signal in Gui.Logics.TrackWire(Around4(A), Gui.Logics.SelectedWire)) trackedWires[signal.X, signal.Y] = true;
            foreach (Gui.WireSignal signal in Gui.Logics.TrackWire(Around4(B), Gui.Logics.SelectedWire)) trackedWires[signal.X, signal.Y] = true;

            Point[] path = AStarPathfinding.AStar(A, B, TimeSpan.FromMilliseconds(100), IsPassable);
            if (path is null) { aStarError = true; return; }
            foreach (Point pos in path)
            {
                newWires[pos.X, pos.Y] = Gui.Logics.SelectedWire;
            }
        }

        private bool IsPassable(AStarPathfinding.Side sideIn, Point pos, AStarPathfinding.Side sideOut)
        {
            if (pos.X < 0 || pos.Y < 0) return false;
            if (Gui.Logics.TileArray[pos.X, pos.Y] is Tiles.JunctionBox box) return JunctionPassable(sideIn, sideOut, box);
            return (Gui.Logics.TileArray[pos.X, pos.Y] is null && !otherWires[pos.X, pos.Y])
                || pos == B
                || trackedWires[pos.X, pos.Y]
                || trackedWires[pos.X, pos.Y + 1] || trackedWires[pos.X, pos.Y - 1]
                || trackedWires[pos.X + 1, pos.Y] || trackedWires[pos.X - 1, pos.Y];
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

}
