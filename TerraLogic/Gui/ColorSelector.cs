using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using TerraLogic.GuiElements;

namespace TerraLogic.Gui
{
    class ColorSelector : UIModal
    {
        public override Pos X => Pos.Width("..", .5f);
        public override Pos Y => Pos.Height("..", .5f);
        public override Pos Width => 200;
        public override Pos Height => 100;

        readonly GradientColorBar Red, Green, Blue;

        public delegate void ColorChosenDelegate(bool cancel, Color color);

        ColorChosenDelegate? Callback;
        Action<Color>? ChangeCallback;
        Color InitColor;
        Texture2D DrawSprite = null!;
        Rectangle? SpriteRect;

        public Color CurrentColor 
        {
            get => new Color(Red.Value, Green.Value, Blue.Value);
            set 
            {
                Red.Value = value.R;
                Green.Value = value.G;
                Blue.Value = value.B;
            }
        }
        public static ColorSelector? Instance;

        public ColorSelector(string name) : base(name) 
        {
            Instance = this;

            BackColor = new Color(32, 32, 32, 128);
            Visible = false;

            Sub = new ElementCollection(this)
            {
                new GradientColorBar(".red")
                {
                    X = 8, Y = 10, Width = 120, Height = 12, SliderColor = Color.Red,
                    ValueChanged = (v) => ChangeCallback?.Invoke(CurrentColor)
                },
                new GradientColorBar(".green")
                {
                    X = 8, Y = 32, Width = 120, Height = 12, SliderColor = Color.Green,
                    ValueChanged = (v) => ChangeCallback?.Invoke(CurrentColor)
                },
                new GradientColorBar(".blue")
                {
                    X = 8, Y = 54, Width = 120, Height = 12, SliderColor = Color.Blue,
                    ValueChanged = (v) => ChangeCallback?.Invoke(CurrentColor)
                },
                new UIButton(".cancel")
                {
                    X = 120,
                    Y = 75,
                    Width = 75,
                    Height = 20,
                    OutlineColor = new Color(64, 64, 64),
                    BackColor = new Color(32, 32, 32, 128),
                    HoverColors = new Colors(Color.White, 64, 64, 64, 128),
                    Text = "Cancel",

                    OnClick = (caller) =>
                    {
                        Callback?.Invoke(true, InitColor);
                        Callback = null;
                        Visible = false;
                    }
                },
                new UIButton(".ok")
                {
                    X = 40,
                    Y = 75,
                    Width = 75,
                    Height = 20,
                    BackColor = new Color(32, 32, 32, 128),
                    OutlineColor = new Color(64, 64, 64),
                    HoverColors = new Colors(Color.White, 64, 64, 64, 128),
                    Text = "Ok",
                    OnClick = (caller) =>
                    {
                        Callback?.Invoke(false, CurrentColor);
                        Callback = null;
                        Visible = false;
                    }
                }
            };

            Red =   ((GradientColorBar)GetElement(".red")!)!; 
            Green = ((GradientColorBar)GetElement(".green")!)!; 
            Blue =  ((GradientColorBar)GetElement(".blue")!)!; 
        }

        public void ShowDialog(Color init, ColorChosenDelegate callback, Action<Color>? colorChanging = null, Texture2D? previewSprite = null, Rectangle? previewSpriteRect = null)
        {
            Visible = true;
            Callback?.Invoke(true, CurrentColor);
            Callback = callback;
            ChangeCallback = colorChanging;
            CurrentColor = init;
            InitColor = init;

            DrawSprite = previewSprite ?? TerraLogic.Pixel;
            SpriteRect = previewSpriteRect;
        }

        public override void Update()
        {
            base.Update();

            byte r = Red.Value;
            byte g = Green.Value;
            byte b = Blue.Value;

            Red.Start.R = 0;
            Red.Start.G = g;
            Red.Start.B = b;

            Red.End.R = 255;
            Red.End.G = g;
            Red.End.B = b;

            Green.Start.R = r;
            Green.Start.G = 0;
            Green.Start.B = b;

            Green.End.R = r;
            Green.End.G = 255;
            Green.End.B = b;

            Blue.Start.R = r;
            Blue.Start.G = g;
            Blue.Start.B = 0;

            Blue.End.R = r;
            Blue.End.G = g;
            Blue.End.B = 255;
        }
        public override void Draw()
        {
            DrawBackground();
            Color outline = BackColor * 2;
            outline.A = 255;
            Graphics.DrawRectangle(Bounds, outline);

            base.Draw();

            Rectangle rect = new Rectangle(Bounds.X + 135, Bounds.Y + 10, 56, 56);

            Color c = CurrentColor;

            TerraLogic.SpriteBatch.End();
            TerraLogic.SpriteBatch.Begin(SpriteSortMode.Deferred, null, SamplerState.PointWrap, null, null);
            TerraLogic.SpriteBatch.Draw(DrawSprite, rect, SpriteRect, c);
            TerraLogic.SpriteBatch.End();
            TerraLogic.SpriteBatch.Begin();

            Graphics.DrawRectangle(rect, Color.White);

            string str = $"{c.R} ({c.R:x2})";
            int x = (56 - (int)TerraLogic.Consolas8.MeasureString(str).X) / 2 + Bounds.X + 135;
            TerraLogic.SpriteBatch.DrawStringShaded(TerraLogic.Consolas8, str, new Vector2(x, Bounds.Y + 17), Color.Red, Color.Black);

            str = $"{c.G} ({c.G:x2})";
            x = (56 - (int)TerraLogic.Consolas8.MeasureString(str).X) / 2 + Bounds.X + 135;
            TerraLogic.SpriteBatch.DrawStringShaded(TerraLogic.Consolas8, str, new Vector2(x, Bounds.Y + 32), Color.Green, Color.Black);

            str = $"{c.B} ({c.B:x2})";
            x = (56 - (int)TerraLogic.Consolas8.MeasureString(str).X) / 2 + Bounds.X + 135;
            TerraLogic.SpriteBatch.DrawStringShaded(TerraLogic.Consolas8, str, new Vector2(x, Bounds.Y + 47), Color.Blue, Color.Black);
        }
    }

