using guideXOS.Kernel.Drivers;
using guideXOS.Graph;
using guideXOS.Misc;
using System.Windows.Forms;
using System.Drawing;
using System;
using guideXOS.GUI;
using System.Collections.Generic;

namespace guideXOS.DefaultApps {
    /// <summary>
    /// Task Manager window with Processes and Performance tabs
    /// </summary>
    internal class TaskManager : Window {
        // Tabs
        private int _tabH = 28;
        private int _tabGap = 6;
        private int _currentTab = 0; // 0 = Processes, 1 = Performance, 2 = Tombstoned

        // Layout
        private int _padding = 10;
        private int _rowHeight = 24;
        private int _scrollOffset = 0;
        private bool _scrollDrag = false;
        private int _scrollStartY,
            _scrollStartOffset;

        // Selection
        private int _selectedIndex = -1;
        private int _selectedTombIndex = -1;

        // Performance tab navigation
        private int _selectedPerfCategory = 0; // 0 = CPU, 1 = Memory, 2 = Disk, 3 = Network
        private bool _perfNavClickLatch = false;

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
                // Initialize with background color
                graphics.FillRectangle(0, 0, width, height, 0xFF222222);
                lastValue = 0; // Start at 0 instead of 100
                this.name = name;
                writeX = 0;
            }

