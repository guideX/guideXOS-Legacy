using guideXOS.Kernel.Drivers;

namespace guideXOS.FS {
    internal unsafe partial class AutoFS : FileSystem {
        // Probe EXT superblock directly. Returns true if 0xEF53 magic present.
        private static bool LooksLikeExt() {
            var tmp = new byte[1024];
            // Read superblock at byte offset 1024 (sector-aligned at 2048)
            var buf = new byte[1024];
            // read two sectors and slice without extra helpers to avoid duplication
            var sec = new byte[512 * 3];
            fixed (byte* p = sec) Disk.Instance.Read(2, 3, p); // sectors 2..4 cover 1024.. 1024+1536
            for (int i = 0; i < 1024; i++) buf[i] = sec[512 + i];
            ushort magic = (ushort)(buf[0x38] | (buf[0x39] << 8));
            return magic == 0xEF53;
        }
    }
}
