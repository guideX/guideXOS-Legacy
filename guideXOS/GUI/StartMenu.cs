using guideXOS.Kernel.Drivers;
using guideXOS.Graph;
using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Drawing;

namespace guideXOS.GUI {
    /// <summary>
    /// Start Menu
    /// </summary>
    internal class StartMenu : Window {
        private static readonly int _x = 15;
        private static readonly int _y = 45;
        private static readonly int _x2 = 420; // wider to fit two columns
        private static readonly int _y2 = 680;

        private bool _powerMenuVisible = false;

        private const int Padding = 14; // was 10
        private const int Spacing = 58; // was 50

        private const int ShutdownBtnW = 100;
        private const int ShutdownBtnH = 28;
        private const int ArrowBtnW = 28;
        private const int ArrowBtnH = 28;
        private const int Gap = 6;

        private const int MenuItemH = 26;
        private const int MenuW = 120;
        private const int MenuPad = 6;

        // Recent programs scrolling
        private int _scroll;
        private bool _scrollDrag;
        private int _scrollStartY;
        private int _scrollStartScroll;

        // Right column width
        private const int RightColW = 160;
        private const int RightColInnerPad = 6; // add inner padding to avoid text touching edge

        // Recent documents popout state
        private bool _docsPopupVisible = false;

        // All Programs view
        private bool _showAllPrograms = false;
        private List<int> _allProgramsOrder; // indices into Desktop.Apps sorted by name

        // Cache for recent program entries to avoid ToArray() each frame
        private AppEntry[] _recentCache;
        private int _recentCacheCount;
        private ulong _recentCacheTick;

        // Cached blurred background for responsiveness
        private Image _bgBlurCache;
        private bool _bgCacheReady;

        public unsafe StartMenu() : base(_x, _y, _x2, _y2) {
            Title = "Start";
            BarHeight = 0;
            ShowInTaskbar = false; // do not show a taskbar button for Start menu
            ShowMaximize = false;
            ShowMinimize = false;
            _showAllPrograms = false;
        }

        public override void OnSetVisible(bool value) {
            base.OnSetVisible(value);
            if (value) {
                // Always bring Start Menu to front when shown
                WindowManager.MoveToEnd(this);
                BuildBackgroundBlurCache();
            } else {
                // dispose cache when hidden to free memory
                if (_bgBlurCache != null) { _bgBlurCache.Dispose(); _bgBlurCache = null; }
                _bgCacheReady = false;
            }
        }

        private void BuildBackgroundBlurCache() {
            // Capture current screen region under the menu and blur once
            int w = Width; int h = Height; if (w <= 0 || h <= 0) { _bgCacheReady = false; return; }
            var img = new Image(w, h);
            for (int yy = 0; yy < h; yy++) {
                int fbY = Y + yy;
                for (int xx = 0; xx < w; xx++) {
                    int fbX = X + xx;
                    img.RawData[yy * w + xx] = (int)Framebuffer.Graphics.GetPoint(fbX, fbY);
                }
            }
            // Box blur into temp buffers (horizontal + vertical), radius 3 to match previous look
            int radius = 3;
            int[] src = img.RawData; int[] tmp = new int[w * h]; int[] dst = new int[w * h];
            // Horizontal
            for (int yy = 0; yy < h; yy++) {
                int row = yy * w;
                for (int xx = 0; xx < w; xx++) {
                    int r = 0, g = 0, b = 0, a = 0, count = 0;
                    int xmin = xx - radius; if (xmin < 0) xmin = 0;
                    int xmax = xx + radius; if (xmax >= w) xmax = w - 1;
                    for (int k = xmin; k <= xmax; k++) {
                        int c = src[row + k];
                        a += (byte)(c >> 24);
                        r += (byte)(c >> 16);
                        g += (byte)(c >> 8);
                        b += (byte)(c);
                        count++;
                    }
                    a /= count; r /= count; g /= count; b /= count;
                    tmp[row + xx] = (int)((a << 24) | (r << 16) | (g << 8) | b);
                }
            }
            // Vertical
            for (int xx = 0; xx < w; xx++) {
                for (int yy = 0; yy < h; yy++) {
                    int r = 0, g = 0, b = 0, a = 0, count = 0;
                    int ymin = yy - radius; if (ymin < 0) ymin = 0;
                    int ymax = yy + radius; if (ymax >= h) ymax = h - 1;
                    for (int k = ymin; k <= ymax; k++) {
                        int c = tmp[k * w + xx];
                        a += (byte)(c >> 24);
                        r += (byte)(c >> 16);
                        g += (byte)(c >> 8);
                        b += (byte)(c);
                        count++;
                    }
                    a /= count; r /= count; g /= count; b /= count;
                    dst[yy * w + xx] = (int)((a << 24) | (r << 16) | (g << 8) | b);
                }
            }
            // write back blurred pixels
            for (int i = 0; i < dst.Length; i++) img.RawData[i] = dst[i];
            // swap into cache
            if (_bgBlurCache != null) _bgBlurCache.Dispose();
            _bgBlurCache = img; _bgCacheReady = true;
            // dispose temps
            tmp.Dispose(); dst.Dispose();
        }

