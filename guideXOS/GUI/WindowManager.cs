using guideXOS.DefaultApps;
using guideXOS.FS;
using guideXOS.Misc;
using guideXOS.Kernel.Drivers;
using System.Collections.Generic;
using System.Drawing;
namespace guideXOS.GUI {
    /// <summary>
    /// Window Manager
    /// </summary>
    internal static class WindowManager {
        /// <summary>
        /// Windows
        /// </summary>
        public static List<Window> Windows;
        /// <summary>
        /// Font
        /// </summary>
        public static IFont font;
        /// <summary>
        /// Close Button
        /// </summary>
        public static Image CloseButton;
        /// <summary>
        /// Minimize Button
        /// </summary>
        public static Image MinimizeButton;
        /// <summary>
        /// Maximize Button
        /// </summary>
        public static Image MaximizeButton;
        struct PendingWindow {
            public int Type;
            public int X,
                Y,
                W,
                H;
        }
        static List<PendingWindow> _pending;
        // Perf tracking toggled off by default (previous logic caused potential hang during early boot)
        private static bool _perfTrackingEnabled = false; // can be enabled later by TaskManager if desired
        //private static Dictionary<int, ulong> _drawMs; // accumulated ms per owner
        //private static Dictionary<int, int> _cpuPct;    // last computed percent
        private static ulong _cpuEpochTick;
        /// <summary>
        /// Initialize
        /// </summary>
        public static void Initialize() {
            Windows = new List<Window>();
            CloseButton = new PNG(File.ReadAllBytes("Images/Close.png"));
            MinimizeButton = new PNG(File.ReadAllBytes("Images/BlueVelvet/16/down.png"));
            MaximizeButton = new PNG(File.ReadAllBytes("Images/BlueVelvet/16/image.png"));
            PNG defaultFont = new PNG(File.ReadAllBytes("Images/defaultfont.png"));
            font = new IFont(
                defaultFont,
                "!\"#$%&'()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[\\]^_`abcdefghijklmnopqrstuvwxyz{|}~",
                18
            );
            MouseHandled = false;
            _pending = new List<PendingWindow>();
            //_drawMs = new Dictionary<int, ulong>();
            //_cpuPct = new Dictionary<int, int>();
            _cpuEpochTick = 0; // defer until enabled
        }
        /// <summary>
        /// Enable performance tracking
        /// </summary>
        public static void EnablePerfTracking() {
            if (_perfTrackingEnabled)
                return;
            _cpuEpochTick = Timer.Ticks;
            _perfTrackingEnabled = true;
        }
        /// <summary>
        /// Disable performance tracking
        /// </summary>
        public static void DisablePerfTracking() {
            _perfTrackingEnabled = false;
            //_drawMs.Clear();
            //_cpuPct.Clear();
            _cpuEpochTick = 0;
        }
        /// <summary>
        /// Enqueue display options window creation after input phase
        /// </summary>
        public static void EnqueueDisplayOptions(int x, int y, int w, int h) {
            PendingWindow pw;
            pw.Type = 1;
            pw.X = x;
            pw.Y = y;
            pw.W = w;
            pw.H = h;
            _pending.Add(pw);
        }
        /// <summary>
        /// Flush pending window creations
        /// </summary>
        public static void FlushPendingCreates() {
            if (_pending.Count == 0)
                return;
            for (int i = 0; i < _pending.Count; i++) {
                var pw = _pending[i];
                if (pw.Type == 1)
                    _ = new DisplayOptions(pw.X, pw.Y, pw.W, pw.H);
            }
            _pending.Clear();
        }
        /// <summary>
        /// Move to End - Ensures no duplicates in the window list
        /// </summary>
        /// <param name="window"></param>
        public static void MoveToEnd(Window window) {
            if (window == null)
                return;
                
            // Remove ALL instances of this window (in case of duplicates)
            for (int i = Windows.Count - 1; i >= 0; i--) {
                if (Windows[i] == window) {
                    Windows.RemoveAt(i);
                }
            }
            
            // Add once at the end
            Windows.Add(window);
        }
        /// <summary>
        /// Draw All
        /// </summary>
        public static void DrawAll() {
            // Basic draw (no timing unless enabled)
            for (int i = 0; i < Windows.Count; i++) {
                var w = Windows[i];
                if (!w.Visible)
                    continue;
                bool isTaskMgr = w is guideXOS.DefaultApps.TaskManager;
                if (!_perfTrackingEnabled || isTaskMgr) {
                    w.OnDraw();
                    continue;
                }
                Allocator.CurrentOwnerId = w.OwnerId;
                ulong t0 = Timer.Ticks;
                w.OnDraw();
                ulong t1 = Timer.Ticks;
                ulong dt = t1 >= t0 ? t1 - t0 : 0UL;
                //int owner = w.OwnerId; if (owner != 0) { if (_drawMs.ContainsKey(owner)) _drawMs[owner] += dt; else _drawMs.Add(owner, dt); }
                Allocator.CurrentOwnerId = 0;
            }
            if (_perfTrackingEnabled)
                UpdateCpuPercents();
        }

