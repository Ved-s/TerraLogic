using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TerraLogic
{
    public static class AStarPathfinding
    {
        public static Point[] AStar(Point start, Point end, TimeSpan timeLimit, Func<Point, Side, bool> isPassable)
        {
            Dictionary<Point, AStarNode> nodes = new Dictionary<Point, AStarNode>()
            {
                { start, CreateNode(start, 0, Side.Up) }
            };

            AStarNode CreateNode(Point at, int g, Side dir)
            {
                int hw = Math.Abs(at.X - end.X);
                int hh = Math.Abs(at.Y - end.Y);
                int h = (int)(Math.Sqrt(hw * hw + hh * hh) * 10);
                return new AStarNode()
                {
                    G = g,
                    H = h,
                    pos = at,
                    dir = dir
                };
            }
            AStarNode FindMinCost()
            {
                AStarNode n = new AStarNode() { G = int.MaxValue };

                foreach (AStarNode node in nodes.Values)
                {
                    if (!node.closed && (node.F < n.F)) n = node; //|| node.H < n.H
                }
                if (n.G == int.MaxValue) return null;
                return n;
            }

            void SetNodeAt(AStarNode me, Point at, Side side)
            {
                if (!isPassable(at, side)) return;

                if (nodes.TryGetValue(at, out AStarNode nei))
                {
                    if (nei.G > me.G + 10)
                    {
                        nei.G = me.G + 10;
                        nei.dir = side;
                    }
                }
                else nodes.Add(at, CreateNode(at, me.G + 10, side));
            }

            DateTime startTime = DateTime.Now;

            while (true)
            {
                if (DateTime.Now - startTime > timeLimit) return null;

                AStarNode node = FindMinCost();
                if (node is null) return null;
                if (node.pos == end)
                {

                    List<Point> track = new List<Point>();
                    while (true)
                    {
                        track.Add(node.pos);
                        if (node.pos == start) return track.ToArray();
                        switch (node.dir)
                        {
                            case Side.Down:  node = nodes[node.pos.Add(0, 1)]; break;
                            case Side.Up:    node = nodes[node.pos.Add(0, -1)]; break;
                            case Side.Left:  node = nodes[node.pos.Add(-1, 0)]; break;
                            case Side.Right: node = nodes[node.pos.Add(1, 0)]; break;
                        }
                    }
                }

                node.closed = true;

                SetNodeAt(node, node.pos.Add(0, 1), Side.Up);
                SetNodeAt(node, node.pos.Add(0, -1), Side.Down);
                SetNodeAt(node, node.pos.Add(1, 0), Side.Left);
                SetNodeAt(node, node.pos.Add(-1, 0), Side.Right);
            }

        }

        class AStarNode
        {
            public Point pos;
            public bool closed;
            public int G, H;
            public int F { get => G + H; }
            public Side dir;

            public override bool Equals(object obj)
            {
                return obj is AStarNode node && node.pos.Equals(pos);
            }

            public override int GetHashCode()
            {
                int hashCode = -757612369;
                hashCode = hashCode * -1521134295 + pos.GetHashCode();
                hashCode = hashCode * -1521134295 + closed.GetHashCode();
                hashCode = hashCode * -1521134295 + G.GetHashCode();
                hashCode = hashCode * -1521134295 + H.GetHashCode();
                hashCode = hashCode * -1521134295 + dir.GetHashCode();
                return hashCode;
            }
        }

        public enum Side { Up, Right, Down, Left }
    }
}