    class GradientColorBar : UIElement
    {
        public Color Start = Color.Black;
        public Color End = Color.White;

        public Color SliderColor = Color.White;

        public byte Value = 0;

        public Action<byte>? ValueChanged = null;

        public GradientColorBar(string name) : base(name)
        {
            BackColor = new Color(128, 128, 128);
        }

        public override string HoverText => Color.Lerp(Start, End, (float)MousePosition.X / Bounds.Width).PackedValue.ToString("x8").Substring(2);

        public override void Draw()
        {
            TerraLogic.SpriteBatch.End();
            TerraLogic.SpriteBatch.Begin(SpriteSortMode.Immediate, null);

            Rectangle rect = Bounds;
            rect.X++; rect.Y++; rect.Width -= 2; rect.Height -= 2;

            Graphics.DrawRectangle(rect, BackColor);
            rect.X++; rect.Y++; rect.Width -= 2; rect.Height -= 2;

            TerraLogic.Gradient.Parameters["Color0"].SetValue(Start.ToVector4());
            TerraLogic.Gradient.Parameters["Color1"].SetValue(End.ToVector4());
            TerraLogic.Gradient.CurrentTechnique.Passes[0].Apply();

            Graphics.FillRectangle( rect, Color.White);

            TerraLogic.SpriteBatch.End();
            TerraLogic.SpriteBatch.Begin();

            int x = 1 + (int)((Bounds.Width - 5) * (Value / 255f));

            Graphics.DrawRectangle(new Rectangle(Bounds.X + x, Bounds.Y, 3, Bounds.Height), SliderColor);
        }

        protected internal override void MouseKeyStateUpdate(MouseKeys key, EventType @event, Point pos)
        {
            if (key == MouseKeys.Left && @event != EventType.Released) 
            {
                byte newValue = (byte)((float)Util.Constrain(0, pos.X, Bounds.Width) / Bounds.Width * 255);

                if (newValue != Value) 
                {
                    Value = newValue;
                    ValueChanged?.Invoke(Value);
                }
            }
            base.MouseKeyStateUpdate(key, @event, pos);
        }

        protected internal override void KeyStateUpdate(Keys key, EventType @event)
        {
            base.KeyStateUpdate(key, @event);

            if (@event == EventType.Presssed && Hover) 
            {
                int interval = Root!.CurrentKeys.IsKeyDown(Keys.LeftShift) ? (byte)16 : (byte)1;
                switch (key)
                {
                    case Keys.Left:
                        {
                            int newValue = Value - interval;
                            if (newValue < 0) newValue = 0;
                            Value = (byte)newValue;
                            break;
                        }

                    case Keys.Right:
                        {
                            int newValue = Value + interval;
                            if (newValue > 255) newValue = 255;
                            Value = (byte)newValue;
                            break;
                        }

                    case Keys.D0: Value <<= 4; break;
                    case Keys.D1: Value <<= 4; Value |= 1; break;
                    case Keys.D2: Value <<= 4; Value |= 2; break;
                    case Keys.D3: Value <<= 4; Value |= 3; break;
                    case Keys.D4: Value <<= 4; Value |= 4; break;
                    case Keys.D5: Value <<= 4; Value |= 5; break;
                    case Keys.D6: Value <<= 4; Value |= 6; break;
                    case Keys.D7: Value <<= 4; Value |= 7; break;
                    case Keys.D8: Value <<= 4; Value |= 8; break;
                    case Keys.D9: Value <<= 4; Value |= 9; break;
                    case Keys.A: Value <<= 4; Value |= 10; break;
                    case Keys.B: Value <<= 4; Value |= 11; break;
                    case Keys.C: Value <<= 4; Value |= 12; break;
                    case Keys.D: Value <<= 4; Value |= 13; break;
                    case Keys.E: Value <<= 4; Value |= 14; break;
                    case Keys.F: Value <<= 4; Value |= 15; break;
                }
            }
        }
    }
}
