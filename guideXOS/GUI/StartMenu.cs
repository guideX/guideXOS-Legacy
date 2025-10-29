using guideXOS.Kernel.Drivers;
using System.Windows.Forms;

namespace guideXOS.GUI {
    /// <summary>
    /// Start Menu
    /// </summary>
    internal class StartMenu : Window {
        /// <summary>
        /// X
        /// </summary>
        private static readonly int _x = 15;
        /// <summary>
        /// Y
        /// </summary>
        private static readonly int _y = 45;
        /// <summary>
        /// Width
        /// </summary>
        private static readonly int _x2 = 200;
        /// <summary>
        /// Height
        /// </summary>
        private static readonly int _y2 = 680; // 15, 45, 200, 680

        // Power submenu visibility
        private bool _powerMenuVisible = false;

        // Layout constants
        private const int Padding = 10;
        private const int Spacing = 50;

        // Power button sizes
        private const int ShutdownBtnW = 100;
        private const int ShutdownBtnH = 28;
        private const int ArrowBtnW = 28;
        private const int ArrowBtnH = 28;
        private const int Gap = 6;

        // Power menu item sizes
        private const int MenuItemH = 26;
        private const int MenuW = 120;
        private const int MenuPad = 6;

        /// <summary>
        /// Constructor
        /// </summary>
        public unsafe StartMenu() : base(_x, _y, _x2, _y2) {
            Title = "Start";
            BarHeight = 0;
        }

        /// <summary>
        /// On Input
        /// </summary>
        public override void OnInput() {
            base.OnInput();

            if (!Visible) return;

            int bottomY = Y + Height - Padding - ShutdownBtnH;
            int shutdownX = X + Width - Padding - ShutdownBtnW - ArrowBtnW - Gap;
            int arrowX = X + Width - Padding - ArrowBtnW;

            int mx = Control.MousePosition.X;
            int my = Control.MousePosition.Y;

            bool leftDown = Control.MouseButtons == MouseButtons.Left;

            // Compute submenu rect (shown above buttons, aligned to right edge)
            int menuH = MenuPad * 2 + (MenuItemH * 2);
            int menuW = MenuW;
            int menuX = X + Width - Padding - menuW;
            int menuY = bottomY - menuH - Gap;

            if (leftDown) {
                // Click on Shutdown
                if (mx >= shutdownX && mx <= shutdownX + ShutdownBtnW &&
                    my >= bottomY && my <= bottomY + ShutdownBtnH) {
                    Power.Shutdown();
                    return;
                }

                // Click on Arrow (toggle submenu)
                if (mx >= arrowX && mx <= arrowX + ArrowBtnW &&
                    my >= bottomY && my <= bottomY + ArrowBtnH) {
                    _powerMenuVisible = !_powerMenuVisible;
                    return;
                }

                // If submenu visible, handle its clicks
                if (_powerMenuVisible) {
                    if (mx >= menuX && mx <= menuX + menuW &&
                        my >= menuY && my <= menuY + menuH) {
                        // Which item?
                        int itemY0 = menuY + MenuPad;
                        int itemY1 = itemY0 + MenuItemH;

                        if (my >= itemY0 && my < itemY0 + MenuItemH) {
                            // Reboot
                            Power.Reboot();
                            return;
                        }
                        if (my >= itemY1 && my < itemY1 + MenuItemH) {
                            // Log Off -> show lockscreen
                            _powerMenuVisible = false;
                            this.Visible = false;
                            Lockscreen.Run();
                            return;
                        }
                    } else {
                        // Clicked outside submenu -> close submenu
                        _powerMenuVisible = false;
                    }
                }
            }
        }

