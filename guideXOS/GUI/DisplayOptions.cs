using guideXOS.Kernel.Drivers;
using guideXOS.FS;
using guideXOS.Misc;
using System;
using System.Windows.Forms;
using System.Drawing;

namespace guideXOS.GUI {
    internal class DisplayOptions : Window {
        private int _itemHeight = 28;
        private int _padding = 10;
        private int _selectedIndex = -1;
        private int _hoverIndex = -1;
        private bool _confirmVisible = false;
        private int _countdown = 15;
        private Resolution _previous;
        private Resolution _pending;
        private ulong _lastCountdownTick;
        private string[] _labels;

        // Background UI
        private int _bgGroupTop;
        private int _btnW = 120;
        private int _btnH = 26;
        private bool _clickLock;
        private OpenDialog _openDlg;
        private ColorPicker _colorDlg;

        public DisplayOptions(int X, int Y, int W = 360, int H = 420) : base(X, Y, W, H) {
            Title = "Display Options";
            var list = DisplayManager.AvailableResolutions;
            if (list == null || list.Length == 0) {
                _labels = new string[0];
                _selectedIndex = -1;
            } else {
                // Select current
                var cur = DisplayManager.Current;
                for (int i = 0; i < list.Length; i++) {
                    if (list[i].Width == cur.Width && list[i].Height == cur.Height) { _selectedIndex = i; break; }
                }
                if (_selectedIndex < 0 && list.Length > 0) _selectedIndex = 0;

                // Precompute display labels once
                _labels = new string[list.Length];
                for (int i = 0; i < list.Length; i++) {
                    _labels[i] = list[i].Width.ToString() + " x " + list[i].Height.ToString();
                }
            }
            _bgGroupTop = Y + H - 180;
            _clickLock = false;
            _openDlg = null;
            _colorDlg = null;
        }

        public override void OnInput() {
            base.OnInput(); if (!Visible) return; var list = DisplayManager.AvailableResolutions; if (list == null) return;

            // If a modal dialog is open, let it handle input
            if ((_openDlg != null && _openDlg.Visible) || (_colorDlg != null && _colorDlg.Visible)) return;

            int listX = X + _padding; int listY = Y + _padding + WindowManager.font.FontSize + 6; int listW = Width - _padding * 2; int count = list.Length;
            int mx = Control.MousePosition.X; int my = Control.MousePosition.Y; _hoverIndex = -1;

            // countdown tick (~60Hz main loop) when visible
            if (_confirmVisible) {
                if (_lastCountdownTick == 0) _lastCountdownTick = RTC.Second;
                if (_lastCountdownTick != RTC.Second) {
                    _lastCountdownTick = RTC.Second;
                    if (_countdown > 0) _countdown--;
                    if (_countdown == 0) {
                        RevertResolution();
                    }
                }
            }

            if (Control.MouseButtons == MouseButtons.Left) {
                // If confirm area visible, handle Yes/No first
                if (_confirmVisible) {
                    int btnW = 80, btnH = 26, gap = 10; int btnY = Y + Height - _padding - btnH; int noX = X + Width - _padding - btnW - gap - btnW; int yesX = X + Width - _padding - btnW;
                    if (mx >= yesX && mx <= yesX + btnW && my >= btnY && my <= btnY + btnH) { KeepResolution(); return; }
                    if (mx >= noX && mx <= noX + btnW && my >= btnY && my <= btnY + btnH) { RevertResolution(); return; }
                }

                // Click on resolution list
                if (mx >= listX && mx <= listX + listW && my >= listY && my <= listY + (count * _itemHeight)) {
                    int index = (my - listY) / _itemHeight;
                    if (index >= 0 && index < count && index != _selectedIndex) {
                        // Start apply
                        _previous = DisplayManager.Current;
                        var chosen = list[index];
                        if (DisplayManager.TrySetResolution(chosen.Width, chosen.Height)) {
                            _pending = chosen;
                            _selectedIndex = index;
                            _confirmVisible = true;
                            _countdown = 15;
                            _lastCountdownTick = 0;
                        } else {
                            NotificationManager.Add(new Nofity("Resolution change not supported on this hardware", NotificationLevel.Error));
                        }
                    }
                }

                // Background group buttons
                int gx = listX; int gy = _bgGroupTop; int gapX = 12; int btnImgX = gx; int btnImgY = gy + WindowManager.font.FontSize + 6;
                int btnClrX = btnImgX + _btnW + gapX; int btnClrY = btnImgY;

                if (mx >= btnImgX && mx <= btnImgX + _btnW && my >= btnImgY && my <= btnImgY + _btnH) {
                    // Open file dialog for image
                    _openDlg = new OpenDialog(X + 30, Y + 60, 520, 320, Desktop.Dir, (path) => {
                        try {
                            var img = new PNG(File.ReadAllBytes(path));
                            Program.Wallpaper.Dispose();
                            Program.Wallpaper = img.ResizeImage(Framebuffer.Width, Framebuffer.Height);
                            img.Dispose();
                        } catch {
                            NotificationManager.Add(new Nofity("Failed to load image", NotificationLevel.Error));
                        }
                    });
                    WindowManager.MoveToEnd(_openDlg); _openDlg.Visible = true; return;
                }
                if (mx >= btnClrX && mx <= btnClrX + _btnW && my >= btnClrY && my <= btnClrY + _btnH) {
                    _colorDlg = new ColorPicker(X + 60, Y + 80, (color) => {
                        // Build a solid-color wallpaper image
                        var img = new System.Drawing.Image(Framebuffer.Width, Framebuffer.Height);
                        for (int yy = 0; yy < img.Height; yy++)
                            for (int xx = 0; xx < img.Width; xx++)
                                img.RawData[yy * img.Width + xx] = (int)color;
                        Program.Wallpaper.Dispose(); Program.Wallpaper = img;
                    });
                    WindowManager.MoveToEnd(_colorDlg); _colorDlg.Visible = true; return;
                }
            }
        }

