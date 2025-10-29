using guideXOS.Kernel.Drivers;
using System.Drawing;
using System.Windows.Forms;
namespace guideXOS.GUI {
    /// <summary>
    /// TaskBar
    /// </summary>
    internal class Taskbar {
        /// <summary>
        /// Start Menu
        /// </summary>
        public StartMenu StartMenu;
        /// <summary>
        /// Bar Height
        /// </summary>
        private int _barHeight;
        /// <summary>
        /// Start Icon
        /// </summary>
        private Image _startIcon;
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="barHeight"></param>
        public Taskbar(int barHeight, Image startIcon) {
            _barHeight = barHeight;
            _startIcon = startIcon;
        }
        /// <summary>
        /// Draw Task Bar
        /// </summary>
        public void Draw() {
            // Semi-transparent taskbar
            Framebuffer.Graphics.AFillRectangle(0, Framebuffer.Height - _barHeight, Framebuffer.Width, _barHeight, 0xCC222222);
            Framebuffer.Graphics.DrawImage(12, Framebuffer.Height - _barHeight + 4, _startIcon);

            // Clock (bottom-right) - HH:MM:SS with manual zero-padding; dispose temporary strings to prevent leaks
            string colon = ":";

            string shour = RTC.Hour.ToString();
            if (RTC.Hour < 10) { string tmp = "0" + shour; shour.Dispose(); shour = tmp; }

            string sminute = RTC.Minute.ToString();
            if (RTC.Minute < 10) { string tmp = "0" + sminute; sminute.Dispose(); sminute = tmp; }

            string ssecond = RTC.Second.ToString();
            if (RTC.Second < 10) { string tmp = "0" + ssecond; ssecond.Dispose(); ssecond = tmp; }

            // Build HH:MM:SS step-by-step to control temporaries
            string time = shour + colon; // HH:
            string tmp2 = time + sminute; time.Dispose(); time = tmp2; // HH:MM
            string tmp3 = time + colon; time.Dispose(); time = tmp3;   // HH:MM:
            string tmp4 = time + ssecond; time.Dispose(); time = tmp4; // HH:MM:SS

            int textW = WindowManager.font.MeasureString(time);
            int textX = Framebuffer.Width - 12 - textW;
            int textY = Framebuffer.Height - _barHeight + ((_barHeight - WindowManager.font.FontSize) / 2);
            WindowManager.font.DrawString(textX, textY, time);

            // Dispose temps
            colon.Dispose();
            shour.Dispose();
            sminute.Dispose();
            ssecond.Dispose();
            time.Dispose();

            if (Control.MouseButtons.HasFlag(MouseButtons.Left)) {
                if (Control.MousePosition.X > 15 && Control.MousePosition.X < 35 && Control.MousePosition.Y > 700 && Control.MousePosition.Y < 800) {
                    if (StartMenu == null) {
                        StartMenu = new StartMenu();
                    } else {
                        if (StartMenu != null && StartMenu.Visible) {
                            StartMenu.Visible = false;
                        } else {
                            StartMenu.Visible = true;
                        }
                    }
                } else {
                    if (StartMenu != null) {
                        if (!StartMenu.IsUnderMouse()) {
                            StartMenu.Visible = false;
                            StartMenu = null;
                        }
                    }
                }
            }
        }
    }
}