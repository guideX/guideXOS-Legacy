using guideXOS.FS;
using guideXOS.Kernel.Drivers;
using guideXOS.Graph;
using System.Windows.Forms;
using guideXOS.Misc;
using System.Collections.Generic;

namespace guideXOS.GUI {
    // File explorer window with drive list, navigation toolbar, grid icons, resizing and vertical scrollbar
    internal class ComputerFiles : Window {
        private string _currentPath = ""; // empty means root of current drive (FileSystem)
        private bool _showDrives = true;

        // History
        private string[] _history = new string[64];
        private int _historyCount = 0;
        private int _historyIndex = -1;

        // Grid/icon sizes (available set)
        private int[] _sizes = new int[] { 16, 24, 32, 48, 128 };
        private int _sizeIndex = 3; // default 48px
        private System.Drawing.Image _iconFolder;
        private System.Drawing.Image _iconDoc;

        // Scrolling
        private int _scroll;
        private bool _scrollDrag;
        private int _scrollDragStartY;
        private int _scrollDragStartScroll;

        // Resizing
        private bool _resizing;
        private int _resizeStartMouseX, _resizeStartMouseY;
        private int _resizeStartW, _resizeStartH;
        private const int ResizeHandle = 14;

        // Cached entries for performance
        private List<FileInfo> _entriesCache;
        private string _entriesCacheFor;
        private bool _entriesDirty;

        public ComputerFiles(int X, int Y, int W = 640, int H = 480) : base(X, Y, W, H) {
            Title = "Computer Files";
            LoadIcons();
            _entriesDirty = true;
            _entriesCacheFor = null;
            _entriesCache = null;
            ShowInTaskbar = true;
        }

        private void LoadIcons() {
            int px = _sizes[_sizeIndex];
            // Match the rest of the project’s asset paths: relative to Ramdisk, capital 'Images'
            string folderPath = $"Images/BlueVelvet/{px}/folder.png";
            string docPath = $"Images/BlueVelvet/{px}/documents.png";
            try { _iconFolder = new PNG(File.ReadAllBytes(folderPath)); } catch { _iconFolder = Icons.FolderIcon; }
            try { _iconDoc = new PNG(File.ReadAllBytes(docPath)); } catch { _iconDoc = Icons.FileIcon; }
        }

        private void ClearEntriesCache() {
            if (_entriesCache != null) {
                for (int i = 0; i < _entriesCache.Count; i++) _entriesCache[i].Dispose();
                _entriesCache.Dispose();
                _entriesCache = null;
            }
            _entriesCacheFor = null;
        }

        private void EnsureEntries() {
            if (_showDrives) return;
            if (_entriesCache != null && !_entriesDirty && _entriesCacheFor != null && _entriesCacheFor == _currentPath) return;
            // Refresh cache
            ClearEntriesCache();
            Busy.Push();
            _entriesCache = File.GetFiles(_currentPath);
            Busy.Pop();
            _entriesCacheFor = _currentPath;
            _entriesDirty = false;
        }

        private void MarkEntriesDirty() { _entriesDirty = true; }

        private void PushHistory(string path) {
            // Truncate forward history
            if (_historyIndex + 1 < _historyCount) _historyCount = _historyIndex + 1;
            if (_historyCount >= _history.Length) {
                for (int i = 1; i < _historyCount; i++) _history[i - 1] = _history[i];
                _historyCount--; _historyIndex--;
            }
            _history[_historyCount++] = path;
            _historyIndex = _historyCount - 1;
        }

        private bool CanGoBack => _historyIndex > 0;
        private bool CanGoForward => _historyIndex >= 0 && (_historyIndex + 1) < _historyCount;

        private void GoBack() {
            if (!CanGoBack) return;
            _historyIndex--;
            _currentPath = _history[_historyIndex];
            _showDrives = _currentPath == null;
            _scroll = 0;
            MarkEntriesDirty();
        }

        private void GoForward() {
            if (!CanGoForward) return;
            _historyIndex++;
            _currentPath = _history[_historyIndex];
            _showDrives = _currentPath == null;
            _scroll = 0;
            MarkEntriesDirty();
        }

        private void GoUpLevel() {
            if (_showDrives) return;
            if (_currentPath == null) return;
            if (_currentPath.Length == 0) {
                _showDrives = true;
                PushHistory(null);
                _scroll = 0;
                MarkEntriesDirty();
                return;
            }
            string path = _currentPath;
            if (path.Length > 0 && path[path.Length - 1] == '/') path = path.Substring(0, path.Length - 1);
            int last = path.LastIndexOf('/');
            if (last >= 0) path = path.Substring(0, last + 1); else path = "";
            _currentPath = path;
            PushHistory(_currentPath);
            _scroll = 0;
            MarkEntriesDirty();
        }

