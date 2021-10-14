using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Diagnostics;
using TerraLogic.GuiElements;

namespace TerraLogic
{
    public class TerraLogic : Game
    {
        internal static GraphicsDeviceManager Graphics;
        internal static SpriteBatch SpriteBatch;
        internal static TerraLogic Instance;

        internal static Texture2D GridTex;
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
                                OnUpdate = (e) =>
                                {
                                    e.Text =
                                        $"{Instance.DrawTimeMS:0.000} ms frame\n" +
                                        $"{Instance.TickTimeMS:0.000} ms tick\n" +
                                        $"{Gui.Logics.WireUpdateWatch.Elapsed.TotalMilliseconds:0.000} ms wire\n" +
                                        $"{Util.MakeSize((ulong)ThisProcess.PrivateMemorySize64)} priv";
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
                            new UIButton("saveData")
                            {
                                X = Pos.Width("..") - Pos.Width() - 5,
                                Y = Pos.Height("..") - Pos.Height() - 5,
                                Width = 80,
                                Height = 20,
                                Text = "Save",
                                BackColor = new Color(48, 48, 48),
                                OutlineColor = new Color(64, 64, 64),
                                HoverBackColor = new Color(64, 64, 64),
                                OnClick = (caller) =>
                                {
                                    caller.Visible = false;
                                    caller.GetElement(".loadData").Visible = false;
                                    (caller.GetElement(".fileSelect") as Gui.FileSelector).ShowDialog(true, (cancel, file) =>
                                    {
                                        if (!cancel) Gui.Logics.SaveToFile(file);
                                        caller.Visible = true;
                                        caller.GetElement(".loadData").Visible = true;
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
                                HoverBackColor = new Color(64, 64, 64),
                                OnClick = (caller) =>
                                {
                                    caller.Visible = false;
                                    caller.GetElement(".saveData").Visible = false;
                                    (caller.GetElement(".fileSelect") as Gui.FileSelector).ShowDialog(false, (cancel, file) =>
                                    {
                                        if (!cancel) Gui.Logics.LoadFromFile(file);
                                        caller.Visible = true;
                                        caller.GetElement(".saveData").Visible = true;
                                    });
                                }
                            },
                        }
                    }
                },
                OnKeyUpdated = (caller, key, @event) => 
                {
                    if (key == Keys.F1 && @event == EventType.Presssed) 
                    {
                        UIElement overlay = caller.GetElement("overlay");
                        overlay.Visible = !overlay.Visible;
                    }
                }
            };
            Gui.Logics.LoadFromFile("latest.tl");
        }

        protected override void OnExiting(object sender, EventArgs args)
        {
            Gui.Logics.SaveToFile("latest.tl");
        }

        protected override void LoadContent()
        {
            SpriteBatch = new SpriteBatch(GraphicsDevice);

            GridTex = Content.Load<Texture2D>("GridTex");
            Gradient = Content.Load<Effect>("HGradient");
            Pixel = new Texture2D(GraphicsDevice, 1, 1);
            Pixel.SetData(new Color[] { Color.White });

            Consolas12 = Content.Load<SpriteFont>("Consolas12");
            Consolas10 = Content.Load<SpriteFont>("Consolas10");
            Consolas8 = Content.Load<SpriteFont>("Consolas8");

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