        /// <summary>
        /// Draw contents of Start Menu
        /// </summary>
        public override void OnDraw() {
            Framebuffer.Graphics.FillRectangle(X, Y, Width, Height, 0xFF222222);

            int y = Y + Padding;

            for (int i = 0; i < Desktop.Apps.Length; i++) {
                var icon = Desktop.Apps.Icon(i);
                if (icon == null) continue;

                int iconX = X + Padding;
                int iconY = y;

                // Stop if we run out of vertical space
                if (iconY + icon.Height > Y + Height - Padding - (ShutdownBtnH + Gap + Padding)) break;

                // Draw icon
                Framebuffer.Graphics.DrawImage(iconX, iconY, icon);

                // Draw name to the right, vertically centered to icon
                string name = Desktop.Apps.Name(i);
                int textX = iconX + icon.Width + 10;
                int textY = iconY + (icon.Height / 2) - (WindowManager.font.FontSize / 2);
                WindowManager.font.DrawString(textX, textY, name);

                y += Spacing;
            }

            // Buttons at bottom-right
            int bottomY = Y + Height - Padding - ShutdownBtnH;
            int shutdownX = X + Width - Padding - ShutdownBtnW - ArrowBtnW - Gap;
            int arrowX = X + Width - Padding - ArrowBtnW;

            // Hover effects
            int mx = Control.MousePosition.X;
            int my = Control.MousePosition.Y;
            bool overShutdown = (mx >= shutdownX && mx <= shutdownX + ShutdownBtnW && my >= bottomY && my <= bottomY + ShutdownBtnH);
            bool overArrow = (mx >= arrowX && mx <= arrowX + ArrowBtnW && my >= bottomY && my <= bottomY + ArrowBtnH);

            uint btnBg = 0xFF2A2A2A;
            uint btnBgHover = 0xFF343434;
            uint border = 0xFF3F3F3F;

            // Shutdown button
            Framebuffer.Graphics.FillRectangle(shutdownX, bottomY, ShutdownBtnW, ShutdownBtnH, overShutdown ? btnBgHover : btnBg);
            Framebuffer.Graphics.DrawRectangle(shutdownX, bottomY, ShutdownBtnW, ShutdownBtnH, border, 1);
            WindowManager.font.DrawString(shutdownX + 10, bottomY + (ShutdownBtnH / 2) - (WindowManager.font.FontSize / 2), "Shutdown");

            // Arrow button
            Framebuffer.Graphics.FillRectangle(arrowX, bottomY, ArrowBtnW, ArrowBtnH, overArrow ? btnBgHover : btnBg);
            Framebuffer.Graphics.DrawRectangle(arrowX, bottomY, ArrowBtnW, ArrowBtnH, border, 1);
            WindowManager.font.DrawString(arrowX + 8, bottomY + (ArrowBtnH / 2) - (WindowManager.font.FontSize / 2), ">");

            // Submenu (if visible)
            if (_powerMenuVisible) {
                int menuH = MenuPad * 2 + (MenuItemH * 2);
                int menuW = MenuW;
                int menuX = X + Width - Padding - menuW;
                int menuY = bottomY - menuH - Gap;

                // Background + border
                Framebuffer.Graphics.FillRectangle(menuX, menuY, menuW, menuH, 0xFF262626);
                Framebuffer.Graphics.DrawRectangle(menuX, menuY, menuW, menuH, border, 1);

                int itemY = menuY + MenuPad;
                // Hover highlight for items
                bool hoverReboot = (mx >= menuX && mx <= menuX + menuW && my >= itemY && my < itemY + MenuItemH);
                bool hoverLogoff = (mx >= menuX && mx <= menuX + menuW && my >= itemY + MenuItemH && my < itemY + (2 * MenuItemH));

                if (hoverReboot) Framebuffer.Graphics.FillRectangle(menuX + 1, itemY, menuW - 2, MenuItemH, 0xFF313131);
                WindowManager.font.DrawString(menuX + 10, itemY + (MenuItemH / 2) - (WindowManager.font.FontSize / 2), "Reboot");

                if (hoverLogoff) Framebuffer.Graphics.FillRectangle(menuX + 1, itemY + MenuItemH, menuW - 2, MenuItemH, 0xFF313131);
                WindowManager.font.DrawString(menuX + 10, itemY + MenuItemH + (MenuItemH / 2) - (WindowManager.font.FontSize / 2), "Log Off");
            }

            // Border (no bar)
            DrawBorder(false);
        }
    }
}