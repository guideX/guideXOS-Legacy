using guideXOS.Kernel.Drivers;
using guideXOS.Misc;
using System.Drawing;
using System.Collections.Generic;

namespace guideXOS.GUI {
    internal struct RecentProgramEntry {
        public string Name;
        public Image Icon;
        public ulong Ticks; // last used
    }
    internal struct RecentDocumentEntry {
        public string Path;
        public Image Icon;
        public ulong Ticks;
    }

    internal static class RecentManager {
        private const int MaxPrograms = 32;
        private const int MaxDocuments = 64;
        private static List<RecentProgramEntry> _programs = new List<RecentProgramEntry>();
        private static List<RecentDocumentEntry> _documents = new List<RecentDocumentEntry>();

        public static void AddProgram(string name, Image icon) {
            if (name == null) return;
            // Remove duplicate by name
            for (int i = 0; i < _programs.Count; i++) {
                if (_programs.ToArray()[i].Name == name) { _programs.RemoveAt(i); break; }
            }
            RecentProgramEntry e;
            e.Name = name;
            e.Icon = icon ?? Icons.DocumentIcon;
            e.Ticks = Timer.Ticks;
            _programs.Insert(0, e);
            if (_programs.Count > MaxPrograms) _programs.RemoveAt(_programs.Count - 1);
        }

        public static void AddDocument(string path, Image icon = null) {
            if (path == null) return;
            for (int i = 0; i < _documents.Count; i++) {
                if (_documents.ToArray()[i].Path == path) { _documents.RemoveAt(i); break; }
            }
            RecentDocumentEntry d;
            d.Path = path;
            d.Icon = icon ?? Icons.DocumentIcon;
            d.Ticks = Timer.Ticks;
            _documents.Insert(0, d);
            if (_documents.Count > MaxDocuments) _documents.RemoveAt(_documents.Count - 1);
        }

        public static List<RecentProgramEntry> Programs { get { return _programs; } }
        public static List<RecentDocumentEntry> Documents { get { return _documents; } }
    }
}
