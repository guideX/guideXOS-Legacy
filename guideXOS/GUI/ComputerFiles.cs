using guideXOS.FS;
using guideXOS.Kernel.Drivers;
using guideXOS.Graph;
using System.Windows.Forms;
using guideXOS.Misc;
using System.Collections.Generic;

namespace guideXOS.GUI {
    // File explorer window with drive list, navigation toolbar, breadcrumb, grid icons, resizing and vertical scrollbar
    internal class ComputerFiles : Window {
        private string _currentPath = ""; // empty means root of current drive (FileSystem)
        private bool _showDrives = true;

        // History
        private string[] _history = new string[64];
        private int _historyCount = 0;
        private int _historyIndex = -1;

        // Grid/icon sizes (available set)
        private int[] _sizes = new int[] { 16, 24, 32, 48, 128 };
        private int _sizeIndex = 1; // default 24px
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

        // Layout
        private const int LeftPaneW = 180;
        private const int MaxLabelLines = 2; // wrap up to 2 lines under icon

        // Cached entries for performance
        private List<FileInfo> _entriesCache;
        private string _entriesCacheFor;
        private bool _entriesDirty;

        // Breadcrumb clickable rects
        private struct Cr { public int X, Y, W, H, Index; }
        private Cr[] _crumbRects = new Cr[16];
        private int _crumbCount;

        // Label cache per folder/size/width
        private List<string> _labelNames;
        private List<string> _labelTexts;
        private string _labelCachePath;
        private int _labelCacheWidth;
        private int _labelCacheSizeIdx;

        public ComputerFiles(int X, int Y, int W = 640, int H = 480) : base(X, Y, W, H) {
            Title = "Computer Files";
            LoadIcons();
            _entriesDirty = true;
            _entriesCacheFor = null;
            _entriesCache = null;
            ShowInTaskbar = true;
            _labelNames = new List<string>(128);
            _labelTexts = new List<string>(128);
            _labelCachePath = null;
        }

        private void LoadIcons() {
            int px = _sizes[_sizeIndex];
            // Match the rest of the project's asset paths: relative to Ramdisk, capital 'Images'
            string folderPath = $"Images/BlueVelvet/{px}/folder.png";
            string docPath = $"Images/BlueVelvet/{px}/documents.png";
            try { _iconFolder = new PNG(File.ReadAllBytes(folderPath)); } catch { _iconFolder = Icons.FolderIcon; }
            try { _iconDoc = new PNG(File.ReadAllBytes(docPath)); } catch { _iconDoc = Icons.DocumentIcon; }
        }

        private void ClearEntriesCache() {
            if (_entriesCache != null) {
                for (int i = 0; i < _entriesCache.Count; i++) _entriesCache[i].Dispose();
                _entriesCache.Dispose();
                _entriesCache = null;
            }
            _entriesCacheFor = null;
            ClearLabelCache();
        }

