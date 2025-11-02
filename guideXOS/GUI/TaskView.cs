using guideXOS.Graph;
using guideXOS.Kernel.Drivers;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace guideXOS.GUI {
    // Simple Task View / Workspace switcher overlay
    internal class TaskView : Window {
        private struct Tile { public Window Win; public int X, Y, W, H; }
        private List<Tile> _tiles = new List<Tile>(32);
        private bool _clickLatch = false;

        public TaskView() : base(0, 40, Framebuffer.Width, Framebuffer.Height - 40) {
            Title = "Task View";
            BarHeight = 0; ShowInTaskbar = false; ShowMaximize = false; ShowMinimize = false;
        }

        public override void OnInput() {
            base.OnInput(); if (!Visible) return;
            // Close on Escape
            if (Keyboard.KeyInfo.Key == System.ConsoleKey.Escape) { Visible = false; return; }

            int mx = Control.MousePosition.X; int my = Control.MousePosition.Y;
            bool left = Control.MouseButtons.HasFlag(MouseButtons.Left);
            if (left) {
                if (_clickLatch) return;
                for (int i = 0; i < _tiles.Count; i++) {
                    var t = _tiles[i];
                    if (mx >= t.X && mx <= t.X + t.W && my >= t.Y && my <= t.Y + t.H) {
                        var w = t.Win;
                        if (w != null) {
                            if (w.IsMinimized) w.Restore();
                            WindowManager.MoveToEnd(w);
                            w.Visible = true;
                            // Hide task view
                            this.Visible = false;
                            _clickLatch = true;
                            return;
                        }
                    }
                }
            } else {
                _clickLatch = false;
            }
        }

        public override void OnDraw() {
            if (!Visible) return;
            // Dim entire background slightly
            Framebuffer.Graphics.AFillRectangle(0, 0, Framebuffer.Width, Framebuffer.Height, 0xAA000000);

            _tiles.Clear();
            // Build window tiles for all taskbar windows
            List<Window> wins = WindowManager.Windows;
            // Determine grid
            int count = 0; for (int i = 0; i < wins.Count; i++) if (wins[i].ShowInTaskbar) count++;
            if (count == 0) { DrawBorder(false); return; }
            int pad = 16; int tileW = 280; int tileH = 180; // base tile size
            int columns = Framebuffer.Width / (tileW + pad);
            if (columns < 1) columns = 1;
            int rows = (count + columns - 1) / columns;
            // Adjust tile size to fit if necessary
            int availW = Framebuffer.Width - pad * (columns + 1);
            tileW = availW / columns;
            if (tileW > 360) tileW = 360;
            int availH = Framebuffer.Height - pad * (rows + 1) - 40; // account for bar
            tileH = rows > 0 ? (availH / rows) : 180; if (tileH > 220) tileH = 220; if (tileH < 120) tileH = 120;

            int idx = 0;
            for (int i = 0; i < wins.Count; i++) {
                var w = wins[i]; if (!w.ShowInTaskbar) continue;
                int col = idx % columns; int row = idx / columns; idx++;
                int x = pad + col * (tileW + pad);
                int y = 40 + pad + row * (tileH + pad);
                // Card background
                Framebuffer.Graphics.AFillRectangle(x, y, tileW, tileH, 0xCC222222);
                Framebuffer.Graphics.DrawRectangle(x, y, tileW, tileH, 0xFF444444, 1);
                // Title and icon
                var icon = w.TaskbarIcon ?? Icons.DocumentIcon;
                int titleY = y + 8;
                Framebuffer.Graphics.DrawImage(x + 8, titleY, icon);
                int tx = x + 8 + icon.Width + 6;
                WindowManager.font.DrawString(tx, titleY + (icon.Height/2) - (WindowManager.font.FontSize/2), w.Title, tileW - (tx - x) - 10, WindowManager.font.FontSize);
                // Window bounds mini-outline
                int previewX = x + 8; int previewY = y + 8 + icon.Height + 8;
                int previewW = tileW - 16; int previewH = tileH - (icon.Height + 8 + 16);
                if (previewW > 10 && previewH > 10) {
                    Framebuffer.Graphics.DrawRectangle(previewX, previewY, previewW, previewH, 0xFF666666, 1);
                }
                // Minimized indicator
                if (w.IsMinimized) {
                    WindowManager.font.DrawString(x + tileW - 70, y + 8, "minimized");
                }
                _tiles.Add(new Tile { Win = w, X = x, Y = y, W = tileW, H = tileH });
            }
        }
    }
}
