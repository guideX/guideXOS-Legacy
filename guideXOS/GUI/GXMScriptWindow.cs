using guideXOS.Kernel.Drivers;
using guideXOS.Misc;
using System.Collections.Generic;
using System.Windows.Forms;

namespace guideXOS.GUI {
    internal class GXMScriptWindow : Window {
        internal struct Btn { public int Id; public string Text; public int X; public int Y; public int W; public int H; }
        internal struct Label { public string Text; public int X; public int Y; }
        internal class ListViewDef { public int Id; public int X; public int Y; public int W; public int H; public List<string> Items = new List<string>(32); public int Selected = -1; }
        internal class DropdownDef { public int Id; public int X; public int Y; public int W; public int H; public List<string> Items = new List<string>(32); public int Selected = -1; public bool Open; }
        internal class Callback { public int Type; public int Id; public string Action; public string Arg; }

        private List<Btn> _buttons = new List<Btn>(16);
        private List<Label> _labels = new List<Label>(16);
        private List<ListViewDef> _lists = new List<ListViewDef>(8);
        private List<DropdownDef> _dropdowns = new List<DropdownDef>(8);
        private List<Callback> _callbacks = new List<Callback>(16); // Type: 1=click, 2=change

        private bool _clickLatch;
        private int _lastClicked = -1;
        public GXMScriptWindow(string title, int w, int h) : base((Framebuffer.Width - w) / 2, (Framebuffer.Height - h) / 2, w, h) { Title = title ?? "Script"; ShowInTaskbar = true; ShowMinimize = true; ShowTombstone = true; ShowRestore = true; }
        public void AddButton(int id, string text, int x, int y, int w, int h) { Btn b; b.Id = id; b.Text = text; b.X = x; b.Y = y; b.W = w; b.H = h; _buttons.Add(b); }
        public void AddLabel(string text, int x, int y) { Label l; l.Text = text; l.X = x; l.Y = y; _labels.Add(l); }
        public void AddList(int id, int x, int y, int w, int h, string items) { var lv = new ListViewDef { Id = id, X = x, Y = y, W = w, H = h }; if (items != null) { int start = 0; for (int i = 0; i <= items.Length; i++) { if (i == items.Length || items[i] == ';') { int len = i - start; if (len > 0) { lv.Items.Add(items.Substring(start, len)); } start = i + 1; } } } _lists.Add(lv); }
        public void AddDropdown(int id, int x, int y, int w, int h, string items) { var dd = new DropdownDef { Id = id, X = x, Y = y, W = w, H = h }; if (items != null) { int start = 0; for (int i = 0; i <= items.Length; i++) { if (i == items.Length || items[i] == ';') { int len = i - start; if (len > 0) { dd.Items.Add(items.Substring(start, len)); } start = i + 1; } } } _dropdowns.Add(dd); }
        public void AddOnClick(int id, string action, string arg) { var cb = new Callback { Type = 1, Id = id, Action = action, Arg = arg }; _callbacks.Add(cb); }
        public void AddOnChange(int id, string action, string arg) { var cb = new Callback { Type = 2, Id = id, Action = action, Arg = arg }; _callbacks.Add(cb); }

