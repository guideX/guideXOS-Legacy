using guideXOS.Kernel.Drivers;
using System;
namespace guideXOS.GUI {
    /// <summary>
    /// Start Menu
    /// </summary>
    internal class StartMenu : Window {
        /// <summary>
        /// X
        /// </summary>
        private static readonly int _x = 15;
        private static readonly int _mouseX = 11;
        private static readonly int _mouseX2 = 17;
        private static readonly int _mouseY = 729;
        private static readonly int _mouseY2 = 770;
        /// <summary>
        /// Y
        /// </summary>
        private static readonly int _y = 45;
        /// <summary>
        /// X2
        /// </summary>
        private static readonly int _x2 = 200;
        /// <summary>
        /// Y2
        /// </summary>
        private static readonly int _y2 = 680;//15, 45, 200, 680
        /// <summary>
        /// Constructor
        /// </summary>
        public unsafe StartMenu() : base(_x, _y, _x2, _y2) {
            Title = "Start";
            BarHeight = 0;
        }
        /// <summary>
        /// X
        /// </summary>
        public static int X {
            get {
                return _x;
            }
        }
        /// <summary>
        /// Y
        /// </summary>
        public static int Y {
            get {
                return _y;
            }
        }
        public static int X2 {
            get {
                return _x2;
            }
        }
        public static int Y2 {
            get {
                return _y2;
            }
        }
        /// <summary>
        /// Draw
        /// </summary>
        public void Draw() {
            var addedTop = 0;
            for (int i = 0; i < Desktop.Apps.Length; i++) {
                Framebuffer.Graphics.DrawImage(_x2, _y2 + addedTop, Desktop.Apps.Icon(i));
                addedTop = addedTop + 50;
            }
        }
    }
}