namespace guideXOS.GUI {
    // Global UI settings for animations
    internal static class UISettings {
        // Fading animations (open/close)
        public static bool EnableFadeAnimations = false; // enable by default
        public static int FadeInDurationMs = 180;
        public static int FadeOutDurationMs = 180;

        // Window slide animations (minimize/restore)
        public static bool EnableWindowSlideAnimations = false; // can be enabled later
        public static int WindowSlideDurationMs = 220;
    }
}
