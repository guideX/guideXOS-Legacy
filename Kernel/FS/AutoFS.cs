using guideXOS.Kernel.Drivers;
using System;

namespace guideXOS.FS {
    /// <summary>
    /// Detects filesystem in the attached Disk and instantiates appropriate FileSystem.
    /// Supports TAR (initrd) and FAT12/16/32 images.
    /// </summary>
    internal unsafe partial class AutoFS : FileSystem {
        private FileSystem _impl;

        public AutoFS() {
            // Peek at sector 0
            var buf = new byte[SectorSize];
            fixed (byte* p = buf) Disk.Instance.Read(0, 1, p);

            if (LooksLikeTar(buf)) {
                _impl = new TarFS();
            } else if (LooksLikeFat(buf)) {
                _impl = new FAT();
            } else if (LooksLikeExt()) {
                _impl = new EXT2();
            } else {
                // Fallback to TarFS for backward compatibility
                _impl = new TarFS();
            }
            // Set as active implementation
            File.Instance = _impl;
        }

        private static bool LooksLikeTar(byte[] sector0) {
            // POSIX ustar magic at offset 257: "ustar\0" or "ustar\x00" and version "00"
            if (sector0.Length < 512) return false;
            return sector0[257] == (byte)'u' && sector0[258] == (byte)'s' && sector0[259] == (byte)'t' &&
                   sector0[260] == (byte)'a' && sector0[261] == (byte)'r';
        }

        private static bool LooksLikeFat(byte[] sector0) {
            if (sector0.Length < 512) return false;
            // Signature 0x55AA at 510
            if (sector0[510] != 0x55 || sector0[511] != 0xAA) return false;
            ushort bytsPerSec = (ushort)(sector0[11] | (sector0[12] << 8));
            byte secPerClus = sector0[13];
            ushort rsvdSec = (ushort)(sector0[14] | (sector0[15] << 8));
            byte numFATs = sector0[16];
            if (bytsPerSec == 0) return false;
            if (secPerClus == 0) return false;
            if (numFATs == 0) return false;
            // Accept typical bytes per sector
            if (bytsPerSec != 512 && bytsPerSec != 1024 && bytsPerSec != 2048 && bytsPerSec != 4096) return false;
            // SecPerClus power-of-two up to 128
            if ((secPerClus & (secPerClus - 1)) != 0 || secPerClus > 128) return false;
            // Reserved sectors at least 1
            if (rsvdSec == 0) return false;
            return true;
        }

        public override System.Collections.Generic.List<FileInfo> GetFiles(string Directory) => _impl.GetFiles(Directory);
        public override void Delete(string Name) => _impl.Delete(Name);
        public override byte[] ReadAllBytes(string Name) => _impl.ReadAllBytes(Name);
        public override void WriteAllBytes(string Name, byte[] Content) => _impl.WriteAllBytes(Name, Content);
        public override void Format() => _impl.Format();
    }
}