        private struct AppEntry { public Image Icon; public string Name; }

        private void RefreshRecentCacheIfNeeded() {
            // Refresh at most once per 250ms to limit work
            if (Timer.Ticks == _recentCacheTick) return;
            _recentCacheTick = Timer.Ticks;
            var list = RecentManager.Programs;
            int count = list.Count;
            if (_recentCache == null || _recentCache.Length < count) _recentCache = new AppEntry[count];
            _recentCacheCount = count;
            // copy references without ToArray()
            for (int i = 0; i < count; i++) {
                var it = list[i];
                _recentCache[i].Icon = it.Icon ?? Icons.DocumentIcon;
                _recentCache[i].Name = it.Name;
            }
        }

        public override void OnInput() {
            base.OnInput();
            if (!Visible) return;

            // Close on Escape
            if (Keyboard.KeyInfo.Key == ConsoleKey.Escape) { _powerMenuVisible = false; _docsPopupVisible = false; Visible = false; return; }

            int mx = Control.MousePosition.X;
            int my = Control.MousePosition.Y;
            bool leftDown = Control.MouseButtons == MouseButtons.Left;

            int bottomY = Y + Height - Padding - ShutdownBtnH;
            int shutdownX = X + Width - Padding - ShutdownBtnW - ArrowBtnW - Gap;
            int arrowX = X + Width - Padding - ArrowBtnW;

            // Right column rect (fixed, not scrollable)
            int rcX = X + Width - Padding - RightColW;
            int rcY = Y + Padding;
            int rcW = RightColW;
            int rcH = Height - Padding * 2 - (ShutdownBtnH + Gap + Padding);

            // Recent/All Programs list rect (left side)
            int listX = X + Padding;
            int listY = Y + Padding;
            int listW = (rcX - Gap) - listX; // space left of right column
            int listH = rcH;

            // All Programs toggle button area (bottom-left)
            int allBtnH = 28;
            int allBtnW = 140;
            int allBtnX = X + Padding;
            int allBtnY = bottomY; // align with shutdown row

            // Scrollbar hit
            int sbW = 8;
            int sbX = listX + listW - sbW;
            if (leftDown) {
                // Handle power buttons
                if (mx >= shutdownX && mx <= shutdownX + ShutdownBtnW && my >= bottomY && my <= bottomY + ShutdownBtnH) {
                    var dlg = new ShutdownDialog();
                    WindowManager.MoveToEnd(dlg);
                    dlg.Visible = true; return;
                }
                if (mx >= arrowX && mx <= arrowX + ArrowBtnW && my >= bottomY && my <= bottomY + ArrowBtnH) { _powerMenuVisible = !_powerMenuVisible; return; }

                // All Programs toggle
                if (mx >= allBtnX && mx <= allBtnX + allBtnW && my >= allBtnY && my <= allBtnY + allBtnH) {
                    ToggleAllPrograms();
                    return;
                }

                // Scrollbar drag start
                if (mx >= sbX && mx <= sbX + sbW && my >= listY && my <= listY + listH) { _scrollDrag = true; _scrollStartY = my; _scrollStartScroll = _scroll; return; }

                // Click in right column
                if (mx >= rcX && mx <= rcX + rcW && my >= rcY && my <= rcY + rcH) {
                    int iy = rcY;
                    // Computer Files
                    int fh = Icons.FolderIcon.Height;
                    int fw = Icons.FolderIcon.Width;
                    if (mx >= rcX && mx <= rcX + fw && my >= iy && my <= iy + fh) {
                        // open Computer Files window
                        var cf = new ComputerFiles(300, 200, 540, 380);
                        WindowManager.MoveToEnd(cf);
                        cf.Visible = true;
                        return;
                    }
                    iy += fh + 16;
                    // Disk Manager
                    int dwh = Icons.FolderIcon.Height; int dww = Icons.FolderIcon.Width;
                    if (mx >= rcX && mx <= rcX + dww && my >= iy && my <= iy + dwh) {
                        var dm = new DiskManager(340, 260);
                        WindowManager.MoveToEnd(dm);
                        dm.Visible = true;
                        return;
                    }
                    iy += dwh + 16;
                    // Recent Documents (toggle popup)
                    int iconW = Icons.DocumentIcon.Width;
                    int iconH = Icons.DocumentIcon.Height;
                    if (mx >= rcX && mx <= rcX + iconW && my >= iy && my <= iy + iconH) { _docsPopupVisible = !_docsPopupVisible; return; }

                    // USB Files entry (only if at least one USB MSC device is present)
                    iy += iconH + 16;
                    if (Kernel.Drivers.USBStorage.Count > 0) {
                        int ux = rcX; int uy = iy; int uw = Icons.FolderIcon.Width; int uh = Icons.FolderIcon.Height;
                        if (mx >= ux && mx <= ux + uw && my >= uy && my <= uy + uh) {
                            var dev = Kernel.Drivers.USBStorage.GetFirst();
                            if (dev != null) {
                                var disk = Kernel.Drivers.USBMSC.TryOpenDisk(dev);
                                if (disk != null && disk.IsReady) {
                                    var win = new USBFiles(disk, 380, 220, 560, 400);
                                    WindowManager.MoveToEnd(win);
                                    win.Visible = true;
                                }
                            }
                            return;
                        }
                        // Provide a second entry for a list view of all USB drives
                        int ux2 = rcX; int uy2 = iy + uh + 12; int uw2 = uw; int uh2 = uh;
                        if (mx >= ux2 && mx <= ux2 + uw2 && my >= uy2 && my <= uy2 + uh2) {
                            var list = new USBDrives(rcX - 280, rcY + 40, 420, 360);
                            WindowManager.MoveToEnd(list);
                            list.Visible = true;
                            return;
                        }
                    }
                }

                // Click in list (Recent or All Programs)
                if (mx >= listX && mx <= listX + listW && my >= listY && my <= listY + listH) {
                    int count = _showAllPrograms ? Desktop.Apps.Length : RecentManager.Programs.Count;
                    int y = listY - _scroll;
                    for (int i = 0; i < count; i++) {
                        int ih;
                        int ix = listX;
                        int iy2 = y;
                        string appName;
                        if (_showAllPrograms) {
                            int ai = _allProgramsOrder != null && i < _allProgramsOrder.Count ? _allProgramsOrder[i] : i;
                            var icon = Desktop.Apps.Icon(ai) ?? Icons.DocumentIcon;
                            ih = icon.Height;
                            if (my >= iy2 && my <= iy2 + ih) {
                                appName = Desktop.Apps.Name(ai);
                                Desktop.Apps.Load(appName);
                                appName.Dispose();
                                return;
                            }
                        } else {
                            // use cache
                            RefreshRecentCacheIfNeeded();
                            if (i >= _recentCacheCount) break;
                            var icon = _recentCache[i].Icon;
                            ih = icon.Height;
                            if (my >= iy2 && my <= iy2 + ih) {
                                Desktop.Apps.Load(_recentCache[i].Name);
                                return;
                            }
                        }
                        y += Spacing;
                    }
                }
            } else { _scrollDrag = false; }

            // Drag update
            if (_scrollDrag) {
                int total = (_showAllPrograms ? Desktop.Apps.Length : RecentManager.Programs.Count) * Spacing;
                int maxScroll = total - listH; if (maxScroll < 0) maxScroll = 0;
                int dy = my - _scrollStartY;
                _scroll = _scrollStartScroll + dy;
                if (_scroll < 0) _scroll = 0; if (_scroll > maxScroll) _scroll = maxScroll;
            }
        }

