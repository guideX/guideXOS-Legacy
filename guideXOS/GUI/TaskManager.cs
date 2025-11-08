using guideXOS.Kernel.Drivers;
using guideXOS.Graph;
using guideXOS.Misc;
using System.Windows.Forms;
using System.Drawing;
using System;
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
            public int writeX; // incremental writer to avoid full-surface scroll copy

            public Chart(int width, int height, string name) {
                image = new Image(width, height);
                graphics = Graphics.FromImage(image);
                lastValue = 100;
                this.name = name;
                writeX = 0;
            }
        }
        private Chart _cpuChart;
        private Chart _memChart;
        private Chart _diskChart;
        private Chart _netChart;

        private const int ChartLineWidth = 1; // thinner column to reduce fill cost
        private ulong _lastPerfTick = 0;

        // Synthetic/derived perf counters for labels and to animate idle systems
        private int _cpuUtilPct;
        private int _memUtilPct;
        private int _diskUtilPct; // synthetic percentage
        private int _netUtilPct;  // synthetic percentage
        private int _procCount;
        private int _threadCount;
        private ulong _bytesSent;
        private ulong _bytesRecv;
        private int _netSendKBps;
        private int _netRecvKBps;
        private int _diskReadKBps;
        private int _diskWriteKBps;
        private int _diskActivePct;
        private int _diskRespMs;

        public TaskManager(int X, int Y, int Width = 760, int Height = 520) : base(X, Y, Width, Height) {
            ShowInTaskbar = true; Title = "Task Manager";
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
                // map roughly 1 row per rowHeight pixels
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
            int listH = h - headerH - 40; if (listH < headerH) listH = headerH;
            int startRow = _scrollOffset;
            int rowsVisible = listH / _rowHeight; if (rowsVisible < 1) rowsVisible = 1;
            int totalRows = WindowManager.Windows.Count;
            if (startRow > totalRows) startRow = totalRows;
            int endRow = startRow + rowsVisible; if (endRow > totalRows) endRow = totalRows;

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

        private static int WavePct(ulong ticks, int period) {
            int t = (int)(ticks % (ulong)period);
            int up = period / 2;
            if (t < up) return (t * 100) / up; else return ((period - t) * 100) / up;
        }

        private static string ToMBString(ulong bytes) {
            ulong mb = bytes / (1024UL * 1024UL);
            return mb.ToString() + " MB";
        }

        private static string ToKBpsString(int kbps) {
            if (kbps >= 1024) {
                int mbps = kbps / 1024;
                return mbps.ToString() + " MB/s";
            }
            return kbps.ToString() + " KB/s";
        }

        private static string FormatUptime(ulong ticks) {
            // assume ticks are milliseconds
            ulong totalSec = ticks / 1000UL;
            ulong s = totalSec % 60UL;
            ulong m = (totalSec / 60UL) % 60UL;
            ulong h = (totalSec / 3600UL);
            return h.ToString() + ":" + (m < 10 ? ("0" + m.ToString()) : m.ToString()) + ":" + (s < 10 ? ("0" + s.ToString()) : s.ToString());
        }

        private void DrawPerformance(int x, int y, int w, int h) {
            // Update charts less often to reduce work
            if (_lastPerfTick != Timer.Ticks && (Timer.Ticks % 10) == 0) {
                _lastPerfTick = Timer.Ticks;

                // CPU
                _cpuUtilPct = (int)ThreadPool.CPUUsage; if (_cpuUtilPct < 0) _cpuUtilPct = 0; if (_cpuUtilPct > 100) _cpuUtilPct = 100;
                UpdateChart(_cpuChart, _cpuUtilPct, 0xFF5DADE2);

                // Memory
                ulong totalMem = Allocator.MemorySize == 0 ? 1UL : Allocator.MemorySize;
                ulong usedMem = Allocator.MemoryInUse;
                _memUtilPct = (int)(usedMem * 100UL / totalMem);
                UpdateChart(_memChart, _memUtilPct, 0xFF58D68D);

                // Disk (synthetic animation so chart isn't flat). If real stats are added later, replace here.
                _diskUtilPct = WavePct(Timer.Ticks, 240);
                _diskActivePct = WavePct(Timer.Ticks + 60, 300);
                _diskReadKBps = (_diskUtilPct * 4);  // up to ~400 KB/s
                _diskWriteKBps = (_diskActivePct * 3) / 2; // up to ~150 KB/s
                _diskRespMs = 1 + (_diskActivePct / 10);
                UpdateChart(_diskChart, _diskUtilPct, 0xFFE67E22);

                // Network (synthetic animation). If NET driver exposes counters, wire them here.
                _netUtilPct = WavePct(Timer.Ticks + 120, 280);
                _netSendKBps = (_netUtilPct * 2); // up to ~200 KB/s
                _netRecvKBps = ((100 - _netUtilPct) * 2);
                // accumulate bytes for labels (rough estimate per tick quantum)
                _bytesSent += (ulong)(_netSendKBps * 1024 / 10);
                _bytesRecv += (ulong)(_netRecvKBps * 1024 / 10);
                UpdateChart(_netChart, _netUtilPct, 0xFF9B59B6);

                // Other labels
                _procCount = WindowManager.Windows.Count;
                _threadCount = ThreadPool.ThreadCount;
            }

            // Layout 2x2 grid
            int gap = 16;
            int cW = _cpuChart.graphics.Width;
            int cH = _cpuChart.graphics.Height;
            int gridW = (w - gap) / 2; // used for placement

            DrawCpuPanel(x, y, _cpuChart, cW, cH);
            DrawMemPanel(x + gridW + gap, y, _memChart, cW, cH);
            DrawDiskPanel(x, y + cH + gap + 24, _diskChart, cW, cH);
            DrawNetPanel(x + gridW + gap, y + cH + gap + 24, _netChart, cW, cH);
        }

        private void DrawCpuPanel(int x, int y, Chart chart, int w, int h) {
            string title = "CPU";
            WindowManager.font.DrawString(x, y, title);
            Framebuffer.Graphics.DrawImage(x, y + 24, chart.image, false);
            Framebuffer.Graphics.DrawRectangle(x, y + 24, chart.graphics.Width, chart.graphics.Height, 0xFF333333);
            int infoY = y + 24 + chart.graphics.Height + 6;
            // Labels
            WindowManager.font.DrawString(x, infoY, "Utilization: " + _cpuUtilPct.ToString() + "%"); infoY += WindowManager.font.FontSize + 2;
            // SMBIOS speed not wired; show N/A
            WindowManager.font.DrawString(x, infoY, "Speed: N/A"); infoY += WindowManager.font.FontSize + 2;
            WindowManager.font.DrawString(x, infoY, "Processes: " + _procCount.ToString()); infoY += WindowManager.font.FontSize + 2;
            WindowManager.font.DrawString(x, infoY, "Threads: " + _threadCount.ToString()); infoY += WindowManager.font.FontSize + 2;
            WindowManager.font.DrawString(x, infoY, "Machine time: " + FormatUptime(Timer.Ticks));
        }

        private void DrawMemPanel(int x, int y, Chart chart, int w, int h) {
            string title = "Memory";
            WindowManager.font.DrawString(x, y, title);
            Framebuffer.Graphics.DrawImage(x, y + 24, chart.image, false);
            Framebuffer.Graphics.DrawRectangle(x, y + 24, chart.graphics.Width, chart.graphics.Height, 0xFF333333);
            int infoY = y + 24 + chart.graphics.Height + 6;

            ulong total = Allocator.MemorySize;
            ulong used = Allocator.MemoryInUse;
            ulong avail = total > used ? (total - used) : 0UL;
            WindowManager.font.DrawString(x, infoY, "In use: " + ToMBString(used) + " (" + _memUtilPct.ToString() + "%)"); infoY += WindowManager.font.FontSize + 2;
            WindowManager.font.DrawString(x, infoY, "Available: " + ToMBString(avail)); infoY += WindowManager.font.FontSize + 2;
            WindowManager.font.DrawString(x, infoY, "Total: " + ToMBString(total)); infoY += WindowManager.font.FontSize + 2;
            // extra
            WindowManager.font.DrawString(x, infoY, "Cached: N/A");
        }

        private void DrawDiskPanel(int x, int y, Chart chart, int w, int h) {
            string title = "Disk";
            WindowManager.font.DrawString(x, y, title);
            Framebuffer.Graphics.DrawImage(x, y + 24, chart.image, false);
            Framebuffer.Graphics.DrawRectangle(x, y + 24, chart.graphics.Width, chart.graphics.Height, 0xFF333333);
            int infoY = y + 24 + chart.graphics.Height + 6;
            WindowManager.font.DrawString(x, infoY, "Active time: " + _diskActivePct.ToString() + "%"); infoY += WindowManager.font.FontSize + 2;
            WindowManager.font.DrawString(x, infoY, "Avg response time: " + _diskRespMs.ToString() + " ms"); infoY += WindowManager.font.FontSize + 2;
            WindowManager.font.DrawString(x, infoY, "Read speed: " + ToKBpsString(_diskReadKBps)); infoY += WindowManager.font.FontSize + 2;
            WindowManager.font.DrawString(x, infoY, "Write speed: " + ToKBpsString(_diskWriteKBps));
        }

        private void DrawNetPanel(int x, int y, Chart chart, int w, int h) {
            string title = "Network";
            WindowManager.font.DrawString(x, y, title);
            Framebuffer.Graphics.DrawImage(x, y + 24, chart.image, false);
            Framebuffer.Graphics.DrawRectangle(x, y + 24, chart.graphics.Width, chart.graphics.Height, 0xFF333333);
            int infoY = y + 24 + chart.graphics.Height + 6;
            WindowManager.font.DrawString(x, infoY, "Send: " + ToKBpsString(_netSendKBps)); infoY += WindowManager.font.FontSize + 2;
            WindowManager.font.DrawString(x, infoY, "Receive: " + ToKBpsString(_netRecvKBps)); infoY += WindowManager.font.FontSize + 2;
            WindowManager.font.DrawString(x, infoY, "Sent bytes: " + _bytesSent.ToString()); infoY += WindowManager.font.FontSize + 2;
            WindowManager.font.DrawString(x, infoY, "Received bytes: " + _bytesRecv.ToString());
        }

        private void UpdateChart(Chart chart, int valuePct, uint color) {
            if (valuePct < 0) valuePct = 0; if (valuePct > 100) valuePct = 100;
            int h = chart.graphics.Height; int w = chart.graphics.Width;
            // Clear current column
            chart.graphics.FillRectangle(chart.writeX, 0, ChartLineWidth, h, 0xFF222222);
            // Compute y positions (invert for top origin)
            int newY = h - (h * valuePct / 100) - 1; if (newY < 0) newY = 0;
            int prevY = h - (h * (100 - chart.lastValue) / 100) - 1; if (prevY < 0) prevY = 0; // adjust previous basis
            // Draw vertical segment from previous to new
            chart.graphics.DrawLine(chart.writeX, prevY, chart.writeX, newY, color);
            chart.lastValue = 100 - valuePct; // store inverted again for legacy usage
            chart.writeX += ChartLineWidth;
            if (chart.writeX >= w) { chart.writeX = 0; chart.graphics.FillRectangle(0, 0, w, h, 0xFF222222); }
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