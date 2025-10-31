using guideXOS.Kernel.Drivers;
using System.Windows.Forms;

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

        // Recent documents popout state
        private bool _docsPopupVisible = false;

        public unsafe StartMenu() : base(_x, _y, _x2, _y2) {
            Title = "Start";
            BarHeight = 0;
        }

        public override void OnInput() {
            base.OnInput();
            if (!Visible) return;

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

            // Recent list rect (left side)
            int listX = X + Padding;
            int listY = Y + Padding;
            int listW = (rcX - Gap) - listX; // space left of right column
            int listH = rcH;

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
                    // Recent Documents (toggle popup)
                    int iconW = Icons.FileIcon.Width;
                    int iconH = Icons.FileIcon.Height;
                    if (mx >= rcX && mx <= rcX + iconW && my >= iy && my <= iy + iconH) { _docsPopupVisible = !_docsPopupVisible; return; }
                }

                // Click in recent programs list
                if (mx >= listX && mx <= listX + listW && my >= listY && my <= listY + listH) {
                    var items = RecentManager.Programs;
                    int y = listY - _scroll;
                    for (int i = 0; i < items.Count; i++) {
                        var it = items.ToArray()[i];
                        int ih = Icons.FileIcon.Height;
                        int iw = Icons.FileIcon.Width;
                        int ix = listX;
                        int iy = y;
                        if (my >= iy && my <= iy + ih) {
                            // launch app by name
                            Desktop.Apps.Load(it.Name);
                            return;
                        }
                        y += Spacing;
                    }
                }
            } else { _scrollDrag = false; }

            // Drag update
            if (_scrollDrag) {
                var total = RecentManager.Programs.Count * Spacing;
                int maxScroll = total - listH; if (maxScroll < 0) maxScroll = 0;
                int dy = my - _scrollStartY;
                _scroll = _scrollStartScroll + dy;
                if (_scroll < 0) _scroll = 0; if (_scroll > maxScroll) _scroll = maxScroll;
            }
        }

        public override void OnDraw() {
            Framebuffer.Graphics.FillRectangle(X, Y, Width, Height, 0xFF222222);

            int bottomY = Y + Height - Padding - ShutdownBtnH;
            int shutdownX = X + Width - Padding - ShutdownBtnW - ArrowBtnW - Gap;
            int arrowX = X + Width - Padding - ArrowBtnW;

            // Fixed right column
            int rcX = X + Width - Padding - RightColW;
            int rcY = Y + Padding;
            int rcW = RightColW;
            int rcH = Height - Padding * 2 - (ShutdownBtnH + Gap + Padding);

            // Left recent list
            int listX = X + Padding;
            int listY = Y + Padding;
            int listW = (rcX - Gap) - listX;
            int listH = rcH;

            // Recent programs
            var items = RecentManager.Programs;
            int y = listY - _scroll;
            for (int i = 0; i < items.Count; i++) {
                var it = items.ToArray()[i];
                var icon = it.Icon ?? Icons.FileIcon;
                int ih = icon.Height;
                Framebuffer.Graphics.DrawImage(listX, y, icon);
                WindowManager.font.DrawString(listX + icon.Width + 10, y + (ih / 2) - (WindowManager.font.FontSize / 2), it.Name);
                y += Spacing;
            }

            // Scrollbar for recents
            int sbW = 8;
            int sbX = listX + listW - sbW;
            Framebuffer.Graphics.FillRectangle(sbX, listY, sbW, listH, 0xFF1A1A1A);
            int total = items.Count * Spacing;
            if (total > listH) {
                int thumbH = (listH * listH) / total; if (thumbH < 16) thumbH = 16; if (thumbH > listH) thumbH = listH;
                int thumbY = (listH * _scroll) / total; if (thumbY + thumbH > listH) thumbY = listH - thumbH;
                Framebuffer.Graphics.FillRectangle(sbX + 1, listY + thumbY, sbW - 2, thumbH, 0xFF2F2F2F);
            }

            // Right column content
            int rcCursorY = rcY;
            // Computer Files icon + label
            var cfIcon = Icons.FolderIcon;
            Framebuffer.Graphics.DrawImage(rcX, rcCursorY, cfIcon);
            WindowManager.font.DrawString(rcX + cfIcon.Width + 8, rcCursorY + (cfIcon.Height / 2) - (WindowManager.font.FontSize / 2), "Computer Files");
            rcCursorY += cfIcon.Height + 16;
            // Recent Documents with popout
            var docIcon = Icons.FileIcon;
            Framebuffer.Graphics.DrawImage(rcX, rcCursorY, docIcon);
            WindowManager.font.DrawString(rcX + docIcon.Width + 8, rcCursorY + (docIcon.Height / 2) - (WindowManager.font.FontSize / 2), "Recent Documents");

            // Popout panel to the right if visible
            if (_docsPopupVisible) {
                int popX = rcX + rcW + 6;
                int popY = rcCursorY;
                int popW = 260;
                int visibleDocs = 8;
                int popH = visibleDocs * (WindowManager.font.FontSize + 6) + 8;
                Framebuffer.Graphics.FillRectangle(popX, popY, popW, popH, 0xFF262626);
                Framebuffer.Graphics.DrawRectangle(popX, popY, popW, popH, 0xFF3F3F3F, 1);
                int py = popY + 4;
                var docs = RecentManager.Documents;
                int count = docs.Count < visibleDocs ? docs.Count : visibleDocs;
                for (int i = 0; i < count; i++) {
                    var d = docs.ToArray()[i];
                    // simple filename display
                    string label = d.Path;
                    WindowManager.font.DrawString(popX + 6, py, label, popW - 12, WindowManager.font.FontSize);
                    py += WindowManager.font.FontSize + 6;
                    label.Dispose();
                }
            }

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
                Framebuffer.Graphics.FillRectangle(menuX, menuY, menuW, menuH, 0xFF262626);
                Framebuffer.Graphics.DrawRectangle(menuX, menuY, menuW, menuH, border, 1);
                int itemY = menuY + MenuPad;
                bool hoverReboot = (mx >= menuX && mx <= menuX + menuW && my >= itemY && my < itemY + MenuItemH);
                bool hoverLogoff = (mx >= menuX && mx <= menuX + menuW && my >= itemY + MenuItemH && my < itemY + (2 * MenuItemH));
                if (hoverReboot) Framebuffer.Graphics.FillRectangle(menuX + 1, itemY, menuW - 2, MenuItemH, 0xFF313131);
                WindowManager.font.DrawString(menuX + 10, itemY + (MenuItemH / 2) - (WindowManager.font.FontSize / 2), "Reboot");
                if (hoverLogoff) Framebuffer.Graphics.FillRectangle(menuX + 1, itemY + MenuItemH, menuW - 2, MenuItemH, 0xFF313131);
                WindowManager.font.DrawString(menuX + 10, itemY + MenuItemH + (MenuItemH / 2) - (WindowManager.font.FontSize / 2), "Log Off");
            }

            DrawBorder(false);
        }
    }
}