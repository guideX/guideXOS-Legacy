using guideXOS.Misc;
using System;

namespace guideXOS {
    public static unsafe class PageTable {
        public enum PageSize {
            Typical = 4096,
            //Huge = 0x200000
        }

        /// <summary>
        /// Page flags for x86-64 page table entries
        /// </summary>
        [Flags]
        public enum PageFlags : ulong {
            None = 0,
            Present = 0b001,        // Page is present in memory
            ReadWrite = 0b010,      // Writable (vs read-only)
            User = 0b100,           // User-accessible (vs supervisor-only)
            WriteThrough = 0b1000,
            CacheDisable = 0b10000,
            Accessed = 0b100000,
            Dirty = 0b1000000,
            Global = 0b100000000,
            NoExecute = 1UL << 63
        }

        public static ulong* PML4;

        // Constants for better readability
        private const ulong MaxPhysicalAddress = 0x000F_FFFF_FFFF_F000UL; // 256 TB limit
        private const ulong AddressMask = 0x000F_FFFF_FFFF_F000UL;
        private const int PML4_SHIFT = 39;
        private const int PML3_SHIFT = 30;
        private const int PML2_SHIFT = 21;
        private const int PML1_SHIFT = 12;
        private const ulong TABLE_ENTRY_MASK = 0x1ff;

        // Thread safety
        private static readonly object _pageLock = new object();

        // Statistics tracking
        private static ulong _pageTableAllocations = 0;
        private static ulong _mappingCount = 0;
        private static ulong _unmappingCount = 0;

        public static ulong PageTableAllocations => _pageTableAllocations;
        public static ulong MappingCount => _mappingCount;
        public static ulong UnmappingCount => _unmappingCount;

        internal static void Initialise() {
            PML4 = (ulong*)SMP.SharedPageTable;

            Native.Stosb(PML4, 0, 0x1000);

            ulong i = 0;
            //Map the first 4KiB-4GiB
            //Reserve 4KiB for null reference exception
            for (i = (ulong)PageSize.Typical; i < 1024 * 1024 * 1024 * 4UL; i += (ulong)PageSize.Typical) {
                Map(i, i, PageSize.Typical);
            }

            Native.WriteCR3((ulong)PML4);
        }

        // Original API: kernel supervisor mappings
        public static ulong* GetPage(ulong VirtualAddress, PageSize pageSize = PageSize.Typical) {
            return GetPageInternal(PML4, VirtualAddress, user: false, pageSize);
        }

        // New: user-accessible page path
        public static ulong* GetPageUser(ulong VirtualAddress, PageSize pageSize = PageSize.Typical) {
            return GetPageInternal(PML4, VirtualAddress, user: true, pageSize);
        }

        // Root-aware variants
        public static ulong* GetPageOnRoot(ulong* rootPml4, ulong VirtualAddress, bool user, PageSize pageSize = PageSize.Typical) {
            return GetPageInternal(rootPml4, VirtualAddress, user, pageSize);
        }

        private static ulong* GetPageInternal(ulong* rootPml4, ulong VirtualAddress, bool user, PageSize pageSize) {
            if ((VirtualAddress % (ulong)PageSize.Typical) != 0) Panic.Error("Invalid address - not page aligned");

            ulong pml4_entry = (VirtualAddress >> PML4_SHIFT) & TABLE_ENTRY_MASK;
            ulong pml3_entry = (VirtualAddress >> PML3_SHIFT) & TABLE_ENTRY_MASK;
            ulong pml2_entry = (VirtualAddress >> PML2_SHIFT) & TABLE_ENTRY_MASK;
            ulong pml1_entry = (VirtualAddress >> PML1_SHIFT) & TABLE_ENTRY_MASK;

            ulong* pml3 = Next(rootPml4, pml4_entry, user);
            ulong* pml2 = Next(pml3, pml3_entry, user);

            /*
            if (pageSize == PageSize.Huge)
            {
                return &pml2[pml2_entry];
            }
            else 
            */
            if (pageSize == PageSize.Typical) {
                ulong* pml1 = Next(pml2, pml2_entry, user);
                return &pml1[pml1_entry];
            }
            return null;
        }