        private void ClearLabelCache() {
            if (_labelNames != null) _labelNames.Clear();
            if (_labelTexts != null) _labelTexts.Clear();
            _labelCachePath = null; _labelCacheWidth = 0; _labelCacheSizeIdx = -1;
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

        private void MarkEntriesDirty() { _entriesDirty = true; ClearLabelCache(); }

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

        // Build breadcrumb labels, also compute clickable rects starting at (bx, by)
        // Returns total width drawn
        private int BuildBreadcrumb(int bx, int by) {
            _crumbCount = 0;
            // Segment 0: Computer Files
            string seg = "Computer Files";
            int w = WindowManager.font.MeasureString(seg);
            _crumbRects[_crumbCount].X = bx; _crumbRects[_crumbCount].Y = by; _crumbRects[_crumbCount].W = w; _crumbRects[_crumbCount].H = WindowManager.font.FontSize + 4; _crumbRects[_crumbCount].Index = 0; _crumbCount++;
            bx += w + 10;
            // If not drives view, add Root and folders
            if (!_showDrives) {
                string root = "Root";
                int rw = WindowManager.font.MeasureString(root);
                _crumbRects[_crumbCount].X = bx; _crumbRects[_crumbCount].Y = by; _crumbRects[_crumbCount].W = rw; _crumbRects[_crumbCount].H = WindowManager.font.FontSize + 4; _crumbRects[_crumbCount].Index = 1; _crumbCount++;
                bx += rw + 10;
                if (_currentPath != null && _currentPath.Length > 0) {
                    // iterate folders split by '/'
                    int start = 0; int len = _currentPath.Length;
                    while (start < len) {
                        int end = start; while (end < len && _currentPath[end] != '/') end++;
                        int partLen = end - start; if (partLen > 0) {
                            string part = _currentPath.Substring(start, partLen);
                            int pw = WindowManager.font.MeasureString(part);
                            if (_crumbCount >= _crumbRects.Length) break;
                            _crumbRects[_crumbCount].X = bx; _crumbRects[_crumbCount].Y = by; _crumbRects[_crumbCount].W = pw; _crumbRects[_crumbCount].H = WindowManager.font.FontSize + 4; _crumbRects[_crumbCount].Index = _crumbCount; // index mapping
                            _crumbCount++;
                            bx += pw + 10;
                            part.Dispose();
                        }
                        start = end + 1;
                    }
                }
            }
            seg.Dispose();
            return bx;
        }

        // Navigate to breadcrumb index
        private void NavigateCrumb(int index) {
            if (index <= 0) { // Computer Files
                _showDrives = true; _currentPath = ""; PushHistory(null); _scroll = 0; MarkEntriesDirty(); return;
            }
            if (index == 1) { // Root
                _showDrives = false; _currentPath = ""; PushHistory(_currentPath); _scroll = 0; MarkEntriesDirty(); return;
            }
            // Build path from first (index-1) folders
            string path = string.Empty;
            int taken = 0; int start = 0; int len = _currentPath.Length;
            while (start < len) {
                int end = start; while (end < len && _currentPath[end] != '/') end++;
                int partLen = end - start; if (partLen > 0) {
                    string part = _currentPath.Substring(start, partLen);
                    // append
                    string newp = path + part + "/";
                    path.Dispose(); part.Dispose();
                    path = newp;
                    taken++;
                    if (taken == (index - 1)) break;
                }
                start = end + 1;
            }
            _showDrives = false; _currentPath = path; PushHistory(_currentPath); _scroll = 0; MarkEntriesDirty();
        }

        // Single-line truncate helper
        private string TruncateToWidth(string text, int maxW) {
            if (WindowManager.font.MeasureString(text) <= maxW) return text;
            string ell = "...";
            int ellW = WindowManager.font.MeasureString(ell);
            int i = 0; int acc = 0;
            for (; i < text.Length; i++) {
                string s = text.Substring(0, i + 1);
                int sw = WindowManager.font.MeasureString(s);
                s.Dispose();
                if (sw + ellW > maxW) break;
                acc = sw;
            }
            string sub = text.Substring(0, i) + ell;
            ell.Dispose();
            return sub;
        }

        // Multi-line wrap with ellipsis for final line (computed once per name/width)
        private string WrapLabel(string text, int maxW, int maxLines) {
            if (text == null) return string.Empty;
            string result = string.Empty;
            int start = 0; int line = 1; int lastBreak = -1;
            for (int i = 0; i < text.Length; i++) {
                char c = text[i];
                if (c == ' ' || c == '-' || c == '_') lastBreak = i;
                string seg = text.Substring(start, (i - start) + 1);
                int w = WindowManager.font.MeasureString(seg);
                seg.Dispose();
                if (w > maxW) {
                    int cut = (lastBreak >= start) ? lastBreak : i;
                    string part = text.Substring(start, cut - start);
                    if (line == maxLines) {
                        string t = TruncateToWidth(text.Substring(start, i - start), maxW);
                        result = result + t; t.Dispose(); part.Dispose();
                        return result;
                    }
                    result = result + part + "\n";
                    part.Dispose();
                    line++;
                    start = (lastBreak >= cut) ? lastBreak + 1 : i; // continue after break or current char
                    lastBreak = -1;
                }
                if (line == maxLines && i == text.Length - 1) {
                    string tail = text.Substring(start);
                    string t = TruncateToWidth(tail, maxW);
                    result = result + t;
                    tail.Dispose(); t.Dispose();
                    return result;
                }
            }
            string rest = text.Substring(start);
            result = result + rest;
            rest.Dispose();
            return result;
        }

        private string GetCachedLabel(string name, int textW) {
            // Rebuild cache key when context changes
            if (_labelCachePath == null || _labelCachePath != _currentPath || _labelCacheWidth != textW || _labelCacheSizeIdx != _sizeIndex) {
                ClearLabelCache();
                _labelCachePath = _currentPath;
                _labelCacheWidth = textW;
                _labelCacheSizeIdx = _sizeIndex;
            }
            int idx = _labelNames.IndexOf(name);
            if (idx >= 0) return _labelTexts[idx];
            string wrapped = WrapLabel(name, textW, MaxLabelLines);
            _labelNames.Add(name);
            _labelTexts.Add(wrapped);
            return wrapped;
        }

        public override void OnInput() {
            base.OnInput();
            if (!Visible) return;

            int tbH = WindowManager.font.FontSize + 12;
            int contentX = X + 8;
            int contentY = Y + 8 + tbH;
            int contentW = Width - 16;
            int contentH = Height - 16 - tbH;

            // Breadcrumb row
            int breadH = WindowManager.font.FontSize + 10;
            int breadY = contentY;
            int gridY = contentY + breadH; // content below breadcrumb
            int gridH = contentH - breadH;

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
                        ClearLabelCache();
                        return;
                    }
                    sx += w + 4;
                }

