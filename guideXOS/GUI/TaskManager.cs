using guideXOS.Kernel.Drivers;
using guideXOS.Graph;
using guideXOS.Misc;
using System.Windows.Forms;
using System.Drawing;
namespace guideXOS.GUI {
    /// <summary>
    /// Task Manager window with Processes and Performance tabs
    /// </summary>
    internal class TaskManager : Window {
        // Tabs
        private int _tabH = 28;
        private int _tabGap = 6;
        private int _currentTab = 0; // 0 = Processes, 1 = Performance

        // Layout
        private int _padding = 10;
        private int _rowHeight = 24;
        private int _scrollOffset = 0;
        private bool _scrollDrag = false;
        private int _scrollStartY, _scrollStartOffset;

        // Selection
        private int _selectedIndex = -1;

        // Performance charts
        class Chart {
            public Image image;
            public Graphics graphics;
            public int lastValue;
            public string name;

            public Chart(int width, int height, string name) {
                image = new Image(width, height);
                graphics = Graphics.FromImage(image);
                lastValue = 100;
                this.name = name;
            }
        }
        private Chart _cpuChart;
        private Chart _memChart;
        private Chart _diskChart;
        private Chart _netChart;

        private const int ChartLineWidth = 3;
        private ulong _lastPerfTick = 0;

        public TaskManager(int X, int Y, int Width = 760, int Height = 520) : base(X, Y, Width, Height) {
            ShowInTaskbar = true;
            Title = "Task Manager";
            // Initialize charts
            int chartW = 280, chartH = 120;
            _cpuChart = new Chart(chartW, chartH, "CPU");
            _memChart = new Chart(chartW, chartH, "Memory");
            _diskChart = new Chart(chartW, chartH, "Disk");
            _netChart = new Chart(chartW, chartH, "Network");
        }

        public override void OnInput() {
            base.OnInput();
            if (!Visible || IsMinimized) return;

            int cx = X + _padding;
            int cy = Y + _padding;
            int cw = Width - _padding * 2;
            int contentY = cy + _tabH + _tabGap;
            int ch = Height - (contentY - (Y)) - _padding;

            int mx = Control.MousePosition.X;
            int my = Control.MousePosition.Y;

            if (Control.MouseButtons == MouseButtons.Left) {
                // Tab clicks
                int tabW = (cw - _tabGap) / 2;
                int t0x = cx;
                int t1x = cx + tabW + _tabGap;
                if (my >= cy && my <= cy + _tabH) {
                    if (mx >= t0x && mx <= t0x + tabW) { _currentTab = 0; return; }
                    if (mx >= t1x && mx <= t1x + tabW) { _currentTab = 1; return; }
                }

                if (_currentTab == 0) {
                    // Processes tab
                    // Hit-test list area
                    int headerH = _rowHeight;
                    int listX = cx;
                    int listY = contentY;
                    int listW = cw;
                    int listH = ch - (_rowHeight + 12); // leave space for footer button

                    // Scrollbar area
                    int sbW = 10;
                    int sbX = X + Width - _padding - sbW;
                    if (mx >= sbX && mx <= sbX + sbW && my >= listY && my <= listY + listH) {
                        if (!_scrollDrag) { _scrollDrag = true; _scrollStartY = my; _scrollStartOffset = _scrollOffset; }
                    }

                    // Row selection
                    if (mx >= listX && mx <= listX + listW && my >= listY + headerH && my <= listY + listH) {
                        int relativeY = (my - (listY + headerH)) + (_scrollOffset * _rowHeight);
                        int row = relativeY / _rowHeight;
                        if (row < 0) row = 0;
                        if (row < WindowManager.Windows.Count) _selectedIndex = row;
                    }

                    // Footer button End Task
                    int btnW = 120, btnH = 28;
                    int btnX = X + Width - _padding - btnW;
                    int btnY = Y + Height - _padding - btnH;
                    if (mx >= btnX && mx <= btnX + btnW && my >= btnY && my <= btnY + btnH) {
                        OnEndTask();
                    }
                }
            } else if (Control.MouseButtons.HasFlag(MouseButtons.None)) {
                _scrollDrag = false;
            }

            // Handle scroll dragging
            if (_scrollDrag) {
                int headerH = _rowHeight;
                int listY = contentY;
                int listH = ch - (_rowHeight + 12);
                int maxRows = WindowManager.Windows.Count;
                int rowsVisible = (listH - headerH) / _rowHeight; if (rowsVisible < 1) rowsVisible = 1;
                int maxScroll = maxRows - rowsVisible; if (maxScroll < 0) maxScroll = 0;
                int dy = my - _scrollStartY;
                int trackH = listH - headerH;
                if (trackH < 1) trackH = 1;
                // crude mapping: 1 pixel = 1 row
                _scrollOffset = _scrollStartOffset + (dy / (_rowHeight));
                if (_scrollOffset < 0) _scrollOffset = 0;
                if (_scrollOffset > maxScroll) _scrollOffset = maxScroll;
            }
        }

