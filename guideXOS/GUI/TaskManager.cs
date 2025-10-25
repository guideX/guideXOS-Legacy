using global::guideXOS.Kernel.Drivers;
using System.Collections.Generic;
using System.Windows.Forms;
namespace guideXOS.GUI {
    /// <summary>
    /// Built-in Task Manager window
    /// Lists WindowManager.Windows and allows bring-to-front / kill
    /// </summary>
    internal class TaskManager : Window {
        // UI state
        private int selectedIndex = -1;
        private int scrollOffset = 0;
        private int rowHeight = 24;
        private int listPadding = 8;

        public TaskManager(int X, int Y, int Width = 480, int Height = 300) : base(X, Y, Width, Height) {
            Title = "Task Manager";
        }

        public override void OnInput() {
            base.OnInput(); // handle window dragging / close button

            // Only accept input when this window is top-most (last in WindowManager)
            // and visible (base.OnInput already updates Move/Visible)
            if (!Visible) return;

            // Mouse coords
            int mx = Control.MousePosition.X;
            int my = Control.MousePosition.Y;

            // Convert to local content area coordinates (content starts at Y)
            int contentX = X + listPadding;
            int contentY = Y + listPadding;
            int contentW = Width - listPadding * 2;
            int contentH = Height - listPadding * 2 - BarHeight;

            // Click handling (left mouse)
            if (Control.MouseButtons == MouseButtons.Left) {
                // If click inside list area, compute clicked row
                if (mx >= contentX && mx <= contentX + contentW && my >= contentY && my <= contentY + contentH) {
                    int relativeY = my - contentY + (scrollOffset * rowHeight);
                    int row = relativeY / rowHeight;

                    // Clamp row
                    if (row < 0) row = 0;

                    // If row exists in WindowManager, select it
                    if (row < WindowManager.Windows.Count) {
                        // Debounce: only change selection on mouse down transitions handled by Window.OnInput,
                        // but we keep this simple: set selection immediately.
                        selectedIndex = row;
                    }
                }

                // Buttons area: draw them in OnDraw, but handle clicks here.
                // We'll define two button rectangles at bottom-right: BringToFront and Kill
                int btnW = 110;
                int btnH = 26;
                int gap = 6;
                int brX = X + Width - listPadding - btnW;
                int brY = Y + Height - listPadding - btnH;
                int killX = brX - gap - btnW;
                int killY = brY;

                // Kill button area
                if (mx >= killX && mx <= killX + btnW && my >= killY && my <= killY + btnH) {
                    OnKillSelected();
                }

                // Bring to front button area
                if (mx >= brX && mx <= brX + btnW && my >= brY && my <= brY + btnH) {
                    OnBringToFrontSelected();
                }
            }
        }

        public override void OnDraw() {
            base.OnDraw();

            // Content area
            int cx = X + listPadding;
            int cy = Y + listPadding;
            int cw = Width - listPadding * 2;
            int ch = Height - listPadding * 2 - BarHeight;

            // Background for list
            Framebuffer.Graphics.FillRectangle(cx, cy, cw, ch, 0xFF1E1E1E);

            // Column headers
            int colIndexW = 40;
            int colTitleW = cw - colIndexW - 120; // leave space for visible and actions
            int colVisibleW = 80;

            int headerY = cy;
            WindowManager.font.DrawString(cx + 4, headerY + 4, "Idx");
            WindowManager.font.DrawString(cx + colIndexW + 4, headerY + 4, "Title");
            WindowManager.font.DrawString(cx + colIndexW + colTitleW + 4, headerY + 4, "Visible");

            // Draw rows
            int rowsVisible = ch / rowHeight;
            int startRow = scrollOffset;
            int endRow = startRow + rowsVisible;
            if (endRow > WindowManager.Windows.Count) endRow = WindowManager.Windows.Count;

            int drawY = cy + rowHeight; // start one row below header
            for (int r = startRow; r < endRow; r++) {
                Window w = WindowManager.Windows[r];

                // Row background highlight if selected
                if (r == selectedIndex) {
                    Framebuffer.Graphics.FillRectangle(cx, drawY, cw, rowHeight, 0xFF2A2A2A);
                }

                // Draw index
                WindowManager.font.DrawString(cx + 6, drawY + 6, r.ToString());

                // Draw title (truncate if necessary)
                string title = w.Title ?? "(no title)";
                string displayTitle = title;
                // Simple truncation to avoid needing substr helper: draw as-is (font may clip)
                WindowManager.font.DrawString(cx + colIndexW + 6, drawY + 6, displayTitle);

                // Draw visible state
                string vis = w.Visible ? "Yes" : "No";
                WindowManager.font.DrawString(cx + colIndexW + colTitleW + 6, drawY + 6, vis);

                drawY += rowHeight;
            }

            // Draw border around list
            Framebuffer.Graphics.DrawRectangle(cx - 1, cy - 1, cw + 2, ch + 2, 0xFF333333, 1);

            // Draw action buttons at bottom-right
            int btnW = 110;
            int btnH = 26;
            int gap = 6;
            int brX = X + Width - listPadding - btnW;
            int brY = Y + Height - listPadding - btnH;
            int killX = brX - gap - btnW;
            int killY = brY;

            // Kill button
            Framebuffer.Graphics.FillRectangle(killX, killY, btnW, btnH, 0xFF3B1E1E);
            WindowManager.font.DrawString(killX + 10, killY + 6, "Kill");

            // Bring to front button
            Framebuffer.Graphics.FillRectangle(brX, brY, btnW, btnH, 0xFF1E3B1E);
            WindowManager.font.DrawString(brX + 10, brY + 6, "Bring to Front");

            // Footer - info
            WindowManager.font.DrawString(cx, Y + Height - BarHeight - 6, "Select a window and use buttons to manage it.");
        }

        private void OnKillSelected() {
            if (selectedIndex < 0) return;
            if (selectedIndex >= WindowManager.Windows.Count) { selectedIndex = -1; return; }

            Window target = WindowManager.Windows[selectedIndex];

            // Do not allow killing this task manager via the Kill button.
            if (target == this) {
                // If user wants to close task manager, they can use the close button in the title bar.
                return;
            }

            // Remove the window from the WindowManager list
            // We need to preserve the WindowManager list integrity
            WindowManager.Windows.RemoveAt(selectedIndex);

            // Adjust selection
            if (selectedIndex >= WindowManager.Windows.Count) selectedIndex = WindowManager.Windows.Count - 1;
        }

        private void OnBringToFrontSelected() {
            if (selectedIndex < 0) return;
            if (selectedIndex >= WindowManager.Windows.Count) { selectedIndex = -1; return; }

            Window target = WindowManager.Windows[selectedIndex];

            // move to end (top-most) using existing helper if available, else remove & add
            // Attempt to use MoveToEnd if present; otherwise fallback to manual:
            try {
                // If WindowManager has MoveToEnd method, prefer it
                WindowManager.MoveToEnd(target);
            } catch {
                // Fallback: remove and re-add
                WindowManager.Windows.RemoveAt(selectedIndex);
                WindowManager.Windows.Add(target);
            }

            // Make sure it is visible and focused
            target.Visible = true;

            // After moving, update selectedIndex to the new position (end)
            selectedIndex = WindowManager.Windows.IndexOf(target);
        }
    }
}