        public override void OnDraw() {
            // Draw cached blurred background once for responsiveness
            if (_bgCacheReady && _bgBlurCache != null) {
                Framebuffer.Graphics.DrawImage(X, Y, _bgBlurCache, false);
                // slight tint for readability
                Framebuffer.Graphics.AFillRectangle(X, Y, Width, Height, 0x66222222);
            } else {
                // Fallback if cache not ready yet
                Framebuffer.Graphics.BlurRectangle(X, Y, Width, Height, 3);
                Framebuffer.Graphics.AFillRectangle(X, Y, Width, Height, 0xCC222222);
            }

            int bottomY = Y + Height - Padding - ShutdownBtnH;
            int shutdownX = X + Width - Padding - ShutdownBtnW - ArrowBtnW - Gap;
            int arrowX = X + Width - Padding - ArrowBtnW;

            // Fixed right column
            int rcX = X + Width - Padding - RightColW;
            int rcY = Y + Padding;
            int rcW = RightColW;
            int rcH = Height - Padding * 2 - (ShutdownBtnH + Gap + Padding);

            // Left list
            int listX = X + Padding;
            int listY = Y + Padding;
            int listW = (rcX - Gap) - listX;
            int listH = rcH;

            // Recent programs or All Programs
            int count = _showAllPrograms ? Desktop.Apps.Length : RecentManager.Programs.Count;
            int y = listY - _scroll;
            if (_showAllPrograms) {
                for (int i = 0; i < count; i++) {
                    int ai = _allProgramsOrder != null && i < _allProgramsOrder.Count ? _allProgramsOrder[i] : i;
                    var icon = Desktop.Apps.Icon(ai) ?? Icons.DocumentIcon;
                    string name = Desktop.Apps.Name(ai);
                    int ih = icon.Height;
                    Framebuffer.Graphics.DrawImage(listX, y, icon);
                    WindowManager.font.DrawString(listX + icon.Width + 10, y + (ih / 2) - (WindowManager.font.FontSize / 2), name, listW - (icon.Width + 22), WindowManager.font.FontSize);
                    y += Spacing;
                    name.Dispose();
                }
            } else {
                // use cached entries
                RefreshRecentCacheIfNeeded();
                int max = _recentCacheCount;
                for (int i = 0; i < max; i++) {
                    var icon = _recentCache[i].Icon;
                    var name = _recentCache[i].Name;
                    int ih = icon.Height;
                    Framebuffer.Graphics.DrawImage(listX, y, icon);
                    WindowManager.font.DrawString(listX + icon.Width + 10, y + (ih / 2) - (WindowManager.font.FontSize / 2), name, listW - (icon.Width + 22), WindowManager.font.FontSize);
                    y += Spacing;
                }
            }

            // Scrollbar for list
            int sbW = 8;
            int sbX = listX + listW - sbW;
            Framebuffer.Graphics.FillRectangle(sbX, listY, sbW, listH, 0xFF1A1A1A);
            int total = count * Spacing;
            if (total > listH) {
                int thumbH = (listH * listH) / total; if (thumbH < 16) thumbH = 16; if (thumbH > listH) thumbH = listH;
                int thumbY = (listH * _scroll) / total; if (thumbY + thumbH > listH) thumbY = listH - thumbH;
                Framebuffer.Graphics.FillRectangle(sbX + 1, listY + thumbY, sbW - 2, thumbH, 0xFF2F2F2F);
            }

            // Right column content (with padding and truncation)
            int rcCursorY = rcY;
            int textMax = rcW - RightColInnerPad - (Icons.FolderIcon.Width + 8);
            // Computer Files icon + label
            var cfIcon = Icons.FolderIcon;
            Framebuffer.Graphics.DrawImage(rcX + RightColInnerPad, rcCursorY, cfIcon);
            string cfText = TruncateToWidth("Computer Files", textMax);
            WindowManager.font.DrawString(rcX + RightColInnerPad + cfIcon.Width + 8, rcCursorY + (cfIcon.Height / 2) - (WindowManager.font.FontSize / 2), cfText);
            rcCursorY += cfIcon.Height + 16;
            cfText.Dispose();
            // Disk Manager icon + label
            Framebuffer.Graphics.DrawImage(rcX + RightColInnerPad, rcCursorY, cfIcon);
            string dmText = TruncateToWidth("Disk Manager", textMax);
            WindowManager.font.DrawString(rcX + RightColInnerPad + cfIcon.Width + 8, rcCursorY + (cfIcon.Height / 2) - (WindowManager.font.FontSize / 2), dmText);
            rcCursorY += cfIcon.Height + 16;
            dmText.Dispose();
            // Recent Documents with popout
            var docIcon = Icons.DocumentIcon;
            Framebuffer.Graphics.DrawImage(rcX + RightColInnerPad, rcCursorY, docIcon);
            string rdText = TruncateToWidth("Recent Documents", textMax);
            WindowManager.font.DrawString(rcX + RightColInnerPad + docIcon.Width + 8, rcCursorY + (docIcon.Height / 2) - (WindowManager.font.FontSize / 2), rdText);

            // USB Files indicator
            rcCursorY += docIcon.Height + 16;
            if (Kernel.Drivers.USBStorage.Count > 0) {
                Framebuffer.Graphics.DrawImage(rcX + RightColInnerPad, rcCursorY, cfIcon);
                string usbText = TruncateToWidth("USB Files", textMax);
                WindowManager.font.DrawString(rcX + RightColInnerPad + cfIcon.Width + 8, rcCursorY + (cfIcon.Height / 2) - (WindowManager.font.FontSize / 2), usbText);
                usbText.Dispose();
                rcCursorY += cfIcon.Height + 16;
                // Draw second item for list view
                Framebuffer.Graphics.DrawImage(rcX + RightColInnerPad, rcCursorY, cfIcon);
                string usbListText = TruncateToWidth("USB Drives", textMax);
                WindowManager.font.DrawString(rcX + RightColInnerPad + cfIcon.Width + 8, rcCursorY + (cfIcon.Height / 2) - (WindowManager.font.FontSize / 2), usbListText);
                usbListText.Dispose();
                rcCursorY += cfIcon.Height + 16;
            }

            // Popout panel to the right if visible (slightly translucent too)
            if (_docsPopupVisible) {
                int popX = rcX + rcW + 6;
                int popY = rcCursorY; // shift below the last item
                int popW = 260;
                int visibleDocs = 8;
                int popH = visibleDocs * (WindowManager.font.FontSize + 6) + 8;
                Framebuffer.Graphics.AFillRectangle(popX, popY, popW, popH, 0xCC262626);
                Framebuffer.Graphics.DrawRectangle(popX, popY, popW, popH, 0xFF3F3F3F, 1);
                int py = popY + 4;
                var docs = RecentManager.Documents;
                int dcount = docs.Count < visibleDocs ? docs.Count : visibleDocs;
                for (int i = 0; i < dcount; i++) {
                    var d = docs.ToArray()[i];
                    // simple filename display
                    string label = d.Path;
                    WindowManager.font.DrawString(popX + 6, py, label, popW - 12, WindowManager.font.FontSize);
                    py += WindowManager.font.FontSize + 6;
                    label.Dispose();
                }
            }
            rdText.Dispose();

            // Buttons at bottom-right
            int mx = Control.MousePosition.X; int my = Control.MousePosition.Y;
            bool overShutdown = (mx >= shutdownX && mx <= shutdownX + ShutdownBtnW && my >= bottomY && my <= bottomY + ShutdownBtnH);
            bool overArrow = (mx >= arrowX && mx <= arrowX + ArrowBtnW && my >= bottomY && my <= bottomY + ArrowBtnH);
            uint btnBg = 0xFF2A2A2A; uint btnBgHover = 0xFF343434; uint border = 0xFF3F3F3F;
            Framebuffer.Graphics.FillRectangle(shutdownX, bottomY, ShutdownBtnW, ShutdownBtnH, overShutdown ? btnBgHover : btnBg);
            Framebuffer.Graphics.DrawRectangle(shutdownX, bottomY, ShutdownBtnW, ShutdownBtnH, border, 1);
            WindowManager.font.DrawString(shutdownX + 10, bottomY + (ShutdownBtnH / 2) - (WindowManager.font.FontSize / 2), "Shutdown");
            Framebuffer.Graphics.FillRectangle(arrowX, bottomY, ArrowBtnW, ArrowBtnH, overArrow ? btnBgHover : btnBg);
            Framebuffer.Graphics.DrawRectangle(arrowX, bottomY, ArrowBtnW, ArrowBtnH, border, 1);
            WindowManager.font.DrawString(arrowX + 8, bottomY + (ArrowBtnH / 2) - (WindowManager.font.FontSize / 2), ">");

            if (_powerMenuVisible) {
                int menuH = MenuPad * 2 + (MenuItemH * 2);
                int menuW = MenuW;
                int menuX = X + Width - Padding - menuW;
                int menuY = bottomY - menuH - Gap;
                // subtle translucency for menu as well
                Framebuffer.Graphics.AFillRectangle(menuX, menuY, menuW, menuH, 0xCC262626);
                Framebuffer.Graphics.DrawRectangle(menuX, menuY, menuW, menuH, border, 1);
                int itemY = menuY + MenuPad;
                bool hoverReboot = (mx >= menuX && mx <= menuX + menuW && my >= itemY && my < itemY + MenuItemH);
                bool hoverLogoff = (mx >= menuX && mx <= menuX + menuW && my >= itemY + MenuItemH && my < itemY + (2 * MenuItemH));
                if (hoverReboot) Framebuffer.Graphics.FillRectangle(menuX + 1, itemY, menuW - 2, MenuItemH, 0xFF313131);
                WindowManager.font.DrawString(menuX + 10, itemY + (MenuItemH / 2) - (WindowManager.font.FontSize / 2), "Reboot");
                if (hoverLogoff) Framebuffer.Graphics.FillRectangle(menuX + 1, itemY + MenuItemH, menuW - 2, MenuItemH, 0xFF313131);
                WindowManager.font.DrawString(menuX + 10, itemY + MenuItemH + (MenuItemH / 2) - (WindowManager.font.FontSize / 2), "Log Off");
            }

            // All Programs toggle button (bottom-left)
            int allBtnH = 28;
            int allBtnW = 140;
            int allBtnX = X + Padding;
            int allBtnY = bottomY;
            bool overAll = (mx >= allBtnX && mx <= allBtnX + allBtnW && my >= allBtnY && my <= allBtnY + allBtnH);
            Framebuffer.Graphics.FillRectangle(allBtnX, allBtnY, allBtnW, allBtnH, overAll ? btnBgHover : btnBg);
            Framebuffer.Graphics.DrawRectangle(allBtnX, allBtnY, allBtnW, allBtnH, border, 1);
            string allText = _showAllPrograms ? "Back" : "All Programs";
            WindowManager.font.DrawString(allBtnX + 10, allBtnY + (allBtnH / 2) - (WindowManager.font.FontSize / 2), allText);
            allText.Dispose();

            DrawBorder(false);
        }