        public override void OnDraw() {
            base.OnDraw();

            int cx = X + _padding;
            int cy = Y + _padding;
            int cw = Width - _padding * 2;
            int chAll = Height - _padding * 2;
            int contentY = cy + _tabH + _tabGap;
            int ch = chAll - _tabH - _tabGap;

            // Tabs background
            int tabW = (cw - _tabGap) / 2;
            DrawTab(cx, cy, tabW, _tabH, "Processes", _currentTab == 0);
            DrawTab(cx + tabW + _tabGap, cy, tabW, _tabH, "Performance", _currentTab == 1);

            // Content panel
            Framebuffer.Graphics.FillRectangle(cx, contentY, cw, ch, 0xFF1E1E1E);
            Framebuffer.Graphics.DrawRectangle(cx - 1, contentY - 1, cw + 2, ch + 2, 0xFF333333, 1);

            if (_currentTab == 0) DrawProcesses(cx, contentY, cw, ch);

            else DrawPerformance(cx, contentY, cw, ch);
        }

        private void DrawTab(int x, int y, int w, int h, string title, bool selected) {
            uint bg = selected ? 0xFF2C2C2C : 0xFF242424;
            Framebuffer.Graphics.FillRectangle(x, y, w, h, bg);
            Framebuffer.Graphics.DrawRectangle(x, y, w, h, 0xFF3A3A3A, 1);
            int tx = x + (w / 2) - (WindowManager.font.MeasureString(title) / 2);
            int ty = y + (h / 2) - (WindowManager.font.FontSize / 2);
            WindowManager.font.DrawString(tx, ty, title);
        }

        private void DrawProcesses(int x, int y, int w, int h) {
            int headerH = _rowHeight;

            // columns: Name, CPU%, Memory%, Disk%, Network%
            int colNameW = w - 320; if (colNameW < 120) colNameW = 120;
            int colCpuW = 60;
            int colMemW = 80;
            int colDiskW = 80;
            int colNetW = 80;

            // Header
            int hx = x;
            int hy = y;
            DrawHeaderCell(hx, hy, colNameW, headerH, "Name"); hx += colNameW;
            DrawHeaderCell(hx, hy, colCpuW, headerH, "CPU%"); hx += colCpuW;
            DrawHeaderCell(hx, hy, colMemW, headerH, "Memory%"); hx += colMemW;
            DrawHeaderCell(hx, hy, colDiskW, headerH, "Disk%"); hx += colDiskW;
            DrawHeaderCell(hx, hy, colNetW, headerH, "Network%");

            // List area
            int listY = y + headerH;
            int listH = h - headerH - 40; if (listH < _rowHeight) listH = _rowHeight;
            int startRow = _scrollOffset;
            int rowsVisible = listH / _rowHeight; if (rowsVisible < 1) rowsVisible = 1;
            int endRow = startRow + rowsVisible; if (endRow > WindowManager.Windows.Count) endRow = WindowManager.Windows.Count;

            int dy = listY;
            for (int i = startRow; i < endRow; i++) {
                var wdw = WindowManager.Windows[i];
                bool sel = (i == _selectedIndex);
                uint bg = sel ? 0xFF2A2A2A : 0x00000000u;
                if (bg != 0) Framebuffer.Graphics.FillRectangle(x, dy, w, _rowHeight, bg);
                int cx = x;
                string name = wdw.Title ?? "(no title)";
                WindowManager.font.DrawString(cx + 6, dy + 6, name, colNameW - 12, WindowManager.font.FontSize);
                cx += colNameW;
                // No per-window metrics available yet -> show dashes
                WindowManager.font.DrawString(cx + 6, dy + 6, "-"); cx += colCpuW;
                WindowManager.font.DrawString(cx + 6, dy + 6, "-"); cx += colMemW;
                WindowManager.font.DrawString(cx + 6, dy + 6, "-"); cx += colDiskW;
                WindowManager.font.DrawString(cx + 6, dy + 6, "-");
                dy += _rowHeight;
            }

            // Scrollbar
            int sbW = 10;
            int sbX = X + Width - _padding - sbW;
            Framebuffer.Graphics.FillRectangle(sbX, listY, sbW, listH, 0xFF1A1A1A);
            int totalRows = WindowManager.Windows.Count;
            int thumbH = (rowsVisible * listH) / (totalRows == 0 ? 1 : totalRows); if (thumbH < 16) thumbH = 16; if (thumbH > listH) thumbH = listH;
            int maxScroll = (totalRows - rowsVisible); if (maxScroll < 0) maxScroll = 0;
            int thumbY = listY + (maxScroll == 0 ? 0 : (_scrollOffset * (listH - thumbH)) / (maxScroll));
            Framebuffer.Graphics.FillRectangle(sbX, thumbY, sbW, thumbH, 0xFF3A3A3A);

            // Footer - End Task button
            int btnW = 120, btnH = 28;
            int btnX = X + Width - _padding - btnW;
            int btnY = Y + Height - _padding - btnH;
            Framebuffer.Graphics.FillRectangle(btnX, btnY, btnW, btnH, 0xFF3B1E1E);
            WindowManager.font.DrawString(btnX + 10, btnY + (btnH / 2 - WindowManager.font.FontSize / 2), "End Task");
        }

