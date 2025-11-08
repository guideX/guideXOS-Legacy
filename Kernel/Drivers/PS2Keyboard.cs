//http://cc.etsii.ull.es/ftp/antiguo/EC/AOA/APPND/Apndxc.pdf
using guideXOS.Misc;
using System;
using static System.ConsoleKey;
namespace guideXOS.Kernel.Drivers {
    public static unsafe class PS2Keyboard {
        private static char[] _keyChars;
        private static char[] _keyCharsShift;
        private static ConsoleKey[] _keys;
        private static bool _shiftPressed = false; private static bool _ctrlPressed = false; private static bool _altPressed = false; private static bool _capsLockOn = false;
        private static bool _extended = false; // track 0xE0 prefix for extended keys
        public static bool Initialize() {
            _keyChars = new char[] {
                '\0','\0','1','2','3','4','5','6','7','8','9','0','-','=','\b','\t',
                'q','w','e','r','t','y','u','i','o','p','[',']','\n','\0',
                'a','s','d','f','g','h','j','k','l',';','\'','`','\0','\\',
                'z','x','c','v','b','n','m',',','.','/','\0','*','\0',' ','\0',
                '\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','7','8','9','-',
                '4','5','6','+','1','2','3','0','.', '\0','\0','\0','\0','\0'
            };
            _keyCharsShift = new char[] {
                '\0','\0','!','@','#','$','%','^','&','*','(',')','_','+','\b','\t',
                'Q','W','E','R','T','Y','U','I','O','P','{','}','\n','\0',
                'A','S','D','F','G','H','J','K','L',':','"','~','\0','|',
                'Z','X','C','V','B','N','M','<','>','?','\0','*','\0',' ','\0',
                '\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','7','8','9','-',
                '4','5','6','+','1','2','3','0','.', '\0','\0','\0','\0','\0'
            };
            _keys = new[] {
                None, Escape, D1, D2, D3, D4, D5, D6, D7, D8, D9, D0, OemMinus, OemPlus, Backspace, Tab,
                Q, W, E, R, T, Y, U, I, O, P, Oem4, Oem6, Return, LControlKey,
                A, S, D, F, G, H, J, K, L, Oem1, Oem7, Oem3, LShiftKey, Oem5,
                Z, X, C, V, B, N, M, OemComma, OemPeriod, Oem2, RShiftKey, Multiply, LMenu, Space, Capital, F1, F2, F3, F4, F5,
                F6, F7, F8, F9, F10, NumLock, Scroll, Home, Up, Prior, Subtract, Left, Clear, Right, Add, End,
                Down, Next, Insert, Delete, Snapshot, None, Oem5, F11, F12
            };
            Keyboard.CleanKeyInfo();
            Interrupts.EnableInterrupt(0x21, &OnInterrupt);
            return true;
        }
        public static void OnInterrupt() {
            byte b = Native.In8(0x60);
            ProcessKey(b);
        }
        public static void ProcessKey(byte b) {
            if (b == 0xE0) { _extended = true; return; }
            bool isRelease = (b & 0x80) != 0; byte scanCode = (byte)(b & 0x7F);

            // Map extended right Ctrl/Alt
            if (_extended) {
                if (scanCode == 0x1D) { _ctrlPressed = !isRelease; } // RCtrl
                if (scanCode == 0x38) { _altPressed = !isRelease; }  // RAlt
                _extended = false; // consume prefix
            } else {
                // Left-side modifiers and Caps
                if (scanCode == 0x1D) { _ctrlPressed = !isRelease; }
                if (scanCode == 0x2A || scanCode == 0x36) { _shiftPressed = !isRelease; }
                if (scanCode == 0x38) { _altPressed = !isRelease; }
                if (scanCode == 0x3A && !isRelease) { _capsLockOn = !_capsLockOn; }
            }

            if (scanCode >= _keys.Length) {
                // Still update modifiers state into KeyInfo then notify
                Keyboard.KeyInfo.ScanCode = b;
                Keyboard.KeyInfo.KeyState = isRelease ? ConsoleKeyState.Released : ConsoleKeyState.Pressed;
                Keyboard.KeyInfo.Modifiers = ConsoleModifiers.None;
                if (_shiftPressed) Keyboard.KeyInfo.Modifiers |= ConsoleModifiers.Shift;
                if (_ctrlPressed) Keyboard.KeyInfo.Modifiers |= ConsoleModifiers.Control;
                if (_altPressed) Keyboard.KeyInfo.Modifiers |= ConsoleModifiers.Alt;
                if (_capsLockOn) Keyboard.KeyInfo.Modifiers |= ConsoleModifiers.CapsLock;
                Keyboard.InvokeOnKeyChanged(Keyboard.KeyInfo);
                return;
            }

            Keyboard.KeyInfo.ScanCode = scanCode;
            Keyboard.KeyInfo.KeyState = isRelease ? ConsoleKeyState.Released : ConsoleKeyState.Pressed;
            Keyboard.KeyInfo.Modifiers = ConsoleModifiers.None;
            if (_shiftPressed) Keyboard.KeyInfo.Modifiers |= ConsoleModifiers.Shift;
            if (_ctrlPressed) Keyboard.KeyInfo.Modifiers |= ConsoleModifiers.Control;
            if (_altPressed) Keyboard.KeyInfo.Modifiers |= ConsoleModifiers.Alt;
            if (_capsLockOn) Keyboard.KeyInfo.Modifiers |= ConsoleModifiers.CapsLock;

            // Character mapping only when key is pressed
            char c = '\0';
            if (!isRelease && scanCode < _keyChars.Length) {
                char baseChar = _keyChars[scanCode];
                if (_shiftPressed && scanCode < _keyCharsShift.Length) c = _keyCharsShift[scanCode];
                else if (_capsLockOn && baseChar >= 'a' && baseChar <= 'z') c = (char)(baseChar - 32);
                else c = baseChar;
            }
            Keyboard.KeyInfo.KeyChar = c;

            // Set ConsoleKey (rough map)
            Keyboard.KeyInfo.Key = _keys[scanCode];

            Keyboard.InvokeOnKeyChanged(Keyboard.KeyInfo);
            Kbd2Mouse.OnKeyChanged(Keyboard.KeyInfo);
        }
    }
}