        private void KeepResolution() { _confirmVisible = false; DisplayManager.SaveResolution(_pending); }
        private void RevertResolution() { _confirmVisible = false; if (!DisplayManager.TrySetResolution(_previous.Width, _previous.Height)) { NotificationManager.Add(new Nofity("Failed to revert resolution", NotificationLevel.Error)); } else { var list = DisplayManager.AvailableResolutions; if (list != null) { for (int i = 0; i < list.Length; i++) if (list[i].Width == _previous.Width && list[i].Height == _previous.Height) { _selectedIndex = i; break; } } } }

        public override void OnDraw() {
            base.OnDraw(); if (WindowManager.font == null) return; int cx = X + _padding; int cy = Y + _padding; int cw = Width - _padding * 2; WindowManager.font.DrawString(cx, cy, "Available resolutions:"); int listX = cx; int listY = cy + WindowManager.font.FontSize + 6; var list = DisplayManager.AvailableResolutions; if (list == null) return; int count = list.Length;
            for (int i = 0; i < count; i++) { int rowY = listY + i * _itemHeight; bool selected = (i == _selectedIndex); uint rowBg = selected ? 0xFF2A2A2A : 0xFF222222; Framebuffer.Graphics.FillRectangle(listX, rowY, cw, _itemHeight - 2, rowBg); string label = (_labels != null && i < _labels.Length && _labels[i] != null) ? _labels[i] : (list[i].Width.ToString() + " x " + list[i].Height.ToString()); WindowManager.font.DrawString(listX + 8, rowY + (_itemHeight / 2) - (WindowManager.font.FontSize / 2), label); }

            // Background group UI
            int gx = listX; int gy = _bgGroupTop; Framebuffer.Graphics.FillRectangle(gx, gy, cw, 100, 0xFF222222);
            WindowManager.font.DrawString(gx + 8, gy + 8, "Background:");
            int btnImgX = gx; int btnImgY = gy + WindowManager.font.FontSize + 6; int btnClrX = btnImgX + _btnW + 12; int btnClrY = btnImgY;
            Framebuffer.Graphics.FillRectangle(btnImgX, btnImgY, _btnW, _btnH, 0xFF2A2A2A); WindowManager.font.DrawString(btnImgX + 10, btnImgY + (_btnH / 2 - WindowManager.font.FontSize / 2), "Change Image");
            Framebuffer.Graphics.FillRectangle(btnClrX, btnClrY, _btnW, _btnH, 0xFF2A2A2A); WindowManager.font.DrawString(btnClrX + 16, btnClrY + (_btnH / 2 - WindowManager.font.FontSize / 2), "Choose Color");

            // confirm area
            if (_confirmVisible) {
                int btnW = 80, btnH = 26, gap = 10;
                int btnY = Y + Height - _padding - btnH;
                int yesX = X + Width - _padding - btnW;
                int noX = yesX - gap - btnW;

                string msg = "Your previous resolution will be re-applied in " + _countdown.ToString() + " seconds";
                WindowManager.font.DrawString(cx, btnY - WindowManager.font.FontSize - 6, msg);

                Framebuffer.Graphics.FillRectangle(noX, btnY, btnW, btnH, 0xFF2A2A2A);
                Framebuffer.Graphics.DrawRectangle(noX, btnY, btnW, btnH, 0xFF3F3F3F, 1);
                WindowManager.font.DrawString(noX + 26, btnY + (btnH / 2) - (WindowManager.font.FontSize / 2), "No");

                Framebuffer.Graphics.FillRectangle(yesX, btnY, btnW, btnH, 0xFF2A2A2A);
                Framebuffer.Graphics.DrawRectangle(yesX, btnY, btnW, btnH, 0xFF3F3F3F, 1);
                WindowManager.font.DrawString(yesX + 22, btnY + (btnH / 2) - (WindowManager.font.FontSize / 2), "Yes");
            }
        }
    }

