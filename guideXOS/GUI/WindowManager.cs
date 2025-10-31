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

        struct PendingWindow {
            public int Type; // 1 = DisplayOptions
            public int X, Y, W, H;
        }
        static List<PendingWindow> _pending;

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
            _pending = new List<PendingWindow>();
        }
        /// <summary>
        /// Enqueue display options window creation after input phase
        /// </summary>
        public static void EnqueueDisplayOptions(int x, int y, int w, int h) {
            PendingWindow pw;
            pw.Type = 1; pw.X = x; pw.Y = y; pw.W = w; pw.H = h;
            _pending.Add(pw);
        }
        /// <summary>
        /// Flush pending window creations
        /// </summary>
        public static void FlushPendingCreates() {
            if (_pending.Count == 0) return;
            for (int i = 0; i < _pending.Count; i++) {
                var pw = _pending[i];
                if (pw.Type == 1) {
                    _ = new DisplayOptions(pw.X, pw.Y, pw.W, pw.H);
                }
            }
            _pending.Clear();
        }
        /// <summary>
        /// Move to End
        /// </summary>
        /// <param name="window"></param>
        public static void MoveToEnd(Window window) {
            int idx = Windows.IndexOf(window);
            if (idx >= 0) {
                Windows.RemoveAt(idx);
                Windows.Add(window);
            } else {
                Windows.Add(window);
            }
        }
        /// <summary>
        /// Draw All
        /// </summary>
        public static void DrawAll() {
            // Draw from bottom to top so the last window (top-most) is drawn last
            for (int i = 0; i < Windows.Count; i++) {
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
        /// Mouse Handled (separate from HasWindowMoving)
        /// </summary>
        static bool _mouseHandled;
        public static bool MouseHandled {
            get => _mouseHandled;
            set => _mouseHandled = value;
        }
    }
}