        public override void OnInput() {
            base.OnInput(); if (!Visible || IsMinimized || IsTombstoned) return; int mx = Control.MousePosition.X; int my = Control.MousePosition.Y; bool left = Control.MouseButtons.HasFlag(MouseButtons.Left);
            if (left) {
                if (!_clickLatch) { // buttons
                    for (int i = 0; i < _buttons.Count; i++) { var b = _buttons[i]; int rx = X + b.X; int ry = Y + b.Y; if (mx >= rx && mx <= rx + b.W && my >= ry && my <= ry + b.H) { _lastClicked = b.Id; _clickLatch = true; RunActions(1, b.Id, null); break; } }
                    // dropdowns
                    for (int i = 0; i < _dropdowns.Count; i++) {
                        var d = _dropdowns[i]; int rx = X + d.X; int ry = Y + d.Y; if (mx >= rx && mx <= rx + d.W && my >= ry && my <= ry + d.H) { d.Open = !d.Open; _clickLatch = true; continue; }
                        if (d.Open) {
                            int itemY = ry + d.H; for (int it = 0; it < d.Items.Count; it++) { int ih = WindowManager.font.FontSize + 6; int iy = itemY + it * ih; if (mx >= rx && mx <= rx + d.W && my >= iy && my <= iy + ih) { d.Selected = it; d.Open = false; _clickLatch = true; RunActions(2, d.Id, d.Items[it]); break; } }
                        }
                    }
                    // lists
                    for (int i = 0; i < _lists.Count; i++) {
                        var l = _lists[i]; int rx = X + l.X; int ry = Y + l.Y; if (mx >= rx && mx <= rx + l.W && my >= ry && my <= ry + l.H) { int rowH = WindowManager.font.FontSize + 6; int rel = my - ry; int idx = rel / rowH; if (idx >= 0 && idx < l.Items.Count) { l.Selected = idx; _clickLatch = true; RunActions(2, l.Id, l.Items[idx]); } }
                    }
                }
            } else { _clickLatch = false; }
        }
        private void RunActions(int type, int id, string value) {
            for (int i = 0; i < _callbacks.Count; i++) {
                var cb = _callbacks[i]; if (cb.Type == type && cb.Id == id) {
                    string act = cb.Action ?? string.Empty; string arg = cb.Arg ?? string.Empty; if (value != null) { // replace $VALUE token
                        arg = ReplaceToken(arg, "$VALUE", value);
                    }
                    ExecuteAction(act, arg);
                }
            }
        }
        private string ReplaceToken(string s, string token, string val) { // naive replace (no allocations beyond new string)
            if (string.IsNullOrEmpty(s) || string.IsNullOrEmpty(token)) return s; int i = IndexOf(s, token); if (i < 0) return s; string a = s.Substring(0, i); string b = s.Substring(i + token.Length); return a + val + b;
        }
        private int IndexOf(string s, string token) { int n = s.Length; int m = token.Length; if (m == 0) return -1; for (int i = 0; i <= n - m; i++) { int k = 0; for (; k < m; k++) { if (s[i + k] != token[k]) break; } if (k == m) return i; } return -1; }
        private void ExecuteAction(string action, string arg) { // supported: MSG, OPENAPP, CLOSE
            // normalize action upper
            string a = action; // manual upper
            char[] ca = new char[a.Length]; for (int i = 0; i < a.Length; i++) { char c = a[i]; if (c >= 'a' && c <= 'z') c = (char)(c - 32); ca[i] = c; }
            a = new string(ca);
            if (a == "MSG") { Notify(arg); } else if (a == "OPENAPP") { if (Desktop.Apps != null && arg != null) { Desktop.Apps.Load(arg); } } else if (a == "CLOSE") { this.Visible = false; }
        }
        private void Notify(string msg) { if (Desktop.msgbox != null) { Desktop.msgbox.SetText(msg); Desktop.msgbox.X = X + 20; Desktop.msgbox.Y = Y + 20; WindowManager.MoveToEnd(Desktop.msgbox); Desktop.msgbox.Visible = true; } }
        public override void OnDraw() {
            base.OnDraw(); if (IsMinimized) return; // labels
            for (int i = 0; i < _labels.Count; i++) { var l = _labels[i]; WindowManager.font.DrawString(X + l.X, Y + l.Y, l.Text ?? "", Width - 16, WindowManager.font.FontSize * 3); }
            // buttons
            for (int i = 0; i < _buttons.Count; i++) { var b = _buttons[i]; uint fill = (b.Id == _lastClicked) ? 0xFF2E86C1u : 0xFF3A3A3A; Framebuffer.Graphics.FillRectangle(X + b.X, Y + b.Y, b.W, b.H, fill); WindowManager.font.DrawString(X + b.X + 6, Y + b.Y + (b.H / 2 - WindowManager.font.FontSize / 2), b.Text ?? "Button"); }
            // lists
            for (int i = 0; i < _lists.Count; i++) { var l = _lists[i]; Framebuffer.Graphics.AFillRectangle(X + l.X, Y + l.Y, l.W, l.H, 0x80282828); int rowH = WindowManager.font.FontSize + 6; int y = Y + l.Y; for (int it = 0; it < l.Items.Count && y + rowH <= Y + l.Y + l.H; it++) { if (it == l.Selected) Framebuffer.Graphics.AFillRectangle(X + l.X, y, l.W, rowH, 0x802E86C1); WindowManager.font.DrawString(X + l.X + 6, y + 3, l.Items[it], l.W - 12, WindowManager.font.FontSize); y += rowH; } }
            // dropdowns
            for (int i = 0; i < _dropdowns.Count; i++) {
                var d = _dropdowns[i]; int rx = X + d.X; int ry = Y + d.Y; Framebuffer.Graphics.FillRectangle(rx, ry, d.W, d.H, 0xFF2E2E2E); string txt = d.Selected >= 0 && d.Selected < d.Items.Count ? d.Items[d.Selected] : "(select)"; WindowManager.font.DrawString(rx + 6, ry + (d.H / 2 - WindowManager.font.FontSize / 2), txt, d.W - 12, WindowManager.font.FontSize); // arrow
                Framebuffer.Graphics.DrawLine(rx + d.W - 16, ry + 6, rx + d.W - 6, ry + 6, 0xFFAAAAAA); Framebuffer.Graphics.DrawLine(rx + d.W - 16, ry + 6, rx + d.W - 11, ry + d.H - 6, 0xFFAAAAAA); Framebuffer.Graphics.DrawLine(rx + d.W - 6, ry + 6, rx + d.W - 11, ry + d.H - 6, 0xFFAAAAAA);
                if (d.Open) { int itemY = ry + d.H; int ih = WindowManager.font.FontSize + 6; for (int it = 0; it < d.Items.Count; it++) { Framebuffer.Graphics.FillRectangle(rx, itemY + it * ih, d.W, ih, 0xFF2A2A2A); WindowManager.font.DrawString(rx + 6, itemY + it * ih + 3, d.Items[it], d.W - 12, WindowManager.font.FontSize); } Framebuffer.Graphics.DrawRectangle(rx, itemY, d.W, d.Items.Count * ih, 0xFF3A3A3A, 1); }
            }
        }
    }
}