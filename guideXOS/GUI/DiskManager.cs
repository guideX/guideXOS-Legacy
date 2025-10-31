using guideXOS.FS;
using guideXOS.Kernel.Drivers;
using System;
using System.Windows.Forms;

namespace guideXOS.GUI {
    internal class DiskManager : Window {
        private string _status = string.Empty;
        private string _detected = "Unknown";
        private bool _clickLock;

        // UI layout
        private const int Pad = 14;
        private const int BtnW = 220;
        private const int BtnH = 28;
        private const int Gap = 10;

        // Button rects (computed in draw)
        private int _bxDetectX, _bxDetectY;
        private int _bxSwitchFatX, _bxSwitchFatY;
        private int _bxSwitchTarX, _bxSwitchTarY;
        private int _bxFormatExfatX, _bxFormatExfatY;
        private int _bxCreatePartX, _bxCreatePartY;
        private int _bxRefreshX, _bxRefreshY;

        // Partition visualization
        private struct Part { public uint Status; public byte Type; public uint LbaStart; public uint LbaCount; }
        private Part[] _parts = new Part[4];
        private ulong _totalSectors; // only available on IDE
        private bool _haveDiskInfo;

        public DiskManager(int x, int y, int w = 700, int h = 340) : base(x, y, w, h) {
            Title = "Disk Manager";
            _status = BuildStatus();
            ReadDiskLayout();
        }

        private string BuildStatus() {
            string driver;
            if (File.Instance == null) driver = "<none>";
            else if (File.Instance is FAT) driver = "FAT";
            else if (File.Instance is FATFS) driver = "FATFS";
            else if (File.Instance is TarFS) driver = "TarFS";
            else driver = "Unknown";
            return $"Driver: {driver}\nDetected media: {_detected}";
        }

        private void ProbeOnce() {
            try {
                var buf = new byte[FileSystem.SectorSize];
                unsafe { fixed (byte* p = buf) Disk.Instance.Read(0, 1, p); }
                if (buf.Length >= 512 && buf[257] == (byte)'u' && buf[258] == (byte)'s' && buf[259] == (byte)'t' && buf[260] == (byte)'a' && buf[261] == (byte)'r') _detected = "TAR (initrd)";
                else if (buf.Length >= 512 && buf[510] == 0x55 && buf[511] == 0xAA) _detected = "FAT (boot sector)";
                else _detected = "Unknown";
            } catch { _detected = "Unknown"; }
            _status = BuildStatus();
        }

        private void ReadDiskLayout() {
            _haveDiskInfo = false; _totalSectors = 0; for (int i = 0; i < 4; i++) _parts[i] = default;
            // Discover disk size for IDE devices
            if (Disk.Instance is IDEDevice ide) {
                _totalSectors = ide.Size / IDEDevice.SectorSize;
                _haveDiskInfo = true;
            }
            // Parse MBR partitions
            try {
                var mbr = new byte[512];
                unsafe { fixed (byte* p = mbr) Disk.Instance.Read(0, 1, p); }
                if (mbr[510] == 0x55 && mbr[511] == 0xAA) {
                    for (int i = 0; i < 4; i++) {
                        int off = 446 + i * 16;
                        Part part = new Part();
                        part.Status = mbr[off + 0];
                        part.Type = mbr[off + 4];
                        part.LbaStart = (uint)(mbr[off + 8] | (mbr[off + 9] << 8) | (mbr[off + 10] << 16) | (mbr[off + 11] << 24));
                        part.LbaCount = (uint)(mbr[off + 12] | (mbr[off + 13] << 8) | (mbr[off + 14] << 16) | (mbr[off + 15] << 24));
                        _parts[i] = part;
                    }
                }
            } catch { }
        }