        /// <summary>
        /// Draw all windows except Task Manager (allows workspace switcher to be drawn on top)
        /// </summary>
        public static void DrawAllExceptTaskManager() {
            for (int i = 0; i < Windows.Count; i++) {
                var w = Windows[i];
                if (!w.Visible)
                    continue;
                // Skip Task Manager - it will be drawn later to stay on top
                if (w is guideXOS.DefaultApps.TaskManager)
                    continue;
                    
                if (!_perfTrackingEnabled) {
                    w.OnDraw();
                    continue;
                }
                Allocator.CurrentOwnerId = w.OwnerId;
                ulong t0 = Timer.Ticks;
                w.OnDraw();
                ulong t1 = Timer.Ticks;
                ulong dt = t1 >= t0 ? t1 - t0 : 0UL;
                Allocator.CurrentOwnerId = 0;
            }
            if (_perfTrackingEnabled)
                UpdateCpuPercents();
        }

        /// <summary>
        /// Draw only Task Manager (always on top)
        /// </summary>
        public static void DrawTaskManager() {
            for (int i = 0; i < Windows.Count; i++) {
                var w = Windows[i];
                if (!w.Visible)
                    continue;
                // Only draw Task Manager
                if (w is guideXOS.DefaultApps.TaskManager) {
                    w.OnDraw();
                    break; // Only one Task Manager should exist
                }
            }
        }

        /// <summary>
        /// Update CPU Percents
        /// </summary>
        private static void UpdateCpuPercents() {
            ulong now = Timer.Ticks;
            ulong elapsed = now >= _cpuEpochTick ? now - _cpuEpochTick : 0UL;
            if (elapsed < 1000UL)
                return;
            if (elapsed == 0)
                elapsed = 1;
            //for (int k = 0; k < _drawMs.Keys.Count; k++) {
            //int owner = _drawMs.Keys[k]; ulong ms = _drawMs[owner]; int pct = (int)((ms * 100UL) / elapsed); if (pct < 0) pct = 0; if (pct > 100) pct = 100; if (_cpuPct.ContainsKey(owner)) _cpuPct[owner] = pct; else _cpuPct.Add(owner, pct);
            //}
            //_drawMs.Clear(); _cpuEpochTick = now;
        }
        /// <summary>
        /// Input All
        /// </summary>
        public static void InputAll() {
            for (int i = 0; i < Windows.Count; i++) {
                var w = Windows[i];
                if (!w.Visible)
                    continue;
                if (_perfTrackingEnabled && !(w is guideXOS.DefaultApps.TaskManager))
                    Allocator.CurrentOwnerId = w.OwnerId;
                w.OnInput();
                if (_perfTrackingEnabled && !(w is guideXOS.DefaultApps.TaskManager))
                    Allocator.CurrentOwnerId = 0;
            }
        }
        /// <summary>
        /// Has Window Moving
        /// </summary>
        public static bool HasWindowMoving = false;
        /// <summary>
        /// Mouse Handled (separate from HasWindowMoving)
        /// </summary>
        static bool _mouseHandled;
        public static bool MouseHandled {
            get => _mouseHandled;
            set => _mouseHandled = value;
        }
        /// <summary>
        /// Expose last CPU% for a window id
        /// </summary>
        public static int GetWindowCpuPct(int ownerId) {
            return 0;
        } // _perfTrackingEnabled && _cpuPct.ContainsKey(ownerId) ? _cpuPct[ownerId] : 0; }
        
        /// <summary>
        /// Get all windows that should appear in Start Menu
        /// </summary>
        public static List<Window> GetStartMenuWindows() {
            var result = new List<Window>();
            for (int i = 0; i < Windows.Count; i++) {
                var w = Windows[i];
                if (w.ShowInStartMenu) {
                    result.Add(w);
                }
            }
            return result;
        }
    }
}