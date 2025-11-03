using guideXOS.Kernel.Drivers;
using System;
using System.Drawing;
using guideXOS; // NETv4, NIC drivers
using guideXOS.OS; // AuthClient, Session

namespace guideXOS.GUI {
    internal class FConsole : Window {
        private string Data;
        public Image ScreenBuf;
        private string Cmd;
        private bool _keyDown = false; private byte _lastScan = 0;

        public FConsole(int X, int Y) : base(X, Y, 640, 320) {
            ShowInTaskbar = true;
            Title = "Console";
            Cmd = string.Empty;
            Data = string.Empty;
            ScreenBuf = new Image(640, 320);
            // Ensure built-in 8x16 font is initialized for ASCII rendering
            ASC16.Initialise();
            Rebind();
            Console.OnWrite += Console_OnWrite;
            Console.WriteLine("Type help to get information!");
        }

        public void Rebind() { Keyboard.OnKeyChanged += Keyboard_OnKeyChanged; }

        private static char MapFromKey(System.ConsoleKeyInfo key) {
            if (key.KeyChar != '\0') return key.KeyChar;
            var k = key.Key;
            bool shift = Keyboard.KeyInfo.Modifiers.HasFlag(System.ConsoleModifiers.Shift);
            bool caps = Keyboard.KeyInfo.Modifiers.HasFlag(System.ConsoleModifiers.CapsLock);
            if (k == System.ConsoleKey.Space) return ' ';
            if (k >= System.ConsoleKey.A && k <= System.ConsoleKey.Z) {
                char c = (char)('a' + (k - System.ConsoleKey.A));
                if (shift ^ caps) { if (c >= 'a' && c <= 'z') c = (char)('A' + (c - 'a')); }
                return c;
            }
            if (k >= System.ConsoleKey.D0 && k <= System.ConsoleKey.D9) {
                int d = (int)(k - System.ConsoleKey.D0);
                if (!shift) return (char)('0' + d);
                // Shifted number row
                switch (d) {
                    case 0: return ')';
                    case 1: return '!';
                    case 2: return '@';
                    case 3: return '#';
                    case 4: return '$';
                    case 5: return '%';
                    case 6: return '^';
                    case 7: return '&';
                    case 8: return '*';
                    case 9: return '(';
                }
            }
            switch (k) {
                case System.ConsoleKey.OemPeriod: return shift ? '>' : '.';
                case System.ConsoleKey.OemComma: return shift ? '<' : ',';
                case System.ConsoleKey.OemMinus: return shift ? '_' : '-';
                case System.ConsoleKey.OemPlus: return shift ? '+' : '=';
                case System.ConsoleKey.Oem1: return shift ? ':' : ';';
                case System.ConsoleKey.Oem2: return shift ? '?' : '/';
                case System.ConsoleKey.Oem3: return shift ? '~' : '`';
                case System.ConsoleKey.Oem4: return shift ? '{' : '[';
                case System.ConsoleKey.Oem5: return shift ? '|' : '\\';
                case System.ConsoleKey.Oem6: return shift ? '}' : ']';
                case System.ConsoleKey.Oem7: return shift ? '"' : '\'';
            }
            return '\0';
        }

        private void Keyboard_OnKeyChanged(object sender, System.ConsoleKeyInfo key) {
            if (!this.Visible) return;
            if (key.KeyState != System.ConsoleKeyState.Pressed) { _keyDown = false; _lastScan = 0; return; }
            if (_keyDown && Keyboard.KeyInfo.ScanCode == _lastScan) return; // debounce same key while held
            _keyDown = true; _lastScan = (byte)Keyboard.KeyInfo.ScanCode;

            if (key.Key == System.ConsoleKey.Backspace) {
                if (Cmd.Length > 0) Cmd = Cmd.Substring(0, Cmd.Length - 1);
                if (Data.Length > 0) Data = Data.Substring(0, Data.Length - 1);
                return;
            }

            if (key.Key == System.ConsoleKey.Enter) {
                HandleCommand(TrimSpaces(Cmd));
                Console_OnWrite('\n'); Cmd = string.Empty; return;
            }

            char ch = MapFromKey(key);
            if (ch != '\0') { Console_OnWrite(ch); Cmd += ch; }
        }

        private static string TrimSpaces(string s){
            if (s == null) return string.Empty;
            int a=0,b=s.Length-1; while(a<=b && s[a]==' ') a++; while(b>=a && s[b]==' ') b--; if (b<a) return string.Empty; return s.Substring(a,b-a+1);
        }