        public override void OnInput() {
            base.OnInput(); if (!Visible) return;
            int mx = Control.MousePosition.X; int my = Control.MousePosition.Y;
            bool left = Control.MouseButtons.HasFlag(MouseButtons.Left);
            if (left) {
                if (_clickLock) return;
                if (Hit(mx, my, _bxDetectX, _bxDetectY, BtnW, BtnH)) { ProbeOnce(); _clickLock = true; return; }
                if (Hit(mx, my, _bxSwitchFatX, _bxSwitchFatY, BtnW, BtnH)) { try { File.Instance = new FAT(); Desktop.InvalidateDirCache(); _status = BuildStatus(); } catch { _status = "Switch to FAT failed."; } _clickLock = true; return; }
                if (Hit(mx, my, _bxSwitchTarX, _bxSwitchTarY, BtnW, BtnH)) { try { File.Instance = new TarFS(); Desktop.InvalidateDirCache(); _status = BuildStatus(); } catch { _status = "Switch to TAR failed."; } _clickLock = true; return; }
                if (Hit(mx, my, _bxFormatExfatX, _bxFormatExfatY, BtnW, BtnH)) {
                    try {
                        var fs = new FATFS();
                        fs.Format();
                        File.Instance = fs;
                        Desktop.InvalidateDirCache();
                        _detected = "FAT (boot sector)";
                        _status = BuildStatus();
                    } catch { _status = "Format failed."; }
                    _clickLock = true; return;
                }
                if (Hit(mx, my, _bxCreatePartX, _bxCreatePartY, BtnW, BtnH)) { TryCreatePartitionLargestFree(); _clickLock = true; return; }
                if (Hit(mx, my, _bxRefreshX, _bxRefreshY, BtnW, BtnH)) { ReadDiskLayout(); _clickLock = true; return; }
            } else {
                _clickLock = false;
            }
        }

        private static bool Hit(int mx, int my, int x, int y, int w, int h) { return mx >= x && mx <= x + w && my >= y && my <= y + h; }

        public override void OnDraw() {
            base.OnDraw();
            // Background panel
            Framebuffer.Graphics.FillRectangle(X + 1, Y + 1, Width - 2, Height - 2, 0xFF2B2B2B);

            // Cache mouse for this frame
            int mx = Control.MousePosition.X; int my = Control.MousePosition.Y;

            // Status (short, single call)
            WindowManager.font.DrawString(X + Pad, Y + Pad, _status);

            int btnY = Y + Pad + WindowManager.font.FontSize * 2 + 12;
            _bxDetectX = X + Pad; _bxDetectY = btnY;
            DrawButton(mx, my, _bxDetectX, _bxDetectY, BtnW, BtnH, "Detect now"); btnY += BtnH + Gap;

            _bxSwitchFatX = X + Pad; _bxSwitchFatY = btnY;
            DrawButton(mx, my, _bxSwitchFatX, _bxSwitchFatY, BtnW, BtnH, "Use FAT driver (RW)"); btnY += BtnH + Gap;

            _bxSwitchTarX = X + Pad; _bxSwitchTarY = btnY;
            DrawButton(mx, my, _bxSwitchTarX, _bxSwitchTarY, BtnW, BtnH, "Use TAR driver (RO)"); btnY += BtnH + Gap;

            _bxFormatExfatX = X + Pad; _bxFormatExfatY = btnY;
            DrawButton(mx, my, _bxFormatExfatX, _bxFormatExfatY, BtnW, BtnH, "Format RAM as exFAT"); btnY += BtnH + Gap;

            _bxCreatePartX = X + Pad; _bxCreatePartY = btnY;
            DrawButton(mx, my, _bxCreatePartX, _bxCreatePartY, BtnW, BtnH, "Create partition (largest free)"); btnY += BtnH + Gap;

            _bxRefreshX = X + Pad; _bxRefreshY = btnY;
            DrawButton(mx, my, _bxRefreshX, _bxRefreshY, BtnW, BtnH, "Refresh partitions");

            // Draw space bar on the right side
            int barX = X + Pad + BtnW + 40;
            int barY = Y + Pad + 8;
            int barW = Width - (barX - X) - Pad;
            int barH = 30;
            DrawSpaceBar(barX, barY, barW, barH);
            // List partitions below with details
            int listY = barY + barH + 12;
            DrawPartitionList(barX, listY);
        }

