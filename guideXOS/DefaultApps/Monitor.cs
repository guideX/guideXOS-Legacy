using guideXOS.Graph;
using guideXOS.GUI;
using guideXOS.Kernel.Drivers;
using guideXOS.Misc;
using System.Drawing;
namespace guideXOS.DefaultApps {
    internal class Monitor : Window {
        class Chart { public Image image; public Graphics graphics; public int lastValue; public string name; public int writeX; public Chart(int Width, int Height, string Name) { image = new Image(Width, Height); graphics = Graphics.FromImage(image); lastValue = 100; name = Name; writeX = 0; } }
        Chart CPUUsage; Chart RAMUsage;
        public Monitor(int X, int Y) : base(X, Y, 200 - 1, 120) { Title = "System Monitor"; CPUUsage = new Chart(100, 100, "CPU"); RAMUsage = new Chart(100, 100, "RAM"); ShowInTaskbar = true; }
        const int LineWidth = 1;
        public override void OnDraw() {
            base.OnDraw(); if (!Visible) return;
            if (Timer.Ticks % 5 == 0) { DrawLineChart((int)ThreadPool.CPUUsage, ref CPUUsage.lastValue, CPUUsage, 0xFF5DADE2); DrawLineChart((int)(Allocator.MemoryInUse * 100 / (Allocator.MemorySize==0?1:Allocator.MemorySize)), ref RAMUsage.lastValue, RAMUsage, 0xFF58D68D); }
            int aX = 0; Render(ref aX, CPUUsage, (int)ThreadPool.CPUUsage); Render(ref aX, RAMUsage, (int)(Allocator.MemoryInUse * 100 / (Allocator.MemorySize==0?1:Allocator.MemorySize))); DrawBorder(); }
        private void Render(ref int aX, Chart chart, int pct) { WindowManager.font.DrawString(X + aX + chart.graphics.Width / 2 - WindowManager.font.MeasureString(chart.name) / 2, Y, chart.name + " " + pct.ToString() + "%"); Framebuffer.Graphics.DrawImage(X + aX, Y + Height - chart.graphics.Height, chart.image, true); Framebuffer.Graphics.DrawRectangle(X + aX, Y, chart.graphics.Width, Height, 0xFF333333); aX += chart.graphics.Width; aX -= 1; }
        private void DrawLineChart(int value, ref int lastValue, Chart chart, uint Color) { int h = chart.graphics.Height; int w = chart.graphics.Width; int val = value; if (val<0) val=0; if (val>100) val=100; int y = h - (h * val / 100) - 1; if (y<0) y=0; // clear column
            chart.graphics.FillRectangle(chart.writeX, 0, LineWidth, h, 0xFF222222); // draw line from previous value
            int prevY = h - (h * lastValue / 100) - 1; if (prevY<0) prevY=0; chart.graphics.DrawLine(chart.writeX, prevY, chart.writeX, y, Color); lastValue = val; chart.writeX += LineWidth; if (chart.writeX >= w) { chart.writeX = 0; // wrap: fade entire surface
                chart.graphics.FillRectangle(0,0,w,h,0xFF222222); }
        }
    }
}