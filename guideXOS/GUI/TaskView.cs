using guideXOS.Graph;
using guideXOS.Kernel.Drivers;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace guideXOS.GUI {
    // GNOME-like Workspace Switcher overlay replacing Task View
    internal class WorkspaceSwitcher : Window {
        private struct Tile { public Window Win; public int X, Y, W, H; public int Workspace; }
        private readonly List<Tile> _tiles = new List<Tile>(64);
        private bool _clickLatch = false;
        private bool _rightClickLatch = false;
        private int _hoverWorkspace = -1;

        public WorkspaceSwitcher() : base(0, 40, Framebuffer.Width, Framebuffer.Height - 40) {
            Title = "Workspace Switcher";
            BarHeight = 0; ShowInTaskbar = false; ShowMaximize = false; ShowMinimize = false;
        }

        public override void OnInput() {
            base.OnInput(); if (!Visible) return;
            // Close on Escape
            if (Keyboard.KeyInfo.Key == System.ConsoleKey.Escape) { Visible = false; return; }

            int mx = Control.MousePosition.X; int my = Control.MousePosition.Y;
            bool left = Control.MouseButtons.HasFlag(MouseButtons.Left);
            bool right = Control.MouseButtons.HasFlag(MouseButtons.Right);

            // Region constants (keep in sync with OnDraw)
            int sideX = 16; int sideW = 100; int sideY = 60; int sideItemH = 46; int sideGap = 10;
            int nWorkspaces = WorkspaceManager.Count;

            if (left) {
                if (_clickLatch) return;
                _clickLatch = true;
                bool handled = false;
                // Check window tiles first
                for (int i = 0; i < _tiles.Count; i++) {
                    var t = _tiles[i];
                    if (mx >= t.X && mx <= t.X + t.W && my >= t.Y && my <= t.Y + t.H) {
                        if (t.Win != null) {
                            if (t.Workspace != WorkspaceManager.Current) WorkspaceManager.SwitchTo(t.Workspace);
                            if (t.Win.IsMinimized) t.Win.Restore();
                            WindowManager.MoveToEnd(t.Win);
                            t.Win.Visible = true; Visible = false; return;
                        }
                    }
                }
                // Click in workspace sidebar selects workspace
                for (int i = 0; i < nWorkspaces; i++) {
                    int iy = sideY + i * (sideItemH + sideGap);
                    if (mx >= sideX && mx <= sideX + sideW && my >= iy && my <= iy + sideItemH) {
                        WorkspaceManager.SwitchTo(i);
                        Visible = false; return; // auto-close after switch
                    }
                }
                // Add workspace button
                int addY = sideY + nWorkspaces * (sideItemH + sideGap);
                if (mx >= sideX && mx <= sideX + sideW && my >= addY && my <= addY + sideItemH) {
                    if (WorkspaceManager.AddWorkspace()) {
                        WorkspaceManager.SwitchTo(WorkspaceManager.Count - 1); // jump to new workspace
                    }
                    Visible = false; return; // close overlay after add
                }
                // Background click outside side bar and tiles -> close
                if (!handled) {
                    bool inSidebar = (mx >= sideX && mx <= sideX + sideW && my >= sideY && my <= addY + sideItemH);
                    bool inTile = false;
                    for (int i = 0; i < _tiles.Count; i++) {
                        var t = _tiles[i]; if (mx >= t.X && mx <= t.X + t.W && my >= t.Y && my <= t.Y + t.H) { inTile = true; break; }
                    }
                    if (!inSidebar && !inTile) { Visible = false; return; }
                }
            } else { _clickLatch = false; }

            // Right click: move hovered window to hovered/next workspace
            if (right) {
                if (_rightClickLatch) return;
                _rightClickLatch = true;
                int hoveredIndex = -1;
                for (int i = 0; i < _tiles.Count; i++) {
                    var t = _tiles[i]; if (mx >= t.X && mx <= t.X + t.W && my >= t.Y && my <= t.Y + t.H) { hoveredIndex = i; break; }
                }
                if (hoveredIndex >= 0) {
                    var t = _tiles[hoveredIndex];
                    int target = (_hoverWorkspace >= 0) ? _hoverWorkspace : (WorkspaceManager.Current + 1 < WorkspaceManager.Count ? WorkspaceManager.Current + 1 : WorkspaceManager.Current);
                    if (target != WorkspaceManager.GetWorkspace(t.Win)) {
                        WorkspaceManager.MoveWindowToWorkspace(t.Win, target);
                    }
                }
            } else { _rightClickLatch = false; }
        }

        public override void OnDraw() {
            if (!Visible) return;
            // Dim entire background slightly with blur
            Framebuffer.Graphics.BlurRectangle(0, 0, Framebuffer.Width, Framebuffer.Height, 4);
            Framebuffer.Graphics.AFillRectangle(0, 0, Framebuffer.Width, Framebuffer.Height, 0xAA0F0F12);

            WorkspaceManager.EnsureAllWindowsTracked();
            _tiles.Clear();

            // Sidebar: list of workspaces like GNOME on the right, but we'll place left for convenience
            int sideX = 16; int sideW = 100; int sideY = 60; int sideItemH = 46; int sideGap = 10;
            int n = WorkspaceManager.Count; int cur = WorkspaceManager.Current;

            int mx = Control.MousePosition.X; int my = Control.MousePosition.Y;
            _hoverWorkspace = -1;

            // Precompute counts once
            int[] counts = WorkspaceManager.WorkspaceWindowCounts();

            for (int i = 0; i < n; i++) {
                int iy = sideY + i * (sideItemH + sideGap);
                bool hover = (mx >= sideX && mx <= sideX + sideW && my >= iy && my <= iy + sideItemH);
                if (hover) _hoverWorkspace = i;
                uint bg = (i == cur) ? 0xFF2E89FFu : hover ? 0xFF333333u : 0xFF232323u;
                Framebuffer.Graphics.FillRectangle(sideX, iy, sideW, sideItemH, bg);
                Framebuffer.Graphics.DrawRectangle(sideX, iy, sideW, sideItemH, 0xFF444444, 1);
                string label = "WS " + (i + 1).ToString();
                int tw = WindowManager.font.MeasureString(label);
                WindowManager.font.DrawString(sideX + (sideW - tw) / 2, iy + (sideItemH - WindowManager.font.FontSize) / 2, label);
                // small dots indicating window count
                int dots = (i < counts.Length) ? counts[i] : 0;
                int dotW = 3; int dotGap = 2; int totalW = dots > 0 ? dots * dotW + (dots - 1) * dotGap : 0;
                int dx = sideX + (sideW - totalW) / 2; int dy = iy + sideItemH - 10;
                for (int d = 0; d < dots; d++) Framebuffer.Graphics.FillRectangle(dx + d * (dotW + dotGap), dy, dotW, dotW, 0xFFAAAAAA);
            }
            // Add workspace button below
            int addY = sideY + n * (sideItemH + sideGap);
            Framebuffer.Graphics.FillRectangle(sideX, addY, sideW, sideItemH, 0xFF1E1E1E);
            Framebuffer.Graphics.DrawRectangle(sideX, addY, sideW, sideItemH, 0xFF444444, 1);
            string add = "+ Add"; int atw = WindowManager.font.MeasureString(add);
            WindowManager.font.DrawString(sideX + (sideW - atw) / 2, addY + (sideItemH - WindowManager.font.FontSize) / 2, add);

            // Main area: grid of windows in current and adjacent workspaces for context
            int areaX = sideX + sideW + 20; int areaY = 60;
            int areaW = Framebuffer.Width - areaX - 20; int areaH = Framebuffer.Height - areaY - 30;

            DrawWorkspaceGrid(areaX, areaY, areaW, areaH, cur);
            if (cur + 1 < n) DrawWorkspaceMini(areaX, areaY + areaH + 6, areaW, 20, cur + 1); // small strip hint
        }

        private void DrawWorkspaceGrid(int x, int y, int w, int h, int wsIndex) {
            // Determine grid based on count
            var wins = WindowManager.Windows; int count = 0;
            for (int i = 0; i < wins.Count; i++) if (wins[i].ShowInTaskbar && WorkspaceManager.GetWorkspace(wins[i]) == wsIndex) count++;
            if (count == 0) {
                // friendly message
                string msg = "No windows in this workspace";
                int tw = WindowManager.font.MeasureString(msg);
                WindowManager.font.DrawString(x + (w - tw) / 2, y + (h - WindowManager.font.FontSize) / 2, msg);
                return;
            }

            int pad = 16; int columns = 3;
            int tileW = (w - pad * (columns + 1)) / columns; if (tileW < 180) tileW = 180;
            int rows = (count + columns - 1) / columns;
            int tileH = (h - pad * (rows + 1)) / (rows == 0 ? 1 : rows); if (tileH < 120) tileH = 120;
            int idx = 0;
            for (int i = 0; i < wins.Count; i++) {
                var win = wins[i]; if (!win.ShowInTaskbar) continue; if (WorkspaceManager.GetWorkspace(win) != wsIndex) continue;
                int col = idx % columns; int row = idx / columns; idx++;
                int tx = x + pad + col * (tileW + pad);
                int ty = y + pad + row * (tileH + pad);
                DrawWindowTile(win, tx, ty, tileW, tileH, wsIndex);
            }
        }

        private void DrawWorkspaceMini(int x, int y, int w, int h, int wsIndex) {
            // Subtle preview strip for next workspace
            Framebuffer.Graphics.AFillRectangle(x, y, w, h, 0x44222222);
            string label = "Next: WS " + (wsIndex + 1).ToString();
            WindowManager.font.DrawString(x + 8, y + (h - WindowManager.font.FontSize) / 2, label);
        }

        private void DrawWindowTile(Window w, int x, int y, int tw, int th, int ws) {
            // Card background with accent line if minimized
            uint bg = 0xCC1E1E1E; uint border = 0xFF3A3A3A; uint accent = w.IsMinimized ? 0xFF8E44ADu : 0xFF2E89FFu;
            Framebuffer.Graphics.AFillRectangle(x, y, tw, th, bg);
            Framebuffer.Graphics.DrawRectangle(x, y, tw, th, border, 1);
            Framebuffer.Graphics.FillRectangle(x, y, tw, 3, accent);
            // Title and icon
            var icon = w.TaskbarIcon ?? Icons.DocumentIcon(32);
            int titleY = y + 8;
            Framebuffer.Graphics.DrawImage(x + 8, titleY, icon);
            int tx = x + 8 + icon.Width + 6;
            WindowManager.font.DrawString(tx, titleY + (icon.Height / 2) - (WindowManager.font.FontSize / 2), w.Title, tw - (tx - x) - 10, WindowManager.font.FontSize);
            // simple preview border box
            int pvX = x + 8; int pvY = y + 8 + icon.Height + 8; int pvW = tw - 16; int pvH = th - (icon.Height + 8 + 16);
            if (pvW > 10 && pvH > 10) {
                Framebuffer.Graphics.DrawRectangle(pvX, pvY, pvW, pvH, 0xFF555555, 1);
            }
            _tiles.Add(new Tile { Win = w, X = x, Y = y, W = tw, H = th, Workspace = ws });
        }
    }
}
