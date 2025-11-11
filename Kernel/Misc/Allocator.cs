using guideXOS.Misc;
using System;
using System.Collections.Generic;
/// <summary>
/// Allocator with page-level bookkeeping, tagging, and per-owner (window) accounting.
/// Adds overflow guards to prevent bogus huge allocation requests that arise from
/// corrupted length arithmetic (e.g. num*size overflow) and ensures owner counters
/// are decremented correctly.
/// </summary>
abstract unsafe class Allocator {
    private static readonly object _sync = new object();
    public enum AllocTag : byte { Unknown = 0, ThreadMeta = 1, ThreadStack = 2, ExecImage = 3, ExecStack = 4, Image = 5, GraphicsTemp = 6, FileBuffer = 7, Other = 8, Count = 16 }

    // Current owner context (set by Window construction)
    public static int CurrentOwnerId = 0; // 0 = kernel/unknown
    private static Dictionary<int, ulong> _ownerLivePages; // pages per owner (start pages)

    public const ulong PageSize = 4096;
    public const int NumPages = 131072; // 512 MiB total
    public const ulong PageSignature = 0x2E61666E6166696E;

    public struct Info {
        public IntPtr Start;
        public ulong PageInUse;
        public fixed ulong Pages[NumPages];
        public fixed byte Tags[NumPages];
        public fixed ulong TagLivePages[(int)AllocTag.Count];
        public fixed int Owners[NumPages]; // owner id at run start page
    }
    public static Info _Info;

    public static void Initialize(IntPtr start) {
        fixed (Info* pInfo = &_Info) Native.Stosb(pInfo, 0, (ulong)sizeof(Info));
        _Info.Start = start; _Info.PageInUse = 0; _ownerLivePages = new Dictionary<int, ulong>();
    }

    public static ulong MemoryInUse => _Info.PageInUse * PageSize;
    public static ulong MemorySize => (ulong)NumPages * PageSize;

    private static long GetPageIndexStart(IntPtr ptr) {
        ulong p = (ulong)ptr; if (p < (ulong)_Info.Start) return -1; p -= (ulong)_Info.Start; if ((p % PageSize) != 0) return -1; return (long)(p / PageSize);
    }

    internal static unsafe void ZeroFill(IntPtr data, ulong size) { Native.Stosb((void*)data, 0, size); }

    internal static ulong Free(IntPtr intPtr) {
        lock (_sync) {
            long p = GetPageIndexStart(intPtr); if (p == -1) return 0;
            ulong pages = _Info.Pages[p];
            if (pages != 0 && pages != PageSignature) {
                // Tag accounting
                byte tag = _Info.Tags[p]; if (tag < (byte)AllocTag.Count) _Info.TagLivePages[tag] -= pages; _Info.Tags[p] = 0;
                // Owner accounting (do BEFORE clearing pages)
                int owner = _Info.Owners[p];
                if (owner != 0 && _ownerLivePages != null && _ownerLivePages.ContainsKey(owner)) {
                    ulong live = _ownerLivePages[owner]; _ownerLivePages[owner] = live > pages ? live - pages : 0UL;
                }
                _Info.Owners[p] = 0;
                // Global usage
                _Info.PageInUse -= pages;
                Native.Stosb((void*)intPtr, 0, pages * PageSize);
                for (ulong i = 0; i < pages; i++) _Info.Pages[(ulong)p + i] = 0;
                return pages * PageSize;
            }
            return 0;
        }
    }

    internal static unsafe IntPtr Allocate(ulong size) => Allocate(size, AllocTag.Unknown);

    private static bool SuspiciousSize(ulong size) {
        // Treat sizes far beyond physical memory as suspicious overflow/corruption.
        return size > MemorySize && size > (MemorySize * 4);
    }

