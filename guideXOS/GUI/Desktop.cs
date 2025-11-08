using guideXOS.DefaultApps;
using guideXOS.FS;
using guideXOS.Kernel.Drivers;
using guideXOS.Misc;
using guideXOS.OS;
using System.Collections.Generic;
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
        /// Home mode: when true, show app icons and special desktop icons. When false, show real filesystem entries for Dir.
        /// </summary>
        public static bool HomeMode;
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
            HomeMode = true;
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
        /*
        private static bool ShouldHideAppOnDesktop(string name) {
            // hide these app icons only on desktop
            // compare by lowercase name for safety
            string n = name;
            // Build a simple lowercase comparable copy
            int len = n.Length;
            bool match = false;
            // quick helpers
            bool Eq(string a) {
                if (a.Length != len) return false;
                for (int i = 0; i < len; i++) {
                    char ca = n[i]; if (ca >= 'A' && ca <= 'Z') ca = (char)(ca + 32);
                    char cb = a[i]; if (cb >= 'A' && cb <= 'Z') cb = (char)(cb + 32);
                    if (ca != cb) return false;
                }
                return true;
            }
            if (Eq("lock") || Eq("notepad") || Eq("task manager") || Eq("start menu") || Eq("monitor") || Eq("console") || Eq("paint") || Eq("clock") || Eq("calculator")) match = true;
            return match;
        }
        */

        /// <summary>
        /// Update
        /// </summary>
        /// <param name="DocumentIcon"></param>
        public static void Update(Image DocumentIcon) {
            var names = GetDirectoryEntries();

            // Precompute frequently used values
            int devide = 60;
            int fw = DocumentIcon.Width;
            int fh = DocumentIcon.Height;
            int screenH = Framebuffer.Graphics.Height;
            int x = devide;
            int y = devide;

            // Compute clickability once per frame
            bool leftDown = Control.MouseButtons.HasFlag(MouseButtons.Left);
            bool mouseOverWindow = IsMouseOverAnyVisibleWindow();
            bool mouseBlocked = WindowManager.HasWindowMoving || WindowManager.MouseHandled || mouseOverWindow;
            bool clickable = leftDown && !mouseBlocked;

            // If mouse is pressed over any window, skip desktop hit-testing to avoid latency
            if (leftDown && mouseOverWindow) clickable = false;

            
            if (HomeMode) {
                /*
                // Draw Apps
                for (int i = 0; i < Apps.Length; i++) {
                    string appName = Apps.Name(i);
                    //bool hide = ShouldHideAppOnDesktop(appName);
                    //if (!hide) {
                    if (y + fh + devide > screenH - devide) { y = devide; x += fw + devide; }
                    ClickEvent(appName, false, x, y, i, clickable, leftDown);
                    Framebuffer.Graphics.DrawImage(x, y, Apps.Icon(i));
                    WindowManager.font.DrawString(x, y + fh, appName, fw + 8, WindowManager.font.FontSize * 3);
                    y += Icons.DocumentIcon.Height + devide;
                    //}
                    appName.Dispose();
                }
                */
                // Special desktop icons: Computer Files and Root
                // Computer Files
                if (y + fh + devide > screenH - devide) { y = devide; x += fw + devide; }
                ClickEvent("Computer Files", false, x, y, Apps.Length, clickable, leftDown);
                // button visual feedback
                {
                    uint col = UI.ButtonFillColor(x, y, Icons.FolderIcon.Width, Icons.FolderIcon.Height, 0xFF2B2B2B, 0xFF343434, 0xFF3A3A3A);
                    Framebuffer.Graphics.FillRectangle(x - 4, y - 4, Icons.FolderIcon.Width + 8, Icons.FolderIcon.Height + 8, col);
                }
                Framebuffer.Graphics.DrawImage(x, y, Icons.FolderIcon);
                WindowManager.font.DrawString(x, y + fh, "Computer Files", fw + 8, WindowManager.font.FontSize * 3);
                y += Icons.DocumentIcon.Height + devide;
                // USB mass storage icons, one per connected device
                if (Kernel.Drivers.USBStorage.Count > 0) {
                    int count = Kernel.Drivers.USBStorage.Count;
                    for (int u = 0; u < count; u++) {
                        if (y + fh + devide > screenH - devide) { y = devide; x += fw + devide; }
                        string label = count == 1 ? "USB Drive" : ("USB Drive " + (u + 1).ToString());
                        ClickEvent(label, true, x, y, 20000 + u, clickable, leftDown);
                        uint col = UI.ButtonFillColor(x, y, Icons.FolderIcon.Width, Icons.FolderIcon.Height, 0xFF2B2B2B, 0xFF343434, 0xFF3A3A3A);
                        Framebuffer.Graphics.FillRectangle(x - 4, y - 4, Icons.FolderIcon.Width + 8, Icons.FolderIcon.Height + 8, col);
                        Framebuffer.Graphics.DrawImage(x, y, Icons.FolderIcon);
                        WindowManager.font.DrawString(x, y + fh, label, fw + 8, WindowManager.font.FontSize * 3);
                        y += Icons.DocumentIcon.Height + devide;
                        label.Dispose();
                    }
                }
                // Root folder
                if (y + fh + devide > screenH - devide) { y = devide; x += fw + devide; }
                ClickEvent("Root", true, x, y, Apps.Length + 1, clickable, leftDown);
                uint colRoot = UI.ButtonFillColor(x, y, Icons.FolderIcon.Width, Icons.FolderIcon.Height, 0xFF2B2B2B, 0xFF343434, 0xFF3A3A3A);
                Framebuffer.Graphics.FillRectangle(x - 4, y - 4, Icons.FolderIcon.Width + 8, Icons.FolderIcon.Height + 8, colRoot);
                Framebuffer.Graphics.DrawImage(x, y, Icons.FolderIcon);
                WindowManager.font.DrawString(x, y + fh, "Root", fw + 8, WindowManager.font.FontSize * 3);
                y += Icons.DocumentIcon.Height + devide;
            }

            // Show real filesystem entries only when not in HomeMode
            if (!HomeMode) {
                // Add special Desktop shortcut to return home
                if (y + fh + devide > screenH - devide) { y = devide; x += fw + devide; }
                ClickEvent("Desktop", false, x, y, -100, clickable, leftDown);
                uint cDesk = UI.ButtonFillColor(x, y, Icons.FolderIcon.Width, Icons.FolderIcon.Height, 0xFF2B2B2B, 0xFF343434, 0xFF3A3A3A);
                Framebuffer.Graphics.FillRectangle(x - 4, y - 4, Icons.FolderIcon.Width + 8, Icons.FolderIcon.Height + 8, cDesk);
                Framebuffer.Graphics.DrawImage(x, y, Icons.FolderIcon);
                WindowManager.font.DrawString(x, y + fh, "Desktop", fw + 8, WindowManager.font.FontSize * 3);
                y += fh + devide;

                for (int i = 0; i < names.Count; i++) {
                    if (y + fh + devide > screenH - devide) { y = devide; x += fw + devide; }
                    string n = names[i].Name;
                    bool isDir = names[i].Attribute == FileAttribute.Directory;

                    ClickEvent(n, isDir, x, y, i + 1000, clickable, leftDown);

                    uint bg = UI.ButtonFillColor(x, y, Icons.DocumentIcon.Width, Icons.DocumentIcon.Height, 0xFF2B2B2B, 0xFF343434, 0xFF3A3A3A);
                    Framebuffer.Graphics.FillRectangle(x - 4, y - 4, Icons.DocumentIcon.Width + 8, Icons.DocumentIcon.Height + 8, bg);

                    // Choose icon by extension/use type
                    if (n.EndsWith(".png") || n.EndsWith(".bmp")) {
                        Framebuffer.Graphics.DrawImage(x, y, Icons.ImageIcon);
                    } else if (n.EndsWith(".wav")) {
                        Framebuffer.Graphics.DrawImage(x, y, Icons.AudioIcon);
                    } else if (isDir) {
                        Framebuffer.Graphics.DrawImage(x, y, Icons.FolderIcon);
                    } else {
                        Framebuffer.Graphics.DrawImage(x, y, DocumentIcon);
                    }
                    WindowManager.font.DrawString(x, y + fh, n, fw + 8, WindowManager.font.FontSize * 3);
                    y += fh + devide;
                }
            }

            // Selection marquee with a single normalized rect draw
            if (leftDown && !WindowManager.HasWindowMoving && !WindowManager.MouseHandled && !mouseOverWindow) {
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
                    Control.MousePosition.X > X && Control.MousePosition.X < X + Icons.DocumentIcon.Width &&
                    Control.MousePosition.Y > Y && Control.MousePosition.Y < Y + Icons.DocumentIcon.Height) {
                    IndexClicked = i;
                    OnClick(name, isDirectory, X, Y);
                }
            } else {
                ClickLock = false;
            }

            if (IndexClicked == i) {
                int w = (int)(Icons.DocumentIcon.Width * 1.5f);
                Framebuffer.Graphics.AFillRectangle(X + ((Icons.DocumentIcon.Width / 2) - (w / 2)), Y, w, Icons.DocumentIcon.Height * 2, 0x7F2E86C1);
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
            ClickLock = true;
            // Special desktop controls
            if (name == "Root" && HomeMode) {
                HomeMode = false;
                _dirCacheDirty = true;
                IndexClicked = -1;
                return;
            }
            if (name == "Computer Files" && HomeMode) {
                var cf = new ComputerFiles(300, 200, 540, 380);
                WindowManager.MoveToEnd(cf);
                cf.Visible = true;
                return;
            }
            // Click on USB drive icon opens Computer Files too (when on Home desktop)
            if (HomeMode) {
                string usbPrefix = "USB Drive";
                bool isUsb = name.Length >= usbPrefix.Length;
                if (isUsb) {
                    for (int pi = 0; pi < usbPrefix.Length; pi++) {
                        if (name[pi] != usbPrefix[pi]) { isUsb = false; break; }
                    }
                }
                usbPrefix.Dispose();
                if (isUsb) {
                    var cf = new ComputerFiles(300, 200, 540, 380);
                    WindowManager.MoveToEnd(cf);
                    cf.Visible = true;
                    return;
                }
            }
            if (name == "Desktop" && !HomeMode) {
                HomeMode = true;
                if (Dir.Length != 0) {
                    Dir.Dispose();
                }
                Dir = "";
                _dirCacheDirty = true;
                IndexClicked = -1;
                return;
            }

            string devider = "/";
            string path = Dir + name;
            if (isDirectory) {
                string newd = Dir + name + devider;
                Dir.Dispose();
                Dir = newd;
                _dirCacheDirty = true;
                IndexClicked = -1;
            } else if (name.EndsWith(".png")) {
                byte[] buffer = File.ReadAllBytes(path);
                PNG png = new(buffer);
                buffer.Dispose();
                imageViewer.SetImage(png);
                png.Dispose();
                WindowManager.MoveToEnd(imageViewer);
                imageViewer.Visible = true;
                RecentManager.AddDocument(path, Icons.ImageIcon);
            } else if (name.EndsWith(".bmp")) {
                byte[] buffer = File.ReadAllBytes(path);
                Bitmap png = new(buffer);
                buffer.Dispose();
                imageViewer.SetImage(png);
                png.Dispose();
                WindowManager.MoveToEnd(imageViewer);
                imageViewer.Visible = true;
                RecentManager.AddDocument(path, Icons.ImageIcon);
            } else if (name.EndsWith(".gxm") || name.EndsWith(".mue")) {
                byte[] buffer = File.ReadAllBytes(path);
                // Prefer in-kernel GXM loader for execution
                string err; bool ok = GXMLoader.TryExecute(buffer, out err);
                if (!ok) {
                    msgbox.X = itemX + 60; msgbox.Y = itemY + 60; msgbox.SetText(err ?? "Failed to run executable"); WindowManager.MoveToEnd(msgbox); msgbox.Visible = true;
                } else {
                    RecentManager.AddDocument(path, Icons.DocumentIcon);
                }
            } else if (name.EndsWith(".wav")) {
                if (Audio.HasAudioDevice) {
                    wavplayer.Visible = true;
                    byte[] buffer = File.ReadAllBytes(path);
                    unsafe {
                        fixed (char* ptr = name)
                            wavplayer.Play(buffer, new string(ptr));
                    }
                    RecentManager.AddDocument(path, Icons.AudioIcon);
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
        /// <summary>
        /// Invalidate Directory Cache
        /// </summary>
        public static void InvalidateDirCache() { _dirCacheDirty = true; }
    }
}