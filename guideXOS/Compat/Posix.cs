using System;
using guideXOS.FS;
using guideXOS.Misc;

namespace guideXOS.Compat {
    // Minimal POSIX compatibility helpers for console commands.
    // Provides path normalization and simple wrappers used by shell-like commands.
    internal static class Posix {
        private static bool StartsWithFast(string s, char ch){ return s!=null && s.Length>0 && s[0]==ch; }
        // Normalize separators: collapse repeated '/', remove './', handle '../' segments.
        public static string NormalizePath(string cwd, string input) {
            if (string.IsNullOrEmpty(input)) return cwd ?? string.Empty;
            bool absolute = StartsWithFast(input,'/');
            string path = absolute ? input : CombineRelative(cwd, input);
            // Split and process components
            string[] parts = Split(path);
            int w = 0; // write index
            for (int i = 0; i < parts.Length; i++) {
                string p = parts[i];
                if (p.Length == 0 || p == ".") continue;
                if (p == "..") { if (w > 0) w--; continue; }
                parts[w++] = p;
            }
            string result = "/";
            for (int i = 0; i < w; i++) {
                result += parts[i];
                if (i != w - 1) result += "/";
            }
            return result;
        }
        private static string CombineRelative(string cwd, string rel) { if (string.IsNullOrEmpty(cwd)) return rel; if (!cwd.EndsWith("/")) cwd += "/"; return cwd + rel; }
        private static string[] Split(string s) { int n=0; for(int i=0;i<s.Length;i++) if(s[i]=='/') n++; string[] arr=new string[n+1]; int idx=0,start=0; for(int i=0;i<=s.Length;i++){ if(i==s.Length||s[i]=='/'){ int len=i-start; arr[idx++]= len>0? s.Substring(start,len):""; start=i+1; } } return arr; }

        // Basic wrappers for listing and reading files (no permissions yet)
        public static string[] List(string path) {
            var list = File.GetFiles(path == "/" ? "" : path);
            if (list == null) return Array.Empty<string>();
            string[] names = new string[list.Count];
            for (int i=0;i<list.Count;i++){ names[i] = list[i].Name; list[i].Dispose(); }
            return names;
        }
        public static byte[] ReadFile(string path) { return File.ReadAllBytes(path); }
        public static bool WriteFile(string path, byte[] data) { File.WriteAllBytes(path, data); return true; }
    }
}
