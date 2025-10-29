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