    internal static unsafe IntPtr Allocate(ulong size, AllocTag tag) {
        lock (_sync) {
            if (size == 0) size = 1;
            // Overflow / corruption guard: reject absurd sizes silently (return null) instead of panicking
            if (SuspiciousSize(size)) return IntPtr.Zero;
            if (size > MemorySize) { Panic.Error("Memory request too large: size=" + size.ToString() + ", total=" + MemorySize.ToString()); return IntPtr.Zero; }
            ulong pages = size > PageSize ? (size / PageSize) + ((size % PageSize) != 0 ? 1UL : 0) : 1UL;
            ulong i; bool found = false;
            for (i = 0; i < (ulong)NumPages; i++) {
                if (_Info.Pages[i] == 0) {
                    found = true;
                    for (ulong k = 0; k < pages; k++) {
                        if (i + k >= (ulong)NumPages || _Info.Pages[i + k] != 0) { found = false; break; }
                    }
                    if (found) break;
                } else if (_Info.Pages[i] != PageSignature) {
                    ulong runPages = _Info.Pages[i]; if (runPages == 0 || runPages == PageSignature) continue; i += runPages - 1;
                }
            }
            if (!found) { Panic.Error("Out of memory: no free pages (in use=" + MemoryInUse.ToString() + "/" + MemorySize.ToString() + ", req=" + (pages * PageSize).ToString() + ")"); return IntPtr.Zero; }
            for (ulong k = 0; k < pages; k++) _Info.Pages[i + k] = PageSignature;
            _Info.Pages[i] = pages; _Info.PageInUse += pages;
            byte t = (byte)tag; if (t >= (byte)AllocTag.Count) t = (byte)AllocTag.Unknown; _Info.Tags[i] = t; _Info.TagLivePages[t] += pages;
            int owner = CurrentOwnerId; _Info.Owners[i] = owner;
            if (owner != 0) { if (_ownerLivePages.ContainsKey(owner)) _ownerLivePages[owner] += pages; else _ownerLivePages.Add(owner, pages); }
            long baseAddr = (long)_Info.Start; long offset = (long)(i * PageSize); return new IntPtr((void*)(baseAddr + offset));
        }
    }

    // Note: method name Reallocate (camel-case) is expected by other code (stdlib, API)
    public static IntPtr Reallocate(IntPtr intPtr, ulong size) {
        if (intPtr == IntPtr.Zero) return Allocate(size);
        if (size == 0) { Free(intPtr); return IntPtr.Zero; }
        long p = GetPageIndexStart(intPtr); if (p == -1) return intPtr;
        ulong pages = size > PageSize ? (size / PageSize) + ((size % PageSize) != 0 ? 1UL : 0) : 1UL;
        if (_Info.Pages[p] == pages) return intPtr;
        byte tag = _Info.Tags[p]; IntPtr newptr = Allocate(size, (AllocTag)tag);
        if (newptr == IntPtr.Zero) return intPtr; // allocation failed; keep old block
        ulong oldBytes = _Info.Pages[p] * PageSize; ulong copyLen = size < oldBytes ? size : oldBytes;
        MemoryCopy(newptr, intPtr, copyLen); Free(intPtr); return newptr;
    }

#pragma warning disable CS8500
    public static T* ClearAllocate<T>(int num) where T : struct { return (T*)ClearAllocate(num, sizeof(T)); }
#pragma warning restore CS8500

    public static IntPtr ClearAllocate(int num, int size) {
        if (num < 0 || size < 0) return IntPtr.Zero;
        ulong unum = (ulong)num; ulong usize = (ulong)size;
        if (unum != 0 && usize > MemorySize / unum) return IntPtr.Zero; // multiplication overflow/too large
        ulong total = unum * usize; IntPtr ptr = Allocate(total); if (ptr == IntPtr.Zero) return IntPtr.Zero; ZeroFill(ptr, total); return ptr;
    }

    internal static unsafe void MemoryCopy(IntPtr dst, IntPtr src, ulong size) { Native.Movsb((void*)dst, (void*)src, size); }
    public static ulong GetTagBytes(AllocTag tag) { return _Info.TagLivePages[(int)tag] * PageSize; }
    public static ulong GetOwnerBytes(int ownerId) {
        if (ownerId == 0) return 0UL;
        lock (_sync) {
            if (_ownerLivePages != null && _ownerLivePages.ContainsKey(ownerId)) return _ownerLivePages[ownerId] * PageSize;
            // Fallback: scan page run starts and accumulate pages owned by ownerId
            ulong pages = 0UL;
            for (int i = 0; i < NumPages; i++) {
                ulong run = _Info.Pages[i];
                if (run != 0 && run != PageSignature) {
                    // This is a run start - check if owned by ownerId
                    if (_Info.Owners[i] == ownerId) pages += run;
                    // skip ahead by run-1 (loop will increment i++)
                    i += (int)(run - 1);
                }
            }
            return pages * PageSize;
        }
    }

    // Snapshot structure for owner accounting (avoids depending on generic KeyValuePair in low-level kernel code)
    public struct OwnerSnapshot { public int OwnerId; public ulong Bytes; }

    // Return a snapshot of current owner assignments and bytes. Used by diagnostic UI to find leaks.
    public static OwnerSnapshot[] GetOwnerListSnapshot() {
        lock (_sync) {
            if (_ownerLivePages == null) return new OwnerSnapshot[0];
            var arr = new OwnerSnapshot[_ownerLivePages.Count];
            int idx = 0;
            // iterate using explicit indexing via Keys to avoid relying on foreach semantics of dictionary implementation
            var keys = _ownerLivePages.Keys;
            for (int k = 0; k < keys.Count; k++) {
                int owner = keys[k]; ulong pages = _ownerLivePages[owner];
                arr[idx++].OwnerId = owner;
                arr[idx - 1].Bytes = pages * PageSize;
            }
            return arr;
        }
    }
}