        private void DrawHeaderCell(int x, int y, int w, int h, string text) {
            Framebuffer.Graphics.FillRectangle(x, y, w, h, 0xFF252525);
            Framebuffer.Graphics.DrawRectangle(x, y, w, h, 0xFF333333, 1);
            WindowManager.font.DrawString(x + 6, y + (h / 2 - WindowManager.font.FontSize / 2), text);
        }

        private void DrawPerformance(int x, int y, int w, int h) {
            // Update charts every few ticks
            if (_lastPerfTick != Timer.Ticks && (Timer.Ticks % 5) == 0) {
                _lastPerfTick = Timer.Ticks;
                UpdateChart(_cpuChart, (int)ThreadPool.CPUUsage, 0xFF5DADE2);
                int memPct = (int)(Allocator.MemoryInUse * 100 / (Allocator.MemorySize == 0 ? 1 : Allocator.MemorySize));
                UpdateChart(_memChart, memPct, 0xFF58D68D);
                // Disk/Network not yet instrumented -> draw zeros
                UpdateChart(_diskChart, 0, 0xFFE67E22);
                UpdateChart(_netChart, 0, 0xFF9B59B6);
            }

            // Layout 2x2 grid
            int gap = 16;
            int cW = _cpuChart.graphics.Width;
            int cH = _cpuChart.graphics.Height;
            int gridW = (w - gap) / 2; // not used for sizing chart, used for placement

            DrawChartPanel(x, y, _cpuChart, cW, cH, "CPU");
            DrawChartPanel(x + gridW + gap, y, _memChart, cW, cH, "Memory");
            DrawChartPanel(x, y + cH + gap + 24, _diskChart, cW, cH, "Disk");
            DrawChartPanel(x + gridW + gap, y + cH + gap + 24, _netChart, cW, cH, "Network");
        }

        private void DrawChartPanel(int x, int y, Chart chart, int w, int h, string title) {
            // Title
            WindowManager.font.DrawString(x, y, title);
            // Image below title
            Framebuffer.Graphics.DrawImage(x, y + 24, chart.image, true);
            Framebuffer.Graphics.DrawRectangle(x, y + 24, chart.graphics.Width, chart.graphics.Height, 0xFF333333);
        }

        private void UpdateChart(Chart chart, int valuePct, uint color) {
            if (valuePct < 0) valuePct = 0; if (valuePct > 100) valuePct = 100;
            int val = 100 - valuePct; // invert for top origin
            chart.graphics.FillRectangle(chart.graphics.Width - ChartLineWidth, 0, ChartLineWidth, chart.graphics.Height, 0xFF222222);
            chart.graphics.DrawLine(chart.graphics.Width - ChartLineWidth, (chart.graphics.Height / 100) * chart.lastValue, chart.graphics.Width, (chart.graphics.Height / 100) * val, color);
            chart.lastValue = val;
            chart.graphics.Copy(-ChartLineWidth, 0, 0, 0, chart.graphics.Width, chart.graphics.Height);
        }

        private void OnEndTask() {
            if (_selectedIndex < 0) return;
            if (_selectedIndex >= WindowManager.Windows.Count) { _selectedIndex = -1; return; }

            Window target = WindowManager.Windows[_selectedIndex];
            if (target == this) return; // do not end self via button

            WindowManager.Windows.RemoveAt(_selectedIndex);
            if (_selectedIndex >= WindowManager.Windows.Count) _selectedIndex = WindowManager.Windows.Count - 1;
        }
    }
}