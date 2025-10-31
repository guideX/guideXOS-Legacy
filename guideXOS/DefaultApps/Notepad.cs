using guideXOS.FS;
using guideXOS.GUI;
using guideXOS.Kernel.Drivers;
using guideXOS.Misc;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace guideXOS.DefaultApps {
    /// <summary>
    /// Simple Notepad app: type text and save to a file to test filesystem writes.
    /// </summary>
    internal class Notepad : Window {
        private string _text;
        private bool _clickLock;
        private int _padding = 10;
        private int _btnH = 28;
        private int _btnWSaveAs = 88; // more padding
        private int _btnWSave = 72;
        private int _btnWWrap = 64;
        private string _fileName = "notes.txt";
        private string _savedPath; // full path of last save
        private bool _dirty;
        private bool _wrap = true;
        private SaveDialog _dlg;
        private SaveChangesDialog _confirmDlg;
        private byte _lastScan; private bool _keyDown;

        public Notepad(int x, int y) : base(x, y, 700, 460) {
            Title = "Notepad";
            _text = string.Empty; _clickLock = false; _savedPath = null; _dirty = false; _dlg = null; _confirmDlg = null;
            // subscribe keyboard handler
            Keyboard.OnKeyChanged += Keyboard_OnKeyChanged;
        }

        public override void OnSetVisible(bool value) {
            // Intercept close when there are unsaved changes
            if (!value && _dirty) {
                if (_confirmDlg == null || !_confirmDlg.Visible) {
                    _confirmDlg = new SaveChangesDialog(this, () => {
                        // Save
                        if (!string.IsNullOrEmpty(_savedPath)) {
                            SaveTo(_savedPath);
                            this.Visible = false;
                        } else {
                            // open save as
                            OpenSaveAs(() => { this.Visible = false; });
                        }
                    }, () => {
                        // Don't Save
                        _dirty = false; this.Visible = false;
                    }, () => {
                        // Cancel close
                        this.Visible = true;
                    });
                    WindowManager.MoveToEnd(_confirmDlg);
                    _confirmDlg.Visible = true;
                }
                // keep notepad visible until decision
                this.Visible = true;
            }
        }

        private void Keyboard_OnKeyChanged(object sender, ConsoleKeyInfo key) {
            if (!Visible) return;
            if ((_dlg != null && _dlg.Visible) || (_confirmDlg != null && _confirmDlg.Visible)) return; // let dialog handle keys when visible
            if (key.KeyState != ConsoleKeyState.Pressed) { _keyDown = false; _lastScan = 0; return; }
            if (_keyDown && Keyboard.KeyInfo.ScanCode == _lastScan) return; // de-bounce to avoid repeats
            _keyDown = true; _lastScan = (byte)Keyboard.KeyInfo.ScanCode;

            // Controls
            if (key.Key == ConsoleKey.Escape) { return; }
            if (key.Key == ConsoleKey.Backspace) { if (_text.Length > 0) { _text = _text.Substring(0, _text.Length - 1); _dirty = true; } return; }
            if (key.Key == ConsoleKey.Enter) { _text += "\n"; _dirty = true; return; }
            if (key.Key == ConsoleKey.Tab) { _text += "    "; _dirty = true; return; }

            // Use KeyChar for printable input (handles space and special chars)
            if (key.KeyChar != '\0') { _text += key.KeyChar; _dirty = true; return; }
        }

        private void SaveTo(string path) {
            // Save to Desktop.Dir + notes.txt
            byte[] data = new byte[_text.Length]; for (int i = 0; i < _text.Length; i++) data[i] = (byte)_text[i];
            File.WriteAllBytes(path, data); data.Dispose();
            _savedPath = path; _fileName = path.Substring(path.LastIndexOf('/') + 1); _dirty = false;
            Desktop.InvalidateDirCache();
            // Feedback
            Desktop.msgbox.X = X + 40; Desktop.msgbox.Y = Y + 80;
            Desktop.msgbox.SetText($"Saved: {path}");
            WindowManager.MoveToEnd(Desktop.msgbox); Desktop.msgbox.Visible = true;
            RecentManager.AddDocument(path, Icons.FileIcon);
        }

        private void OpenSaveAs(Action afterSaveClose = null) {
            _dlg = new SaveDialog(X + 40, Y + 40, 520, 360, Desktop.Dir, _fileName, (p) => { SaveTo(p); afterSaveClose?.Invoke(); });
            WindowManager.MoveToEnd(_dlg); _dlg.Visible = true;
        }

        public override void OnInput() {
            base.OnInput(); if ((_dlg != null && _dlg.Visible) || (_confirmDlg != null && _confirmDlg.Visible)) return;
            bool left = Control.MouseButtons.HasFlag(MouseButtons.Left);
            int mx = Control.MousePosition.X; int my = Control.MousePosition.Y;
            int bxSaveAs = X + _padding; int by = Y + _padding;
            int bxSave = bxSaveAs + _btnWSaveAs + 8; int bxWrap = bxSave + _btnWSave + 8;
            bool canSave = !string.IsNullOrEmpty(_savedPath) && _dirty;
            if (left) {
                if (!_clickLock) {
                    if (mx >= bxSaveAs && mx <= bxSaveAs + _btnWSaveAs && my >= by && my <= by + _btnH) { OpenSaveAs(); _clickLock = true; return; }
                    if (canSave && mx >= bxSave && mx <= bxSave + _btnWSave && my >= by && my <= by + _btnH) { SaveTo(_savedPath); _clickLock = true; return; }
                    if (mx >= bxWrap && mx <= bxWrap + _btnWWrap && my >= by && my <= by + _btnH) { _wrap = !_wrap; _clickLock = true; return; }
                }
            } else { _clickLock = false; }
        }

        public override void OnDraw() {
            base.OnDraw(); int cx = X + _padding; int cy = Y + _padding; int cw = Width - _padding * 2; int ch = Height - _padding * 2;
            // Buttons
            int bxSaveAs = cx; int by = cy; int bxSave = bxSaveAs + _btnWSaveAs + 8; int bxWrap = bxSave + _btnWSave + 8;
            Framebuffer.Graphics.FillRectangle(bxSaveAs, by, _btnWSaveAs, _btnH, 0xFF3A3A3A); WindowManager.font.DrawString(bxSaveAs + 6, by + (_btnH / 2 - WindowManager.font.FontSize / 2), "Save As");
            bool canSave = !string.IsNullOrEmpty(_savedPath) && _dirty;
            Framebuffer.Graphics.FillRectangle(bxSave, by, _btnWSave, _btnH, canSave ? 0xFF3A3A3Au : 0xFF2A2A2Au);
            WindowManager.font.DrawString(bxSave + 12, by + (_btnH / 2 - WindowManager.font.FontSize / 2), "Save");
            Framebuffer.Graphics.FillRectangle(bxWrap, by, _btnWWrap, _btnH, 0xFF3A3A3A); WindowManager.font.DrawString(bxWrap + 10, by + (_btnH / 2 - WindowManager.font.FontSize / 2), _wrap ? "Wrap" : "NoWrap");

            int tx = cx; int ty = cy + _btnH + 8; int tw = cw; int th = ch - (_btnH + 8);
            Framebuffer.Graphics.AFillRectangle(tx, ty, tw, th, 0x80282828);
            // Word wrap toggle
            if (_wrap) WindowManager.font.DrawString(tx + 6, ty + 6, _text, tw - 12, WindowManager.font.FontSize * 3);
            else WindowManager.font.DrawString(tx + 6, ty + 6, _text);
        }
    }
}