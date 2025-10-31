using guideXOS.FS;
using guideXOS.Kernel.Drivers;
using guideXOS.Misc;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace guideXOS.GUI {
    /// <summary>
    /// Save As dialog: simple explorer with filename entry and Save/Cancel buttons.
    /// Blocks user by appearing as a window; caller should check Visible state.
    /// </summary>
    internal class SaveDialog : Window {
        private string _currentPath;
        private string _fileName;
        private List<FileInfo> _entries;
        private bool _clickLock;
        private readonly Action<string> _onSave;
        private int _padding = 10;
        private int _paddingRight = 20; // extra right padding so buttons aren’t flush
        private int _rowH = 36; // ensure enough height for icons
        private int _btnW = 120; // wider buttons
        private int _btnH = 28;
        private bool _fnameFocus;

        // Key de-bounce
        private byte _lastScan;
        private bool _keyDown;

        public SaveDialog(int x, int y, int w, int h, string startPath, string defaultName, Action<string> onSave) : base(x, y, w, h) {
            Title = "Save As";
            _currentPath = startPath ?? "";
            _fileName = defaultName ?? "untitled.txt";
            _entries = new List<FileInfo>();
            _onSave = onSave;
            _clickLock = false;
            _fnameFocus = true;
            // Adjust row height based on icon size
            int minRow = Icons.FileIcon != null ? Icons.FileIcon.Height + 10 : 36;
            if (minRow > _rowH) _rowH = minRow;
            Keyboard.OnKeyChanged += Keyboard_OnKeyChanged;
            RefreshEntries();
        }

        private void RefreshEntries() {
            if (_entries != null) { for (int i = 0; i < _entries.Count; i++) _entries[i].Dispose(); _entries.Clear(); }
            _entries = File.GetFiles(_currentPath);
        }

        private void GoUp() {
            if (string.IsNullOrEmpty(_currentPath)) return;
            string path = _currentPath;
            if (path.Length > 0 && path[path.Length - 1] == '/') path = path.Substring(0, path.Length - 1);
            int last = path.LastIndexOf('/');
            _currentPath = last >= 0 ? path.Substring(0, last + 1) : "";
            RefreshEntries();
        }

        private void Keyboard_OnKeyChanged(object sender, ConsoleKeyInfo key) {
            if (!Visible) return;
            // de-bounce
            if (key.KeyState != ConsoleKeyState.Pressed) { _keyDown = false; _lastScan = 0; return; }
            if (_keyDown && Keyboard.KeyInfo.ScanCode == _lastScan) return;
            _keyDown = true; _lastScan = (byte)Keyboard.KeyInfo.ScanCode;

            // Escape cancels dialog
            if (key.Key == ConsoleKey.Escape) { this.Visible = false; return; }

            if (!_fnameFocus) return;

            if (key.Key == ConsoleKey.Backspace) { if (_fileName.Length > 0) _fileName = _fileName.Substring(0, _fileName.Length - 1); return; }
            if (key.Key == ConsoleKey.Enter) { SaveAction(); return; }
            // spaces
            if (key.Key == ConsoleKey.Space || Keyboard.KeyInfo.ScanCode == 57) { _fileName += " "; return; }
            if (key.Key >= ConsoleKey.A && key.Key <= ConsoleKey.Z) { char c = (char)('a' + (key.Key - ConsoleKey.A)); _fileName += c; return; }
            if (key.Key >= ConsoleKey.D0 && key.Key <= ConsoleKey.D9) { char c = (char)('0' + (key.Key - ConsoleKey.D0)); _fileName += c; return; }
            switch (key.Key) {
                case ConsoleKey.OemPeriod: _fileName += "."; break;
                case ConsoleKey.OemMinus: _fileName += "-"; break;
                case ConsoleKey.OemPlus: _fileName += "+"; break;
                case ConsoleKey.Oem1: _fileName += ";"; break;
                case ConsoleKey.Oem2: _fileName += "/"; break;
                case ConsoleKey.Oem3: _fileName += "`"; break;
                case ConsoleKey.Oem4: _fileName += "["; break;
                case ConsoleKey.Oem5: _fileName += "\\"; break;
                case ConsoleKey.Oem6: _fileName += "]"; break;
                case ConsoleKey.Oem7: _fileName += "'"; break;
                case ConsoleKey.OemComma: _fileName += ","; break;
            }
        }

        private void SaveAction() {
            if (string.IsNullOrEmpty(_fileName)) return;
            string path = _currentPath + _fileName;
            _onSave?.Invoke(path);
            path.Dispose();
            this.Visible = false;
        }

        public override void OnInput() {
            base.OnInput();
            bool left = Control.MouseButtons.HasFlag(MouseButtons.Left);
            int mx = Control.MousePosition.X; int my = Control.MousePosition.Y;
            int cx = X + _padding;
            int cw = Width - _padding - _paddingRight; // content width with right padding
            int cy = Y + _padding + 28;
            int listX = cx;
            int listY = cy + 24;
            int listW = cw;
            int listH = Height - _padding - (cy - Y) - 90;

            int upW = 60; int upH = 22; int upX = cx; int upY = cy;

            int fnY = Y + Height - _padding - 44; // a bit more space
            int fnH = 26; int fnLabelW = 80; int fnX = cx + fnLabelW; int fnW = listW - fnLabelW - (_btnW * 2 + 16 + 8);
            int saveX = fnX + fnW + 8; int cancelX = saveX + _btnW + 8;

            if (left) {
                if (!_clickLock) {
                    // Up
                    if (mx >= upX && mx <= upX + upW && my >= upY && my <= upY + upH) { GoUp(); _clickLock = true; return; }
                    // Filename focus
                    if (mx >= fnX && mx <= fnX + fnW && my >= fnY && my <= fnY + fnH) { _fnameFocus = true; _clickLock = true; return; } else { if (my >= listY && my <= listY + listH && mx >= listX && mx <= listX + listW) _fnameFocus = false; }
                    // Save
                    if (mx >= saveX && mx <= saveX + _btnW && my >= fnY && my <= fnY + _btnH) { SaveAction(); _clickLock = true; return; }
                    // Cancel
                    if (mx >= cancelX && mx <= cancelX + _btnW && my >= fnY && my <= fnY + _btnH) { this.Visible = false; _clickLock = true; return; }

                    // List click: compute index
                    if (mx >= listX && mx <= listX + listW && my >= listY && my <= listY + listH) {
                        int idx = (my - listY) / _rowH;
                        if (idx >= 0 && idx < _entries.Count) {
                            var e = _entries[idx];
                            if (e.Attribute == FileAttribute.Directory) {
                                // enter dir
                                _currentPath = _currentPath + e.Name + "/";
                                RefreshEntries();
                            } else {
                                _fileName = e.Name;
                            }
                            _clickLock = true; return;
                        }
                    }
                }
            } else { _clickLock = false; }
        }

        public override void OnDraw() {
            base.OnDraw();
            int cx = X + _padding; int cw = Width - _padding - _paddingRight; int cy = Y + _padding + 28; int listX = cx; int listY = cy + 24; int listW = cw; int listH = Height - _padding - (cy - Y) - 90;
            // Title bar area inside content (opaque, no full-screen dim)
            Framebuffer.Graphics.FillRectangle(cx, cy, listW, 22, 0xFF3A3A3A);
            int upW = 60; int upH = 22; Framebuffer.Graphics.FillRectangle(cx, cy, upW, upH, 0xFF4A4A4A); WindowManager.font.DrawString(cx + 8, cy + 4, "Up");
            WindowManager.font.DrawString(cx + upW + 8, cy + 4, _currentPath ?? "");

            // List background
            Framebuffer.Graphics.FillRectangle(listX, listY, listW, listH, 0xFF2B2B2B);
            int y = listY; int iconW = Icons.FileIcon.Width; int iconH = Icons.FileIcon.Height;
            for (int i = 0; i < _entries.Count; i++) {
                var e = _entries[i];
                // row bg alternating
                uint rowBg = ((i & 1) == 0) ? 0xFF303030u : 0xFF2B2B2Bu;
                Framebuffer.Graphics.FillRectangle(listX, y, listW, _rowH, rowBg);
                var icon = (e.Attribute == FileAttribute.Directory) ? Icons.FolderIcon : Icons.FileIcon;
                int iconY = y + (_rowH / 2 - iconH / 2);
                Framebuffer.Graphics.DrawImage(listX + 6, iconY, icon);
                WindowManager.font.DrawString(listX + 12 + iconW, y + (_rowH / 2 - WindowManager.font.FontSize / 2), e.Name);
                y += _rowH;
                if (y > listY + listH - _rowH) break;
            }

            // Filename + buttons
            int fnLabelW = 80; int fnY = Y + Height - _padding - 44; int fnH = 26; int fnX = cx + fnLabelW; int fnW = listW - fnLabelW - (_btnW * 2 + 16 + 8);
            WindowManager.font.DrawString(cx, fnY + (fnH / 2 - WindowManager.font.FontSize / 2), "File name:");
            Framebuffer.Graphics.FillRectangle(fnX, fnY, fnW, fnH, _fnameFocus ? 0xFF3A3A3A : 0xFF2A2A2A);
            WindowManager.font.DrawString(fnX + 6, fnY + (fnH / 2 - WindowManager.font.FontSize / 2), _fileName ?? "");
            int saveX = fnX + fnW + 8; int cancelX = saveX + _btnW + 8;
            Framebuffer.Graphics.FillRectangle(saveX, fnY, _btnW, _btnH, 0xFF3A3A3A); WindowManager.font.DrawString(saveX + 16, fnY + (_btnH / 2 - WindowManager.font.FontSize / 2), "Save");
            Framebuffer.Graphics.FillRectangle(cancelX, fnY, _btnW, _btnH, 0xFF3A3A3A); WindowManager.font.DrawString(cancelX + 12, fnY + (_btnH / 2 - WindowManager.font.FontSize / 2), "Cancel");
        }
    }
}
