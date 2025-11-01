using guideXOS.Kernel.Drivers;
using System.Drawing;
namespace guideXOS.GUI {
    /// <summary>
    /// Image Viewer
    /// </summary>
    internal class ImageViewer : Window {
        /// <summary>
        /// Image
        /// </summary>
        Image image;
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="X"></param>
        /// <param name="Y"></param>
        public ImageViewer(int X, int Y) : base(X, Y, 250, 200) {
            ShowInTaskbar = true;
            image = null;
            Title = "ImageViewer";
        }
        /// <summary>
        /// On Draw
        /// </summary>
        public override void OnDraw() {
            base.OnDraw();
            if (image != null)
                Framebuffer.Graphics.DrawImage(X + (Width / 2) - (image.Width / 2), Y + (Height / 2) - (image.Height / 2), image);
        }
        /// <summary>
        /// Set Image
        /// </summary>
        /// <param name="image"></param>
        public void SetImage(Image image) {
            if (this.image != null) {
                this.image.Dispose();
            }
            if (image.Width > image.Height) {
                float ratio = image.Height / (float)image.Width;
                this.image = image.ResizeImage((int)(Width * 0.8f), (int)(Width * ratio * 0.8f));
            } else {
                float ratio = image.Height / (float)image.Width;
                this.image = image.ResizeImage((int)(Height * 0.8f), (int)(Height * ratio * 0.8f));
            }
        }
    }
}