using guideXOS.FS;
using guideXOS.Kernel.Drivers;
using guideXOS.Misc;
using guideXOS.OS;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
namespace guideXOS.GUI {
    /// <summary>
    /// Desktop
    /// </summary>
    internal static class Desktop {
        /// <summary>
        /// Dir
        /// </summary>
        public static string Dir;
        /// <summary>
        /// Taskbar
        /// </summary>
        public static Taskbar Taskbar;
        /// <summary>
        /// Image Viewer
        /// </summary>
        public static ImageViewer imageViewer;
        /// <summary>
        /// Message Box
        /// </summary>
        public static MessageBox msgbox;
        /// <summary>
        /// Wav Player
        /// </summary>
        public static WAVPlayer wavplayer;
        /// <summary>
        /// Apps
        /// </summary>
        public static AppCollection Apps;
        /// <summary>
        /// Is At Root
        /// </summary>
        public static bool IsAtRoot {
            get => Desktop.Dir.Length < 1;
        }
        /// <summary>
        /// Initialize
        /// </summary>
        public static void Initialize() {
            Apps = new AppCollection();
            IndexClicked = -1;
            Taskbar = new Taskbar(40, Icons.TaskbarIcon);
            Dir = "";
            imageViewer = new ImageViewer(400, 400);
            msgbox = new MessageBox(100, 300);
            wavplayer = new WAVPlayer(450, 200);
            imageViewer.Visible = false;
            msgbox.Visible = false;
            wavplayer.Visible = false;
            LastPoint.X = -1;
            LastPoint.Y = -1;
        }
        /// <summary>
        /// Bar Height
        /// </summary>
        const int BarHeight = 40;
        /// <summary>
        /// Update
        /// </summary>
        public static void Update() {
            var fileIcon = Icons.FileIcon;
            List <FileInfo> names = File.GetFiles(Dir);
            int Devide = 60;
            int X = Devide;
            int Y = Devide;
            if (IsAtRoot) {
                for (int i = 0; i < Apps.Length; i++) {
                    if (Y + fileIcon.Height + Devide > Framebuffer.Graphics.Height - Devide) {
                        Y = Devide;
                        X += fileIcon.Width + Devide;
                    }
                    ClickEvent(Apps.Name(i), false, X, Y, i);
                    Framebuffer.Graphics.DrawImage(X, Y, Apps.Icon(i));
                    WindowManager.font.DrawString(X, Y + fileIcon.Height, Apps.Name(i), fileIcon.Width + 8, WindowManager.font.FontSize * 3);
                    Y += Icons.FileIcon.Height + Devide;
                }
            }

            for (int i = 0; i < names.Count; i++) {
                if (Y + fileIcon.Height + Devide > Framebuffer.Graphics.Height - Devide) {
                    Y = Devide;
                    X += fileIcon.Width + Devide;
                }
                ClickEvent(names[i].Name, names[i].Attribute == FileAttribute.Directory, X, Y, i + (IsAtRoot ? Apps.Length : 0));
                if (names[i].Name.EndsWith(".png") || names[i].Name.EndsWith(".bmp")) {
                    Framebuffer.Graphics.DrawImage(X, Y, Icons.IamgeIcon);
                } else if (names[i].Name.EndsWith(".wav")) {
                    Framebuffer.Graphics.DrawImage(X, Y, Icons.AudioIcon);
                } else if (names[i].Attribute == FileAttribute.Directory) {
                    Framebuffer.Graphics.DrawImage(X, Y, Icons.FolderIcon);
                } else {
                    Framebuffer.Graphics.DrawImage(X, Y, fileIcon);
                }
                WindowManager.font.DrawString(X, Y + fileIcon.Height, names[i].Name, fileIcon.Width + 8, WindowManager.font.FontSize * 3);
                Y += fileIcon.Height + Devide;
                names[i].Dispose();
            }
            names.Dispose();
            if (Control.MouseButtons.HasFlag(MouseButtons.Left) && !WindowManager.HasWindowMoving && !WindowManager.MouseHandled) {
                if (LastPoint.X == -1 && LastPoint.Y == -1) {
                    LastPoint.X = Control.MousePosition.X;
                    LastPoint.Y = Control.MousePosition.Y;
                } else {
                    if (Control.MousePosition.X > LastPoint.X && Control.MousePosition.Y > LastPoint.Y) {
                        Framebuffer.Graphics.AFillRectangle(
                            LastPoint.X,
                            LastPoint.Y,
                            Control.MousePosition.X - LastPoint.X,
                            Control.MousePosition.Y - LastPoint.Y,
                            0x7F2E86C1);
                    }

                    if (Control.MousePosition.X < LastPoint.X && Control.MousePosition.Y < LastPoint.Y) {
                        Framebuffer.Graphics.AFillRectangle(
                            Control.MousePosition.X,
                            Control.MousePosition.Y,
                            LastPoint.X - Control.MousePosition.X,
                            LastPoint.Y - Control.MousePosition.Y,
                            0x7F2E86C1);
                    }

                    if (Control.MousePosition.X < LastPoint.X && Control.MousePosition.Y > LastPoint.Y) {
                        Framebuffer.Graphics.AFillRectangle(
                            Control.MousePosition.X,
                            LastPoint.Y,
                            LastPoint.X - Control.MousePosition.X,
                            Control.MousePosition.Y - LastPoint.Y,
                            0x7F2E86C1);
                    }

                    if (Control.MousePosition.X > LastPoint.X && Control.MousePosition.Y < LastPoint.Y) {
                        Framebuffer.Graphics.AFillRectangle(
                            LastPoint.X,
                            Control.MousePosition.Y,
                            Control.MousePosition.X - LastPoint.X,
                            LastPoint.Y - Control.MousePosition.Y,
                            0x7F2E86C1);
                    }
                }
            } else {
                LastPoint.X = -1;
                LastPoint.Y = -1;
            }

            Taskbar.Draw();
        }
        /// <summary>
        /// Last Point
        /// </summary>
        public static Point LastPoint;
        /// <summary>
        /// Click Event
        /// </summary>
        /// <param name="name"></param>
        /// <param name="isDirectory"></param>
        /// <param name="X"></param>
        /// <param name="Y"></param>
        /// <param name="i"></param>
        private static void ClickEvent(string name, bool isDirectory, int X, int Y, int i) {
            if (Control.MouseButtons == MouseButtons.Left) {
                bool clickable = true;
                for (int d = 0; d < WindowManager.Windows.Count; d++) {
                    if (WindowManager.Windows[d].Visible)
                        if (WindowManager.Windows[d].IsUnderMouse()) {
                            clickable = false;
                        }
                }

                if (!WindowManager.HasWindowMoving && clickable && !ClickLock && Control.MousePosition.X > X && Control.MousePosition.X < X + Icons.FileIcon.Width && Control.MousePosition.Y > Y && Control.MousePosition.Y < Y + Icons.FileIcon.Height) {
                    IndexClicked = i;
                    OnClick(name, isDirectory, X, Y);
                }
            } else {
                ClickLock = false;
            }

            if (IndexClicked == i) {
                int w = (int)(Icons.FileIcon.Width * 1.5f);
                Framebuffer.Graphics.AFillRectangle(X + ((Icons.FileIcon.Width / 2) - (w / 2)), Y, w, Icons.FileIcon.Height * 2, 0x7F2E86C1);
            }
        }
        static bool ClickLock = false;
        static int IndexClicked;
        /// <summary>
        /// On Click
        /// </summary>
        /// <param name="name"></param>
        /// <param name="isDirectory"></param>
        /// <param name="itemX"></param>
        /// <param name="itemY"></param>
        public static void OnClick(string name, bool isDirectory, int itemX, int itemY) {
            //if (!string.IsNullOrWhiteSpace(name)) { guideXOS.GUI.NotificationManager.Add(new Nofity("Clicked: " + name)); }
            ClickLock = true;
            string devider = "/";
            string path = Dir + name;
            if (isDirectory) {
                string newd = Dir + name + devider;
                Dir.Dispose();
                Dir = newd;
                //guideXOS.GUI.NotificationManager.Add(new Nofity("New Dir: " + Dir));
            } else if (name.EndsWith(".png")) {
                byte[] buffer = File.ReadAllBytes(path);
                PNG png = new PNG(buffer);
                buffer.Dispose();
                imageViewer.SetImage(png);
                png.Dispose();
                WindowManager.MoveToEnd(imageViewer);
                imageViewer.Visible = true;
            } else if (name.EndsWith(".bmp")) {
                byte[] buffer = File.ReadAllBytes(path);
                Bitmap png = new Bitmap(buffer);
                buffer.Dispose();
                imageViewer.SetImage(png);
                png.Dispose();
                WindowManager.MoveToEnd(imageViewer);
                imageViewer.Visible = true;
            } else if (name.EndsWith(".mue")) {
                byte[] buffer = File.ReadAllBytes(path);
                Process.Start(buffer);
            } else if (name.EndsWith(".wav")) {
                if (Audio.HasAudioDevice) {
                    wavplayer.Visible = true;
                    byte[] buffer = File.ReadAllBytes(path);
                    unsafe {
                        //name will be disposed after this loop so create a new one
                        fixed (char* ptr = name)
                            wavplayer.Play(buffer, new string(ptr));
                    }
                } else {
                    msgbox.X = itemX + 75;
                    msgbox.Y = itemY + 75;
                    msgbox.SetText("Audio controller is unavailable!");
                    WindowManager.MoveToEnd(msgbox);
                    msgbox.Visible = true;
                }
            } else if (!Apps.Load(name)) {
                msgbox.X = itemX + 75;
                msgbox.Y = itemY + 75;
                msgbox.SetText("No application can open this file!");
                WindowManager.MoveToEnd(msgbox);
                msgbox.Visible = true;
            }
            path.Dispose();
            devider.Dispose();
        }
    }
}