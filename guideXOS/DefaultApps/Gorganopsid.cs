/*
using guideXOS.GUI;
using guideXOS.Misc;
using guideXOS.Kernel.Drivers;
using System.Windows.Forms;

namespace guideXOS.DefaultApps {
    // Minimal FTP client stub
    internal class Gorganopsid : Window {
        private string _host = "ftp.example.com";
        private string _status = "Disconnected";
        private bool _clickLock;
        public Gorganopsid(int x, int y) : base(x, y, 740, 500) { Title = "Gorganopsid"; }
        public override void OnDraw() {
            base.OnDraw();
            int pad = 10; int cx = X + pad; int cy = Y + pad; int cw = Width - pad * 2; int ch = Height - pad * 2;
            // connection bar
            Framebuffer.Graphics.FillRectangle(cx, cy, cw, 28, 0xFF2E2E2E);
            WindowManager.font.DrawString(cx + 8, cy + 6, $"Host: {_host}  Status: {_status}");
            // local/remote split
            int splitY = cy + 28 + 8; int listH = (ch - 28 - 8 - 40) / 2;
            Framebuffer.Graphics.AFillRectangle(cx, splitY, cw, listH, 0x80262626);
            WindowManager.font.DrawString(cx + 8, splitY + 8, "Local Files (not implemented)");
            int remoteY = splitY + listH + 8;
            Framebuffer.Graphics.AFillRectangle(cx, remoteY, cw, listH, 0x80282828);
            WindowManager.font.DrawString(cx + 8, remoteY + 8, "Remote Files (not implemented)");
            // status bar
            int sbY = Y + Height - pad - 24; Framebuffer.Graphics.FillRectangle(cx, sbY, cw, 24, 0xFF252525);
            WindowManager.font.DrawString(cx + 8, sbY + 4, "Use toolbar to connect. Functional FTP not implemented.");
        }
        public override void OnInput() { base.OnInput(); if (Control.MouseButtons.HasFlag(MouseButtons.Left)) _clickLock = true; else _clickLock = false; }
    }
}
*/