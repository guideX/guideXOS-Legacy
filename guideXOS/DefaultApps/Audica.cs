/*
using guideXOS.GUI;
using guideXOS.Misc;
using guideXOS.Kernel.Drivers;
using System.Windows.Forms;

namespace guideXOS.DefaultApps {
    // Minimal audio player shell (separate from WAVPlayer)
    internal class Audica : Window {
        private bool _clickLock;
        public Audica(int x, int y) : base(x, y, 360, 260) { Title = "Audica"; }
        public override void OnDraw() {
            base.OnDraw();
            int pad = 12; int w = Width - pad * 2; int h = Height - pad * 2; int x = X + pad; int y = Y + pad;
            Framebuffer.Graphics.AFillRectangle(x, y, w, h - 60, 0x80282828);
            WindowManager.font.DrawString(x + 8, y + 8, "Drop a .wav file from Computer Files or use WAV Player.", w - 16, WindowManager.font.FontSize * 3);
            // transport row placeholder
            int by = Y + Height - pad - 40; Framebuffer.Graphics.FillRectangle(x, by, w, 32, 0xFF2E2E2E);
            WindowManager.font.DrawString(x + 8, by + 8, "Play  Pause  Stop  <<  >>");
        }
        public override void OnInput() { base.OnInput(); if (Control.MouseButtons.HasFlag(MouseButtons.Left)) _clickLock = true; else _clickLock = false; }
    }
}*/