using guideXOS.Kernel.Drivers;
using guideXOS.Misc;

namespace guideXOS.Misc {
    internal static class BootSplash {
        static string sTeam;
        static string sOS;
        static string sVer;
        static int phase;
        static ulong lastTick;
        static bool inited;

        public static void Initialize(string team = "Team Nexgen", string os = "guideXOS", string ver = "Version: 0.1") {
            sTeam = team;
            sOS = os;
            sVer = ver;
            phase = 0;
            lastTick = 0;
            inited = true;
        }

        public static void Tick() {
            if (!inited) return;
            // advance phase ~ at ~60Hz timer; fallback to simple increment
            if (lastTick != Timer.Ticks) {
                lastTick = Timer.Ticks;
                phase = (phase + 1) % 3;
            }

            // Clear
            Framebuffer.Graphics.Clear(0x00000000);

            // Draw centered texts using ASC16 (8x16)
            int w = Framebuffer.Width;
            int h = Framebuffer.Height;

            // Team (small)
            int teamW = sTeam.Length * 8;
            int teamX = (w / 2) - (teamW / 2);
            int teamY = (h / 2) - 48;
            ASC16.DrawString(sTeam, teamX, teamY, 0xFFFFFFFF);

            // OS (large look by drawing twice offset)
            int osW = sOS.Length * 8;
            int osX = (w / 2) - (osW / 2);
            int osY = teamY + 22;
            // drop shadow
            ASC16.DrawString(sOS, osX + 1, osY + 1, 0xFF202020);
            ASC16.DrawString(sOS, osX, osY, 0xFFFFFFFF);

            // Version (very small - just normal font with gray)
            int verW = sVer.Length * 8;
            int verX = (w / 2) - (verW / 2);
            int verY = osY + 22;
            ASC16.DrawString(sVer, verX, verY, 0xFFAAAAAA);

            // Animated 3 blocks below text
            int blocksY = verY + 40;
            int size = 10;
            int gap = 12;
            int totalW = size * 3 + gap * 2;
            int startX = (w / 2) - (totalW / 2);
            for (int i = 0; i < 3; i++) {
                uint col = (i == phase) ? 0xFF2E86C1u : 0xFF3A3A3Au;
                Framebuffer.Graphics.FillRectangle(startX + i * (size + gap), blocksY, size, size, col);
            }

            Framebuffer.Update();
        }
    }
}