                // Breadcrumb clicks
                _crumbCount = 0; BuildBreadcrumb(contentX + 6, breadY + (breadH - WindowManager.font.FontSize) / 2 - 2);
                for (int i = 0; i < _crumbCount; i++) {
                    var r = _crumbRects[i];
                    if (mx >= r.X && mx <= r.X + r.W && my >= r.Y && my <= r.Y + r.H) { NavigateCrumb(r.Index); return; }
                }

                // Left navigation clicks (below toolbar + breadcrumb)
                int leftX0 = X + 1;
                int leftX1 = X + LeftPaneW - 2;
                int leftY0 = gridY;
                int cursorY = leftY0 + 10;
                int iconH = _iconFolder != null ? _iconFolder.Height : 48;
                if (mx >= leftX0 && mx <= leftX1 && my >= leftY0 && my <= leftY0 + gridH) {
                    if (my >= cursorY && my <= cursorY + iconH) { _showDrives = true; _currentPath = ""; PushHistory(null); _scroll = 0; MarkEntriesDirty(); return; }
                    cursorY += iconH + 10;
                    if (my >= cursorY && my <= cursorY + iconH) { _showDrives = true; _currentPath = ""; PushHistory(null); _scroll = 0; MarkEntriesDirty(); return; }
                    cursorY += iconH + 10;
                    if (Kernel.Drivers.USBStorage.Count > 0) {
                        if (my >= cursorY && my <= cursorY + iconH) { var list = new USBDrives(X + LeftPaneW + 20, Y + 40, 420, 360); WindowManager.MoveToEnd(list); list.Visible = true; return; }
                    }
                }

                // Scrollbar drag start (right content area)
                int sbW = 10;
                int sbX = X + Width - 6 - sbW;
                if (mx >= sbX && mx <= sbX + sbW && my >= gridY && my <= gridY + gridH) { _scrollDrag = true; _scrollDragStartY = my; _scrollDragStartScroll = _scroll; return; }

                // Content clicks (right grid)
                if (_showDrives) {
                    int tile = (_iconFolder != null ? _iconFolder.Height : 48) + (WindowManager.font.FontSize * MaxLabelLines) + 16;
                    int rowY = gridY;
                    int rowW = contentW - LeftPaneW - 8; // exclude left panel width
                    if (my >= rowY && my <= rowY + tile && mx >= X + LeftPaneW + 8 && mx <= X + LeftPaneW + 8 + rowW) { _showDrives = false; _currentPath = ""; PushHistory(_currentPath); _scroll = 0; MarkEntriesDirty(); return; }
                } else {
                    EnsureEntries();
                    var list = _entriesCache;
                    int pad = 12; int icon = _iconFolder != null ? _iconFolder.Width : 48; int tileW = icon + pad * 2; int tileH = (icon + (WindowManager.font.FontSize * MaxLabelLines) + pad * 2);
                    int rcX = X + LeftPaneW + 8; int rcW = contentW - LeftPaneW - 8;
                    int cols = tileW > 0 ? (rcW / tileW) : 1; if (cols < 1) cols = 1;
                    // Compute visible rows range to limit iteration
                    int startRow = _scroll / tileH; if (startRow < 0) startRow = 0;
                    int endRow = (gridH + _scroll) / tileH; int totalRows = (list.Count + cols - 1) / cols; if (endRow >= totalRows) endRow = totalRows - 1;
                    int startIndex = startRow * cols; if (startIndex < 0) startIndex = 0;
                    int endIndex = (endRow + 1) * cols - 1; if (endIndex >= list.Count) endIndex = list.Count - 1;
                    for (int i = startIndex; i <= endIndex; i++) {
                        int gridX = i % cols; int gridY2 = i / cols;
                        int gx = rcX + gridX * tileW + pad;
                        int gy = gridY + gridY2 * tileH + pad - _scroll;
                        if (mx >= gx && mx <= gx + tileW && my >= gy && my <= gy + tileH) {
                            string name = list[i].Name;
                            bool isDir = (list[i].Attribute == FileAttribute.Directory);
                            if (isDir) { _currentPath = _currentPath + name + "/"; PushHistory(_currentPath); _scroll = 0; MarkEntriesDirty(); }
                            else { /* open file if needed */ }
                            break;
                        }
                    }
                }
            } else {
                _scrollDrag = false;
            }

