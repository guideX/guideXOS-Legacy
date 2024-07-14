using System.Windows.Forms;
namespace guideXOS.GUI {
    /// <summary>
    /// Right Menu
    /// </summary>
    internal class RightMenu : Window {
        /// <summary>
        /// Right Menu
        /// </summary>
        public RightMenu() : base(Control.MousePosition.X, Control.MousePosition.Y, 100, 50) {
            str = "..";
            Visible = false;
        }
        /// <summary>
        /// Str
        /// </summary>
        string str;
        /// <summary>
        /// On Set Visible
        /// </summary>
        /// <param name="value"></param>
        public override void OnSetVisible(bool value) {
            base.OnSetVisible(value);
            if (value) {
                X = Control.MousePosition.X - 8;
                Y = Control.MousePosition.Y - 8;
            }
        }
        /// <summary>
        /// On Input
        /// </summary>
        public override void OnInput() {
            if (Visible) {
                if (Control.MouseButtons.HasFlag(MouseButtons.Left)) {
                    if (IsUnderMouse() && Desktop.Dir.Length > 0) {
                        Desktop.Dir.Length--;

                        if (Desktop.Dir.IndexOf('/') != -1) {
                            string ndir = $"{Desktop.Dir.Substring(0, Desktop.Dir.LastIndexOf('/'))}/";
                            Desktop.Dir.Dispose();
                            Desktop.Dir = ndir;
                        } else {
                            Desktop.Dir = "";
                        }
                    }
                    this.Visible = false;
                }
            }
        }
        /// <summary>
        /// On Draw
        /// </summary>
        public override void OnDraw() {
            int len = WindowManager.font.MeasureString(str);
            Height = WindowManager.font.FontSize * 2;
            Width = len;
            Framebuffer.Graphics.FillRectangle(X, Y, Width, Height, 0xFF222222);
            WindowManager.font.DrawString(X, Y + (WindowManager.font.FontSize / 2), str);
            DrawBorder(false);
        }
    }
}