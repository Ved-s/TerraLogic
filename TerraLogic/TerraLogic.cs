using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Diagnostics;
using System.Globalization;
using TerraLogic.GuiElements;

namespace TerraLogic
{
    public class TerraLogic : Game
    {
        internal static GraphicsDeviceManager Graphics;
        internal static SpriteBatch SpriteBatch;
        internal static TerraLogic Instance;

        internal static Texture2D GridTex;
        internal static Texture2D MoreTex;
        internal static Texture2D Pixel;
        internal static Effect Gradient;

        internal static SpriteFont Consolas12;
        internal static SpriteFont Consolas10;
        internal static SpriteFont Consolas8;

        internal Process ThisProcess = Process.GetCurrentProcess();

        float DrawTimeMS = 0f;
        float TickTimeMS = 0f;
        DateTime TmpTime = DateTime.Now;

        ulong RanUpdates = 0;

        internal static UIRoot Root;

        public TerraLogic()
        {
            Instance = this;
            Graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";

        }
        protected override void Initialize()
        {
            Window.AllowUserResizing = true;
            IsMouseVisible = true;
            base.Initialize();

            Root = new UIRoot()
            {
                Font = Consolas12,
                Sub = {
                    new Gui.Logics("logics")
                    {
                        X = 0,
                        Y = 0,
                        Width = Pos.Width("root"),
                        Height = Pos.Height("root"),
                        Priority = int.MaxValue
                    },
                    new UIPanel("overlay")
                    {
                        Width = Pos.Width("root"),
                        Height = Pos.Height("root"),
                        HoverTransparentBackground = true,
                        Sub =
                        {
                            new UILabel("timingInfo")
                            {
                                Y = 0,
                                X = Pos.Width("..") - Pos.Width(),
                                BackColor = new Color(0,0,0,64),
                                TextColor = Color.Lime,
                                Font = Consolas8,
                                Visible = false,
                                OnUpdate = (e) =>
                                {
                                    e.Text =
                                        $"{Instance.DrawTimeMS:0.000} ms frame\n" +
                                        $"{Instance.TickTimeMS:0.000} ms tick\n" +
                                        $"{Gui.Logics.WireUpdateWatch.Elapsed.TotalMilliseconds:0.000} ms wire\n" +
                                        $"{Util.MakeSize((ulong)ThisProcess.PrivateMemorySize64)} priv";
                                },
                                OnKeyUpdated = (e, k, t) => 
                                {
                                    if (k == Keys.F10 && t == EventType.Presssed) e.Visible = !e.Visible;
                                }
                            },
                            new Gui.TileSelector("tileSelect"),
                            new Gui.ToolSelector("toolSelect")
                            {
                                Y = Pos.Bottom("tileSelect")
                            },
                            new Gui.WireColorSelector("wireColorSelect")
                            {
                                Y = Pos.Bottom("toolSelect")
                            },
                            new Gui.FileSelector("fileSelect")
                            {
                                X = Pos.Right("..") - Pos.Width(),
                                Y = Pos.Bottom("..") - Pos.Height(),
                                Width = 300, Height = 300,
                            },
                            new Gui.ColorSelector("colorSelect")
                            {
                                Y = Pos.Height("..") - Pos.Height(),
                            },
                            new Gui.CompactSizeSelector("sizeSelect"),
                            new UIButton("saveData")
                            {
                                X = Pos.Width("..") - Pos.Width() - 5,
                                Y = Pos.Height("..") - Pos.Height() - 5,
                                Width = 80,
                                Height = 20,
                                Text = "Save",
                                BackColor = new Color(48, 48, 48),
                                OutlineColor = new Color(64, 64, 64),
                                HoverColors = new Colors(Color.White, 64, 64, 64),
                                OnClick = (caller) =>
                                {
                                    caller.Visible = false;
                                    caller.GetElement("../loadData").Visible = false;
                                    (caller.GetElement("../fileSelect") as Gui.FileSelector).ShowDialog(true, (cancel, file) =>
                                    {
                                        if (!cancel) Gui.Logics.SaveFile(file);
                                        caller.Visible = true;
                                        caller.GetElement("../loadData").Visible = true;
                                    });
                                }
                            },
                            new UIButton("loadData")
                            {
                                X = Pos.Width("..") - Pos.Width() - 90,
                                Y = Pos.Height("..") - Pos.Height() - 5,
                                Width = 80,
                                Height = 20,
                                Text = "Load",
                                OutlineColor = new Color(64, 64, 64),
                                BackColor = new Color(48, 48, 48),
                                HoverColors = new Colors(Color.White, 64, 64, 64),
                                OnClick = (caller) =>
                                {
                                    caller.Visible = false;
                                    caller.GetElement("../saveData").Visible = false;
                                    (caller.GetElement("../fileSelect") as Gui.FileSelector).ShowDialog(false, (cancel, file) =>
                                    {
                                        if (!cancel) Gui.Logics.LoadFile(file);
                                        caller.Visible = true;
                                        caller.GetElement("../saveData").Visible = true;
                                    });
                                }
                            },
                            new UIPanel("debug")
                            {
                                Font = Consolas10,
                                Y = Pos.Height("..") - Pos.Height(),
                                X = 0,
                                Height = 60,
                                Width = 200,
                                BackColor = new Color(32,32,32),
                                OutlineColor = new Color(48,48,48),
                                Sub =
                                {
                                    new UILabel()
                                    {
                                        X = 5, Y = 5,
                                        OnUpdate = (UIElement @this) =>
                                        {
                                            if (Gui.Logics.HoverWorld == Gui.Logics.World || Gui.Logics.HoverWorld is null)
                                            {
                                                Point pos = (PanNZoom.ScreenToWorld(Gui.Logics.Instance.MousePosition) / Gui.Logics.TileSize.ToVector2()).ToPoint();
                                                @this.Text = $"X: {pos.X}, Y: {pos.Y}\n" +
                                                $"Z: {PanNZoom.Zoom.ToString(NumberFormatInfo.InvariantInfo)}";
                                            }
                                            else 
                                            {
                                                Vector2 global = Gui.Logics.World.ScreenToTiles(Gui.Logics.Instance.MousePosition);
                                                Point local = Gui.Logics.HoverWorld.ScreenToTiles(Gui.Logics.Instance.MousePosition).ToPoint();

                                                @this.Text = 
                                                $"G: {global.X.ToString(NumberFormatInfo.InvariantInfo)}, {global.Y.ToString(NumberFormatInfo.InvariantInfo)}\n" +
                                                $"L: {local.X}, {local.Y} Z: {PanNZoom.Zoom.ToString(NumberFormatInfo.InvariantInfo)}";
                                            }
                                        }
                                    },
                                    new UICheckButton()
                                    {
                                        X = 5, Y = 35,
                                        Width = 125,
                                        Height = 20,
                                        Text = "Pause simulation",
                                        BackColor = new Color(48,48,48),
                                        HoverColors = new Colors(Color.White, 64, 64, 64),
                                        OutlineColor = new Color(64,64,64),

                                        OnClick = (b) =>
                                        {
                                            UICheckButton cb = b as UICheckButton;
                                            cb.Text = cb.Checked? "Run" : "Pause simulation";
                                            cb.GetElement("../step").Visible = cb.Checked;
                                            cb.Width = cb.Checked? 60 : 125;

                                            Gui.Logics.UpdatePaused = cb.Checked;
                                        }
                                    },
                                    new UIButton(".step")
                                    {
                                        X = 75, Y = 35,
                                        Width = 55,
                                        Height = 20,
                                        BackColor = new Color(48,48,48),
                                        HoverColors = new Colors(Color.White, 64, 64, 64),
                                        ClickColors = new Colors(Color.White, 128, 128, 128),
                                        OutlineColor = new Color(64,64,64),
                                        Text = "Step",
                                        Visible = false,
                                        OnClick = (b) => Gui.Logics.UpdateTick = true
                                    },
                                    new UICheckButton(".wiredebug")
                                    {
                                        X = 140, Y = 35,
                                        Width = 55,
                                        Height = 20,
                                        BackColor = new Color(48,48,48),
                                        HoverColors = new Colors(Color.White, 64, 64, 64),
                                        ClickColors = new Colors(Color.White, 128, 128, 128),
                                        OutlineColor = new Color(64,64,64),
                                        Text = "Debug",
                                        OnClick = (b) => Gui.Logics.WireDebugActive = (b as UICheckButton).Checked
                                    }
                                }
                            }
                        }
                    }
                },
                OnKeyUpdated = (caller, key, @event) => 
                {
                    if (@event == EventType.Presssed) 
                    {
                        switch(key) 
                        {
                            case Keys.F1:
                                UIElement overlay = caller.GetElement(".overlay");
                                overlay.Visible = !overlay.Visible;
                                break;

                            case Keys.F9:
                                UIElement debug = caller.GetElement(".overlay/debug");
                                debug.Visible = !debug.Visible;
                                break;
                        }
                        
                    }
                }
            };
            Gui.Logics.LoadFile("latest.tl");
        }