        private void DrawButton(int mx, int my, int x, int y, int w, int h, string text) {
            bool hover = Hit(mx, my, x, y, w, h);
            uint bg = hover ? 0xFF3A3A3A : 0xFF323232;
            Framebuffer.Graphics.FillRectangle(x, y, w, h, bg);
            WindowManager.font.DrawString(x + 10, y + (h / 2 - WindowManager.font.FontSize / 2), text);
        }

        private void DrawSpaceBar(int x, int y, int w, int h) {
            // Background for bar
            Framebuffer.Graphics.FillRectangle(x, y, w, h, 0xFF1E1E1E);
            if (!_haveDiskInfo) {
                WindowManager.font.DrawString(x, y + h + 6, "Disk size unavailable (non-IDE)");
                return;
            }
            // Draw used/free segments
            // Compute free/used based on MBR entries
            // First, draw free as grey
            Framebuffer.Graphics.FillRectangle(x + 1, y + 1, w - 2, h - 2, 0xFF2C2C2C);

            ulong total = _totalSectors;
            if (total == 0) return;

            for (int i = 0; i < 4; i++) {
                var p = _parts[i];
                if (p.LbaCount == 0) continue;
                // Clamp to total range
                ulong start = p.LbaStart;
                ulong count = p.LbaCount;
                if (start > total) continue;
                if (start + count > total) count = total - start;
                int segX = x + (int)((start * (ulong)w) / total);
                int segW = (int)((count * (ulong)w) / total);
                if (segW <= 0) segW = 1;
                uint color = 0xFF4C8BF5; // blue for partition
                Framebuffer.Graphics.FillRectangle(segX, y, segW, h, color);
            }

            // Caption
            string cap = $"Total: {FmtSize(total * 512)}";
            WindowManager.font.DrawString(x, y + h + 6, cap);
        }

        private void DrawPartitionList(int x, int y) {
            if (!_haveDiskInfo) return;
            ulong total = _totalSectors;
            for (int i = 0; i < 4; i++) {
                var p = _parts[i];
                string line;
                if (p.LbaCount == 0 || p.Type == 0) {
                    line = $"Slot {i + 1}: <empty>";
                } else {
                    ulong start = p.LbaStart;
                    ulong count = p.LbaCount;
                    ulong end = start + count - 1;
                    double pct = total != 0 ? (double)count * 100.0 / (double)total : 0.0;
                    line = $"Slot {i + 1}: Type=0x{p.Type.ToString("x2")}, Start={start}, Sectors={count} ({pct:0.0}%), Size={FmtSize(count * 512)}";
                }
                WindowManager.font.DrawString(x, y + i * (WindowManager.font.FontSize + 6), line);
            }
        }

        private static string FmtSize(ulong bytes) {
            const ulong KB = 1024; const ulong MB = 1024 * 1024; const ulong GB = 1024 * 1024 * 1024;
            if (bytes >= GB) return ((bytes + (GB / 10)) / GB).ToString() + " GB";
            if (bytes >= MB) return ((bytes + (MB / 10)) / MB).ToString() + " MB";
            if (bytes >= KB) return ((bytes + (KB / 10)) / KB).ToString() + " KB";
            return bytes.ToString() + " B";
        }

