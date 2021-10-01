using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using TerraLogic.GuiElements;

namespace TerraLogic.Gui
{
    class FileSelector : UIElement
    {
        private string Path = System.IO.Path.DirectorySeparatorChar.ToString();
        private DialogClosedDelegate Callback;
        private bool AllowNonexistentFiles;

        public delegate void DialogClosedDelegate(bool cancel, string filename);

        public override string Text { get => GetElement(".title").Text; set => GetElement(".title").Text = value; }

        public FileSelector(string name) : base(name)
        {
            BackColor = new Color(16, 16, 16, 128);
            Visible = false;

            Sub = new ElementCollection(this)
            {
                new UILabel(".title")
                {
                    Width = Pos.Width(".."),
                    Height = 20,
                    AutoSize = false,
                    CenterText = true,
                    Text = "Select a file"
                },
                new UILabel(".path")
                {
                    Y = 25,
                    Width = Pos.Width(".."),
                    Height = 20,
                    AutoSize = false,
                    CenterText = true,
                    Text = "/"
                },
                new UIList(".files")
                {
                    X = 5,
                    BackColor = new Color(32, 32, 32, 128),
                    Y = Pos.Bottom(".path"),
                    Width = Pos.Width("..") - 10,
                    Height = Pos.Height("..") - 100,
                    ItemClick = (caller, index, item, doubleClick) =>
                    {
                        string i = item as string;

                        bool dir = i.EndsWith(System.IO.Path.DirectorySeparatorChar.ToString()) || i.EndsWith(System.IO.Path.AltDirectorySeparatorChar.ToString());
                        bool back = i == "..";
                        bool root = Path == System.IO.Path.DirectorySeparatorChar.ToString();

                        if (doubleClick)
                        {
                            if (root) 
                            {
                                if (i == "Current User")
                                {
                                    Path = System.Environment.GetEnvironmentVariable("USERPROFILE");
                                    UpdateItems();
                                    return;
                                }
                                else if (i == "App Directory")
                                {
                                    Path = AppDomain.CurrentDomain.BaseDirectory;
                                    UpdateItems();
                                    return;
                                }
                            }

                            if (dir)
                            {
                                if (!root) i = i.Substring(0, i.Length - 1);
                                Path = System.IO.Path.Combine(Path, i);
                                UpdateItems();
                            }
                            else if (back)
                            {
                                Path = System.IO.Path.GetDirectoryName(Path) ?? System.IO.Path.DirectorySeparatorChar.ToString();
                                UpdateItems();
                            }
                            else 
                            {
                                string file = System.IO.Path.Combine(Path, i);

                                Callback(false, file);
                                Visible = false;
                                
                            }
                        }
                        if (!dir && !back && !root)
                        {
                            caller.GetElement(".ok").Visible = true;
                            UIInput curFile = caller.GetElement(".curFile") as UIInput;
                            curFile.Visible = true;
                            curFile.Text = i;
                        }
                        else
                        {
                            UIInput curFile = caller.GetElement(".curFile") as UIInput;
                            curFile.Visible = !root;
                            caller.GetElement(".ok").Visible = AllowNonexistentFiles && curFile.TextBuilder.Length > 0 && !root;
                            if (AllowNonexistentFiles) curFile.Text = "";
                            else curFile.Visible = false;
                        }
                    }
                },
                new UIInput(".curFile") 
                {
                    X = 5,
                    Y = Pos.Bottom(".files") + 5,
                    Width = Pos.Width(".files"),
                    Height = 20,
                    BackColor = new Color(32, 32, 32, 128),
                    OnTextChanged = (caller) => 
                    {
                        if (AllowNonexistentFiles) 
                        {
                            caller.GetElement(".ok").Visible = caller.TextBuilder.Length > 0;
                        }
                    },
                    OnEnter = (caller) => 
                    {
                        if (caller.TextBuilder.Length == 0) return;

                        string file = System.IO.Path.Combine(Path, caller.Text);
                        Callback(false, file);
                        Visible = false;
                    }
                },
                new UIButton(".cancel")
                {
                    X = Pos.Width("..") - 80,
                    Y = Pos.Bottom(".files") + 30,
                    Width = 75,
                    Height = 20,
                    OutlineColor = new Color(64, 64, 64),
                    BackColor = new Color(32, 32, 32, 128),
                    HoverBackColor = new Color(64, 64, 64, 128),
                    Text = "Cancel",

                    OnClick = (caller) => 
                    {
                        Callback(true, null);
                        Callback = null;
                        Visible = false;
                    }
                },
                new UIButton(".ok")
                {
                    X = Pos.X(".cancel") - 80,
                    Y = Pos.Bottom(".files") + 30,
                    Width = 75,
                    Height = 20,
                    BackColor = new Color(32, 32, 32, 128),
                    OutlineColor = new Color(64, 64, 64),
                    HoverBackColor = new Color(64, 64, 64, 128),
                    Text = "Ok",
                    Visible = false,
                    OnClick = (caller) =>
                    {
                        string file;
                        if (AllowNonexistentFiles)
                        {
                            file = GetElement(".curFile").Text;
                        }
                        else 
                        {
                            UIList files = caller.GetElement(".files") as UIList;
                            file = files.Items[files.SelectedIndex] as string;
                        }
                        file = System.IO.Path.Combine(Path, file);

                        Callback(false, file);
                        Callback = null;
                        Visible = false;
                    }
                }
            };
            UpdateItems();
        }

        public void ShowDialog(bool allowNonexistentFiles, DialogClosedDelegate callback) 
        {
            Visible = true;

            Callback?.Invoke(true, null);

            Callback = callback;
            AllowNonexistentFiles = allowNonexistentFiles;

            (GetElement(".curFile") as UIInput).ReadOnly = !allowNonexistentFiles;
        }

        private void UpdateItems()
        {
            GetElement("path").Text = Path;

            UIList files = GetElement("files") as UIList;
            files.Items.Clear();
            files.SelectedIndex = -1;

            if (Path == System.IO.Path.DirectorySeparatorChar.ToString())
            {
                foreach (string drive in System.IO.Directory.GetLogicalDrives()) files.Items.Add(drive);
                files.Items.Add("Current User");
                files.Items.Add("App Directory");
            }
            else
            {
                files.Items.Add("..");
                try
                {
                    foreach (string dir in System.IO.Directory.GetDirectories(Path)) files.Items.Add(System.IO.Path.GetFileName(dir) + "/");
                    foreach (string file in System.IO.Directory.GetFiles(Path)) files.Items.Add(System.IO.Path.GetFileName(file));
                }
                catch (System.IO.IOException) { }
            }
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            DrawBackground(spriteBatch);

            base.Draw(spriteBatch);
        }
    }
}
