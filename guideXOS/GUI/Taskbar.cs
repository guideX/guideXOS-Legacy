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

        public Taskbar(int barHeight, Image startIcon) { _barHeight = barHeight; _startIcon = startIcon; }

        public void Draw() {
            Framebuffer.Graphics.AFillRectangle(0, Framebuffer.Height - _barHeight, Framebuffer.Width, _barHeight, 0xCC222222);
            int startX = 12; int startY = Framebuffer.Height - _barHeight + 4;
            Framebuffer.Graphics.DrawImage(startX, startY, _startIcon);

            string time;
            if (_clockUse12Hour) {
                bool isPM = RTC.Hour >= 12; int hour12 = (RTC.Hour % 12 == 0) ? 12 : (RTC.Hour % 12);
                string sfx = isPM ? "PM" : "AM";
                string min = RTC.Minute < 10 ? ("0" + RTC.Minute.ToString()) : RTC.Minute.ToString();
                time = hour12.ToString() + ":" + min + " " + sfx;
                sfx.Dispose(); min.Dispose();
            } else {
                string h = RTC.Hour < 10 ? ("0" + RTC.Hour.ToString()) : RTC.Hour.ToString();
                string m = RTC.Minute < 10 ? ("0" + RTC.Minute.ToString()) : RTC.Minute.ToString();
                string s = RTC.Second < 10 ? ("0" + RTC.Second.ToString()) : RTC.Second.ToString();
                time = h + ":" + m + ":" + s; h.Dispose(); m.Dispose(); s.Dispose();
            }
            int textW = WindowManager.font.MeasureString(time);
            int textX = Framebuffer.Width - 12 - textW;
            int textY = Framebuffer.Height - _barHeight + ((_barHeight - WindowManager.font.FontSize) / 2);
            WindowManager.font.DrawString(textX, textY, time);
            time.Dispose();

            // Input handling
            if (Control.MouseButtons.HasFlag(MouseButtons.Left)) {
                int mx = Control.MousePosition.X; int my = Control.MousePosition.Y;
                // Clock toggle
                if (mx >= textX && mx <= textX + textW && my >= Framebuffer.Height - _barHeight && my <= Framebuffer.Height) {
                    if (!_clockClickLatch) { _clockUse12Hour = !_clockUse12Hour; _clockClickLatch = true; }
                }
                int sW = _startIcon.Width; int sH = _startIcon.Height;
                if (mx >= startX && mx <= startX + sW && my >= startY && my <= startY + sH) {
                    if (!_startClickLatch) { if (StartMenu == null) StartMenu = new StartMenu(); StartMenu.Visible = !StartMenu.Visible; _startClickLatch = true; }
                } else {
                    if (StartMenu != null && StartMenu.Visible && !StartMenu.IsUnderMouse()) { StartMenu.Visible = false; }
                }
            } else { _clockClickLatch = false; _startClickLatch = false; }
        }
    }
}