    // Simple color picker: 6x6x6 cube palette
    internal class ColorPicker : Window {
        private readonly Action<uint> _onChoose;
        private bool _clickLock;
        private int _padding = 10;
        public ColorPicker(int x, int y, Action<uint> onChoose) : base(x, y, 260, 200) { Title = "Choose Color"; _onChoose = onChoose; _clickLock = false; }
        public override void OnInput() {
            base.OnInput(); bool left = Control.MouseButtons.HasFlag(MouseButtons.Left); int mx = Control.MousePosition.X; int my = Control.MousePosition.Y; int cx = X + _padding; int cy = Y + _padding; int sw = 20; int sh = 20; int cols = 12; int rows = 8; if (left) { if (!_clickLock) { for (int r = 0; r < rows; r++) { for (int c = 0; c < cols; c++) { int px = cx + c * (sw + 2); int py = cy + r * (sh + 2); if (mx >= px && mx <= px + sw && my >= py && my <= py + sh) { uint color = SampleColor(c, r); _onChoose?.Invoke(color); this.Visible = false; _clickLock = true; return; } } } } } else { _clickLock = false; } }
        public override void OnDraw() { base.OnDraw(); int cx = X + _padding; int cy = Y + _padding; int sw = 20; int sh = 20; int cols = 12; int rows = 8; for (int r = 0; r < rows; r++) { for (int c = 0; c < cols; c++) { int px = cx + c * (sw + 2); int py = cy + r * (sh + 2); uint color = SampleColor(c, r); Framebuffer.Graphics.FillRectangle(px, py, sw, sh, color); } } }
        private static uint SampleColor(int c, int r) {
            // Basic palette rows: grayscale + RGB mixes
            if (r == 0) { byte v = (byte)(c * 21); return (uint)(0xFF000000 | (v << 16) | (v << 8) | v); }
            byte rr = (byte)(c * 21); byte gg = (byte)(r * 30); byte bb = (byte)((c ^ r) * 16);
            return (uint)(0xFF000000 | (rr << 16) | (gg << 8) | bb);
        }
    }
}
