/*
using guideXOS.GUI;
using guideXOS.Misc;
using guideXOS.Kernel.Drivers;
using System.Windows.Forms;
using guideXOS; // NETv4
using System;
using guideXOS.OS;

namespace guideXOS.DefaultApps {
    internal class Anomalocaris : Window {
        private string _url = "http://example.com/";
        private string _status = "";
        private string _pageText = string.Empty;
        private bool _clickLock;
        private bool _typingUrl = false;
        private string _urlEdit = string.Empty;
        private NETv4.TCPClient _http;
        private NETv4.IPAddress _hostIp;
        private byte[] _recvBuf;
        private int _contentStartIndex = -1;
        public Anomalocaris(int x, int y) : base(x, y, 720, 480) { Title = "Anomalocaris"; ShowInTaskbar = true; Keyboard.OnKeyChanged += OnKey; }

        private void OnKey(object s, System.ConsoleKeyInfo key) { if (!_typingUrl || !Visible) return; if (key.KeyState != System.ConsoleKeyState.Pressed) return; if (key.Key == System.ConsoleKey.Enter) { _url = _urlEdit; _typingUrl = false; StartRequest(_url); return; } if (key.Key == System.ConsoleKey.Escape) { _typingUrl = false; return; } if (key.Key == System.ConsoleKey.Backspace) { if (_urlEdit.Length > 0) _urlEdit = _urlEdit.Substring(0, _urlEdit.Length - 1); return; } char c = MapChar(key); if (c != '\0') _urlEdit += c; }
        private char MapChar(System.ConsoleKeyInfo key) { if (key.KeyChar != '\0') return key.KeyChar; bool shift = Keyboard.KeyInfo.Modifiers.HasFlag(System.ConsoleModifiers.Shift); switch (key.Key) { case System.ConsoleKey.Space: return ' '; case System.ConsoleKey.OemPeriod: return shift ? '>' : '.'; case System.ConsoleKey.OemComma: return shift ? '<' : ','; case System.ConsoleKey.OemMinus: return shift ? '_' : '-'; case System.ConsoleKey.OemPlus: return shift ? '+' : '='; case System.ConsoleKey.Oem2: return shift ? '?' : '/'; case System.ConsoleKey.Oem3: return shift ? '~' : '`'; case System.ConsoleKey.Oem4: return shift ? '{' : '['; case System.ConsoleKey.Oem5: return shift ? '|' : '\\'; case System.ConsoleKey.Oem6: return shift ? '}' : ']'; case System.ConsoleKey.Oem7: return shift ? '"' : '\''; } return '\0'; }

        public override void OnInput() {
            base.OnInput(); if (!Visible) return;
            if (Control.MouseButtons.HasFlag(MouseButtons.Left)) {
                int pad = 10; int tx = X + pad; int ty = Y + pad; int w = Width - pad * 2; int h = 28;
                if (Control.MousePosition.X >= tx && Control.MousePosition.X <= tx + w && Control.MousePosition.Y >= ty && Control.MousePosition.Y <= ty + h) { _typingUrl = true; _urlEdit = _url; }
            }
        }
        public override void OnDraw() {
            base.OnDraw();
            // poll network receive while drawing
            PollReceive();
            int pad = 10; int h = 28; int tx = X + pad; int ty = Y + pad; int w = Width - pad * 2;
            Framebuffer.Graphics.FillRectangle(tx, ty, w, h, 0xFF2E2E2E);
            string show = _typingUrl ? _urlEdit : _url; WindowManager.font.DrawString(tx + 8, ty + (h / 2 - WindowManager.font.FontSize / 2), show, w - 16, WindowManager.font.FontSize);
            int py = ty + h + 8; int ph = Height - (py - Y) - pad; Framebuffer.Graphics.AFillRectangle(tx, py, w, ph, 0x80282828);
            // status
            WindowManager.font.DrawString(tx + 8, py + 4, _status);
            // page text
            WindowManager.font.DrawString(tx + 8, py + 24, _pageText, w - 16, ph - 32);
        }

        private void PollReceive() { if (_http == null) return; for (; ; ) { var data = _http.Receive(); if (data == null) break; AppendData(data); } }

        private void StartRequest(string url) {
            _status = "Requesting..."; _pageText = string.Empty; _contentStartIndex = -1; if (_http != null) { _http.Close(); _http.Remove(); _http = null; }
            // parse http://host/path
            if (!StartsWithFast(url, "http://")) { _status = "Only http:// supported"; return; }
            string rest = url.Substring(7); int slash = IndexOf(rest, '/'); string host = slash >= 0 ? rest.Substring(0, slash) : rest; string path = slash >= 0 ? rest.Substring(slash) : "/";
            _hostIp = NETv4.DNSQuery(host); if (_hostIp.P1 == 0 && _hostIp.P2 == 0 && _hostIp.P3 == 0 && _hostIp.P4 == 0) { _status = "DNS failed"; return; }
            ushort local = NextEphemeral();
            if (!Firewall.Check("Anomalocaris", "tcp-connect")) { _status = "Blocked by firewall"; return; }
            _http = new NETv4.TCPClient(_hostIp, 80, local); _http.Connect(); _recvBuf = new byte[0];
            string req = "GET " + path + " HTTP/1.0\r\nHost: " + host + "\r\nUser-Agent: guideXOS/0.1\r\nConnection: close\r\n\r\n"; var b = ToAscii(req); unsafe { fixed (byte* p = b) _http.Send(p, b.Length); }
            _status = "Waiting response...";
        }
        private static ushort _ephem = 41000; private static ushort NextEphemeral() { if (_ephem < 41000 || _ephem > 60000) _ephem = 41000; return _ephem++; }
        private static byte[] ToAscii(string s) { byte[] b = new byte[s.Length]; for (int i = 0; i < s.Length; i++) { char c = s[i]; b[i] = c < 128 ? (byte)c : (byte)'?'; } return b; }
        public override void OnSetVisible(bool value) { base.OnSetVisible(value); if (value) StartRequest(_url); }
        private void AppendData(byte[] data) { // append
            int old = _recvBuf == null ? 0 : _recvBuf.Length; byte[] nb = new byte[old + data.Length]; for (int i = 0; i < old; i++) nb[i] = _recvBuf[i]; for (int i = 0; i < data.Length; i++) nb[old + i] = data[i]; _recvBuf = nb; // try parse headers once
            if (_contentStartIndex < 0) { for (int i = 3; i < _recvBuf.Length; i++) { if (_recvBuf[i - 3] == '\r' && _recvBuf[i - 2] == '\n' && _recvBuf[i - 1] == '\r' && _recvBuf[i] == '\n') { _contentStartIndex = i + 1; break; } } }
            if (_contentStartIndex >= 0) { int len = _recvBuf.Length - _contentStartIndex; char[] chars = new char[len]; for (int i = 0; i < len; i++) { byte c = _recvBuf[_contentStartIndex + i]; chars[i] = c < 128 ? (char)c : '?'; } _pageText = new string(chars); _status = "OK"; }
        }

        private static int IndexOf(string s, char ch) { for (int i = 0; i < s.Length; i++) { if (s[i] == ch) return i; } return -1; }
        private static bool StartsWithFast(string s, string prefix) { int l = prefix.Length; if (s.Length < l) return false; for (int i = 0; i < l; i++) { if (s[i] != prefix[i]) return false; } return true; }
    }
}*/