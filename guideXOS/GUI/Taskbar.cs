using guideXOS.Kernel.Drivers;
using System.Drawing;
using System.Windows.Forms;
using System.Collections.Generic;
using guideXOS.DefaultApps;
namespace guideXOS.GUI {
    internal class Taskbar {
        public StartMenu StartMenu;
        private int _barHeight;
        private Image _startIcon;
        private bool _clockUse12Hour = false;
        private bool _clockClickLatch = false;
        private bool _startClickLatch = false;
        
        // Right-click context menu
        private TaskbarMenu _menu;
        private bool _rightClickLatch = false;

        // Network indicator animation
        private int _netAnimPhase = 0;
        private ulong _lastTick = 0;
        private bool _netConnectedShown = false;

        // Network animation scheduling
        private readonly ulong _bootTicks;
        private ulong _animWindowStart;
        private ulong _animWindowEnd;
        private ulong _nextCycleStart;
        private const ulong TenSeconds = 10_000;       // ms
        private const ulong FiveMinutes = 300_000;     // ms

        // New: latches and references for Workspace Switcher and Show Desktop
        private bool _taskViewLatch = false;
        private bool _showDesktopLatch = false;
        
        // DON'T use singleton - let it be created fresh but DELAY the creation
        private bool _needsWorkspaceSwitcher = false;
        private WorkspaceSwitcher _workspaceSwitcher; // Add field for the switcher instance

        // Public property to check if workspace switcher is visible (blocks input to windows)
        public bool IsWorkspaceSwitcherVisible => _workspaceSwitcher != null && _workspaceSwitcher.Visible;

        // Track windows minimized by Show Desktop to restore them on toggle
        private bool _desktopShown = false;
        private readonly List<Window> _minimizedByShowDesktop = new List<Window>(32);

        // Latch for pinned quicklaunch
        private bool _pinnedClickLatch = false;

        public Taskbar(int barHeight, Image startIcon) { 
            _barHeight = barHeight; 
            _startIcon = startIcon; 
            // schedule: show animation for first 10 seconds after boot
            _bootTicks = Timer.Ticks;
            _animWindowStart = _bootTicks;
            _animWindowEnd = _bootTicks + TenSeconds;
            _nextCycleStart = _bootTicks + FiveMinutes;
        }

        public void CloseWorkspaceSwitcher() {
            if (_workspaceSwitcher != null) {
                _workspaceSwitcher.Visible = false;
            }
        }

