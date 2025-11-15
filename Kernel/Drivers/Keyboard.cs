using System;
namespace guideXOS.Kernel.Drivers {
    /// <summary>
    /// On Key Handler
    /// </summary>
    /// <param name="key"></param>
    public delegate void OnKeyHandler(ConsoleKeyInfo key);
    /// <summary>
    /// Keyboard
    /// </summary>
    public static class Keyboard {
        /// <summary>
        /// Key Info
        /// </summary>
        public static ConsoleKeyInfo KeyInfo;
        /// <summary>
        /// On key Changed
        /// </summary>
        public static EventHandler<ConsoleKeyInfo> OnKeyChanged;
        /// <summary>
        /// Initialize
        /// </summary>
        public static void Initialize() {
            OnKeyChanged = null;
        }
        /// <summary>
        /// Invoke On Key Changed
        /// </summary>
        /// <param name="info"></param>
        internal static void InvokeOnKeyChanged(ConsoleKeyInfo info) {
            OnKeyChanged?.Invoke(null, info);
        }
        /// <summary>
        /// Simulate Key - for On-Screen Keyboard
        /// </summary>
        /// <param name="info"></param>
        public static void SimulateKey(ConsoleKeyInfo info) {
            KeyInfo = info;
            OnKeyChanged?.Invoke(null, info);
        }
        /// <summary>
        /// Clean Key Info
        /// </summary>
        /// <param name="NoModifiers"></param>
        public static void CleanKeyInfo(bool NoModifiers = false) {
            KeyInfo.KeyChar = '\0';
            KeyInfo.ScanCode = 0;
            KeyInfo.KeyState = ConsoleKeyState.None;
            if (!NoModifiers) {
                KeyInfo.Modifiers = ConsoleModifiers.None;
            }
        }
    }
}