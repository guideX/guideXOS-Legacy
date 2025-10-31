namespace guideXOS.GUI {
    public static class Busy {
        private static int _count;
        public static bool IsBusy { get { return _count > 0; } }
        public static void Push() { _count++; }
        public static void Pop() { if (_count > 0) _count--; }
    }
}
