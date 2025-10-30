using guideXOS.FS;
using guideXOS.Kernel.Drivers;
using guideXOS.Misc;
using guideXOS.OS;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using guideXOS.Graph;
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
        /// File Explorer window
        /// </summary>
        static ComputerFiles compFiles;
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
            _dirCacheDirty = true;
            _dirCacheFor = null;
            _dirCache = null;
            compFiles = null;
        }
        /// <summary>
        /// Bar Height
        /// </summary>
        const int BarHeight = 40;
        static List<FileInfo> _dirCache;
        static string _dirCacheFor;
        static bool _dirCacheDirty;

        static void ClearDirCache() {
            if (_dirCache != null) {
                for (int i = 0; i < _dirCache.Count; i++) {
                    _dirCache[i].Dispose();
                }
                _dirCache.Dispose();
                _dirCache = null;
            }
            _dirCacheFor = null;
        }
        /// <summary>
        /// Get Directory Entries
        /// </summary>
        /// <returns></returns>
        static List<FileInfo> GetDirectoryEntries() {
            if (_dirCache == null || _dirCacheDirty || _dirCacheFor == null || _dirCacheFor != Dir) {
                // Dispose previous cache
                ClearDirCache();
                // Refresh cache for current Dir
                _dirCache = File.GetFiles(Dir);
                _dirCacheFor = Dir;
                _dirCacheDirty = false;
            }
            return _dirCache;
        }

        static bool IsMouseOverAnyVisibleWindow() {
            for (int d = 0; d < WindowManager.Windows.Count; d++) {
                if (WindowManager.Windows[d].Visible && WindowManager.Windows[d].IsUnderMouse())
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Update
        /// </summary>
        /// <param name="fileIcon"></param>
        public static void Update(Image fileIcon) {
            var names = GetDirectoryEntries();

            // Precompute frequently used values
            int devide = 60;
            int fw = fileIcon.Width;
            int fh = fileIcon.Height;
            int screenH = Framebuffer.Graphics.Height;
            int x = devide;
            int y = devide;

            // Compute clickability once per frame
            bool leftDown = Control.MouseButtons.HasFlag(MouseButtons.Left);
            bool mouseBlocked = WindowManager.HasWindowMoving || WindowManager.MouseHandled || IsMouseOverAnyVisibleWindow();
            bool clickable = leftDown && !mouseBlocked;

            // Add a Computer Files icon at root
            if (IsAtRoot) {
                // Draw Computer Files icon and label
                if (y + fh + devide > screenH - devide) { y = devide; x += fw + devide; }
                // Use folder icon as placeholder
                Framebuffer.Graphics.DrawImage(x, y, Icons.FolderIcon);
                string cf = "Computer Files";
                WindowManager.font.DrawString(x, y + fh, cf, fw + 8, WindowManager.font.FontSize * 3);
                if (clickable && Control.MousePosition.X > x && Control.MousePosition.X < x + Icons.FileIcon.Width && Control.MousePosition.Y > y && Control.MousePosition.Y < y + Icons.FileIcon.Height) {
                    if (compFiles == null) compFiles = new ComputerFiles(300, 200, 540, 380);
                    else compFiles.Visible = true;
                    WindowManager.MoveToEnd(compFiles);
                }
                y += Icons.FileIcon.Height + devide;
            }

            if (IsAtRoot) {
                for (int i = 0; i < Apps.Length; i++) {
                    if (y + fh + devide > screenH - devide) {
                        y = devide;
                        x += fw + devide;
                    }
                    ClickEvent(Apps.Name(i), false, x, y, i, clickable, leftDown);
                    Framebuffer.Graphics.DrawImage(x, y, Apps.Icon(i));
                    WindowManager.font.DrawString(x, y + fh, Apps.Name(i), fw + 8, WindowManager.font.FontSize * 3);
                    y += Icons.FileIcon.Height + devide;
                }
            }

            for (int i = 0; i < names.Count; i++) {
                if (y + fh + devide > screenH - devide) {
                    y = devide;
                    x += fw + devide;
                }
                string n = names[i].Name;
                bool isDir = names[i].Attribute == FileAttribute.Directory;

                ClickEvent(n, isDir, x, y, i + (IsAtRoot ? Apps.Length : 0), clickable, leftDown);

                // Choose icon by extension/type
                if (n.EndsWith(".png") || n.EndsWith(".bmp")) {
                    Framebuffer.Graphics.DrawImage(x, y, Icons.IamgeIcon);
                } else if (n.EndsWith(".wav")) {
                    Framebuffer.Graphics.DrawImage(x, y, Icons.AudioIcon);
                } else if (isDir) {
                    Framebuffer.Graphics.DrawImage(x, y, Icons.FolderIcon);
                } else {
                    Framebuffer.Graphics.DrawImage(x, y, fileIcon);
                }
                WindowManager.font.DrawString(x, y + fh, n, fw + 8, WindowManager.font.FontSize * 3);
                y += fh + devide;
            }

            // Selection marquee with a single normalized rect draw
            if (leftDown && !WindowManager.HasWindowMoving && !WindowManager.MouseHandled) {
                int mx = Control.MousePosition.X;
                int my = Control.MousePosition.Y;
                if (LastPoint.X == -1 && LastPoint.Y == -1) {
                    LastPoint.X = mx;
                    LastPoint.Y = my;
                } else {
                    int rx = LastPoint.X < mx ? LastPoint.X : mx;
                    int ry = LastPoint.Y < my ? LastPoint.Y : my;
                    int rw = (LastPoint.X < mx ? mx - LastPoint.X : LastPoint.X - mx);
                    int rh = (LastPoint.Y < my ? my - LastPoint.Y : LastPoint.Y - my);
                    Framebuffer.Graphics.AFillRectangle(rx, ry, rw, rh, 0x7F2E86C1);
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
        /// <param name="clickable"></param>
        /// <param name="leftDown"></param>
        private static void ClickEvent(string name, bool isDirectory, int X, int Y, int i, bool clickable, bool leftDown) {
            if (leftDown) {
                if (!WindowManager.HasWindowMoving && clickable && !ClickLock &&
                    Control.MousePosition.X > X && Control.MousePosition.X < X + Icons.FileIcon.Width &&
                    Control.MousePosition.Y > Y && Control.MousePosition.Y < Y + Icons.FileIcon.Height) {
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

                // Mark directory cache dirty so it refreshes next frame
                _dirCacheDirty = true;
                IndexClicked = -1;
                //guideXOS.GUI.NotificationManager.Add(new Nofity("New Dir: " + Dir));
            } else if (name.EndsWith(".png")) {
                byte[] buffer = File.ReadAllBytes(path);
                PNG png = new(buffer);
                buffer.Dispose();
                imageViewer.SetImage(png);
                png.Dispose();
                WindowManager.MoveToEnd(imageViewer);
                imageViewer.Visible = true;
            } else if (name.EndsWith(".bmp")) {
                byte[] buffer = File.ReadAllBytes(path);
                Bitmap png = new(buffer);
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