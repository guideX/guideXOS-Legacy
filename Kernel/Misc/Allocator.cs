using guideXOS.Misc;
using System;
/// <summary>
/// Allocator
/// </summary>
abstract unsafe class Allocator {
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
            return NumPages * PageSize;
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
    internal static unsafe IntPtr Allocate(ulong size) {
        lock (null) {
            ulong pages = 1;
            if (size > PageSize) {
                pages = (size / PageSize) + ((size % 4096) != 0 ? 1UL : 0);
            }
            ulong i = 0;
            bool found = false;
            for (i = 0; i < NumPages; i++) {
                if (_Info.Pages[i] == 0) {
                    found = true;
                    for (ulong k = 0; k < pages; k++) {
                        if (_Info.Pages[i + k] != 0) {
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
                Panic.Error("Memory leak");
                return IntPtr.Zero;
            }
            for (ulong k = 0; k < pages; k++) {
                _Info.Pages[i + k] = PageSignature;
            }
            _Info.Pages[i] = pages;
            _Info.PageInUse += pages;
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
        IntPtr newptr = Allocate(size);
        MemoryCopy(newptr, intPtr, size);
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
    public static T* ClearAllocate<T>(int num) where T : struct {
        return (T*)ClearAllocate(num, sizeof(T));
    }
    #pragma warning restore CS8500
    /// <summary>
    /// Clear Allocate
    /// </summary>
    /// <param name="num"></param>
    /// <param name="size"></param>
    /// <returns></returns>
    public static IntPtr ClearAllocate(int num, int size) {
        IntPtr ptr = Allocate((ulong)(num * size));
        ZeroFill(ptr, (ulong)(num * size));
        return ptr;
    }
    /// <summary>
    /// Memory Copy
    /// </summary>
    /// <param name="dst"></param>
    /// <param name="src"></param>
    /// <param name="size"></param>
    internal static unsafe void MemoryCopy(IntPtr dst, IntPtr src, ulong size) {
        Native.Movsb((void*)dst, (void*)src, size);
    }
}