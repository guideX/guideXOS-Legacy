using guideXOS.Misc;
using System;
/// <summary>
/// Allocator
/// </summary>
abstract unsafe class Allocator {
    // Allocation tags to attribute page usage
    public enum AllocTag : byte {
        Unknown = 0,
        ThreadMeta = 1,
        ThreadStack = 2,
        ExecImage = 3,
        ExecStack = 4,
        Image = 5,
        GraphicsTemp = 6,
        FileBuffer = 7,
        Other = 8,
        Count = 16
    }
    /// <summary>
    /// Zero Fill
    /// </summary>
    /// <param name="data"></param>
    /// <param name="size"></param>
    internal static unsafe void ZeroFill(IntPtr data, ulong size) {
        Native.Stosb((void*)data, 0, size);
    }
    /// <summary>
    /// Get Page Index Start
    /// </summary>
    /// <param name="ptr"></param>
    /// <returns></returns>
    private static long GetPageIndexStart(IntPtr ptr) {
        ulong p = (ulong)ptr;
        if (p < (ulong)_Info.Start) return -1;
        p -= (ulong)_Info.Start;
        if ((p % PageSize) != 0) return -1;
        p /= PageSize;
        return (long)p;
    }
    /// <summary>
    /// Free
    /// </summary>
    /// <param name="intPtr"></param>
    /// <returns></returns>
    internal static ulong Free(IntPtr intPtr) {
        lock (null) {
            long p = GetPageIndexStart(intPtr);
            if (p == -1) return 0;
            ulong pages = _Info.Pages[p];
            if (pages != 0 && pages != PageSignature) {
                // decrement tag counters
                byte tag = _Info.Tags[p];
                if (tag < (byte)AllocTag.Count) _Info.TagLivePages[tag] -= pages;
                _Info.Tags[p] = 0;

                _Info.PageInUse -= pages;
                Native.Stosb((void*)intPtr, 0, pages * PageSize);
                for (ulong i = 0; i < pages; i++) {
                    _Info.Pages[(ulong)p + i] = 0;
                }
                _Info.Pages[p] = 0;
                return pages * PageSize;
            }
            return 0;
        }
    }
    /// <summary>
    /// Memory In Use
    /// </summary>
    public static ulong MemoryInUse {
        get {
            return _Info.PageInUse * PageSize;
        }
    }
    /// <summary>
    /// Memory Size
    /// </summary>
    public static ulong MemorySize {
        get {
            return (ulong)NumPages * PageSize;
        }
    }
    /// <summary>
    /// Page Signature
    /// </summary>
    public const ulong PageSignature = 0x2E61666E6166696E;
    /// <summary>
    /// Num Pages
    /// </summary>
    public const int NumPages = 131072;
    /// <summary>
    /// Page Size
    /// </summary>
    public const ulong PageSize = 4096;
    /// <summary>
    /// Info
    /// </summary>
    public struct Info {
        /// <summary>
        /// Start
        /// </summary>
        public IntPtr Start;
        /// <summary>
        /// Page In Use
        /// </summary>
        public UInt64 PageInUse;
        /// <summary>
        /// Pages
        /// </summary>
        public fixed ulong Pages[NumPages]; //Max 512MiB
        // Tagging: tag recorded at the start page of an allocation run
        public fixed byte Tags[NumPages];
        // Live pages per tag
        public fixed ulong TagLivePages[(int)AllocTag.Count];
    }
    /// <summary>
    /// Info
    /// </summary>
    public static Info _Info;
    /// <summary>
    /// Initialization
    /// </summary>
    /// <param name="Start"></param>
    public static void Initialize(IntPtr Start) {
        fixed (Info* pInfo = &_Info)
            Native.Stosb(pInfo, 0, (ulong)sizeof(Info));
        _Info.Start = Start;
        _Info.PageInUse = 0;
    }
    /// <summary>
    /// Returns a 4KB aligned address
    /// </summary>
    /// <param name="size"></param>
    /// <returns></returns>
    internal static unsafe IntPtr Allocate(ulong size) { return Allocate(size, AllocTag.Unknown); }

