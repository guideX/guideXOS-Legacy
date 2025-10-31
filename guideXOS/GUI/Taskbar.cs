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
                int bw = 3; int gap = 2;
                for (int i = 0; i < 3; i++) {
                    int h2 = 4 + i * 4;
                    Framebuffer.Graphics.FillRectangle(netX + i * (bw + gap), netY + (iconSize - h2), bw, h2, 0xFF5FB878);
                }
                _netConnectedShown = true;
            } else {
                // animate 3 dots while determining
                int dot = 3; int gap = 4;
                for (int i = 0; i < 3; i++) {
                    uint c = (i == _netAnimPhase) ? 0xFFAAAAAAu : 0xFF555555u;
                    Framebuffer.Graphics.FillRectangle(netX + i * (dot + gap), netY + (iconSize/2) - (dot/2), dot, dot, c);
                }
            }

            // Input handling
            if (Control.MouseButtons.HasFlag(MouseButtons.Left)) {
                int mx = Control.MousePosition.X; int my = Control.MousePosition.Y;
                if (mx >= timeX && mx <= timeX + timeW && my >= Framebuffer.Height - _barHeight && my <= Framebuffer.Height) {
                    if (!_clockClickLatch) { _clockUse12Hour = !_clockUse12Hour; _clockClickLatch = true; }
                }
                if (_startIcon != null) {
                    int sW = _startIcon.Width; int sH = _startIcon.Height;
                    if (mx >= startX && mx <= startX + sW && my >= startY && my <= startY + sH) {
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