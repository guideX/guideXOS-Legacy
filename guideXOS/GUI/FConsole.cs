using guideXOS.Kernel.Drivers;
using System.Drawing;
using System; 

namespace guideXOS.GUI {
    internal class FConsole : Window {
        private string Data; public Image ScreenBuf; private string Cmd;
        public FConsole(int X, int Y) : base(X, Y, 640, 320) { Title = "Console"; Cmd = string.Empty; Data = string.Empty; ScreenBuf = new Image(640, 320); Rebind(); Console.OnWrite += Console_OnWrite; Console.WriteLine("Type help to get information!"); }
        public void Rebind() { Keyboard.OnKeyChanged += Keyboard_OnKeyChanged; }
        private static char MapFromKey(System.ConsoleKeyInfo key) {
            if (key.KeyChar != '\0') return key.KeyChar;
            var k = key.Key; if (k == System.ConsoleKey.Space) return ' ';
            if (k >= System.ConsoleKey.A && k <= System.ConsoleKey.Z) { bool upper = Keyboard.KeyInfo.Modifiers.HasFlag(System.ConsoleModifiers.Shift) ^ Keyboard.KeyInfo.Modifiers.HasFlag(System.ConsoleModifiers.CapsLock); char c = (char)('a' + (k - System.ConsoleKey.A)); return upper ? c.ToUpper() : c; }
            if (k >= System.ConsoleKey.D0 && k <= System.ConsoleKey.D9) return (char)('0' + (k - System.ConsoleKey.D0));
            switch (k) { case System.ConsoleKey.OemPeriod: return '.'; case System.ConsoleKey.OemComma: return ','; case System.ConsoleKey.OemMinus: return '-'; case System.ConsoleKey.OemPlus: return '+'; case System.ConsoleKey.Oem1: return ';'; case System.ConsoleKey.Oem2: return '/'; case System.ConsoleKey.Oem3: return '`'; case System.ConsoleKey.Oem4: return '['; case System.ConsoleKey.Oem5: return '\\'; case System.ConsoleKey.Oem6: return ']'; case System.ConsoleKey.Oem7: return '\''; } return '\0'; }
        private void Keyboard_OnKeyChanged(object sender, System.ConsoleKeyInfo key) {
            if (key.KeyState != System.ConsoleKeyState.Pressed || !this.Visible) return;
            if (key.Key == System.ConsoleKey.Backspace) { if (Cmd.Length > 0) Cmd = Cmd.Substring(0, Cmd.Length - 1); if (Data.Length > 0) Data = Data.Substring(0, Data.Length - 1); return; }
            if (key.Key == System.ConsoleKey.Enter) { switch (Cmd) { case "help": Console.WriteLine("help: to get this information"); break; case "shutdown": Power.Shutdown(); break; case "cpu": break; case "null": unsafe { uint* ptr = null; *ptr = 0xDEADBEEF; } break; case "reboot": Power.Reboot(); break; default: Console.WriteLine($"No such command: \"{Cmd}\""); break; } Console_OnWrite('\n'); Cmd = string.Empty; return; }
            char ch = MapFromKey(key); if (ch != '\0') { Console_OnWrite(ch); Cmd += ch; }
        }
        public override void OnDraw() { base.OnDraw(); string s1 = Data + "_"; DrawString(X, Y, s1, Height, Width); }
        public void DrawString(int X, int Y, string Str, int HeightLimit = -1, int LineLimit = -1) { int w = 0, h = 0; for (int i = 0; i < Str.Length; i++) { w += WindowManager.font.DrawChar(Framebuffer.Graphics, X + w, Y + h, Str[i]); if ((LineLimit != -1 && w + WindowManager.font.FontSize > LineLimit) || Str[i] == '\n') { w = 0; h += WindowManager.font.FontSize; if (HeightLimit != -1 && h >= HeightLimit) { Framebuffer.Graphics.Copy(X, Y, X, Y + WindowManager.font.FontSize, LineLimit, HeightLimit - WindowManager.font.FontSize); Framebuffer.Graphics.FillRectangle(X, Y + HeightLimit - WindowManager.font.FontSize, LineLimit, WindowManager.font.FontSize, 0xFF222222); h -= WindowManager.font.FontSize; } } } }
        private void Console_OnWrite(char chr) { if (Program.FConsole != null && Program.FConsole.Visible == false) { WindowManager.MoveToEnd(Program.FConsole); Program.FConsole.Visible = true; } Data += chr; }
        public void WriteLine(string line) { Console.WriteLine(line); }
    }
}