        private void ToggleAllPrograms() {
            _showAllPrograms = !_showAllPrograms;
            _scroll = 0;
            if (_showAllPrograms) BuildAllProgramsOrder();
        }

        private void BuildAllProgramsOrder() {
            int n = Desktop.Apps.Length;
            _allProgramsOrder = _allProgramsOrder ?? new List<int>(n);
            _allProgramsOrder.Clear();
            for (int i = 0; i < n; i++) _allProgramsOrder.Add(i);
            // simple selection sort by name (case-insensitive)
            for (int i = 0; i < n - 1; i++) {
                int min = i;
                string minName = Desktop.Apps.Name(_allProgramsOrder[min]);
                for (int j = i + 1; j < n; j++) {
                    string nameJ = Desktop.Apps.Name(_allProgramsOrder[j]);
                    // compare
                    int cmp = CompareIgnoreCase(nameJ, minName);
                    if (cmp < 0) { min = j; minName.Dispose(); minName = nameJ; } else { nameJ.Dispose(); }
                }
                minName.Dispose();
                if (min != i) { int tmp = _allProgramsOrder[i]; _allProgramsOrder[i] = _allProgramsOrder[min]; _allProgramsOrder[min] = tmp; }
            }
        }

        private static int CompareIgnoreCase(string a, string b) {
            int la = a.Length; int lb = b.Length; int l = la < lb ? la : lb;
            for (int i = 0; i < l; i++) {
                char ca = a[i]; if (ca >= 'A' && ca <= 'Z') ca = (char)(ca + 32);
                char cb = b[i]; if (cb >= 'A' && cb <= 'Z') cb = (char)(cb + 32);
                if (ca != cb) return ca < cb ? -1 : 1;
            }
            if (la == lb) return 0; return la < lb ? -1 : 1;
        }

        private string TruncateToWidth(string text, int maxW) {
            if (WindowManager.font.MeasureString(text) <= maxW) return text;
            // Leave space for ellipsis '...'
            string ell = "...";
            int ellW = WindowManager.font.MeasureString(ell);
            int w = 0;
            int i = 0;
            for (; i < text.Length; i++) {
                int chW = WindowManager.font.MeasureString(text[i].ToString());
                if (w + chW + ellW > maxW) break;
                w += chW;
            }
            string sub = text.Substring(0, i) + ell;
            ell.Dispose();
            return sub;
        }
    }
}