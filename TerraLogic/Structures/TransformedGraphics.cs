using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TerraLogic.Gui;

namespace TerraLogic.Structures
{
    public struct Transform
    {
        public Vector2 Offset { get; }
        public float Scale { get; }

        public Transform(Vector2 offset, float scale)
        {
            Offset = offset;
            Scale = scale;
        }

        public Vector2 WorldToScreen(Vector2 worldPos)
        {
            return worldPos * Scale + Offset;
        }
        public Vector2 ScreenToWorld(Vector2 screenPos)
        {
            return (screenPos - Offset) / Scale;
        }

        public Rect WorldToScreen(Rect worldRect)
        {
            worldRect.Location = WorldToScreen(worldRect.Location);
            worldRect.Size *= Scale;
            return worldRect;
        }
        public Rect ScreenToWorld(Rect screenRect)
        {
            screenRect.Location = ScreenToWorld(screenRect.Location);
            screenRect.Size /= Scale;
            return screenRect;
        }

        public Matrix ToMatrix()
        {
            return Matrix.Multiply(Matrix.CreateScale(Scale), Matrix.CreateTranslation(Offset.X, Offset.Y, 0));
        }

    }
}