        /// <summary>
        /// Get page table entry without allocating intermediate tables (read-only)
        /// </summary>
        private static ulong* GetPageInternalReadOnly(ulong* rootPml4, ulong VirtualAddress) {
            if ((VirtualAddress % (ulong)PageSize.Typical) != 0) return null;

            ulong pml4_entry = (VirtualAddress >> PML4_SHIFT) & TABLE_ENTRY_MASK;
            if ((rootPml4[pml4_entry] & 0x01) == 0) return null;
            
            ulong* pml3 = (ulong*)(rootPml4[pml4_entry] & AddressMask);
            ulong pml3_entry = (VirtualAddress >> PML3_SHIFT) & TABLE_ENTRY_MASK;
            if ((pml3[pml3_entry] & 0x01) == 0) return null;
            
            ulong* pml2 = (ulong*)(pml3[pml3_entry] & AddressMask);
            ulong pml2_entry = (VirtualAddress >> PML2_SHIFT) & TABLE_ENTRY_MASK;
            if ((pml2[pml2_entry] & 0x01) == 0) return null;
            
            ulong* pml1 = (ulong*)(pml2[pml2_entry] & AddressMask);
            ulong pml1_entry = (VirtualAddress >> PML1_SHIFT) & TABLE_ENTRY_MASK;
            
            return &pml1[pml1_entry];
        }

        /// <summary>
        /// Map Physical Address At Virtual Address Specified (kernel root)
        /// </summary>
        public static void Map(ulong VirtualAddress, ulong PhysicalAddress, PageSize pageSize = PageSize.Typical) {
            lock (_pageLock) {
                MapOnRoot(PML4, VirtualAddress, PhysicalAddress, user: false, pageSize);
            }
        }

        public static void MapUser(ulong VirtualAddress, ulong PhysicalAddress, PageSize pageSize = PageSize.Typical) {
            lock (_pageLock) {
                MapOnRoot(PML4, VirtualAddress, PhysicalAddress, user: true, pageSize);
            }
        }

        // Root-aware mapping
        public static void MapOnRoot(ulong* rootPml4, ulong VirtualAddress, ulong PhysicalAddress, bool user, PageSize pageSize = PageSize.Typical) {
            // CRITICAL FIX: Validate physical address bounds
            if (PhysicalAddress > MaxPhysicalAddress) {
                Panic.Error("Physical address too large: 0x" + PhysicalAddress.ToString("X"));
            }
            
            // CRITICAL FIX: Validate physical address alignment
            if ((PhysicalAddress & 0xFFF) != 0) {
                Panic.Error("Physical address not page-aligned: 0x" + PhysicalAddress.ToString("X"));
            }

            /*
            if (pageSize == PageSize.Huge)
            {
                *GetPage(VirtualAddress, pageSize) = PhysicalAddress | 0b10000011;
            }
            else 
            */
            if (pageSize == PageSize.Typical) {
                ulong* pte = GetPageInternal(rootPml4, VirtualAddress, user, pageSize);
                // present | rw | user(optional)
                ulong flags = (ulong)PageFlags.Present | (ulong)PageFlags.ReadWrite | (user ? (ulong)PageFlags.User : 0);
                *pte = (PhysicalAddress & AddressMask) | flags;
                
                _mappingCount++;
            }

            // CRITICAL FIX: Use virtual address for TLB invalidation (was using physical address)
            Native.Invlpg(VirtualAddress);
        }

        /// <summary>
        /// Unmap a virtual address (kernel root) - HIGH PRIORITY FIX
        /// </summary>
        public static void Unmap(ulong VirtualAddress, PageSize pageSize = PageSize.Typical) {
            lock (_pageLock) {
                UnmapOnRoot(PML4, VirtualAddress, pageSize);
            }
        }

