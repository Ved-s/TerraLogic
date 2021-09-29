using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
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
        internal static Texture2D RedCross;

        internal static SpriteFont Consolas12;
        internal static SpriteFont Consolas10;
        internal static SpriteFont Consolas8;

        float DrawTimeMS = 0f;
        float TickTimeMS = 0f;
        DateTime TmpTime = DateTime.Now;

        static UIRoot Root;

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
                    new UILabel("timingInfo")
                    {
                        Y = 0,
                        X = Pos.Width("root") - Pos.Width(),
                        BackColor = new Color(0,0,0,64),
                        TextColor = Color.Lime,
                        Font = Consolas8,
                        OnUpdate = (e) =>
                        {
                            e.Text =
                                $"{Instance.DrawTimeMS:0.000} ms frame\n" +
                                $"{Instance.TickTimeMS:0.000} ms tick\n" +
                                //$"{(Instance.DrawTimeMS == 0? 0 : 1000/ Instance.DrawTimeMS):0} fps max\n" +
                                $"{Util.MakeSize((ulong)Process.GetCurrentProcess().PrivateMemorySize64)} priv";
                        }
                    },
                    new Gui.TileSelector("tileSelect"),
                    new Gui.WireColorSelector("wireColorSelect")
                    {
                        Y = Pos.Bottom("tileSelect")
                    },
                    new Gui.Logics("logics")
                    {
                        X = 0,
                        Y = 0,
                        Width = Pos.Width("root"),
                        Height = Pos.Height("root"),
                        Priority = int.MaxValue
                    },
                    new Gui.FileSelector("fileSelect")
                    {
                        X = Pos.Right("root") - Pos.Width(),
                        Y = Pos.Bottom("root") - Pos.Height(),
                        Width = 300, Height = 300,
                    },
                    new UIButton("saveData")
                    {
                        X = Pos.Width("root") - Pos.Width() - 5,
                        Y = Pos.Height("root") - Pos.Height() - 5,
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
                        X = Pos.Width("root") - Pos.Width() - 90,
                        Y = Pos.Height("root") - Pos.Height() - 5,
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
                    }
                }
            };
        }
        protected override void LoadContent()
        {
            SpriteBatch = new SpriteBatch(GraphicsDevice);

            GridTex = Content.Load<Texture2D>("GridTex");
            RedCross = Content.Load<Texture2D>("RedCross");
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