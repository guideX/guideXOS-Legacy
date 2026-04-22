using System.Runtime.InteropServices;

namespace Notepad.GXM
{
    /// <summary>
    /// Minimal GXM entry point for Notepad
    /// This is a stub - the actual GUI is defined in the notepad-simple.txt script
    /// </summary>
    public static class Program
    {
        /// <summary>
        /// Entry point for the GXM application
        /// For GUI script-based GXMs, this provides a minimal entry point
        /// </summary>
        [UnmanagedCallersOnly(EntryPoint = "NotepadMain")]
        public static void Main()
        {
            // Minimal entry point for script-based GXM
            // The GXMLoader in guideXOS will parse the accompanying script
            // and create the window/buttons/controls automatically
        }
    }
}
