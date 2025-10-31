using guideXOS.Kernel.Drivers;
using System.Drawing;
using System.Windows.Forms;
namespace guideXOS.GUI {
    internal class Taskbar {
        public StartMenu StartMenu;
        private int _barHeight;
        private Image _startIcon;
        private bool _clockUse12Hour = false;
        private bool _clockClickLatch = false;
        private bool _startClickLatch = false;

        // Network indicator animation
        private int _netAnimPhase = 0;
        private ulong _lastTick = 0;
        private bool _netConnectedShown = false;

        public Taskbar(int barHeight, Image startIcon) { _barHeight = barHeight; _startIcon = startIcon; }

        public void Draw() {
            Framebuffer.Graphics.AFillRectangle(0, Framebuffer.Height - _barHeight, Framebuffer.Width, _barHeight, 0xCC222222);
            int startX = 12; int startY = Framebuffer.Height - _barHeight + 4;
            // Draw Start icon on the taskbar
            if (_startIcon != null) {
                Framebuffer.Graphics.DrawImage(startX, startY, _startIcon);
            }

            // Draw task buttons for open windows
            int btnX = startX + (_startIcon != null ? _startIcon.Width + 12 : 12);
            int btnY = Framebuffer.Height - _barHeight + 6;
            int btnH = _barHeight - 12;
            int btnW = 140; // fixed width
            int gap = 8;

            int mx = Control.MousePosition.X; int my = Control.MousePosition.Y;
            bool left = Control.MouseButtons.HasFlag(MouseButtons.Left);

            for (int i = 0; i < WindowManager.Windows.Count; i++) {
                var w = WindowManager.Windows[i];
                if (!w.Visible) continue;
                // button rect
                int x = btnX; int y = btnY; int wRect = btnW; int hRect = btnH;
                bool hover = (mx >= x && mx <= x + wRect && my >= y && my <= y + hRect);
                uint bg = hover ? 0xFF3A3A3A : 0xFF303030;
                Framebuffer.Graphics.FillRectangle(x, y, wRect, hRect, bg);
                Framebuffer.Graphics.DrawRectangle(x, y, wRect, hRect, 0xFF454545, 1);
                // icon and title
                var icon = w.TaskbarIcon ?? Icons.FileIcon;
                int iconY = y + (hRect / 2) - (icon.Height / 2);
                Framebuffer.Graphics.DrawImage(x + 6, iconY, icon);
                int textX = x + 6 + icon.Width + 6;
                int textWidth = wRect - (textX - x) - 6;
                if (textWidth > 0) WindowManager.font.DrawString(textX, y + (hRect / 2) - (WindowManager.font.FontSize / 2), w.Title, textWidth, WindowManager.font.FontSize);
                // click -> focus window
                if (left && hover) {
                    WindowManager.MoveToEnd(w);
                    w.Visible = true;
                }
                btnX += wRect + gap;
                if (btnX > Framebuffer.Width - 300) break; // leave space for clock area
            }

            // Time and date strings
            string time;
            if (_clockUse12Hour) {
                bool isPM = RTC.Hour >= 12; int hour12 = (RTC.Hour % 12 == 0) ? 12 : (RTC.Hour % 12);
                string sfx = isPM ? "PM" : "AM";
                string min = RTC.Minute < 10 ? ("0" + RTC.Minute.ToString()) : RTC.Minute.ToString();
                time = hour12.ToString() + ":" + min + " " + sfx; sfx.Dispose(); min.Dispose();
            } else {
                string h = RTC.Hour < 10 ? ("0" + RTC.Hour.ToString()) : RTC.Hour.ToString();
                string m = RTC.Minute < 10 ? ("0" + RTC.Minute.ToString()) : RTC.Minute.ToString();
                string s = RTC.Second < 10 ? ("0" + RTC.Second.ToString()) : RTC.Second.ToString();
                time = h + ":" + m + ":" + s; h.Dispose(); m.Dispose(); s.Dispose();
            }
            string date = RTC.Month.ToString() + "/" + RTC.Day.ToString() + "/" + RTC.Year.ToString();

            int timeW = WindowManager.font.MeasureString(time);
            int timeX = Framebuffer.Width - 12 - timeW;
            int timeY = Framebuffer.Height - _barHeight + ((_barHeight - WindowManager.font.FontSize) / 2) - (WindowManager.font.FontSize/2);
            WindowManager.font.DrawString(timeX, timeY, time);
            // Date below time
            int dateY = timeY + WindowManager.font.FontSize;
            WindowManager.font.DrawString(timeX, dateY, date);

            // Network indicator left of time
            int iconSize = 14;
            int netX = timeX - iconSize - 8;
            int netY = timeY + (WindowManager.font.FontSize / 2) - (iconSize/2);

            // Simple animation while determining
            if (_lastTick != Timer.Ticks) { _lastTick = Timer.Ticks; _netAnimPhase = (_netAnimPhase + 1) % 3; }

            bool connected = false;
#if NETWORK
            connected = NETv4.Initialized; // if networking compiled in
#else
            connected = false;
#endif
            // draw signal bars or spinner
            if (connected) {
                // draw 3 bars
                int bw = 3; int gap2 = 2;
                for (int i = 0; i < 3; i++) {
                    int h2 = 4 + i * 4;
                    Framebuffer.Graphics.FillRectangle(netX + i * (bw + gap2), netY + (iconSize - h2), bw, h2, 0xFF5FB878);
                }
                _netConnectedShown = true;
            } else {
                // animate 3 dots while determining
                int dot = 3; int gap2 = 4;
                for (int i = 0; i < 3; i++) {
                    uint c = (i == _netAnimPhase) ? 0xFFAAAAAAu : 0xFF555555u;
                    Framebuffer.Graphics.FillRectangle(netX + i * (dot + gap2), netY + (iconSize/2) - (dot/2), dot, dot, c);
                }
            }

            // Input handling for start/time areas
            if (Control.MouseButtons.HasFlag(MouseButtons.Left)) {
                int mx2 = Control.MousePosition.X; int my2 = Control.MousePosition.Y;
                if (mx2 >= timeX && mx2 <= timeX + timeW && my2 >= Framebuffer.Height - _barHeight && my2 <= Framebuffer.Height) {
                    if (!_clockClickLatch) { _clockUse12Hour = !_clockUse12Hour; _clockClickLatch = true; }
                }
                if (_startIcon != null) {
                    int sW = _startIcon.Width; int sH = _startIcon.Height;
                    if (mx2 >= startX && mx2 <= startX + sW && my2 >= startY && my2 <= startY + sH) {
                        if (!_startClickLatch) { if (StartMenu == null) StartMenu = new StartMenu(); StartMenu.Visible = !StartMenu.Visible; _startClickLatch = true; }
                    } else {
                        if (StartMenu != null && StartMenu.Visible && !StartMenu.IsUnderMouse()) { StartMenu.Visible = false; }
                    }
                }
            } else { _clockClickLatch = false; _startClickLatch = false; }

            time.Dispose(); date.Dispose();
        }
    }
}