        public override void OnInput() {
            base.OnInput();
            if (!Visible) return;

            int tbH = WindowManager.font.FontSize + 12;
            int contentX = X + 8;
            int contentY = Y + 8 + tbH;
            int contentW = Width - 16;
            int contentH = Height - 16 - tbH;

            int mx = Control.MousePosition.X;
            int my = Control.MousePosition.Y;

            // Mark mouse handled if inside this window
            if (Control.MouseButtons.HasFlag(MouseButtons.Left) && mx >= X && mx <= X + Width && my >= Y - BarHeight && my <= Y - BarHeight + BarHeight + Height) {
                WindowManager.MouseHandled = true;
            }

            // Resizing in bottom-right handle
            int rhX = X + Width - ResizeHandle;
            int rhY = Y + Height - ResizeHandle;
            if (Control.MouseButtons.HasFlag(MouseButtons.Left)) {
                if (!_resizing && mx >= rhX && mx <= X + Width && my >= rhY && my <= Y + Height) {
                    _resizing = true;
                    _resizeStartMouseX = mx; _resizeStartMouseY = my;
                    _resizeStartW = Width; _resizeStartH = Height;
                    return;
                }
            } else {
                _resizing = false;
            }
            if (_resizing) {
                int dw = mx - _resizeStartMouseX;
                int dh = my - _resizeStartMouseY;
                int newW = _resizeStartW + dw;
                int newH = _resizeStartH + dh;
                if (newW < 280) newW = 280;
                if (newH < 200) newH = 200;
                Width = newW; Height = newH;
                return;
            }

            if (Control.MouseButtons == MouseButtons.Left) {
                // Toolbar buttons
                int btnW = 80; int btnH = WindowManager.font.FontSize + 8; int gap = 6;
                int tbY = Y + 6;
                int bx0 = X + 8;                 // Back
                int bx1 = bx0 + btnW + gap;      // Up Level
                int bx2 = bx1 + btnW + gap;      // Forward

                // Size options start after Forward, add a gap
                int sizeStartX = bx2 + btnW + gap + 10;

                if (mx >= bx0 && mx <= bx0 + btnW && my >= tbY && my <= tbY + btnH) { GoBack(); return; }
                if (mx >= bx1 && mx <= bx1 + btnW && my >= tbY && my <= tbY + btnH) { GoUpLevel(); return; }
                if (mx >= bx2 && mx <= bx2 + btnW && my >= tbY && my <= tbY + btnH) { GoForward(); return; }

                // Size buttons
                int sx = sizeStartX;
                for (int i = 0; i < _sizes.Length; i++) {
                    int w = 36;
                    if (mx >= sx && mx <= sx + w && my >= tbY && my <= tbY + btnH) {
                        _sizeIndex = i;
                        LoadIcons();
                        return;
                    }
                    sx += w + 4;
                }

                // Scrollbar drag start
                int sbW = 10;
                int sbX = X + Width - 6 - sbW;
                if (mx >= sbX && mx <= sbX + sbW && my >= contentY && my <= contentY + contentH) {
                    _scrollDrag = true; _scrollDragStartY = my; _scrollDragStartScroll = _scroll; return;
                }

                // Content clicks
                if (_showDrives) {
                    // Single drive tile
                    int tile = (_iconFolder != null ? _iconFolder.Height : 48) + WindowManager.font.FontSize + 16;
                    int rowY = contentY;
                    int rowW = contentW;
                    if (mx >= contentX && mx <= contentX + rowW && my >= rowY && my <= rowY + tile) {
                        _showDrives = false; _currentPath = ""; PushHistory(_currentPath); _scroll = 0; MarkEntriesDirty(); return;
                    }
                } else {
                    EnsureEntries();
                    var list = _entriesCache;
                    // Grid layout
                    int pad = 12; int icon = _iconFolder != null ? _iconFolder.Width : 48; int tileW = icon + pad * 2; int tileH = (icon + WindowManager.font.FontSize + pad * 2);
                    int cols = tileW > 0 ? (contentW / tileW) : 1; if (cols < 1) cols = 1;
                    for (int i = 0; i < list.Count; i++) {
                        int gridX = i % cols; int gridY = i / cols;
                        int gx = contentX + gridX * tileW;
                        int gy = contentY + gridY * tileH - _scroll;
                        if (gy + tileH < contentY || gy > contentY + contentH) continue;
                        if (mx >= gx && mx <= gx + tileW && my >= gy && my <= gy + tileH) {
                            string name = list[i].Name;
                            bool isDir = (list[i].Attribute == FileAttribute.Directory);
                            if (isDir) {
                                _currentPath = _currentPath + name + "/"; PushHistory(_currentPath); _scroll = 0; MarkEntriesDirty();
                            } else {
                                // opening files from here can be implemented later
                            }
                            break;
                        }
                    }
                }
            } else {
                _scrollDrag = false;
            }

            // Scrollbar dragging update
            if (_scrollDrag) {
                int tbH2 = WindowManager.font.FontSize + 12;
                int cH = Height - 16 - tbH2;
                int total = GetTotalContentHeight(contentW);
                int maxScroll = total > cH ? (total - cH) : 0;
                if (maxScroll < 0) maxScroll = 0;
                int trackH = cH;
                int thumbH = total > 0 ? (cH * cH) / total : cH;
                if (thumbH < 16) thumbH = 16; if (thumbH > trackH) thumbH = trackH;
                int range = trackH - thumbH;
                if (range <= 0) { _scroll = 0; return; }
                int dy = my - _scrollDragStartY;
                int newThumbTop = (trackH * _scrollDragStartScroll) / (total == 0 ? 1 : total) + dy;
                if (newThumbTop < 0) newThumbTop = 0; if (newThumbTop > range) newThumbTop = range;
                _scroll = (newThumbTop * total) / trackH;
                if (_scroll < 0) _scroll = 0; if (_scroll > maxScroll) _scroll = maxScroll;
            }
        }

