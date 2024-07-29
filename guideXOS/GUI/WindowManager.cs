using guideXOS.FS;
using guideXOS.Misc;
using System.Collections.Generic;
using System.Drawing;
namespace guideXOS.GUI {
    /// <summary>
    /// Window Manager
    /// </summary>
    internal static class WindowManager {
        /// <summary>
        /// Windows
        /// </summary>
        public static List<Window> Windows;
        /// <summary>
        /// Font
        /// </summary>
        public static IFont font;
        /// <summary>
        /// Close Button
        /// </summary>
        public static Image CloseButton;
        /// <summary>
        /// Minimize Button
        /// </summary>
        public static Image MinimizeButton;
        /// <summary>
        /// Initialize
        /// </summary>
        public static void Initialize() {
            Windows = new List<Window>();
            CloseButton = new PNG(File.ReadAllBytes("Images/Close.png"));
            //MinimizeButton = new PNG(File.ReadAllBytes("Images/Close.png"));
            PNG defaultFont = new PNG(File.ReadAllBytes("Images/defaultfont.png"));
            font = new IFont(defaultFont, "!\"#$%&'()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[\\]^_`abcdefghijklmnopqrstuvwxyz{|}~", 18);
            MouseHandled = false;
        }
        /// <summary>
        /// Move to End
        /// </summary>
        /// <param name="window"></param>
        public static void MoveToEnd(Window window) {
            Windows.Insert(0, window, true);
        }
        /// <summary>
        /// Draw All
        /// </summary>
        public static void DrawAll() {
            for (int i = Windows.Count - 1; i >= 0; i--) {
                if (Windows[i].Visible)
                    Windows[i].OnDraw();
            }
        }
        /// <summary>
        /// Input All
        /// </summary>
        public static void InputAll() {
            for (int i = 0; i < Windows.Count; i++) {
                if (Windows[i].Visible)
                    Windows[i].OnInput();
            }
        }
        /// <summary>
        /// Has Window Moving
        /// </summary>
        public static bool HasWindowMoving = false;
        /// <summary>
        /// Mouse Handled
        /// </summary>
        public static bool MouseHandled {
            get => HasWindowMoving;
            set => HasWindowMoving = value;
        }
    }
}