        private static bool TryParseIp(string s, out NETv4.IPAddress ip) {
            ip = default;
            int b1=-1,b2=-1,b3=-1,b4=-1; int part=0; int acc=0; bool any=false;
            for (int i=0;i<=s.Length;i++){
                char c = i<s.Length? s[i]: '.'; // sentinel
                if (c>='0'&&c<='9'){ acc = acc*10 + (c-'0'); if (acc>255) return false; any = true; }
                else if (c=='.'){
                    if (!any) return false;
                    if (part==0) b1=acc; else if (part==1) b2=acc; else if (part==2) b3=acc; else if (part==3) b4=acc; else return false;
                    part++; acc=0; any=false;
                } else { return false; }
            }
            if (part!=4) return false;
            ip = new NETv4.IPAddress((byte)b1,(byte)b2,(byte)b3,(byte)b4);
            return true;
        }

        private void HandleCommand(string cmdLine) {
            if (string.IsNullOrEmpty(cmdLine)) return;
            string[] parts = SplitArgs(cmdLine);
            string cmd = parts[0];
            switch (cmd) {
                case "help":
                    Console.WriteLine("Commands: help, shutdown, reboot, cpu, null, netinit, ifconfig, arp, dns <host>, ping <hostOrIp>, authurl <httpUrl>, authlogin <user> <pass>, authregister <user> <pass>, authtoken, logout");
                    break;
                case "shutdown": Power.Shutdown(); break;
                case "reboot": Power.Reboot(); break;
                case "cpu": break;
                case "null": unsafe { uint* ptr = null; *ptr = 0xDEADBEEF; } break;
                case "netinit":
                    Console.WriteLine("[NET] Initializing stack");
                    NETv4.Initialize();
                    Console.WriteLine("[NET] Initializing NICs");
                    bool nic=false;
                    try { Intel825xx.Initialize(); Console.WriteLine("[NET] Intel825xx initialized"); nic=true; } catch { }
                    try { RTL8111.Initialize(); Console.WriteLine("[NET] RTL8111 initialized"); nic=true; } catch { }
                    if (!nic) Console.WriteLine("[NET] No supported NIC found");
                    Console.WriteLine("[NET] DHCP discover...");
                    bool dhcp = NETv4.DHCPDiscover();
                    if (!dhcp) Console.WriteLine("[NET] DHCP failed"); else Console.WriteLine("[NET] DHCP OK");
                    break;
                case "ifconfig":
                    Console.WriteLine($"IP: {NETv4.IP.P1}.{NETv4.IP.P2}.{NETv4.IP.P3}.{NETv4.IP.P4}");
                    Console.WriteLine($"Mask: {NETv4.Mask.P1}.{NETv4.Mask.P2}.{NETv4.Mask.P3}.{NETv4.Mask.P4}");
                    Console.WriteLine($"Gateway: {NETv4.GatewayIP.P1}.{NETv4.GatewayIP.P2}.{NETv4.GatewayIP.P3}.{NETv4.GatewayIP.P4}");
                    Console.WriteLine($"MAC: {NETv4.MAC.P1:x2}:{NETv4.MAC.P2:x2}:{NETv4.MAC.P3:x2}:{NETv4.MAC.P4:x2}:{NETv4.MAC.P5:x2}:{NETv4.MAC.P6:x2}");
                    break;
                case "arp":
                    if (NETv4.ARPTable == null) { Console.WriteLine("ARP table not initialized"); break; }
                    for (int i=0;i<NETv4.ARPTable.Count;i++) {
                        var e = NETv4.ARPTable[i];
                        Console.WriteLine($"{e.IP.P1}.{e.IP.P2}.{e.IP.P3}.{e.IP.P4} -> {e.MAC.P1:x2}:{e.MAC.P2:x2}:{e.MAC.P3:x2}:{e.MAC.P4:x2}:{e.MAC.P5:x2}:{e.MAC.P6:x2}");
                    }
                    break;
                case "dns":
                    if (parts.Length < 2) { Console.WriteLine("Usage: dns <host>"); break; }
                    var ip = NETv4.DNSQuery(parts[1]);
                    if (ip.P1==0 && ip.P2==0 && ip.P3==0 && ip.P4==0) Console.WriteLine("DNS failed");
                    else Console.WriteLine($"Resolved: {ip.P1}.{ip.P2}.{ip.P3}.{ip.P4}");
                    break;
                case "ping":
                    if (parts.Length < 2) { Console.WriteLine("Usage: ping <hostOrIp>"); break; }
                    NETv4.IPAddress dip;
                    if (!TryParseIp(parts[1], out dip)) {
                        dip = NETv4.DNSQuery(parts[1]);
                        if (dip.P1==0 && dip.P2==0 && dip.P3==0 && dip.P4==0) { Console.WriteLine("Unable to resolve host"); break; }
                    }
                    Console.WriteLine($"Pinging {dip.P1}.{dip.P2}.{dip.P3}.{dip.P4} with {NETv4.ICMPPingBytes} bytes of data:");
                    NETv4.IsICMPRespond = false; NETv4.ICMPReplyTTL = 0; NETv4.ICMPReplyBytes=0;
                    NETv4.ICMPPing(dip);
                    {
                        ulong start = Timer.Ticks; int timeout=2000;
                        while (!NETv4.IsICMPRespond) { if ((long)(Timer.Ticks - start) > timeout) break; ACPITimer.Sleep(10); }
                        if (NETv4.IsICMPRespond) {
                            Console.WriteLine($"Reply from {dip.P1}.{dip.P2}.{dip.P3}.{dip.P4}: bytes={NETv4.ICMPReplyBytes} ttl={NETv4.ICMPReplyTTL}");
                        } else {
                            Console.WriteLine("Request timed out.");
                        }
                    }
                    break;
                case "authurl":
                    if (parts.Length < 2) { Console.WriteLine("Usage: authurl <http://host:port>"); break; }
                    Session.ServiceBaseUrl = parts[1];
                    Console.WriteLine("ServiceBaseUrl set to: " + Session.ServiceBaseUrl);
                    break;
                case "authlogin":
                    if (parts.Length < 3) { Console.WriteLine("Usage: authlogin <username> <password>"); break; }
                    {
                        string token; string msg;
                        bool ok = AuthClient.TryLogin(parts[1], parts[2], out token, out msg);
                        if (ok) { Session.LoginToken = token; Console.WriteLine("Login OK. Token=" + token); }
                        else { Console.WriteLine("Login failed: " + (msg ?? "")); }
                    }
                    break;
                case "authregister":
                    if (parts.Length < 3) { Console.WriteLine("Usage: authregister <username> <password>"); break; }
                    {
                        string msg;
                        bool ok = AuthClient.TryRegister(parts[1], parts[2], out msg);
                        if (ok) Console.WriteLine("Register OK. Now run authlogin."); else Console.WriteLine("Register failed: " + (msg ?? ""));
                    }
                    break;
                case "authtoken":
                    Console.WriteLine("Token: " + (Session.LoginToken ?? ""));
                    break;
                case "logout":
                    Session.LoginToken = string.Empty; Console.WriteLine("Logged out.");
                    break;
                default:
                    Console.WriteLine($"No such command: \"{cmdLine}\"");
                    break;
            }
        }