        /// <summary>
        /// Unmap a virtual address on specific page table root - HIGH PRIORITY FIX
        /// </summary>
        public static void UnmapOnRoot(ulong* rootPml4, ulong VirtualAddress, PageSize pageSize = PageSize.Typical) {
            if ((VirtualAddress % (ulong)PageSize.Typical) != 0) {
                Panic.Error("Unmap: Virtual address not page-aligned");
            }
            
            ulong* pte = GetPageInternalReadOnly(rootPml4, VirtualAddress);
            if (pte != null && (*pte & 0x01) != 0) {
                *pte = 0; // Clear present bit and entire entry
                Native.Invlpg(VirtualAddress); // Flush TLB for this page
                _unmappingCount++;
            }
        }

        /// <summary>
        /// Check if a virtual address is mapped - HIGH PRIORITY FIX
        /// </summary>
        public static bool IsMapped(ulong VirtualAddress) {
            lock (_pageLock) {
                return IsMappedOnRoot(PML4, VirtualAddress);
            }
        }

        public static bool IsMappedOnRoot(ulong* rootPml4, ulong VirtualAddress) {
            if ((VirtualAddress % (ulong)PageSize.Typical) != 0) return false;
            
            ulong* pte = GetPageInternalReadOnly(rootPml4, VirtualAddress);
            return pte != null && (*pte & 0x01) != 0;
        }

        /// <summary>
        /// Map a contiguous range of virtual to physical memory - HIGH PRIORITY FIX
        /// </summary>
        public static void MapRange(ulong VirtualStart, ulong PhysicalStart, ulong sizeBytes, bool user = false) {
            if ((VirtualStart & 0xFFF) != 0 || (PhysicalStart & 0xFFF) != 0) {
                Panic.Error("MapRange: Addresses must be page-aligned");
            }
            
            lock (_pageLock) {
                ulong pages = (sizeBytes + (ulong)PageSize.Typical - 1) / (ulong)PageSize.Typical;
                
                for (ulong i = 0; i < pages; i++) {
                    ulong virt = VirtualStart + (i * (ulong)PageSize.Typical);
                    ulong phys = PhysicalStart + (i * (ulong)PageSize.Typical);
                    
                    MapOnRoot(PML4, virt, phys, user, PageSize.Typical);
                }
            }
        }

        /// <summary>
        /// Get physical address for a virtual address (debugging/diagnostics)
        /// </summary>
        public static ulong VirtualToPhysical(ulong VirtualAddress) {
            lock (_pageLock) {
                ulong* pte = GetPageInternalReadOnly(PML4, VirtualAddress);
                if (pte == null || (*pte & 0x01) == 0) return 0;
                
                ulong physBase = *pte & AddressMask;
                ulong offset = VirtualAddress & 0xFFF;
                return physBase + offset;
            }
        }

        public static ulong* Next(ulong* Directory, ulong Entry) {
            return Next(Directory, Entry, user: false);
        }

        private static ulong* Next(ulong* Directory, ulong Entry, bool user) {
            ulong* p = null;

            if (((Directory[Entry]) & 0x01) != 0) {
                p = (ulong*)(Directory[Entry] & AddressMask);
                if (user) Directory[Entry] |= (ulong)PageFlags.User; // Ensure user bit if requested
            } else {
                // HIGH PRIORITY FIX: Track page table allocations with special owner ID
                int prevOwner = Allocator.CurrentOwnerId;
                Allocator.CurrentOwnerId = -1; // Special ID for page tables
                p = (ulong*)Allocator.Allocate(0x1000, Allocator.AllocTag.Other);
                Allocator.CurrentOwnerId = prevOwner;
                
                if (p == null) {
                    Panic.Error("Failed to allocate page table");
                }
                
                Native.Stosb(p, 0, 0x1000);

                // present | rw | user(optional)
                Directory[Entry] = (((ulong)p) & AddressMask) | (ulong)PageFlags.Present | (ulong)PageFlags.ReadWrite | (user ? (ulong)PageFlags.User : 0);
                
                _pageTableAllocations++;
            }

            return p;
        }
    }
}