        protected override void OnExiting(object sender, EventArgs args)
        {
            Gui.Logics.SaveFile("latest.tl");
        }

        protected override void LoadContent()
        {
            SpriteBatch = new SpriteBatch(GraphicsDevice);

            GridTex = Content.Load<Texture2D>("GridTex");
            MoreTex = Content.Load<Texture2D>("MoreTex");
            Gradient = Content.Load<Effect>("HGradient");
            Pixel = new Texture2D(GraphicsDevice, 1, 1);
            Pixel.SetData(new Color[] { Color.White });

            Consolas12 = Content.Load<SpriteFont>("Consolas12");
            Consolas10 = Content.Load<SpriteFont>("Consolas10");
            Consolas8 = Content.Load<SpriteFont>("Consolas8");


            BackgroundGridRenderer.LoadContent(Content);
            Gui.Logics.LoadTileContent(Content);

        }
        protected override void UnloadContent()
        {
        }
        protected override void Update(GameTime gameTime)
        {
            TmpTime = DateTime.Now;
            base.Update(gameTime);
            Root.Update();
            TickTimeMS = (float)(DateTime.Now - TmpTime).TotalMilliseconds;
            if (RanUpdates % 60 == 0) ThisProcess = Process.GetCurrentProcess();
            RanUpdates++;
        }
        protected override void Draw(GameTime gameTime)
        {
            TmpTime = DateTime.Now;
            GraphicsDevice.Clear(new Color(48, 48, 48, 128));

            Root.Draw(SpriteBatch);

            base.Draw(gameTime);
            DrawTimeMS = (float)(DateTime.Now - TmpTime).TotalMilliseconds;
        }
    }
}
