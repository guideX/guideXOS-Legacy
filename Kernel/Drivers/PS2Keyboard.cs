using guideXOS;
using System;

namespace guideXOS.Kernel.Drivers {
    public static class PS2Keyboard {
        private static bool _shift = false;
        private static bool _capsLock = false;
        private static bool _extended = false; // <-- Tracks 0xE0 state

        private static readonly char[] _scanCodeMap = new char[]
        {
            '\0','\x1B','1','2','3','4','5','6','7','8','9','0','-','=', '\b',
            '\t','q','w','e','r','t','y','u','i','o','p','[',']','\n','\0',
            'a','s','d','f','g','h','j','k','l',';','\'','`','\0','\\',
            'z','x','c','v','b','n','m',',','.','/','\0','*','\0',' ','\0',
            '\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0',
            '\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0',
            '\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0'
        };

        private static char ToUpper(char c) {
            if (c >= 'a' && c <= 'z') return (char)(c - 32);
            return c;
        }

        public static void Initialize() { }

        public static void HandleScanCode(byte scanCode) {
            // Check for extended sequence prefix
            if (scanCode == 0xE0) {
                _extended = true;
                return; // wait for next interrupt
            }

            bool keyReleased = (scanCode & 0x80) != 0;
            scanCode &= 0x7F;

            // Handle modifier keys
            if (!_extended) {
                if (scanCode == 42 || scanCode == 54) // Shift
                {
                    _shift = !keyReleased;
                    return;
                }

                if (scanCode == 58 && !keyReleased) // Caps lock toggle
                {
                    _capsLock = !_capsLock;
                    return;
                }
            }

            // Handle extended keys (like right ctrl, arrow keys, numpad enter, etc.)
            if (_extended) {
                // Example: ignore arrows, handle keypad symbols properly
                if (scanCode == 0x35 && !keyReleased) {
                    // '/' key on keypad or extended keyboard
                    Console.Write('/');
                }
                _extended = false; // reset extended flag
                return;
            }

            // Standard key processing
            if (scanCode >= _scanCodeMap.Length) return;

            char c = _scanCodeMap[scanCode];
            if (c == '\0') return;

            if ((_shift ^ _capsLock) && c >= 'a' && c <= 'z')
                c = ToUpper(c);
            else if (_shift) {
                // Handle shifted symbols
                switch (c) {
                    case '1': c = '!'; break;
                    case '2': c = '@'; break;
                    case '3': c = '#'; break;
                    case '4': c = '$'; break;
                    case '5': c = '%'; break;
                    case '6': c = '^'; break;
                    case '7': c = '&'; break;
                    case '8': c = '*'; break;
                    case '9': c = '('; break;
                    case '0': c = ')'; break;
                    case '-': c = '_'; break;
                    case '=': c = '+'; break;
                    case '[': c = '{'; break;
                    case ']': c = '}'; break;
                    case '\\': c = '|'; break;
                    case ';': c = ':'; break;
                    case '\'': c = '"'; break;
                    case ',': c = '<'; break;
                    case '.': c = '>'; break;
                    case '/': c = '?'; break;
                }
            }

            if (!keyReleased)
                Console.Write(c);
        }
    }
}