        public void Draw() {
            // Handle delayed workspace switcher creation at the START of Draw()
            // This ensures it's created OUTSIDE of any mouse button handling
            if (_needsWorkspaceSwitcher) {
                _needsWorkspaceSwitcher = false;
                
                // Create the workspace switcher (now it's NOT a Window!)
                if (_workspaceSwitcher == null) {
                    _workspaceSwitcher = new WorkspaceSwitcher();
                }
                
                // Build the cache BEFORE showing
                _workspaceSwitcher.RefreshWindowCache();
                
                // Make it visible
                _workspaceSwitcher.Visible = true;
            }
            
            // Handle workspace switcher input FIRST (if visible)
            if (_workspaceSwitcher != null && _workspaceSwitcher.Visible) {
                _workspaceSwitcher.OnInput();
            }
            
            // Draw workspace switcher BEFORE taskbar (if visible)
            if (_workspaceSwitcher != null && _workspaceSwitcher.Visible) {
                _workspaceSwitcher.OnDraw();
                // If workspace switcher is visible, skip drawing the rest of taskbar
                return;
            }
            
            int yTop = Framebuffer.Height - _barHeight;
            // Blur area behind taskbar, then tint
            Framebuffer.Graphics.BlurRectangle(0, yTop, Framebuffer.Width, _barHeight, 3);
            Framebuffer.Graphics.AFillRectangle(0, yTop, Framebuffer.Width, _barHeight, 0x66111111);

            int startX = 12; int startY = Framebuffer.Height - _barHeight + 4;
            // Start icon
            if (_startIcon != null) {
                Framebuffer.Graphics.DrawImage(startX, startY, _startIcon);
            }

            // Quicklaunch pinned row
            int qx = startX + (_startIcon != null ? _startIcon.Width + 8 : 0) + 8;
            int qy = Framebuffer.Height - _barHeight + 6;
            int qh = _barHeight - 12;
            bool leftMousePinned = Control.MouseButtons.HasFlag(MouseButtons.Left);
            for (int i=0;i<PinnedManager.Count;i++){
                var ic = PinnedManager.Icon(i);
                int iw = ic.Width; int ih = ic.Height; int bx = qx; int by = qy + (qh/2 - ih/2);
                // hover
                bool hoverPinned = Control.MousePosition.X>=bx && Control.MousePosition.X<=bx+iw && Control.MousePosition.Y>=by && Control.MousePosition.Y<=by+ih;
                if (hoverPinned) { UIPrimitives.AFillRoundedRect(bx-3, by-3, iw+6, ih+6, 0x333F7FBF, 4); }
                Framebuffer.Graphics.DrawImage(bx, by, ic);
                // edge-click to launch pinned item
                if (hoverPinned && leftMousePinned && !_pinnedClickLatch) {
                    _pinnedClickLatch = true;
                    string nm = PinnedManager.Name(i); byte kind = PinnedManager.Kind(i);
                    if (kind==0) { Desktop.Apps.Load(nm); }
                    else if (kind==2) { var cf = new ComputerFiles(300,200,540,380); WindowManager.MoveToEnd(cf); cf.Visible=true; }
                    else if (kind==1) { string path = PinnedManager.Path(i); if(path!=null){ byte[] buf = guideXOS.FS.File.ReadAllBytes(path); if(buf!=null){ string err; guideXOS.Misc.GXMLoader.TryExecute(buf, out err); buf.Dispose(); } } }
                }
                qx += iw + 8; if (qx > Framebuffer.Width - 420) break; // leave space for task buttons
            }
            if (!leftMousePinned) _pinnedClickLatch = false;

            // Draw task buttons after pinned
            int btnX = qx + 12;
            int btnY = Framebuffer.Height - _barHeight + 6;
            int btnH = _barHeight - 12;
            int btnW = 140; // fixed width
            int gap = 8;

            int mx = Control.MousePosition.X; int my = Control.MousePosition.Y;
            bool left = Control.MouseButtons.HasFlag(MouseButtons.Left);
            bool right = Control.MouseButtons.HasFlag(MouseButtons.Right);

            // Handle right click -> show menu
            int barTop = Framebuffer.Height - _barHeight;
            bool onBar = (my >= barTop && my <= Framebuffer.Height);
            if (right && onBar) {
                if (!_rightClickLatch) {
                    if (_menu == null) _menu = new TaskbarMenu(mx, my);
                    else { _menu.Visible = true; _menu.OnSetVisible(true); }
                    _rightClickLatch = true;
                }
            } else {
                _rightClickLatch = false;
            }

            for (int i = 0; i < WindowManager.Windows.Count; i++) {
                var w = WindowManager.Windows[i];
                if (!w.Visible || !w.ShowInTaskbar) continue;
                // button rect
                int x = btnX; int y = btnY; int wRect = btnW; int hRect = btnH;
                bool hover = (mx >= x && mx <= x + wRect && my >= y && my <= y + hRect);
                uint bg = hover ? 0xFF3A3A3A : 0xFF303030;
                Framebuffer.Graphics.FillRectangle(x, y, wRect, hRect, bg);
                Framebuffer.Graphics.DrawRectangle(x, y, wRect, hRect, 0xFF454545, 1);
                // icon and title
                var icon = w.TaskbarIcon ?? Icons.DocumentIcon(32);
                int iconY = y + (hRect / 2) - (icon.Height / 2);
                Framebuffer.Graphics.DrawImage(x + 6, iconY, icon);
                int textX = x + 6 + icon.Width + 6;
                int textWidth = wRect - (textX - x) - 6;
                if (textWidth > 0) WindowManager.font.DrawString(textX, y + (hRect / 2) - (WindowManager.font.FontSize / 2), w.Title, textWidth, WindowManager.font.FontSize);
                // click -> focus window
                if (left && hover) {
                    if (w.IsMinimized) w.Restore();
                    WindowManager.MoveToEnd(w);
                    w.Visible = true;
                }
                btnX += wRect + gap;
                if (btnX > Framebuffer.Width - 300) break; // leave space for clock + right controls
            }

            // Time and date strings
            string time;
            if (_clockUse12Hour) {
                bool isPM = RTC.Hour >= 12; int hour12 = (RTC.Hour % 12 == 0) ? 12 : (RTC.Hour % 12);
                string sfx = isPM ? "PM" : "AM";
                string min = RTC.Minute < 10 ? ("0" + RTC.Minute.ToString()) : RTC.Minute.ToString();
                time = hour12.ToString() + ":" + min + " " + sfx; sfx.Dispose(); min.Dispose();
            } else {
                string h = RTC.Hour < 10 ? ("0" + RTC.Hour.ToString()) : RTC.Hour.ToString();
                string m = RTC.Minute < 10 ? ("0" + RTC.Minute.ToString()) : RTC.Minute.ToString();
                string s = RTC.Second < 10 ? ("0" + RTC.Second.ToString()) : RTC.Second.ToString();
                time = h + ":" + m + ":" + s; h.Dispose(); m.Dispose(); s.Dispose();
            }
            string date = RTC.Month.ToString() + "/" + RTC.Day.ToString() + "/" + RTC.Year.ToString();

            int timeW = WindowManager.font.MeasureString(time);
            int timeX = Framebuffer.Width - 12 - timeW;
            int timeY = Framebuffer.Height - _barHeight + ((_barHeight - WindowManager.font.FontSize) / 2) - (WindowManager.font.FontSize/2);
            WindowManager.font.DrawString(timeX, timeY, time);
            // Date below time
            int dateY = timeY + WindowManager.font.FontSize;
            WindowManager.font.DrawString(timeX, dateY, date);

            // Network indicator left of time
            int iconSize = 14;
            int netX = timeX - iconSize - 8;
            int netY = timeY + (WindowManager.font.FontSize / 2) - (iconSize/2);

            // Simple animation clock
            if (_lastTick != Timer.Ticks) { _lastTick = Timer.Ticks; _netAnimPhase = (_netAnimPhase + 1) % 3; }

            bool connected = false;
#if NETWORK
            connected = NETv4.Initialized; // if networking compiled in
#else
            connected = false;
#endif
            // Determine if animation window is active
            ulong now = Timer.Ticks;
            if (now >= _nextCycleStart) {
                // start a new 10s animation window, then schedule next in 5 minutes
                _animWindowStart = now;
                _animWindowEnd = now + TenSeconds;
                _nextCycleStart = now + FiveMinutes;
            }
            bool animActive = (now >= _animWindowStart && now <= _animWindowEnd);

            if (connected) {
                // draw 3 bars
                int bw = 3; int gap2 = 2;
                for (int i = 0; i < 3; i++) {
                    int h2 = 4 + i * 4;
                    Framebuffer.Graphics.FillRectangle(netX + i * (bw + gap2), netY + (iconSize - h2), bw, h2, 0xFF5FB878);
                }
                _netConnectedShown = true;
            } else {
                // show animated dots only during the allowed window; otherwise static dim dots
                int dot = 3; int gap2 = 4;
                for (int i = 0; i < 3; i++) {
                    uint c;
                    if (animActive) {
                        c = (i == _netAnimPhase) ? 0xFFAAAAAAu : 0xFF555555u;
                    } else {
                        c = 0xFF555555u; // static dim dots when idle
                    }
                    Framebuffer.Graphics.FillRectangle(netX + i * (dot + gap2), netY + (iconSize/2) - (dot/2), dot, dot, c);
                }
            }

            // Workspace Switcher button (left of time/date)
            int tvSize = _barHeight - 12; if (tvSize < 18) tvSize = 18; if (tvSize > 24) tvSize = 24;
            int tvX = netX - tvSize - 10;
            int tvY = Framebuffer.Height - _barHeight + (_barHeight - tvSize) / 2;
            bool overTV = (mx >= tvX && mx <= tvX + tvSize && my >= tvY && my <= tvY + tvSize);
            uint tvBg = overTV ? 0xFF3A3A3A : 0xFF303030;
            Framebuffer.Graphics.FillRectangle(tvX, tvY, tvSize, tvSize, tvBg);
            Framebuffer.Graphics.DrawRectangle(tvX, tvY, tvSize, tvSize, 0xFF454545, 1);
            // draw workspace glyph (stacked rectangles)
            int sq = tvSize / 2;
            Framebuffer.Graphics.DrawRectangle(tvX + 5, tvY + 4, sq, sq, 0xFFAAAAAA, 1);
            Framebuffer.Graphics.DrawRectangle(tvX + 8, tvY + 7, sq, sq, 0xFF888888, 1);
            Framebuffer.Graphics.DrawRectangle(tvX + 11, tvY + 10, sq, sq, 0xFF666666, 1);

            // Show Desktop sliver at far right
            int sdW = 6; int sdX = Framebuffer.Width - sdW - 1; int sdY = yTop + 2; int sdH = _barHeight - 4;
            // bevel effect
            Framebuffer.Graphics.FillRectangle(sdX, sdY, sdW, sdH, 0x33222222); // subtle fill
            Framebuffer.Graphics.DrawRectangle(sdX, sdY, sdW, sdH, 0xFF444444, 1);
            Framebuffer.Graphics.DrawRectangle(sdX + 1, sdY + 1, sdW - 2, sdH - 2, 0xFF777777, 1);

            // Input handling for start/time areas
            if (Control.MouseButtons.HasFlag(MouseButtons.Left)) {
                int mx2 = Control.MousePosition.X; int my2 = Control.MousePosition.Y;
                if (mx2 >= timeX && mx2 <= timeX + timeW && my2 >= Framebuffer.Height - _barHeight && my2 <= Framebuffer.Height) {
                    if (!_clockClickLatch) { _clockUse12Hour = !_clockUse12Hour; _clockClickLatch = true; }
                }
                if (_startIcon != null) {
                    int sW = _startIcon.Width; int sH = _startIcon.Height;
                    if (mx2 >= startX && mx2 <= startX + sW && my2 >= startY && my2 <= startY + sH) {
                        if (!_startClickLatch) { if (StartMenu == null) StartMenu = new StartMenu(); StartMenu.Visible = !StartMenu.Visible; _startClickLatch = true; }
                    } else {
                        // only close on a new click (mouse down that didn't originate from Start button)
                        if (StartMenu != null && StartMenu.Visible && !_startClickLatch && !StartMenu.IsUnderMouse()) { StartMenu.Visible = false; }
                    }
                }
                // Workspace button click: SET FLAG instead of creating directly
                if (mx2 >= tvX && mx2 <= tvX + tvSize && my2 >= tvY && my2 <= tvY + tvSize) {
                    if (!_taskViewLatch) { 
<<<<<<< HEAD
                        // Just set flag - will be created at the start of next Draw()
                        _needsWorkspaceSwitcher = true;
=======
                        // Open workspace switcher overlay instead of calling Next() (which freezes)
                        var switcher = new WorkspaceSwitcher();
                        WindowManager.MoveToEnd(switcher);
                        switcher.Visible = true;
>>>>>>> 22925cd52525686aecc7943cb94186b7502460fe
                        _taskViewLatch = true; 
                    }
                }
                // Show Desktop click handling (right sliver)
                if (mx2 >= sdX && mx2 <= sdX + sdW && my2 >= sdY && my2 <= sdY + sdH) {
                    if (!_showDesktopLatch) {
                        ToggleShowDesktop();
                        _showDesktopLatch = true;
                    }
                }
            } else { _clockClickLatch = false; _startClickLatch = false; _taskViewLatch = false; _showDesktopLatch = false; }

            time.Dispose(); date.Dispose();
        }

        private void ToggleShowDesktop() {
            if (!_desktopShown) {
                _minimizedByShowDesktop.Clear();
                // minimize all visible taskbar windows
                for (int i = 0; i < WindowManager.Windows.Count; i++) {
                    var w = WindowManager.Windows[i];
                    if (!w.ShowInTaskbar) continue; // skip non-taskbar elements like Start Menu
                    if (w.Visible && !w.IsMinimized) {
                        _minimizedByShowDesktop.Add(w);
                        w.Minimize();
                    }
                }
                // Hide Start menu if open
                if (StartMenu != null && StartMenu.Visible) StartMenu.Visible = false;
                _desktopShown = true;
            } else {
                // restore only those we minimized
                for (int i = 0; i < _minimizedByShowDesktop.Count; i++) {
                    var w = _minimizedByShowDesktop[i];
                    if (w != null && w.IsMinimized) w.Restore();
                }
                _minimizedByShowDesktop.Clear();
                _desktopShown = false;
            }
        }
    }
}