using System;

namespace guideXOS.Misc {
    // Minimal loader for GXM (formerly MUE) single-image executables.
    // Layout: [0..3] 'G','X','M','\0' (or legacy 'M','U','E','\0')
    //         [4..7]  version (u32)
    //         [8..11] entry RVA (u32)
    //         [12..15] image size (u32)
    //         [16..]  raw image
    public static unsafe class GXMLoader {
        public static bool TryExecute(byte[] image, out string error) {
            error = null; if (image == null || image.Length < 16) { error = "Executable too small"; return false; }
            byte b0 = image[0], b1 = image[1], b2 = image[2], b3 = image[3];
            bool sigGXM = (b0=='G' && b1=='X' && b2=='M' && b3==0);
            bool sigMUE = (b0=='M' && b1=='U' && b2=='E' && b3==0);
            if (!sigGXM && !sigMUE) { error = "Bad signature"; return false; }
            uint ver = ReadU32(image, 4);
            uint entryRva = ReadU32(image, 8);
            uint size = ReadU32(image, 12);
            if (size > (uint)image.Length) size = (uint)image.Length; if (entryRva >= size || size < 16) { error = "Invalid header"; return false; }
            ulong allocSize = AlignUp(size, 4096);
            byte* basePtr = (byte*)Allocator.Allocate(allocSize); if (basePtr == null) { error = "OOM"; return false; }
            fixed (byte* src = image) Native.Movsb(basePtr, src, size);
            PageTable.MapUser((ulong)basePtr, (ulong)basePtr);
            for (ulong off = 4096; off < allocSize; off += 4096) PageTable.MapUser((ulong)basePtr + off, (ulong)basePtr + off);
            const ulong StackSize = 64 * 1024; byte* stack = (byte*)Allocator.Allocate(StackSize); if (stack == null) { error = "OOM stack"; return false; }
            PageTable.MapUser((ulong)stack, (ulong)stack);
            for (ulong off = 4096; off < StackSize; off += 4096) PageTable.MapUser((ulong)stack + off, (ulong)stack + off);
            ulong rsp = (ulong)stack + StackSize - 16; ulong rip = (ulong)basePtr + entryRva;
            SchedulerExtensions.EnterUserMode(rip, rsp);
            return true;
        }
        private static uint ReadU32(byte[] b, int off){ return (uint)(b[off] | (b[off+1]<<8) | (b[off+2]<<16) | (b[off+3]<<24)); }
        private static ulong AlignUp(uint v, uint a){ uint r = (v + a - 1) & ~(a - 1); return r; }
    }
}