    internal static unsafe IntPtr Allocate(ulong size, AllocTag tag) {
        lock (null) {
            if (size == 0) size = 1; // avoid zero-page requests causing false leak
            // Hard guard against impossible requests
            if (size > MemorySize) {
                string msg2 = "Memory request too large: size=" + size.ToString() + ", total=" + MemorySize.ToString();
                Panic.Error(msg2);
                return IntPtr.Zero;
            }
            ulong pages = 1;
            if (size > PageSize) {
                pages = (size / PageSize) + ((size % PageSize) != 0 ? 1UL : 0);
            }
            ulong i = 0;
            bool found = false;
            for (i = 0; i < (ulong)NumPages; i++) {
                if (_Info.Pages[i] == 0) {
                    found = true;
                    for (ulong k = 0; k < pages; k++) {
                        if (i + k >= (ulong)NumPages || _Info.Pages[i + k] != 0) { // bounds check
                            found = false;
                            break;
                        }
                    }
                    if (found) break;
                } else if (_Info.Pages[i] != PageSignature) {
                    i += _Info.Pages[i];
                }
            }
            if (!found) {
                // Provide more diagnostic info before panic
                string msg = "Memory leak: no free pages (in use=" + (MemoryInUse).ToString() + "/" + (MemorySize).ToString() + ", req=" + (pages * PageSize).ToString() + ")";
                Panic.Error(msg);
                return IntPtr.Zero;
            }
            for (ulong k = 0; k < pages; k++) {
                _Info.Pages[i + k] = PageSignature;
            }
            _Info.Pages[i] = pages;
            _Info.PageInUse += pages;
            // record tag
            byte t = (byte)tag; if (t >= (byte)AllocTag.Count) t = (byte)AllocTag.Unknown;
            _Info.Tags[i] = t;
            _Info.TagLivePages[t] += pages;

            IntPtr ptr = _Info.Start + (i * PageSize);
            return ptr;
        }
    }
    /// <summary>
    /// Reallocate
    /// </summary>
    /// <param name="intPtr"></param>
    /// <param name="size"></param>
    /// <returns></returns>
    public static IntPtr Reallocate(IntPtr intPtr, ulong size) {
        if (intPtr == IntPtr.Zero)
            return Allocate(size);
        if (size == 0) {
            Free(intPtr);
            return IntPtr.Zero;
        }
        long p = GetPageIndexStart(intPtr);
        if (p == -1) return intPtr;
        ulong pages = 1;
        if (size > PageSize) {
            pages = (size / PageSize) + ((size % 4096) != 0 ? 1UL : 0);
        }
        if (_Info.Pages[p] == pages) return intPtr;
        // preserve tag for reallocation
        byte tag = _Info.Tags[p];
        IntPtr newptr = Allocate(size, (AllocTag)tag);
        // Copy only the smaller of old block and requested new size to avoid overruns
        ulong oldBytes = _Info.Pages[p] * PageSize;
        ulong copyLen = size < oldBytes ? size : oldBytes;
        MemoryCopy(newptr, intPtr, copyLen);
        Free(intPtr);
        return newptr;
    }
    /// <summary>
    /// Clear Allocate
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="num"></param>
    /// <returns></returns>
#pragma warning disable CS8500
    public static T* ClearAllocate<T>(int num) where T : struct { return (T*)ClearAllocate(num, sizeof(T)); }
#pragma warning restore CS8500
    /// <summary>
    /// Clear Allocate
    /// </summary>
    /// <param name="num"></param>
    /// <param name="size"></param>
    /// <returns></returns>
    public static IntPtr ClearAllocate(int num, int size) {
        // use 64-bit multiply to avoid overflow
        ulong total = (ulong)num * (ulong)size;
        IntPtr ptr = Allocate(total);
        ZeroFill(ptr, total);
        return ptr;
    }
    /// <summary>
    /// Memory Copy
    /// </summary>
    /// <param name="dst"></param>
    /// <param name="src"></param>
    /// <param name="size"></param>
    internal static unsafe void MemoryCopy(IntPtr dst, IntPtr src, ulong size) { Native.Movsb((void*)dst, (void*)src, size); }

    // Public helpers to query live bytes by tag
    public static ulong GetTagBytes(AllocTag tag) { return _Info.TagLivePages[(int)tag] * PageSize; }
}