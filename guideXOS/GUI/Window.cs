using guideXOS.Kernel.Drivers;
using System.Windows.Forms;
using System.Drawing;
namespace guideXOS.GUI {
    /// <summary>
    /// Window
    /// </summary>
    abstract class Window {
        /// <summary>
        /// Visible
        /// </summary>
        public bool Visible {
            set {
                _visible = value;
                OnSetVisible(value);
            }
            get {
                return _visible;
            }
        }
        /// <summary>
        /// Is Under Mouse
        /// </summary>
        /// <returns></returns>
        public bool IsUnderMouse() {
            if (Control.MousePosition.X > X &&
                Control.MousePosition.X < X + Width &&
                Control.MousePosition.Y > Y &&
                Control.MousePosition.Y < Y + Height) return true;
            return false;
        }
        /// <summary>
        /// Visible
        /// </summary>
        public bool _visible;
        /// <summary>
        /// On Set Visible
        /// </summary>
        /// <param name="value"></param>
        public virtual void OnSetVisible(bool value) { }
        /// <summary>
        /// Variables
        /// </summary>
        public int X, Y, Width, Height;
        /// <summary>
        /// Remember normal bounds for restore
        /// </summary>
        private int _normX, _normY, _normW, _normH;
        /// <summary>
        /// Minimized or maximized state
        /// </summary>
        public bool IsMinimized { get; private set; }
        public bool IsMaximized { get; private set; }
        /// <summary>
        /// Controls whether this window shows in the taskbar
        /// </summary>
        public bool ShowInTaskbar = false;
        /// <summary>
        /// Controls visibility of Minimize/Maximize buttons
        /// </summary>
        public bool ShowMinimize = true;
        public bool ShowMaximize = true;
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="X"></param>
        /// <param name="Y"></param>
        /// <param name="Width"></param>
        /// <param name="Height"></param>
        public Window(int X, int Y, int Width, int Height) {
            this.X = X;
            this.Y = Y;
            this.Width = Width;
            this.Height = Height;
            _normX = X; _normY = Y; _normW = Width; _normH = Height;
            ClampToScreen();
            this.Visible = true;
            WindowManager.Windows.Add(this);
            Title = "Window1";
            TaskbarIcon = Icons.FileIcon;
            // Do not force MoveToEnd here to avoid modifying the list during input iteration
            // Callers can bring to front explicitly when appropriate
        }
        /// <summary>
        /// Bar Height
        /// </summary>
        public int BarHeight = 40;
        /// <summary>
        /// Title
        /// </summary>
        public string Title;
        /// <summary>
        /// Icon used on taskbar for this window
        /// </summary>
        public Image TaskbarIcon;
        /// <summary>
        /// Move
        /// </summary>
        bool Move;
        /// <summary>
        /// Offset X
        /// </summary>
        int OffsetX;
        /// <summary>
        /// Offset Y
        /// </summary>
        int OffsetY;
        /// <summary>
        /// Index
        /// </summary>
        public int Index { get => WindowManager.Windows.IndexOf(this); }
        /// <summary>
        /// On Input
        /// </summary>
        public virtual void OnInput() {
            if (!Visible) return;
            if (IsMinimized) return;

            if (Control.MouseButtons == MouseButtons.Left) {
                // Close
                if (
                    !WindowManager.HasWindowMoving &&
                    Control.MousePosition.X > CloseButtonX && Control.MousePosition.X < CloseButtonX + WindowManager.CloseButton.Width &&
                    Control.MousePosition.Y > CloseButtonY && Control.MousePosition.Y < CloseButtonY + WindowManager.CloseButton.Height
                ) {
                    this.Visible = false;
                    return;
                }
                // Minimize
                if (ShowMinimize && !WindowManager.HasWindowMoving && Control.MousePosition.X > MinButtonX && Control.MousePosition.X < MinButtonX + WindowManager.MinimizeButton.Width && Control.MousePosition.Y > ButtonsY && Control.MousePosition.Y < ButtonsY + WindowManager.MinimizeButton.Height) {
                    Minimize(); return;
                }
                // Maximize/Restore
                if (ShowMaximize && !WindowManager.HasWindowMoving && Control.MousePosition.X > MaxButtonX && Control.MousePosition.X < MaxButtonX + WindowManager.MaximizeButton.Width && Control.MousePosition.Y > ButtonsY && Control.MousePosition.Y < ButtonsY + WindowManager.MaximizeButton.Height) {
                    if (IsMaximized) Restore(); else Maximize(); return;
                }
                // Drag title bar
                if (!WindowManager.HasWindowMoving && !Move && Control.MousePosition.X > X && Control.MousePosition.X < X + Width && Control.MousePosition.Y > Y - BarHeight && Control.MousePosition.Y < Y) {
                    WindowManager.MoveToEnd(this);
                    Move = true;
                    WindowManager.HasWindowMoving = true;
                    OffsetX = Control.MousePosition.X - X;
                    OffsetY = Control.MousePosition.Y - Y;
                }
            } else {
                Move = false;
                WindowManager.HasWindowMoving = false;
            }

            if (Move) {
                X = Control.MousePosition.X - OffsetX;
                Y = Control.MousePosition.Y - OffsetY;
                ClampToScreen();
                _normX = X; _normY = Y; _normW = Width; _normH = Height;
            }
        }
        /// <summary>
        /// Button placement
        /// </summary>
        private int ButtonsY => Y - BarHeight + (BarHeight / 2) - (WindowManager.CloseButton.Height / 2);
        private int CloseButtonX => X + Width + 2 - (BarHeight / 2) - (WindowManager.CloseButton.Width / 2);
        private int CloseButtonY => Y - BarHeight + (BarHeight / 2) - (WindowManager.CloseButton.Height / 2);
        private int MaxButtonX => CloseButtonX - (WindowManager.MaximizeButton.Width + 6);
        private int MinButtonX => MaxButtonX - (WindowManager.MinimizeButton.Width + 6);
        /// <summary>
        /// On Draw
        /// </summary>
        public virtual void OnDraw() {
            if (!Visible) return;
            if (IsMinimized) return;
            if (Framebuffer.Graphics == null || WindowManager.font == null) return;
            // Semi-transparent title bar and content
            Framebuffer.Graphics.AFillRectangle(X, Y - BarHeight, Width, BarHeight, 0xCC111111);
            string title = Title;
            if (title == null) title = string.Empty;
            int measured = WindowManager.font.MeasureString(title);
            int tx = X + (Width / 2) - (measured / 2);
            int ty = Y - BarHeight + (BarHeight / 4);
            WindowManager.font.DrawString(tx, ty, title);
            // Buttons
            if (WindowManager.CloseButton != null)
                Framebuffer.Graphics.DrawImage(CloseButtonX, CloseButtonY, WindowManager.CloseButton);
            if (ShowMaximize && WindowManager.MaximizeButton != null)
                Framebuffer.Graphics.DrawImage(MaxButtonX, ButtonsY, WindowManager.MaximizeButton);
            if (ShowMinimize && WindowManager.MinimizeButton != null)
                Framebuffer.Graphics.DrawImage(MinButtonX, ButtonsY, WindowManager.MinimizeButton);
            // Content
            Framebuffer.Graphics.AFillRectangle(X, Y, Width, Height, 0xCC222222);
            DrawBorder();
        }
        /// <summary>
        /// Draw Border
        /// </summary>
        /// <param name="HasBar"></param>
        public void DrawBorder(bool HasBar = true) {
            if (Framebuffer.Graphics == null) return;
            Framebuffer.Graphics.DrawRectangle(X - 1, Y - (HasBar ? BarHeight : 0) - 1, Width + 2, (HasBar ? BarHeight : 0) + Height + 2, 0xFF333333, 1);
        }
        /// <summary>
        /// Clamp To Screen
        /// </summary>
        private void ClampToScreen() {
            int maxX = Framebuffer.Width - Width;
            if (X > maxX) X = maxX;
            if (X < 0) X = 0;
            int maxY = Framebuffer.Height - Height;
            if (Y > maxY) Y = maxY;
            if (Y < BarHeight) Y = BarHeight;
        }
        /// <summary>
        /// Minimize window
        /// </summary>
        public void Minimize() {
            if (IsMinimized) return;
            IsMinimized = true;
        }
        /// <summary>
        /// Restore from minimized or maximized
        /// </summary>
        public void Restore() {
            if (!IsMinimized && !IsMaximized) return;
            IsMinimized = false; IsMaximized = false;
            X = _normX; Y = _normY; Width = _normW; Height = _normH;
            ClampToScreen();
        }
        /// <summary>
        /// Maximize to full screen area (minus taskbar)
        /// </summary>
        public void Maximize() {
            if (IsMaximized) return;
            // remember
            _normX = X; _normY = Y; _normW = Width; _normH = Height;
            IsMaximized = true; IsMinimized = false;
            X = 0; Y = BarHeight; Width = Framebuffer.Width; Height = Framebuffer.Height - (BarHeight + WindowManagerMinBar());
        }
        private int WindowManagerMinBar() { return 40; } // taskbar height
    }
}