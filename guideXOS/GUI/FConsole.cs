using guideXOS.Kernel.Drivers;
using System.Drawing;

namespace guideXOS.GUI {
    /// <summary>
    /// FConsole
    /// </summary>
    internal class FConsole : Window {
        /// <summary>
        /// Data
        /// </summary>
        private string Data;
        /// <summary>
        /// Screen Buffer
        /// </summary>
        public Image ScreenBuf;
        /// <summary>
        /// Cmd
        /// </summary>
        private string Cmd;
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="X"></param>
        /// <param name="Y"></param>
        public FConsole(int X, int Y) : base(X, Y, 640, 320) {
            Title = "Console"; Cmd = string.Empty; Data = string.Empty; ScreenBuf = new Image(640, 320);
            Rebind(); Console.OnWrite += Console_OnWrite; Console.WriteLine("Type help to get information!");
        }
        /// <summary>
        /// Rebind
        /// </summary>
        public void Rebind() { Keyboard.OnKeyChanged += Keyboard_OnKeyChanged; }
        /// <summary>
        /// PS2 Keyboard OnChanged
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="key"></param>
        private void Keyboard_OnKeyChanged(object sender, System.ConsoleKeyInfo key) {
            // Handle input for this window instance when it's visible.
            if (key.KeyState != System.ConsoleKeyState.Pressed || !this.Visible) return;

            if (key.Key == System.ConsoleKey.Backspace) { if (Cmd.Length > 0) Cmd = Cmd.Substring(0, Cmd.Length - 1); if (Data.Length > 0) Data = Data.Substring(0, Data.Length - 1); return; }

            if (key.Key == System.ConsoleKey.Enter) {
                // Execute command on Enter
                switch (Cmd) { case "help": Console.WriteLine("help: to get this information"); break; case "shutdown": Power.Shutdown(); break; case "cpu": break; case "null": unsafe { uint* ptr = null; *ptr = 0xDEADBEEF; } break; case "reboot": Power.Reboot(); break; default: Console.WriteLine($"No such command: \"{Cmd}\""); break; }
                // Move cursor to next line visually.
                Console_OnWrite('\n'); Cmd = string.Empty; return;
            }

            if (key.KeyChar != '\0') { Console_OnWrite(key.KeyChar); Cmd += key.KeyChar; }
        }
        /// <summary>
        /// On Draw
        /// </summary>
        public override void OnDraw() { base.OnDraw(); string s1 = Data + "_"; DrawString(X, Y, s1, Height, Width); }
        /// <summary>
        /// Draw String
        /// </summary>
        /// <param name="X"></param>
        /// <param name="Y"></param>
        /// <param name="Str"></param>
        /// <param name="HeightLimit"></param>
        /// <param name="LineLimit"></param>
        public void DrawString(int X, int Y, string Str, int HeightLimit = -1, int LineLimit = -1) { int w = 0, h = 0; for (int i = 0; i < Str.Length; i++) { w += WindowManager.font.DrawChar(Framebuffer.Graphics, X + w, Y + h, Str[i]); if ((LineLimit != -1 && w + WindowManager.font.FontSize > LineLimit) || Str[i] == '\n') { w = 0; h += WindowManager.font.FontSize; if (HeightLimit != -1 && h >= HeightLimit) { Framebuffer.Graphics.Copy(X, Y, X, Y + WindowManager.font.FontSize, LineLimit, HeightLimit - WindowManager.font.FontSize); Framebuffer.Graphics.FillRectangle(X, Y + HeightLimit - WindowManager.font.FontSize, LineLimit, WindowManager.font.FontSize, 0xFF222222); h -= WindowManager.font.FontSize; } } } }
        /// <summary>
        /// Console OnWrite
        /// </summary>
        /// <param name="chr"></param>
        private void Console_OnWrite(char chr) {
            if (Program.FConsole != null && Program.FConsole.Visible == false) { WindowManager.MoveToEnd(Program.FConsole); Program.FConsole.Visible = true; } Data += chr;
        }
        /// <summary>
        /// WriteLine
        /// </summary>
        /// <param name="line"></param>
        public void WriteLine(string line) { Console.WriteLine(line); }
    }
}