        private int GetTotalContentHeight(int contentW) {
            if (_showDrives) {
                int icon = _iconFolder != null ? _iconFolder.Width : 48; int tileH = (icon + WindowManager.font.FontSize + 24);
                return tileH;
            }
            EnsureEntries();
            var list = _entriesCache;
            int pad = 12; int ic = _iconFolder != null ? _iconFolder.Width : 48; int tileW = ic + pad * 2; int tileH2 = (ic + WindowManager.font.FontSize + pad * 2);
            int cols = tileW > 0 ? (contentW / tileW) : 1; if (cols < 1) cols = 1;
            int rows = (list.Count + cols - 1) / cols; if (rows < 1) rows = 1;
            return rows * tileH2;
        }

        public override void OnDraw() {
            base.OnDraw();
            if (WindowManager.font == null) return;

            int tbH = WindowManager.font.FontSize + 12;
            // toolbar
            int tbY = Y + 6;
            Framebuffer.Graphics.FillRectangle(X + 6, Y + 6, Width - 12, tbH, 0xFF1E1E1E);
            int btnW = 80; int btnH = WindowManager.font.FontSize + 8; int gap = 6;
            int bx0 = X + 8; int bx1 = bx0 + btnW + gap; int bx2 = bx1 + btnW + gap;
            // Back
            Framebuffer.Graphics.FillRectangle(bx0, tbY, btnW, btnH, CanGoBack ? 0xFF2A2A2A : 0xFF202020);
            WindowManager.font.DrawString(bx0 + 14, tbY + 4, "Back");
            // Up Level
            Framebuffer.Graphics.FillRectangle(bx1, tbY, btnW, btnH, 0xFF2A2A2A);
            WindowManager.font.DrawString(bx1 + 6, tbY + 4, "Up Level");
            // Forward
            Framebuffer.Graphics.FillRectangle(bx2, tbY, btnW, btnH, CanGoForward ? 0xFF2A2A2A : 0xFF202020);
            WindowManager.font.DrawString(bx2 + 6, tbY + 4, "Forward");

            // Size options
            int sx = bx2 + btnW + gap + 10;
            for (int i = 0; i < _sizes.Length; i++) {
                int w = 36;
                uint bg = (i == _sizeIndex) ? 0xFF355C9C : 0xFF2A2A2A;
                Framebuffer.Graphics.FillRectangle(sx, tbY, w, btnH, bg);
                WindowManager.font.DrawString(sx + 6, tbY + 4, _sizes[i].ToString());
                sx += w + 4;
            }

            // content
            int contentX = X + 8;
            int contentY = Y + 8 + tbH;
            int contentW = Width - 16;
            int contentH = Height - 16 - tbH;

            // background area
            Framebuffer.Graphics.FillRectangle(contentX, contentY, contentW, contentH, 0xFF202020);

            if (_showDrives) {
                // Single drive tile (root of current FS)
                int icon = _iconFolder != null ? _iconFolder.Width : 48;
                int tileH = icon + WindowManager.font.FontSize + 16;
                int cx = contentX + (contentW - icon) / 2;
                int cy = contentY + 12 - _scroll;
                if (_iconFolder != null) Framebuffer.Graphics.DrawImage(cx, cy, _iconFolder);
                WindowManager.font.DrawString(cx - 24, cy + icon + 6, "Root");
            } else {
                EnsureEntries();
                var list = _entriesCache;
                int pad = 12; int icon = _iconFolder != null ? _iconFolder.Width : 48; int tileW = icon + pad * 2; int tileH = (icon + WindowManager.font.FontSize + pad * 2);
                int cols = tileW > 0 ? (contentW / tileW) : 1; if (cols < 1) cols = 1;
                for (int i = 0; i < list.Count; i++) {
                    int gridX = i % cols; int gridY = i / cols;
                    int gx = contentX + gridX * tileW + pad;
                    int gy = contentY + gridY * tileH + pad - _scroll;
                    if (gy + tileH < contentY || gy > contentY + contentH) continue;
                    bool isDir = (list[i].Attribute == FileAttribute.Directory);
                    if (isDir) Framebuffer.Graphics.DrawImage(gx, gy, _iconFolder); else Framebuffer.Graphics.DrawImage(gx, gy, _iconDoc);
                    string name = list[i].Name;
                    WindowManager.font.DrawString(gx, gy + icon + 6, name);
                }
            }

            // vertical scrollbar
            int total = GetTotalContentHeight(contentW);
            int maxScroll = total > contentH ? (total - contentH) : 0;
            if (maxScroll < 0) maxScroll = 0;
            int sbW = 10;
            int sbX = X + Width - 6 - sbW;
            int trackH = contentH;
            Framebuffer.Graphics.FillRectangle(sbX, contentY, sbW, trackH, 0xFF1A1A1A);
            if (total > 0 && maxScroll > 0) {
                int thumbH = (contentH * contentH) / total; if (thumbH < 16) thumbH = 16; if (thumbH > trackH) thumbH = trackH;
                int thumbY = (trackH * _scroll) / (total == 0 ? 1 : total);
                if (thumbY + thumbH > trackH) thumbY = trackH - thumbH;
                Framebuffer.Graphics.FillRectangle(sbX + 1, contentY + thumbY, sbW - 2, thumbH, 0xFF2F2F2F);
            }

            // resize handle visual
            Framebuffer.Graphics.FillRectangle(X + Width - ResizeHandle, Y + Height - ResizeHandle, ResizeHandle, ResizeHandle, 0xFF333333);

            // Left column with drives
            int leftW = 180;
            Framebuffer.Graphics.FillRectangle(X + 1, Y + 1, leftW - 2, Height - 2, 0xFF2A2A2A);
            int cursorY = Y + 10;
            // Root
            Framebuffer.Graphics.DrawImage(X + 10, cursorY, _iconFolder);
            WindowManager.font.DrawString(X + 10 + _iconFolder.Width + 8, cursorY + (_iconFolder.Height / 2) - (WindowManager.font.FontSize / 2), "Desktop");
            cursorY += _iconFolder.Height + 10;
            // Computer Files root
            Framebuffer.Graphics.DrawImage(X + 10, cursorY, _iconFolder);
            WindowManager.font.DrawString(X + 10 + _iconFolder.Width + 8, cursorY + (_iconFolder.Height / 2) - (WindowManager.font.FontSize / 2), "Computer Files");
            cursorY += _iconFolder.Height + 10;
            // USB drive item when present
            if (Kernel.Drivers.USBStorage.Count > 0) {
                string label = Kernel.Drivers.USBStorage.Count == 1 ? "USB Drive" : "USB Drives";
                Framebuffer.Graphics.DrawImage(X + 10, cursorY, _iconFolder);
                WindowManager.font.DrawString(X + 10 + _iconFolder.Width + 8, cursorY + (_iconFolder.Height / 2) - (WindowManager.font.FontSize / 2), label);
                cursorY += _iconFolder.Height + 10;
                label.Dispose();
            }

            // Main panel placeholder
            Framebuffer.Graphics.FillRectangle(X + leftW, Y + 1, Width - leftW - 2, Height - 2, 0xFF2B2B2B);
        }
    }
}
