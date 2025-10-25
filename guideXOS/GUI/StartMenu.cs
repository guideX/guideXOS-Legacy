using guideXOS.Kernel.Drivers;
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

        /// <summary>
        /// Constructor
        /// </summary>
        public unsafe StartMenu() : base(_x, _y, _x2, _y2) {
            Title = "Start";
            BarHeight = 0;
        }

        /// <summary>
        /// Draw contents of Start Menu
        /// </summary>
        public override void OnDraw() {
            Framebuffer.Graphics.FillRectangle(X, Y, Width, Height, 0xFF222222);

            int padding = 10;
            int spacing = 50;
            int y = Y + padding;

            for (int i = 0; i < Desktop.Apps.Length; i++) {
                var icon = Desktop.Apps.Icon(i);
                if (icon == null) continue;

                int iconX = X + padding;
                int iconY = y;

                // Stop if we run out of vertical space
                if (iconY + icon.Height > Y + Height - padding) break;

                // Draw icon
                Framebuffer.Graphics.DrawImage(iconX, iconY, icon);

                // Draw name to the right, vertically centered to icon
                string name = Desktop.Apps.Name(i);
                int textX = iconX + icon.Width + 10;
                int textY = iconY + (icon.Height / 2) - (WindowManager.font.FontSize / 2);
                WindowManager.font.DrawString(textX, textY, name);

                y += spacing;
            }

            // Border (no bar)
            DrawBorder(false);
        }
    }
}