            // Method to resize chart if needed
            public void EnsureSize(int width, int height) {
                if (image.Width != width || image.Height != height) {
                    // Create new chart with new dimensions
                    image = new Image(width, height);
                    graphics = Graphics.FromImage(image);
                    graphics.FillRectangle(0, 0, width, height, 0xFF222222);
                    writeX = 0;
                    lastValue = 0;
                }
            }
        }
        private Chart _cpuChart;
        private Chart _memChart;
        private Chart _diskChart;
        private Chart _netChart;

        private const int ChartLineWidth = 4; // wider column to make updates more visible
        private ulong _lastPerfTick = 0;

        // Synthetic/derived perf counters for labels and to animate idle systems
        private int _cpuUtilPct;
        private int _memUtilPct;
        private int _diskUtilPct; // synthetic percentage
        private int _netUtilPct; // synthetic percentage
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

        // Owner memory sampling for leak detection
        private Dictionary<int, ulong> _lastOwnerBytes;
        private Dictionary<int, int> _ownerKBps; // KB per second (positive = growth)
        private ulong _lastOwnerSampleTick;

        // Flag to enable perf tracking on first draw (not in constructor to prevent freeze)
        private bool _perfTrackingInitialized = false;

        public TaskManager(int X, int Y, int Width = 760, int Height = 520)
            : base(X, Y, Width, Height) {
            ShowMinimize = true;
            ShowInTaskbar = true;
            ShowMaximize = true;
            //ShowRestore = false;
            ShowTombstone = false;
            IsResizable = true;
            Title = "Task Manager";
            // Initial chart size - will be resized dynamically
            int chartW = 480,
                chartH = 180;
            _cpuChart = new Chart(chartW, chartH, "CPU");
            _memChart = new Chart(chartW, chartH, "Memory");
            _diskChart = new Chart(chartW, chartH, "Disk");
            _netChart = new Chart(chartW, chartH, "Network");

            // DON'T enable perf tracking here - it causes freeze during construction
            //g WindowManager.EnablePerfTracking();

            // initialize owner sampling
            _lastOwnerBytes = new Dictionary<int, ulong>();
            _ownerKBps = new Dictionary<int, int>();
            _lastOwnerSampleTick = Timer.Ticks;
        }

        public override void OnSetVisible(bool value) {
            base.OnSetVisible(value);
            if (!value) { // when hiding, stop perf tracking to reduce contention
                WindowManager.DisablePerfTracking();
            }
        }

        public override void OnInput() {
            base.OnInput();
            if (!Visible || IsMinimized)
                return;
            int cx = X + _padding;
            int cy = Y + _padding;
            int cw = Width - _padding * 2;
            int contentY = cy + _tabH + _tabGap;
            int ch = Height - (contentY - Y) - _padding;
            int mx = Control.MousePosition.X;
            int my = Control.MousePosition.Y;

            // If right mouse button is pressed within this window, mark input as handled so desktop doesn't open its context menu
            if (Control.MouseButtons.HasFlag(MouseButtons.Right)) {
                if (mx >= X && mx <= X + Width && my >= Y && my <= Y + Height) {
                    WindowManager.MouseHandled = true;
                }
            }

            if (Control.MouseButtons == MouseButtons.Left) {
                // Tab clicks
                int tabCount = 3;
                int tabW = (cw - _tabGap * (tabCount - 1)) / tabCount;
                int tx = cx;
                for (int t = 0; t < tabCount; t++) {
                    int tx2 = tx;
                    if (my >= cy && my <= cy + _tabH && mx >= tx2 && mx <= tx2 + tabW) {
                        _currentTab = t;
                        return;
                    }
                    tx += tabW + _tabGap;
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
                        if (!_scrollDrag) {
                            _scrollDrag = true;
                            _scrollStartY = my;
                            _scrollStartOffset = _scrollOffset;
                        }
                    }

                    // Row selection
                    if (
                        mx >= listX
                        && mx <= listX + listW
                        && my >= listY + headerH
                        && my <= listY + listH
                    ) {
                        int relativeY = my - (listY + headerH) + _scrollOffset * _rowHeight;
                        int row = relativeY / _rowHeight;
                        if (row < 0)
                            row = 0;
                        if (row < WindowManager.Windows.Count)
                            _selectedIndex = row;
                    }

                    // Footer button End Task
                    int btnW = 120,
                        btnH = 28;
                    int btnX = X + Width - _padding - btnW;
                    int btnY = Y + Height - _padding - btnH;
                    if (mx >= btnX && mx <= btnX + btnW && my >= btnY && my <= btnY + btnH) {
                        OnEndTask();
                    }
                } else if (_currentTab == 1) {
                    // Performance tab navigation
                    if (!_perfNavClickLatch) {
                        int navW = (int)(cw * 0.25f); // 25% for navigation pane
                        int navX = cx;
                        int navY = contentY;
                        int itemH = 70; // height for each navigation item

                        // Check if clicking on navigation items
                        for (int i = 0; i < 4; i++) {
                            int itemY = navY + i * itemH;
                            if (
                                mx >= navX
                                && mx <= navX + navW
                                && my >= itemY
                                && my <= itemY + itemH
                            ) {
                                _selectedPerfCategory = i;
                                _perfNavClickLatch = true;
                                break;
                            }
                        }
                    }
                } else if (_currentTab == 2) {
                    // Tombstoned tab
                    // Hit-test list area
                    int headerH = _rowHeight;
                    int listX = cx;
                    int listY = contentY;
                    int listW = cw;
                    int listH = ch - (_rowHeight + 60); // leave space for buttons

                    // Row selection
                    if (
                        mx >= listX
                        && mx <= listX + listW
                        && my >= listY + headerH
                        && my <= listY + listH
                    ) {
                        int row = (my - (listY + headerH)) / _rowHeight;
                        if (row < 0)
                            row = 0;
                        int tombCount = CountTombstoned();
                        if (row < tombCount)
                            _selectedTombIndex = row;
                    }

                    // Buttons
                    int btnW = 150,
                        btnH = 28;
                    int btnRestoreX = X + Width - _padding - btnW;
                    int btnRestoreY = Y + Height - _padding - btnH * 2 - 8;
                    int btnEndX = X + Width - _padding - btnW;
                    int btnEndY = Y + Height - _padding - btnH;
                    if (
                        mx >= btnRestoreX
                        && mx <= btnRestoreX + btnW
                        && my >= btnRestoreY
                        && my <= btnRestoreY + btnH
                    ) {
                        RestoreTombstoned();
                    }

                    if (
                        mx >= btnEndX
                        && mx <= btnEndX + btnW
                        && my >= btnEndY
                        && my <= btnEndY + btnH
                    ) {
                        EndTombstoned();
                    }
                }
            } else if (Control.MouseButtons.HasFlag(MouseButtons.None)) {
                _scrollDrag = false;
                _perfNavClickLatch = false;
            }

            // Handle scroll dragging
            if (_scrollDrag) {
                int headerH = _rowHeight;
                int listH = ch - (_rowHeight + 12);
                int maxRows = WindowManager.Windows.Count;
                int rowsVisible = (listH - headerH) / _rowHeight;
                if (rowsVisible < 1)
                    rowsVisible = 1;
                int maxScroll = maxRows - rowsVisible;
                if (maxScroll < 0)
                    maxScroll = 0;
                int dy = my - _scrollStartY;
                _scrollOffset = _scrollStartOffset + dy / _rowHeight;
                if (_scrollOffset < 0)
                    _scrollOffset = 0;
                if (_scrollOffset > maxScroll)
                    _scrollOffset = maxScroll;
            }
        }

        public override void OnDraw() {
            base.OnDraw();

            // Perf tracking is now enabled OUTSIDE of draw loop (in Program.cs after all windows created)
            // so there's no reentrancy issue - we don't enable it here anymore
            if (!_perfTrackingInitialized) {
                _perfTrackingInitialized = true;
                // WindowManager.EnablePerfTracking();  // ← MOVED to Program.cs to avoid reentrancy
            }

            int cx = X + _padding;
            int cy = Y + _padding;
            int cw = Width - _padding * 2;
            int chAll = Height - _padding * 2;
            int contentY = cy + _tabH + _tabGap;
            int ch = chAll - _tabH - _tabGap;

            // Tabs background
            int tabCount = 3;
            int tabW = (cw - _tabGap * (tabCount - 1)) / tabCount;
            int tx = cx;
            DrawTab(tx, cy, tabW, _tabH, "Processes", _currentTab == 0);
            tx += tabW + _tabGap;
            DrawTab(tx, cy, tabW, _tabH, "Performance", _currentTab == 1);
            tx += tabW + _tabGap;
            DrawTab(tx, cy, tabW, _tabH, "Tombstoned", _currentTab == 2);

            // Content panel
            Framebuffer.Graphics.FillRectangle(cx, contentY, cw, ch, 0xFF1E1E1E);
            Framebuffer.Graphics.DrawRectangle(cx - 1, contentY - 1, cw + 2, ch + 2, 0xFF333333, 1);

            if (_currentTab == 0)
                DrawProcesses(cx, contentY, cw, ch);
            else if (_currentTab == 1)
                DrawPerformance(cx, contentY, cw, ch);
            else
                DrawTombstoned(cx, contentY, cw, ch);
        }

        private void DrawTombstoned(int x, int y, int w, int h) {
            int headerH = _rowHeight;
            DrawHeaderCell(x, y, w, headerH, "Tombstoned Apps");
            int listY = y + headerH;
            int listH = h - headerH - 60; // leave space for buttons
            int dy = listY;
            int tombCount = CountTombstoned();
            for (int i = 0; i < tombCount; i++) {
                var win = GetTombstonedAt(i);
                bool sel = i == _selectedTombIndex;
                if (sel)
                    Framebuffer.Graphics.FillRectangle(x, dy, w, _rowHeight, 0xFF2A2A2A);
                string name = win != null ? win.Title ?? "(no title)" : "(null)";
                WindowManager.font.DrawString(
                    x + 6,
                    dy + 6,
                    name,
                    w - 12,
                    WindowManager.font.FontSize
                );
                dy += _rowHeight;
            }

            // Buttons
            int btnW = 150,
                btnH = 28;
            int btnRestoreX = X + Width - _padding - btnW;
            int btnRestoreY = Y + Height - _padding - btnH * 2 - 8;
            int btnEndX = X + Width - _padding - btnW;
            int btnEndY = Y + Height - _padding - btnH;
            Framebuffer.Graphics.FillRectangle(btnRestoreX, btnRestoreY, btnW, btnH, 0xFF26482E);
            WindowManager.font.DrawString(
                btnRestoreX + 10,
                btnRestoreY + (btnH / 2 - WindowManager.font.FontSize / 2),
                "Restore"
            );
            Framebuffer.Graphics.FillRectangle(btnEndX, btnEndY, btnW, btnH, 0xFF482626);
            WindowManager.font.DrawString(
                btnEndX + 10,
                btnEndY + (btnH / 2 - WindowManager.font.FontSize / 2),
                "End Tombstoned App"
            );
        }

        private int CountTombstoned() {
            int c = 0;
            for (int i = 0; i < WindowManager.Windows.Count; i++) {
                if (WindowManager.Windows[i].IsTombstoned)
                    c++;
            }
            return c;
        }

        private Window GetTombstonedAt(int idx) {
            int c = 0;
            for (int i = 0; i < WindowManager.Windows.Count; i++) {
                var w = WindowManager.Windows[i];
                if (w.IsTombstoned) {
                    if (c == idx)
                        return w;
                    c++;
                }
            }
            return null;
        }

        private void RestoreTombstoned() {
            if (_selectedTombIndex < 0)
                return;
            var w = GetTombstonedAt(_selectedTombIndex);
            if (w != null) {
                w.Untombstone();
                WindowManager.MoveToEnd(w);
            }
        }

        private void EndTombstoned() {
            if (_selectedTombIndex < 0)
                return;
            var w = GetTombstonedAt(_selectedTombIndex);
            if (w != null) {
                // Store owner ID before removing
                int targetOwnerId = w.OwnerId;

                // Remove from window list
                WindowManager.Windows.Remove(w);
                _selectedTombIndex = -1;

                // Free all memory owned by this window
                FreeOwnerMemory(targetOwnerId);
            }
        }

        private void DrawTab(int x, int y, int w, int h, string title, bool selected) {
            uint bg = selected ? 0xFF2C2C2C : 0xFF242424;
            Framebuffer.Graphics.FillRectangle(x, y, w, h, bg);
            Framebuffer.Graphics.DrawRectangle(x, y, w, h, 0xFF3A3A3A, 1);
            int tx = x + w / 2 - WindowManager.font.MeasureString(title) / 2;
            int ty = y + h / 2 - WindowManager.font.FontSize / 2;
            WindowManager.font.DrawString(tx, ty, title);
        }

        private void DrawProcesses(int x, int y, int w, int h) {
            // Sample owner bytes less frequently to avoid freezing - changed from 500ms to 2000ms
            if ((long)(Timer.Ticks - _lastOwnerSampleTick) >= 2000) {
                try {
                    SampleOwnerBytes();
                    _lastOwnerSampleTick = Timer.Ticks;
                } catch {
                    // If sampling fails, skip it this frame to prevent freeze
                }
            }

            int headerH = _rowHeight;

            // columns: Name, CPU%, Memory, Disk%, Network%
            int colNameW = w - 380;
            if (colNameW < 120)
                colNameW = 120;
            int colCpuW = 60;
            int colMemW = 140; // show absolute usage
            int colDiskW = 80;
            int colNetW = 80;

            // Header
            int hx = x;
            int hy = y;
            DrawHeaderCell(hx, hy, colNameW, headerH, "Name");
            hx += colNameW;
            DrawHeaderCell(hx, hy, colCpuW, headerH, "CPU%");
            hx += colCpuW;
            DrawHeaderCell(hx, hy, colMemW, headerH, "Memory");
            hx += colMemW;
            DrawHeaderCell(hx, hy, colDiskW, headerH, "Disk%");
            hx += colDiskW;
            DrawHeaderCell(hx, hy, colNetW, headerH, "Network%");

            // List area
            int listY = y + headerH;
            int listH = h - headerH - 40;
            if (listH < headerH)
                listH = headerH;
            int startRow = _scrollOffset;
            int rowsVisible = listH / _rowHeight;
            if (rowsVisible < 1)
                rowsVisible = 1;
            int totalRows = WindowManager.Windows.Count;
            if (startRow > totalRows)
                startRow = totalRows;
            int endRow = startRow + rowsVisible;
            if (endRow > totalRows)
                endRow = totalRows;

            int dy = listY;
            for (int i = startRow; i < endRow; i++) {
                var wdw = WindowManager.Windows[i];
                bool sel = i == _selectedIndex;
                uint bg = sel ? 0xFF2A2A2A : 0x00000000u;
                if (bg != 0)
                    Framebuffer.Graphics.FillRectangle(x, dy, w, _rowHeight, bg);
                int cx = x;
                string name = wdw.Title ?? "(no title)";
                WindowManager.font.DrawString(
                    cx + 6,
                    dy + 6,
                    name,
                    colNameW - 12,
                    WindowManager.font.FontSize
                );
                cx += colNameW;

                // CPU: per-window counter via WindowManager
                int ownerId = wdw.OwnerId;
                int cpuPct = WindowManager.GetWindowCpuPct(ownerId);
                WindowManager.font.DrawString(cx + 6, dy + 6, cpuPct.ToString());
                cx += colCpuW;

                // Memory per window: use allocator per-owner accounting with try-catch to prevent freeze
                ulong bytes = 0;
                try {
                    bytes = Allocator.GetOwnerBytes(ownerId);
                    // Also get total memory in use for debugging - show global total for now
                    if (bytes == 0) {
                        // No owner-specific memory found - show global memory divided by window count for rough estimate
                        ulong globalMem = Allocator.MemoryInUse;
                        int winCount = WindowManager.Windows.Count;
                        if (winCount > 0)
                            bytes = globalMem / (ulong)winCount;
                    }
                } catch {
                    // If memory query fails, show 0
                    bytes = 0;
                }

                // Format memory value more clearly - show KB if < 1MB
                string memText;
                if (bytes < 1024UL * 1024UL) {
                    ulong kb = bytes / 1024UL;
                    // Cache common KB values to reduce allocations
                    if (kb == 0)
                        memText = "0 KB";
                    else
                        memText = kb.ToString() + " KB";
                } else {
                    memText = ToMBString(bytes);
                }
                WindowManager.font.DrawString(cx + 6, dy + 6, memText);
                memText.Dispose(); // Explicitly dispose the string to free memory
                cx += colMemW;

                // Disk/Net per-window not implemented
                WindowManager.font.DrawString(cx + 6, dy + 6, "-");
                cx += colDiskW;
                WindowManager.font.DrawString(cx + 6, dy + 6, "-");
                dy += _rowHeight;
            }

            // Scrollbar
            int sbW = 10;
            int sbX = X + Width - _padding - sbW;
            Framebuffer.Graphics.FillRectangle(sbX, listY, sbW, listH, 0xFF1A1A1A);
            int thumbH = rowsVisible * listH / (totalRows == 0 ? 1 : totalRows);
            if (thumbH < 16)
                thumbH = 16;
            if (thumbH > listH)
                thumbH = listH;
            int maxScroll = totalRows - rowsVisible;
            if (maxScroll < 0)
                maxScroll = 0;
            int thumbY =
                listY + (maxScroll == 0 ? 0 : _scrollOffset * (listH - thumbH) / maxScroll);
            Framebuffer.Graphics.FillRectangle(sbX, thumbY, sbW, thumbH, 0xFF3A3A3A);

            // Footer - End Task button
            int btnW = 120,
                btnH = 28;
            int btnX = X + Width - _padding - btnW;
            int btnY = Y + Height - _padding - btnH;
            Framebuffer.Graphics.FillRectangle(btnX, btnY, btnW, btnH, 0xFF3B1E1E);
            WindowManager.font.DrawString(
                btnX + 10,
                btnY + (btnH / 2 - WindowManager.font.FontSize / 2),
                "End Task"
            );
        }

        private void DrawHeaderCell(int x, int y, int w, int h, string text) {
            Framebuffer.Graphics.FillRectangle(x, y, w, h, 0xFF252525);
            Framebuffer.Graphics.DrawRectangle(x, y, w, h, 0xFF333333, 1);
            WindowManager.font.DrawString(
                x + 6,
                y + (h / 2 - WindowManager.font.FontSize / 2),
                text
            );
        }

        private static int WavePct(ulong ticks, int period) {
            int t = (int)(ticks % (ulong)period);
            int up = period / 2;
            if (t < up)
                return t * 100 / up;
            else
                return (period - t) * 100 / up;
        }

        private static string ToMBString(ulong bytes) {
            ulong mb = bytes / (1024UL * 1024UL);
            // Avoid repeated string allocation by caching common values
            if (mb == 0)
                return "0 MB";
            if (mb < 10)
                return mb.ToString() + " MB";
            // For larger values, round to avoid excessive string allocations
            if (mb < 100)
                return mb.ToString() + " MB";
            // Round to nearest 10 MB for very large values
            ulong rounded = (mb + 5) / 10 * 10;
            return rounded.ToString() + " MB";
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
            ulong m = totalSec / 60UL % 60UL;
            ulong h = totalSec / 3600UL;
            return h.ToString()
                + ":"
                + (m < 10 ? "0" + m.ToString() : m.ToString())
                + ":"
                + (s < 10 ? "0" + s.ToString() : s.ToString());
        }

        private void DrawPerformance(int x, int y, int w, int h) {
            // Update charts less often to reduce work
            if (_lastPerfTick != Timer.Ticks && Timer.Ticks % 10 == 0) {
                _lastPerfTick = Timer.Ticks;

                // CPU
                _cpuUtilPct = (int)ThreadPool.CPUUsage;
                if (_cpuUtilPct < 0)
                    _cpuUtilPct = 0;
                if (_cpuUtilPct > 100)
                    _cpuUtilPct = 100;
                UpdateChart(_cpuChart, _cpuUtilPct, 0xFF5DADE2);

                // Memory - fix calculation
                ulong totalMem = Allocator.MemorySize == 0 ? 1UL : Allocator.MemorySize;
                ulong usedMem = Allocator.MemoryInUse;
                _memUtilPct = (int)(usedMem * 100UL / totalMem);
                if (_memUtilPct < 0)
                    _memUtilPct = 0;
                if (_memUtilPct > 100)
                    _memUtilPct = 100;
                UpdateChart(_memChart, _memUtilPct, 0xFF58D68D);

                // Disk (synthetic animation so chart isn't flat). If real stats are added later, replace here.
                _diskUtilPct = WavePct(Timer.Ticks, 240);
                _diskActivePct = WavePct(Timer.Ticks + 60, 300);
                _diskReadKBps = _diskUtilPct * 4; // up to ~400 KB/s
                _diskWriteKBps = _diskActivePct * 3 / 2; // up to ~150 KB/s
                _diskRespMs = 1 + _diskActivePct / 10;
                UpdateChart(_diskChart, _diskUtilPct, 0xFFE67E22);

                // Network (synthetic animation). If NET driver exposes counters, wire them here.
                _netUtilPct = WavePct(Timer.Ticks + 120, 280);
                _netSendKBps = _netUtilPct * 2; // up to ~200 KB/s
                _netRecvKBps = (100 - _netUtilPct) * 2;
                // accumulate bytes for labels (rough estimate per tick quantum)
                _bytesSent += (ulong)(_netSendKBps * 1024 / 10);
                _bytesRecv += (ulong)(_netRecvKBps * 1024 / 10);
                UpdateChart(_netChart, _netUtilPct, 0xFF9B59B6);

                // Other labels
                _procCount = WindowManager.Windows.Count;
                _threadCount = ThreadPool.ThreadCount;

                // Sample owner bytes every ~1000ms to compute KB/s per owner
                if ((long)(Timer.Ticks - _lastOwnerSampleTick) >= 1000) {
                    SampleOwnerBytes();
                    _lastOwnerSampleTick = Timer.Ticks;
                }
            }

            // Windows-style layout: left sidebar + right detail pane
            int navW = (int)(w * 0.25f); // 25% for navigation
            int detailW = w - navW - 12; // remaining width for detail pane, minus gap
            int navX = x;
            int detailX = x + navW + 12;

            // Dynamically size charts to fit available space
            int chartW = Math.Min(detailW - 20, 600); // max 600px wide
            int chartH = Math.Min((h - 200) / 1, 220); // leave room for title and details
            if (chartW < 200)
                chartW = 200;
            if (chartH < 120)
                chartH = 120;

            // Ensure charts are properly sized
            _cpuChart.EnsureSize(chartW, chartH);
            _memChart.EnsureSize(chartW, chartH);
            _diskChart.EnsureSize(chartW, chartH);
            _netChart.EnsureSize(chartW, chartH);

            // Draw navigation pane background
            Framebuffer.Graphics.FillRectangle(navX, y, navW, h, 0xFF181818);

            // Draw navigation items
            string[] navLabels = { "CPU", "Memory", "Disk", "Network" };
            uint[] navColors = { 0xFF5DADE2, 0xFF58D68D, 0xFFE67E22, 0xFF9B59B6 };
            int[] navValues = { _cpuUtilPct, _memUtilPct, _diskUtilPct, _netUtilPct };

            int itemH = Math.Min(70, h / 4); // Divide space evenly
            int miniGraphW = 50;
            int miniGraphH = 30;

            for (int i = 0; i < 4; i++) {
                int itemY = y + i * itemH;
                bool selected = i == _selectedPerfCategory;

                // Background for selected item
                if (selected) {
                    Framebuffer.Graphics.FillRectangle(navX, itemY, navW, itemH, 0xFF252525);
                }

                // Separator line
                if (i > 0) {
                    Framebuffer.Graphics.DrawRectangle(navX + 4, itemY, navW - 8, 1, 0xFF333333, 1);
                }

                // Draw mini graph (simplified line chart)
                int graphX = navX + 8;
                int graphY = itemY + 32;
                Chart chart = GetChartForCategory(i);
                DrawMiniGraph(graphX, graphY, miniGraphW, miniGraphH, chart, navColors[i]);

                // Label and percentage
                int textX = graphX + miniGraphW + 8;
                int labelY = itemY + 8;
                WindowManager.font.DrawString(textX, labelY, navLabels[i]);

                string pctText = navValues[i].ToString() + "%";
                int pctY = labelY + WindowManager.font.FontSize + 4;
                WindowManager.font.DrawString(textX, pctY, pctText);
                pctText.Dispose();
            }

            // Draw separator between navigation and detail
            Framebuffer.Graphics.FillRectangle(detailX - 6, y, 1, h, 0xFF333333);

            // Draw detail pane based on selected category
            switch (_selectedPerfCategory) {
                case 0:
                    DrawCpuDetail(detailX, y, detailW, h);
                    break;
                case 1:
                    DrawMemDetail(detailX, y, detailW, h);
                    break;
                case 2:
                    DrawDiskDetail(detailX, y, detailW, h);
                    break;
                case 3:
                    DrawNetDetail(detailX, y, detailW, h);
                    break;
            }
        }

        private Chart GetChartForCategory(int category) {
            switch (category) {
                case 0:
                    return _cpuChart;
                case 1:
                    return _memChart;
                case 2:
                    return _diskChart;
                case 3:
                    return _netChart;
                default:
                    return _cpuChart;
            }
        }

        private void DrawMiniGraph(int x, int y, int w, int h, Chart chart, uint color) {
            // Draw a simplified version of the chart for the navigation pane
            Framebuffer.Graphics.FillRectangle(x, y, w, h, 0xFF222222);

            // Sample every Nth point to fit in the mini graph
            int chartW = chart.graphics.Width;
            int sampleInterval = Math.Max(1, chartW / w);

            for (int i = 0; i < w; i++) {
                int srcX = (chart.writeX + i * sampleInterval) % chartW;

                // Read pixel value from chart to determine height
                uint pixel = chart.image.GetPixel(srcX, h / 2);
                if (pixel != 0xFF222222 && pixel != 0) { // not background
                    // Find the colored pixel in this column
                    for (int sy = 0; sy < chart.graphics.Height; sy++) {
                        uint p = chart.image.GetPixel(srcX, sy);
                        if (p == color) {
                            // Map to mini graph coordinates
                            int miniY = y + sy * h / chart.graphics.Height;
                            Framebuffer.Graphics.FillRectangle(x + i, miniY, 1, 1, color);
                            break;
                        }
                    }
                }
            }

            Framebuffer.Graphics.DrawRectangle(x, y, w, h, 0xFF444444, 1);
        }

        private void DrawCpuDetail(int x, int y, int w, int h) {
            // Title
            string title = "CPU";
            WindowManager.font.DrawString(x, y, title);

            // Large graph
            int graphY = y + WindowManager.font.FontSize + 12;
            int graphH = Math.Min(_cpuChart.graphics.Height, h - WindowManager.font.FontSize - 120);
            Framebuffer.Graphics.DrawImage(x, graphY, _cpuChart.image, true);
            Framebuffer.Graphics.DrawRectangle(
                x,
                graphY,
                _cpuChart.graphics.Width,
                _cpuChart.graphics.Height,
                0xFF333333,
                1
            );

            // Utilization percentage on graph
            string pct = _cpuUtilPct.ToString() + "%";
            int pctX = x + _cpuChart.graphics.Width - WindowManager.font.MeasureString(pct) - 8;
            WindowManager.font.DrawString(pctX, graphY + 8, pct);
            pct.Dispose();

            // Details below graph
            int detailY = graphY + _cpuChart.graphics.Height + 16;
            int col1X = x;
            int col2X = x + w / 2;

            WindowManager.font.DrawString(col1X, detailY, "Utilization:");
            WindowManager.font.DrawString(col2X, detailY, _cpuUtilPct.ToString() + "%");
            detailY += WindowManager.font.FontSize + 6;

            WindowManager.font.DrawString(col1X, detailY, "Speed:");
            WindowManager.font.DrawString(col2X, detailY, "N/A");
            detailY += WindowManager.font.FontSize + 6;

            WindowManager.font.DrawString(col1X, detailY, "Processes:");
            WindowManager.font.DrawString(col2X, detailY, _procCount.ToString());
            detailY += WindowManager.font.FontSize + 6;

            WindowManager.font.DrawString(col1X, detailY, "Threads:");
            WindowManager.font.DrawString(col2X, detailY, _threadCount.ToString());
            detailY += WindowManager.font.FontSize + 6;

            WindowManager.font.DrawString(col1X, detailY, "Machine time:");
            WindowManager.font.DrawString(col2X, detailY, FormatUptime(Timer.Ticks));
        }

        private void DrawMemDetail(int x, int y, int w, int h) {
            // Title
            string title = "Memory";
            WindowManager.font.DrawString(x, y, title);

            // Large graph
            int graphY = y + WindowManager.font.FontSize + 12;
            Framebuffer.Graphics.DrawImage(x, graphY, _memChart.image, true);
            Framebuffer.Graphics.DrawRectangle(
                x,
                graphY,
                _memChart.graphics.Width,
                _memChart.graphics.Height,
                0xFF333333,
                1
            );

            // Utilization percentage on graph
            string pct = _memUtilPct.ToString() + "%";
            int pctX = x + _memChart.graphics.Width - WindowManager.font.MeasureString(pct) - 8;
            WindowManager.font.DrawString(pctX, graphY + 8, pct);
            pct.Dispose();

            // Details below graph
            int detailY = graphY + _memChart.graphics.Height + 16;
            int col1X = x;
            int col2X = x + w / 2;

            ulong total = Allocator.MemorySize;
            ulong used = Allocator.MemoryInUse;
            ulong avail = total > used ? total - used : 0UL;

            WindowManager.font.DrawString(col1X, detailY, "In use:");
            string usedStr = ToMBString(used) + " (" + _memUtilPct.ToString() + "%)";
            WindowManager.font.DrawString(col2X, detailY, usedStr);
            usedStr.Dispose();
            detailY += WindowManager.font.FontSize + 6;

            WindowManager.font.DrawString(col1X, detailY, "Available:");
            string availStr = ToMBString(avail);
            WindowManager.font.DrawString(col2X, detailY, availStr);
            availStr.Dispose();
            detailY += WindowManager.font.FontSize + 6;

            WindowManager.font.DrawString(col1X, detailY, "Total:");
            string totalStr = ToMBString(total);
            WindowManager.font.DrawString(col2X, detailY, totalStr);
            totalStr.Dispose();
            detailY += WindowManager.font.FontSize + 6;

            int topOwner = 0;
            int topKbps = 0;
            var ownerKeys = _ownerKBps.Keys;
            for (int _i = 0; _i < ownerKeys.Count; _i++) {
                int ok = ownerKeys[_i];
                int val = _ownerKBps[ok];
                if (Math.Abs(val) > Math.Abs(topKbps)) {
                    topKbps = val;
                    topOwner = ok;
                }
            }
            WindowManager.font.DrawString(col1X, detailY, "Top owner:");
            if (topOwner != 0) {
                string ownerStr =
                    "#" + topOwner + " " + (topKbps > 0 ? "+" : "") + topKbps.ToString() + " KB/s";
                WindowManager.font.DrawString(col2X, detailY, ownerStr);
                ownerStr.Dispose();
            } else {
                WindowManager.font.DrawString(col2X, detailY, "N/A");
            }
            detailY += WindowManager.font.FontSize + 6;

            int topTag = -1;
            ulong topTagBytes = 0;
            for (int t = 0; t < (int)Allocator.AllocTag.Count; t++) {
                ulong tb = Allocator.GetTagBytes((Allocator.AllocTag)t);
                if (tb > topTagBytes) {
                    topTagBytes = tb;
                    topTag = t;
                }
            }
            WindowManager.font.DrawString(col1X, detailY, "Top tag:");
            if (topTag >= 0) {
                string tagStr =
                    ((Allocator.AllocTag)topTag).ToString() + " " + ToMBString(topTagBytes);
                WindowManager.font.DrawString(col2X, detailY, tagStr);
                tagStr.Dispose();
            } else {
                WindowManager.font.DrawString(col2X, detailY, "N/A");
            }
        }

        private void DrawDiskDetail(int x, int y, int w, int h) {
            // Title
            string title = "Disk";
            WindowManager.font.DrawString(x, y, title);

            // Large graph
            int graphY = y + WindowManager.font.FontSize + 12;
            Framebuffer.Graphics.DrawImage(x, graphY, _diskChart.image, true);
            Framebuffer.Graphics.DrawRectangle(
                x,
                graphY,
                _diskChart.graphics.Width,
                _diskChart.graphics.Height,
                0xFF333333,
                1
            );

            // Utilization percentage on graph
            string pct = _diskUtilPct.ToString() + "%";
            int pctX = x + _diskChart.graphics.Width - WindowManager.font.MeasureString(pct) - 8;
            WindowManager.font.DrawString(pctX, graphY + 8, pct);
            pct.Dispose();

            // Details below graph
            int detailY = graphY + _diskChart.graphics.Height + 16;
            int col1X = x;
            int col2X = x + w / 2;

            WindowManager.font.DrawString(col1X, detailY, "Active time:");
            WindowManager.font.DrawString(col2X, detailY, _diskActivePct.ToString() + "%");
            detailY += WindowManager.font.FontSize + 6;

            WindowManager.font.DrawString(col1X, detailY, "Avg response time:");
            WindowManager.font.DrawString(col2X, detailY, _diskRespMs.ToString() + " ms");
            detailY += WindowManager.font.FontSize + 6;

            WindowManager.font.DrawString(col1X, detailY, "Read speed:");
            string readStr = ToKBpsString(_diskReadKBps);
            WindowManager.font.DrawString(col2X, detailY, readStr);
            readStr.Dispose();
            detailY += WindowManager.font.FontSize + 6;

            WindowManager.font.DrawString(col1X, detailY, "Write speed:");
            string writeStr = ToKBpsString(_diskWriteKBps);
            WindowManager.font.DrawString(col2X, detailY, writeStr);
            writeStr.Dispose();
        }

        private void DrawNetDetail(int x, int y, int w, int h) {
            // Title
            string title = "Network";
            WindowManager.font.DrawString(x, y, title);

            // Large graph
            int graphY = y + WindowManager.font.FontSize + 12;
            Framebuffer.Graphics.DrawImage(x, graphY, _netChart.image, true);
            Framebuffer.Graphics.DrawRectangle(
                x,
                graphY,
                _netChart.graphics.Width,
                _netChart.graphics.Height,
                0xFF333333,
                1
            );

            // Utilization percentage on graph
            string pct = _netUtilPct.ToString() + "%";
            int pctX = x + _netChart.graphics.Width - WindowManager.font.MeasureString(pct) - 8;
            WindowManager.font.DrawString(pctX, graphY + 8, pct);
            pct.Dispose();

            // Details below graph
            int detailY = graphY + _netChart.graphics.Height + 16;
            int col1X = x;
            int col2X = x + w / 2;

            WindowManager.font.DrawString(col1X, detailY, "Send:");
            string sendStr = ToKBpsString(_netSendKBps);
            WindowManager.font.DrawString(col2X, detailY, sendStr);
            sendStr.Dispose();
            detailY += WindowManager.font.FontSize + 6;

            WindowManager.font.DrawString(col1X, detailY, "Receive:");
            string recvStr = ToKBpsString(_netRecvKBps);
            WindowManager.font.DrawString(col2X, detailY, recvStr);
            recvStr.Dispose();
            detailY += WindowManager.font.FontSize + 6;

            WindowManager.font.DrawString(col1X, detailY, "Sent bytes:");
            WindowManager.font.DrawString(col2X, detailY, _bytesSent.ToString());
            detailY += WindowManager.font.FontSize + 6;

            WindowManager.font.DrawString(col1X, detailY, "Received bytes:");
            WindowManager.font.DrawString(col2X, detailY, _bytesRecv.ToString());
        }

        private void UpdateChart(Chart chart, int valuePct, uint color) {
            if (valuePct < 0)
                valuePct = 0;
            if (valuePct > 100)
                valuePct = 100;
            int h = chart.graphics.Height;
            int w = chart.graphics.Width;

            // Clear current column
            chart.graphics.FillRectangle(chart.writeX, 0, ChartLineWidth, h, 0xFF222222);

            // Compute y positions (invert for top origin)
            int newY = h - (h * valuePct / 100);
            if (newY < 0)
                newY = 0;
            if (newY >= h)
                newY = h - 1;

            int prevY = h - (h * chart.lastValue / 100);
            if (prevY < 0)
                prevY = 0;
            if (prevY >= h)
                prevY = h - 1;

            // Draw vertical line from previous to new value
            if (prevY < newY) {
                // Line going down (value decreasing)
                for (int dy = prevY; dy <= newY; dy++) {
                    chart.graphics.FillRectangle(chart.writeX, dy, ChartLineWidth, 1, color);
                }
            } else {
                // Line going up (value increasing)
                for (int dy = newY; dy <= prevY; dy++) {
                    chart.graphics.FillRectangle(chart.writeX, dy, ChartLineWidth, 1, color);
                }
            }

            // Store current value for next iteration
            chart.lastValue = valuePct;

            // Move write position
            chart.writeX += ChartLineWidth;
            if (chart.writeX >= w) {
                chart.writeX = 0;
                chart.graphics.FillRectangle(0, 0, w, h, 0xFF222222);
            }
        }

        private void OnEndTask() {
            if (_selectedIndex < 0)
                return;
            if (_selectedIndex >= WindowManager.Windows.Count) {
                _selectedIndex = -1;
                return;
            }

            Window target = WindowManager.Windows[_selectedIndex];
            if (target == this)
                return; // do not end self via button

            // CRITICAL FIX: Free all memory allocated by this window's OwnerId
            int targetOwnerId = target.OwnerId;

            // Remove from window list first
            WindowManager.Windows.RemoveAt(_selectedIndex);
            if (_selectedIndex >= WindowManager.Windows.Count)
                _selectedIndex = WindowManager.Windows.Count - 1;

            // Free all memory owned by this window
            FreeOwnerMemory(targetOwnerId);
        }

        // Free all memory allocated by a specific owner (window)
        private unsafe void FreeOwnerMemory(int ownerId) {
            if (ownerId == 0)
                return;

            try {
                // Scan all pages and free runs owned by this ownerId
                fixed (Allocator.Info* pInfo = &Allocator._Info) {
                    for (int i = 0; i < Allocator.NumPages;) {
                        ulong run = pInfo->Pages[i];
                        if (run != 0 && run != Allocator.PageSignature) {
                            // This is a run start - check if owned by ownerId
                            if (pInfo->Owners[i] == ownerId) {
                                // Free this allocation
                                long baseAddr = (long)pInfo->Start;
                                long offset = (long)(i * Allocator.PageSize);
                                IntPtr ptr = new IntPtr((void*)(baseAddr + offset));
                                Allocator.Free(ptr);
                                // Don't increment i - the Free() cleared the Pages[] entries
                                continue;
                            }
                            // Skip ahead by run length
                            i += (int)run;
                        } else {
                            i++;
                        }
                    }
                }
            } catch {
                // If memory cleanup fails, at least the window is gone from the list
            }
        }

        private void SampleOwnerBytes() {
            var snap = Allocator.GetOwnerListSnapshot();
            if (snap == null || snap.Length == 0)
                return;

            // Build new snapshot values - removed lock to prevent deadlock
            // Compute diffs for owners present in snap
            _ownerKBps.Clear();
            for (int i = 0; i < snap.Length; i++) {
                int owner = snap[i].OwnerId;
                ulong bytes = snap[i].Bytes;
                ulong prev = _lastOwnerBytes.ContainsKey(owner) ? _lastOwnerBytes[owner] : 0UL;
                long diff = (long)bytes - (long)prev; // bytes per ~1s
                int kbs = (int)(diff / 1024L);
                _ownerKBps[owner] = kbs;
            }

            // Owners that disappeared -> negative freed rate
            // Create a temporary list to avoid modifying dictionary during iteration
            var keysToCheck = new List<int>(_lastOwnerBytes.Keys.Count);
            for (int i = 0; i < _lastOwnerBytes.Keys.Count; i++) {
                keysToCheck.Add(_lastOwnerBytes.Keys[i]);
            }

            for (int i = 0; i < keysToCheck.Count; i++) {
                int owner = keysToCheck[i];
                bool found = false;
                for (int j = 0; j < snap.Length; j++) {
                    if (snap[j].OwnerId == owner) {
                        found = true;
                        break;
                    }
                }
                if (!found) {
                    int freed = (int)(-((long)_lastOwnerBytes[owner] / 1024L));
                    _ownerKBps[owner] = freed;
                }
            }

            // Replace _lastOwnerBytes with new snapshot
            _lastOwnerBytes.Clear();
            for (int i = 0; i < snap.Length; i++) {
                _lastOwnerBytes[snap[i].OwnerId] = snap[i].Bytes;
            }

            // Dispose the temporary list
            keysToCheck.Dispose();
        }
    }
}