            // Scrollbar dragging update
            if (_scrollDrag) {
                int total = GetTotalContentHeight(contentW - LeftPaneW - 8);
                int maxScroll = total > gridH ? (total - gridH) : 0;
                if (maxScroll < 0) maxScroll = 0;
                int trackH = gridH;
                int thumbH = total > 0 ? (gridH * gridH) / total : gridH;
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
                int icon = _iconFolder != null ? _iconFolder.Width : 48; int tileH = (icon + WindowManager.font.FontSize * MaxLabelLines + 24);
                return tileH;
            }
            EnsureEntries();
            var list = _entriesCache;
            int pad = 12; int ic = _iconFolder != null ? _iconFolder.Width : 48; int tileW = ic + pad * 2; int tileH2 = (ic + (WindowManager.font.FontSize * MaxLabelLines) + pad * 2);
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

            // content area bounds
            int contentX = X + 8;
            int contentY = Y + 8 + tbH;
            int contentW = Width - 16;
            int contentH = Height - 16 - tbH;

            // background area
            Framebuffer.Graphics.FillRectangle(contentX, contentY, contentW, contentH, 0xFF202020);

            // Breadcrumb bar
            int breadH = WindowManager.font.FontSize + 10;
            int breadY = contentY;
            Framebuffer.Graphics.FillRectangle(contentX, breadY, contentW, breadH, 0xFF252525);
            // Draw breadcrumb labels
            int bx = contentX + 6; int by = breadY + (breadH - WindowManager.font.FontSize) / 2 - 2;
            _crumbCount = 0; BuildBreadcrumb(bx, by);
            for (int i = 0; i < _crumbCount; i++) {
                int lx = _crumbRects[i].X; int ly = _crumbRects[i].Y;
                string label;
                if (i == 0) label = "Computer Files";
                else if (i == 1) label = "Root";
                else {
                    int level = i - 1; int start = 0; int len = _currentPath.Length; int found = 0; int st = 0; int en = 0;
                    while (start < len) { int end = start; while (end < len && _currentPath[end] != '/') end++; int partLen = end - start; if (partLen > 0) { if (++found == level) { st = start; en = end; break; } } start = end + 1; }
                    label = partLenHelper(st, en);
                }
                WindowManager.font.DrawString(lx, ly, label);
                if (i < _crumbCount - 1) WindowManager.font.DrawString(_crumbRects[i].X + _crumbRects[i].W + 4, ly, ">");
                label.Dispose();
            }

            // Left pane below breadcrumb
            int gridY = contentY + breadH;
            int gridH = contentH - breadH;
            Framebuffer.Graphics.FillRectangle(X + 1, gridY, LeftPaneW - 2, gridH, 0xFF2A2A2A);
            int cursorY = gridY + 10;
            Framebuffer.Graphics.DrawImage(X + 10, cursorY, _iconFolder);
            WindowManager.font.DrawString(X + 10 + _iconFolder.Width + 8, cursorY + (_iconFolder.Height / 2) - (WindowManager.font.FontSize / 2), "Desktop");
            cursorY += _iconFolder.Height + 10;
            Framebuffer.Graphics.DrawImage(X + 10, cursorY, _iconFolder);
            WindowManager.font.DrawString(X + 10 + _iconFolder.Width + 8, cursorY + (_iconFolder.Height / 2) - (WindowManager.font.FontSize / 2), "Computer Files");
            cursorY += _iconFolder.Height + 10;
            if (Kernel.Drivers.USBStorage.Count > 0) { string label = Kernel.Drivers.USBStorage.Count == 1 ? "USB Drive" : "USB Drives"; Framebuffer.Graphics.DrawImage(X + 10, cursorY, _iconFolder); WindowManager.font.DrawString(X + 10 + _iconFolder.Width + 8, cursorY + (_iconFolder.Height / 2) - (WindowManager.font.FontSize / 2), label); label.Dispose(); }

