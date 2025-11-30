using guideXOS.FS;
using guideXOS.Kernel.Drivers;
using Internal.Runtime.CompilerHelpers;
using System;
using System.Runtime;
using System.Runtime.InteropServices;
namespace guideXOS.Misc {
    internal static unsafe class EntryPoint {
        [RuntimeExport("Entry")]
        public static void Entry(MultibootInfo* Info, IntPtr Modules, IntPtr Trampoline) {
            Allocator.Initialize((IntPtr)0x20000000);

            StartupCodeHelpers.InitializeModules(Modules);

            PageTable.Initialise();

            ASC16.Initialise();

            VBEInfo* info = (VBEInfo*)Info->VBEInfo;
            if (info->PhysBase != 0) {
                Framebuffer.Initialize(info->ScreenWidth, info->ScreenHeight, (uint*)info->PhysBase);
                Framebuffer.Graphics.Clear(0x0);
            } else {
                for (; ; ) Native.Hlt();
            }

            // Boot splash init
            BootSplash.Initialize("Team Nexgen", "guideXOS", "Version: 0.2");

            Console.Setup();
            IDT.Disable();
            GDT.Initialise();

            // Set initial kernel stack for privilege transitions
            {
                const ulong kStackSize = 64 * 1024;
                ulong rsp0 = (ulong)Allocator.Allocate(kStackSize) + kStackSize;
                GDT.SetKernelStack(rsp0);
            }

            IDT.Initialize();

            // Make syscall vector user-accessible via int 0x80
            IDT.AllowUserSoftwareInterrupt(0x80);

            Interrupts.Initialize();
            IDT.Enable();

            SSE.enable_sse();
            //AVX.init_avx();

            ACPI.Initialize();
#if UseAPIC
            PIC.Disable();
            LocalAPIC.Initialize();
            IOAPIC.Initialize();
#else
        PIC.Enable();
#endif
            Timer.Initialize();

            Keyboard.Initialize();

            Serial.Initialise();

            PS2Controller.Initialize();
            VMwareTools.Initialize();

            SMBIOS.Initialise();

            PCI.Initialise();

            IDE.Initialize();
            SATA.Initialize();

            ThreadPool.Initialize();

            Console.WriteLine($"[SMP] Trampoline: 0x{((ulong)Trampoline).ToString("x2")}");
            Native.Movsb((byte*)SMP.Trampoline, (byte*)Trampoline, 512);

            SMP.Initialize((uint)SMP.Trampoline);

            //Only fixed size vhds are supported!
            Console.Write("[Initrd] Initrd: 0x");
            Console.WriteLine((Info->Mods[0]).ToString("x2"));
            //Console.WriteLine("[Initrd] Initializing Ramdisk");
            new Ramdisk((IntPtr)(Info->Mods[0]));
            // Initialize filesystem: Auto-detect FAT (12/16/32) or TAR
            new AutoFS();

            // While we are still here (single core boot), animate splash a bit
            for (int i = 0; i < 120; i++) { // ~2 seconds at 60Hz
                BootSplash.Tick();
            }

            // Cleanup boot splash resources before transitioning to desktop
            BootSplash.Cleanup();

            KMain();
        }

        [DllImport("*")]
        public static extern void KMain();
    }
}