        private static string[] SplitArgs(string s) {
            // simple split by spaces
            int n=0; for(int i=0;i<s.Length;i++){ if (s[i]==' ') n++; }
            string[] arr = new string[n+1]; int idx=0; int start=0;
            for (int i=0;i<=s.Length;i++){
                if (i==s.Length || s[i]==' '){ int len=i-start; if (len>0) arr[idx++] = s.Substring(start,len); start=i+1; }
            }
            // compact nulls
            int count=0; for(int i=0;i<arr.Length;i++) if (arr[i]!=null) count++;
            string[] res = new string[count]; int j=0; for(int i=0;i<arr.Length;i++) if (arr[i]!=null) res[j++]=arr[i];
            return res;
        }

        public override void OnDraw() { base.OnDraw(); string s1 = Data + "_"; DrawString(X, Y, s1, Height, Width); }

        public void DrawString(int X, int Y, string Str, int HeightLimit = -1, int LineLimit = -1) {
            // Render with built-in ASC16 font to guarantee ASCII punctuation shows
            int wpx = 0, hpx = 0;
            for (int i = 0; i < Str.Length; i++) {
                char ch = Str[i];
                if (ch == '\n') { wpx = 0; hpx += 16; goto handle_wrap; }
                // draw char (ASCII range supported by ASC16)
                ASC16.DrawChar(ch, X + wpx, Y + hpx, 0xFFFFFFFF);
                wpx += 8;
                if (LineLimit != -1 && wpx + 8 > LineLimit) { wpx = 0; hpx += 16; }
            handle_wrap:
                if (HeightLimit != -1 && hpx >= HeightLimit) {
                    // Scroll up one row inside the console window area
                    Framebuffer.Graphics.Copy(X, Y, X, Y + 16, LineLimit, HeightLimit - 16);
                    Framebuffer.Graphics.FillRectangle(X, Y + HeightLimit - 16, LineLimit, 16, 0xFF222222);
                    hpx -= 16;
                }
            }
        }

        private void Console_OnWrite(char chr) {
            if (Program.FConsole != null && Program.FConsole.Visible == false) { WindowManager.MoveToEnd(Program.FConsole); Program.FConsole.Visible = true; }
            Data += chr;
        }

        public void WriteLine(string line) { Console.WriteLine(line); }
    }
}