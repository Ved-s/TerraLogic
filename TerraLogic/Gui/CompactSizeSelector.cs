using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Text.RegularExpressions;
using TerraLogic.GuiElements;
using TerraLogic.Tiles;

namespace TerraLogic.Gui
{
    class CompactSizeSelector : UIModal
    {
        public override Pos X => Pos.Width("..", .5f);
        public override Pos Y => Pos.Height("..", .5f);
        public override Pos Width => 200;
        public override Pos Height => 150;

        CompactMachine Tile = null!;
        public static CompactSizeSelector? Instance;

        public CompactSizeSelector(string name) : base(name) 
        {
            Instance = this;

            BackColor = new Color(32, 32, 32, 128);
            Visible = false;

            Regex valid = new Regex("^[0-9]{0,5}$");

            Sub = new ElementCollection(this)
            {
                new UILabel(".innerLabel") 
                {
                    X = 5, Y = 12,
                    Text = "Inner: "
                },
                new UILabel(".innerLabelX")
                {
                    X = 125, Y = 12,
                    Text = "x"
                },
                new UIInput(".innerW") 
                {
                    X = 70, Y = 10,
                    Width = 50, Height = 20,
                    BackColor = new Color(32, 32, 32, 128),
                    OutlineColor = new Color(64, 64, 64),
                    ValidationRegex = valid,
                    
                },
                new UIInput(".innerH")
                {
                    X = 140, Y = 10,
                    Width = 50, Height = 20,
                    BackColor = new Color(32, 32, 32, 128),
                    OutlineColor = new Color(64, 64, 64),
                    ValidationRegex = valid,
                },
                new UILabel(".outerLabel")
                {
                    X = 5, Y = 42,
                    Text = "Outer: "
                },
                new UILabel(".outerLabelX")
                {
                    X = 125, Y = 42,
                    Text = "x"
                },
                new UIInput(".outerW")
                {
                    X = 70, Y = 40,
                    Width = 50, Height = 20,
                    BackColor = new Color(32, 32, 32, 128),
                    OutlineColor = new Color(64, 64, 64),
                    ValidationRegex = valid,
                },
                new UIInput(".outerH")
                {
                    X = 140, Y = 40,
                    Width = 50, Height = 20,
                    BackColor = new Color(32, 32, 32, 128),
                    OutlineColor = new Color(64, 64, 64),
                    ValidationRegex = valid,
                },
                new UICheckButton(".respectColor") 
                {
                    X = 10,
                    Y = 70,
                    Height = 20,
                    Width = Pos.Width("..") - 20,
                    Text = "Respect color",

                    TextColor = Color.White,
                    BackColor = new Color(32, 32, 32, 128),
                    OutlineColor = new Color(64, 64, 64),
                    HoverColors = new Colors(Color.White, 64, 64, 64),
                },
                new UIButton(".cancel")
                {
                    X = 120,
                    Y = Pos.Height("..") - 25,
                    Width = 75,
                    Height = 20,
                    OutlineColor = new Color(64, 64, 64),
                    BackColor = new Color(32, 32, 32, 128),
                    HoverColors = new Colors(Color.White, 64, 64, 64, 128),
                    Text = "Cancel",

                    OnClick = (caller) =>
                    {
                        Visible = false;
                    }
                },
                new UIButton(".ok")
                {
                    X = 40,
                    Y = Pos.Height("..") - 25,
                    Width = 75,
                    Height = 20,
                    BackColor = new Color(32, 32, 32, 128),
                    OutlineColor = new Color(64, 64, 64),
                    HoverColors = new Colors(Color.White, 64, 64, 64, 128),
                    Text = "Ok",
                    OnClick = (caller) =>
                    {
                        Point inner = new(), outer = new();
                        if (!int.TryParse(GetElement("./outerW")!.Text, out outer.X))
                            outer.X = 1;

                        if (!int.TryParse(GetElement("./outerH")!.Text, out outer.Y))
                            outer.Y = 1;

                        if (!int.TryParse(GetElement("./innerW")!.Text, out inner.X))
                            inner.X = outer.X + 2;

                        if (!int.TryParse(GetElement("./innerH")!.Text, out inner.Y))
                            inner.Y = outer.Y + 2;

                        inner.X = Math.Max(outer.X + 2, inner.X);
                        inner.Y = Math.Max(outer.Y + 2, inner.Y);

                        Tile.RespectWire = ((UICheckButton)GetElement("./respectColor")!)!.Checked;
                        Tile.InternalSize = inner;
                        Tile.ExternalSize = outer;
                        Visible = false;
                    }
                }
            };
        }

        public void ShowDialog(CompactMachine tile)
        {
            Visible = true;
            Tile = tile;

            GetElement("./outerW")!.Text = Tile.ExternalSize.X.ToString();
            GetElement("./outerH")!.Text = Tile.ExternalSize.Y.ToString();
            GetElement("./innerW")!.Text = Tile.InternalSize.X.ToString();
            GetElement("./innerH")!.Text = Tile.InternalSize.Y.ToString();
            ((UICheckButton)GetElement("./respectColor")!)!.Checked = Tile.RespectWire;
        }

        public override void Update()
        {
            base.Update();
        }
        public override void Draw()
        {
            DrawBackground();
            Color outline = BackColor * 2;
            outline.A = 255;
            Graphics.DrawRectangle(Bounds, outline);

            base.Draw();

            //Rectangle rect = new Rectangle(Bounds.X + 135, Bounds.Y + 10, 56, 56);
            //
            //Color c = CurrentColor;
            //
            //spriteBatch.End();
            //spriteBatch.Begin(SpriteSortMode.Deferred, null, SamplerState.PointWrap, null, null);
            //spriteBatch.Draw(DrawSprite, rect, SpriteRect, c);
            //spriteBatch.End();
            //spriteBatch.Begin();
            //
            //Graphics.DrawRectangle(rect, Color.White);
            //
            //string str = $"{c.R} ({c.R:x2})";
            //int x = (56 - (int)TerraLogic.Consolas8.MeasureString(str).X) / 2 + Bounds.X + 135;
            //spriteBatch.DrawStringShaded(TerraLogic.Consolas8, str, new Vector2(x, Bounds.Y + 17), Color.Red, Color.Black);
            //
            //str = $"{c.G} ({c.G:x2})";
            //x = (56 - (int)TerraLogic.Consolas8.MeasureString(str).X) / 2 + Bounds.X + 135;
            //spriteBatch.DrawStringShaded(TerraLogic.Consolas8, str, new Vector2(x, Bounds.Y + 32), Color.Green, Color.Black);
            //
            //str = $"{c.B} ({c.B:x2})";
            //x = (56 - (int)TerraLogic.Consolas8.MeasureString(str).X) / 2 + Bounds.X + 135;
            //spriteBatch.DrawStringShaded(TerraLogic.Consolas8, str, new Vector2(x, Bounds.Y + 47), Color.Blue, Color.Black);
        }
    }
}