        private void TryCreatePartitionLargestFree() {
            if (!_haveDiskInfo) { _status = "Partitioning only supported on IDE disks"; return; }
            // Read current MBR
            var mbr = new byte[512];
            unsafe { fixed (byte* p = mbr) Disk.Instance.Read(0, 1, p); }
            // Build ranges of used
            ulong total = _totalSectors;
            ulong firstUsable = 2048; // align to 1MiB boundary
            if (firstUsable >= total) { _status = "Disk too small"; return; }

            // Collect used ranges from existing partitions
            ulong[] usedStart = new ulong[4];
            ulong[] usedEnd = new ulong[4];
            int usedCount = 0;
            for (int i = 0; i < 4; i++) {
                var p = _parts[i];
                if (p.Type != 0 && p.LbaCount != 0) {
                    ulong s = p.LbaStart; if (s < firstUsable) s = firstUsable;
                    ulong e = p.LbaStart + p.LbaCount; if (e > total) e = total;
                    if (s < e) { usedStart[usedCount] = s; usedEnd[usedCount] = e; usedCount++; }
                }
            }
            // Find a free slot in partition table
            int freeSlot = -1;
            for (int i = 0; i < 4; i++) if (_parts[i].Type == 0 || _parts[i].LbaCount == 0) { freeSlot = i; break; }
            if (freeSlot < 0) { _status = "No free MBR slots"; return; }

            // Find largest free gap between ranges [firstUsable,total)
            ulong bestS = firstUsable; ulong bestE = total; ulong bestLen = 0;
            // Sort used ranges by start (simple bubble sort for small N)
            for (int a = 0; a < usedCount - 1; a++) {
                for (int b = a + 1; b < usedCount; b++) {
                    if (usedStart[b] < usedStart[a]) {
                        ulong ts = usedStart[a]; usedStart[a] = usedStart[b]; usedStart[b] = ts;
                        ulong te = usedEnd[a]; usedEnd[a] = usedEnd[b]; usedEnd[b] = te;
                    }
                }
            }
            ulong cursor = firstUsable;
            for (int i = 0; i < usedCount; i++) {
                if (usedStart[i] > cursor) {
                    ulong gapS = cursor;
                    ulong gapE = usedStart[i];
                    ulong gapLen = gapE - gapS;
                    if (gapLen > bestLen) { bestLen = gapLen; bestS = gapS; bestE = gapE; }
                }
                if (usedEnd[i] > cursor) cursor = usedEnd[i];
            }
            if (cursor < total) {
                ulong gapS = cursor; ulong gapE = total; ulong gapLen = gapE - gapS;
                if (gapLen > bestLen) { bestLen = gapLen; bestS = gapS; bestE = gapE; }
            }
            if (bestLen < 2048) { _status = "No sufficient free space"; return; }

            // Prepare a new partition entry in freeSlot
            int off = 446 + freeSlot * 16;
            // status bootable = 0x00
            mbr[off + 0] = 0x00;
            // CHS start (not used), set to 0xFEFFFF
            mbr[off + 1] = 0xFE; mbr[off + 2] = 0xFF; mbr[off + 3] = 0xFF;
            // Type: 0x07 for exFAT/NTFS
            mbr[off + 4] = 0x07;
            // CHS end
            mbr[off + 5] = 0xFE; mbr[off + 6] = 0xFF; mbr[off + 7] = 0xFF;
            // LBA start and count
            uint start = (uint)bestS;
            uint count = (uint)(bestE - bestS);
            mbr[off + 8] = (byte)(start & 0xFF);
            mbr[off + 9] = (byte)((start >> 8) & 0xFF);
            mbr[off + 10] = (byte)((start >> 16) & 0xFF);
            mbr[off + 11] = (byte)((start >> 24) & 0xFF);
            mbr[off + 12] = (byte)(count & 0xFF);
            mbr[off + 13] = (byte)((count >> 8) & 0xFF);
            mbr[off + 14] = (byte)((count >> 16) & 0xFF);
            mbr[off + 15] = (byte)((count >> 24) & 0xFF);
            // Ensure signature
            mbr[510] = 0x55; mbr[511] = 0xAA;
            // Write MBR back
            try {
                unsafe { fixed (byte* p = mbr) Disk.Instance.Write(0, 1, p); }
                _status = "Partition created. Refresh to view.";
                ReadDiskLayout();
            } catch {
                _status = "Failed to write MBR";
            }
        }
    }
}