            // Right content panel
            int rcX = X + LeftPaneW + 8;
            int rcW = contentW - LeftPaneW - 8;

            if (_showDrives) {
                int icon = _iconFolder != null ? _iconFolder.Width : 48;
                int tileH = icon + (WindowManager.font.FontSize * MaxLabelLines) + 16;
                int cx2 = rcX + (rcW - icon) / 2;
                int cy = gridY + 12 - _scroll;
                if (_iconFolder != null) Framebuffer.Graphics.DrawImage(cx2, cy, _iconFolder);
                WindowManager.font.DrawString(cx2 - 24, cy + icon + 6, "Root");
            } else {
                EnsureEntries();
                var list = _entriesCache;
                int pad = 12; int icon = _iconFolder != null ? _iconFolder.Width : 48; int tileW = icon + pad * 2; int tileH = (icon + (WindowManager.font.FontSize * MaxLabelLines) + pad * 2);
                int cols = tileW > 0 ? (rcW / tileW) : 1; if (cols < 1) cols = 1;
                int textW = tileW - pad * 2; int textH = WindowManager.font.FontSize * MaxLabelLines;
                // Prepare label cache context
                if (_labelCachePath == null || _labelCachePath != _currentPath || _labelCacheWidth != textW || _labelCacheSizeIdx != _sizeIndex) { _labelCachePath = _currentPath; _labelCacheWidth = textW; _labelCacheSizeIdx = _sizeIndex; _labelNames.Clear(); _labelTexts.Clear(); }
                // Compute visible index range
                int startRow = _scroll / tileH; if (startRow < 0) startRow = 0;
                int endRow = (gridH + _scroll) / tileH; int totalRows = (list.Count + cols - 1) / cols; if (endRow >= totalRows) endRow = totalRows - 1;
                int startIndex = startRow * cols; if (startIndex < 0) startIndex = 0;
                int endIndex = (endRow + 1) * cols - 1; if (endIndex >= list.Count) endIndex = list.Count - 1;
                for (int i = startIndex; i <= endIndex; i++) {
                    int gridX2 = i % cols; int gridY2 = i / cols;
                    int gx = rcX + gridX2 * tileW + pad;
                    int gy = gridY + gridY2 * tileH + pad - _scroll;
                    bool isDir = (list[i].Attribute == FileAttribute.Directory);
                    if (isDir) Framebuffer.Graphics.DrawImage(gx, gy, _iconFolder); else Framebuffer.Graphics.DrawImage(gx, gy, _iconDoc);
                    string name = list[i].Name;
                    string label = GetCachedLabel(name, textW);
                    WindowManager.font.DrawString(gx, gy + icon + 6, label, textW, textH);
                }
            }

            // vertical scrollbar for right content
            int total = GetTotalContentHeight(rcW);
            int maxScroll = total > gridH ? (total - gridH) : 0;
            if (maxScroll < 0) maxScroll = 0;
            int sbW = 10;
            int sbX = X + Width - 6 - sbW;
            int trackH = gridH;
            Framebuffer.Graphics.FillRectangle(sbX, gridY, sbW, trackH, 0xFF1A1A1A);
            if (total > 0 && maxScroll > 0) {
                int thumbH = (gridH * gridH) / total; if (thumbH < 16) thumbH = 16; if (thumbH > trackH) thumbH = trackH;
                int thumbY = (trackH * _scroll) / (total == 0 ? 1 : total);
                if (thumbY + thumbH > trackH) thumbY = trackH - thumbH;
                Framebuffer.Graphics.FillRectangle(sbX + 1, gridY + thumbY, sbW - 2, thumbH, 0xFF2F2F2F);
            }

            // resize handle visual
            Framebuffer.Graphics.FillRectangle(X + Width - ResizeHandle, Y + Height - ResizeHandle, ResizeHandle, ResizeHandle, 0xFF333333);
        }

        private string partLenHelper(int st, int en) {
            if (en <= st || st < 0 || en > _currentPath.Length) return string.Empty;
            string s = _currentPath.Substring(st, en - st);
            return s;
        }
    }
}
