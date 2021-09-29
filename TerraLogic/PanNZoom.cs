﻿using Microsoft.Xna.Framework;
using System;
using System.Diagnostics;

namespace TerraLogic
{
    public static class PanNZoom
    {
        public static float Zoom = 1f;
        public static Vector2 Position = Vector2.Zero;
        public static Vector2 ScreenPosition = Vector2.Zero;

        public static bool Dragging { get; private set; }
        private static Point DragPos;

        public static void SetZoom(float zoom, Point at) 
        {
            if (zoom == Zoom) return;
            Vector2 atWorldBefore = ScreenToWorld(at);
            Zoom = zoom;
            Vector2 atWorldAfter = ScreenToWorld(at);
            Position -= atWorldAfter - atWorldBefore;
            Position.X = Math.Max(0, Position.X);
            Position.Y = Math.Max(0, Position.Y);
            ScreenPosition = WorldToScreenF(Vector2.Zero);
        }

        public static Point WorldToScreen(Vector2 v) 
        {
            Point p = new Point();
            p.X = (int)((v.X - Position.X) * Zoom);
            p.Y = (int)((v.Y - Position.Y) * Zoom);
            return p;
        }
        public static Vector2 WorldToScreenF(Vector2 v)
        {
            Vector2 p = new Vector2();
            p.X = (v.X - Position.X) * Zoom;
            p.Y = (v.Y - Position.Y) * Zoom;
            return p;
        }
        public static Vector2 ScreenToWorld(Point p)
        {
            Vector2 v = new Vector2();
            v.X = (p.X / Zoom) + Position.X;
            v.Y = (p.Y / Zoom) + Position.Y;
            return v;
        }
        public static Rectangle WorldToScreen(RectangleF rect) 
        {
            Rectangle r = new Rectangle();
            r.Location = WorldToScreen(rect.Location);
            r.Width = (int)(rect.Width * Zoom);
            r.Height = (int)(rect.Height * Zoom);
            return r;
        }
        public static RectangleF ScreenToWorld(Rectangle rect)
        {
            RectangleF rf = new RectangleF();
            rf.Location = ScreenToWorld(rect.Location);
            rf.Width = rect.Width / Zoom;
            rf.Height = rect.Height / Zoom;
            return rf;
        }

        public static void UpdateDragging(bool drag, Point screenPoint) 
        {
            if (drag && !Dragging)
            {
                DragPos = screenPoint;
                Dragging = true;
            }
            else if (drag && Dragging)
            {
                Position -= screenPoint.Subtract(DragPos).ToVector2() / Zoom;

                Position.X = Math.Max(0, Position.X);
                Position.Y = Math.Max(0, Position.Y);

                ScreenPosition = WorldToScreenF(Vector2.Zero);
                DragPos = screenPoint;
            }
            else if (!drag && Dragging) 
            {
                Dragging = false;
            }

        }

    }

    public static class PointExtension 
    {
        public static Point Multiply(this Point p, float f) 
        {
            return new Point((int)(p.X * f), (int)(p.Y * f));
        }
        public static Point Divide(this Point p, float f)
        {
            return new Point((int)(p.X / f), (int)(p.Y / f));
        }
        public static Point Subtract(this Point p, Point v)
        {
            return new Point((int)(p.X - v.X), (int)(p.Y - v.Y));
        }
        public static Point Add(this Point p, Point v)
        {
            return new Point((int)(p.X + v.X), (int)(p.Y + v.Y));
        }
        public static Vector2 ToVector2(this Point p) 
        {
            return new Vector2(p.X, p.Y);
        }
        public static Point ToPoint(this Vector2 v)
        {
            return new Point((int)v.X, (int)v.Y);
        }
    }
}
