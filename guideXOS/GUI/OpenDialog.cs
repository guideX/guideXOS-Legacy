using guideXOS.FS;
using guideXOS.Kernel.Drivers;
using guideXOS.Misc;
using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace guideXOS.GUI {
    internal class OpenDialog : Window {
        private string _currentPath;
        private List<FileInfo> _entries;
        private int _selectedIndex;
        private bool _clickLock;
        private readonly Action<string> _onOpen;
        private int _padding = 10;
        private int _rowH = 28;
        private int _btnW = 80;
        private int _btnH = 26;

        public OpenDialog(int x, int y, int w, int h, string startPath, Action<string> onOpen) : base(x, y, w, h) {
            Title = "Open";
            _currentPath = startPath ?? "";
            _entries = new List<FileInfo>();
            _selectedIndex = -1;
            _clickLock = false;
            _onOpen = onOpen;
            RefreshEntries();
        }

        private void RefreshEntries() {
            if (_entries != null) { for (int i = 0; i < _entries.Count; i++) _entries[i].Dispose(); _entries.Clear(); }
            _entries = File.GetFiles(_currentPath);
            _selectedIndex = -1;
        }

        private void GoUp() {
            if (string.IsNullOrEmpty(_currentPath)) return;
            string path = _currentPath;
            if (path.Length > 0 && path[path.Length - 1] == '/') path = path.Substring(0, path.Length - 1);
            int last = path.LastIndexOf('/');
            _currentPath = last >= 0 ? path.Substring(0, last + 1) : "";
            RefreshEntries();
        }

        private void OpenSelected() {
            if (_selectedIndex < 0 || _selectedIndex >= _entries.Count) return;
            var e = _entries[_selectedIndex];
            if (e.Attribute == FileAttribute.Directory) {
                _currentPath = _currentPath + e.Name + "/";
                RefreshEntries();
            } else {
                string path = _currentPath + e.Name;
                _onOpen?.Invoke(path);
                path.Dispose();
                this.Visible = false;
            }
        }

        public override void OnInput() {
            base.OnInput();
            bool left = Control.MouseButtons.HasFlag(MouseButtons.Left);
            int mx = Control.MousePosition.X; int my = Control.MousePosition.Y;
            int cx = X + _padding; int cy = Y + _padding + 28; int listX = cx; int listY = cy + 24; int listW = Width - _padding * 2; int listH = Height - _padding * 2 - 60;
            int upW = 60; int upH = 22; int upX = cx; int upY = cy;
            int openX = X + Width - _padding - _btnW; int openY = Y + Height - _padding - _btnH;
            int cancelX = openX - 8 - _btnW; int cancelY = openY;

            if (left) {
                if (!_clickLock) {
                    if (mx >= upX && mx <= upX + upW && my >= upY && my <= upY + upH) { GoUp(); _clickLock = true; return; }
                    if (mx >= openX && mx <= openX + _btnW && my >= openY && my <= openY + _btnH) { OpenSelected(); _clickLock = true; return; }
                    if (mx >= cancelX && mx <= cancelX + _btnW && my >= cancelY && my <= cancelY + _btnH) { this.Visible = false; _clickLock = true; return; }
                    if (mx >= listX && mx <= listX + listW && my >= listY && my <= listY + listH) {
                        int idx = (my - listY) / _rowH;
                        if (idx >= 0 && idx < _entries.Count) { _selectedIndex = idx; _clickLock = true; return; }
                    }
                }
            } else { _clickLock = false; }
        }

        public override void OnDraw() {
            base.OnDraw();
            int cx = X + _padding; int cy = Y + _padding + 28; int listX = cx; int listY = cy + 24; int listW = Width - _padding * 2; int listH = Height - _padding * 2 - 60;
            // Toolbar (more opaque)
            Framebuffer.Graphics.FillRectangle(cx, cy, listW, 22, 0xFF3A3A3A);
            // Up
            int upW = 60; int upH = 22; Framebuffer.Graphics.FillRectangle(cx, cy, upW, upH, 0xFF4A4A4A); WindowManager.font.DrawString(cx + 8, cy + 4, "Up");
            // Path
            WindowManager.font.DrawString(cx + upW + 8, cy + 4, _currentPath ?? "");

            // List background (opaque)
            Framebuffer.Graphics.FillRectangle(listX, listY, listW, listH, 0xFF2B2B2B);
            int y = listY; int iconW = Icons.FileIcon.Width; int iconH = Icons.FileIcon.Height;
            for (int i = 0; i < _entries.Count; i++) {
                var e = _entries[i];
                uint row = (i == _selectedIndex) ? 0xFF404040u : ((i & 1) == 0 ? 0xFF303030u : 0xFF2B2B2Bu);
                Framebuffer.Graphics.FillRectangle(listX, y, listW, _rowH, row);
                var icon = (e.Attribute == FileAttribute.Directory) ? Icons.FolderIcon : Icons.FileIcon;
                Framebuffer.Graphics.DrawImage(listX + 4, y + (_rowH / 2 - iconH / 2), icon);
                WindowManager.font.DrawString(listX + 8 + iconW, y + (_rowH / 2 - WindowManager.font.FontSize / 2), e.Name);
                y += _rowH; if (y > listY + listH - _rowH) break;
            }

            int openX = X + Width - _padding - _btnW; int openY = Y + Height - _padding - _btnH; int cancelX = openX - 8 - _btnW;
            Framebuffer.Graphics.FillRectangle(openX, openY, _btnW, _btnH, 0xFF3A3A3A); WindowManager.font.DrawString(openX + 16, openY + (_btnH / 2 - WindowManager.font.FontSize / 2), "Open");
            Framebuffer.Graphics.FillRectangle(cancelX, openY, _btnW, _btnH, 0xFF3A3A3A); WindowManager.font.DrawString(cancelX + 8, openY + (_btnH / 2 - WindowManager.font.FontSize / 2), "Cancel");
        }
    }
}
