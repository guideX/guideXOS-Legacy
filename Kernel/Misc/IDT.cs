using guideXOS;
using guideXOS.Kernel.Drivers;
using guideXOS.Kernel.Helpers;
using guideXOS.Misc;
using Internal.Runtime.CompilerServices;
using System.Runtime;
using System.Runtime.InteropServices;
using static Internal.Runtime.CompilerHelpers.InteropHelpers;

public static class IDT {
    [DllImport("*")]
    private static extern unsafe void set_idt_entries(void* idt);

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    private struct IDTEntry {
        public ushort BaseLow;
        public ushort Selector;
        public byte Reserved0;
        public byte Type_Attributes;
        public ushort BaseMid;
        public uint BaseHigh;
        public uint Reserved1;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct IDTDescriptor {
        public ushort Limit;
        public ulong Base;
    }

    private static IDTEntry[] idt;
    public static IDTDescriptor idtr;


    public static bool Initialized { get; private set; }


    public static unsafe bool Initialize() {
        idt = new IDTEntry[256];

        set_idt_entries(Unsafe.AsPointer(ref idt[0]));

        fixed (IDTEntry* _idt = idt) {
            idtr.Limit = (ushort)((sizeof(IDTEntry) * 256) - 1);
            idtr.Base = (ulong)_idt;
        }

        Native.Load_IDT(ref idtr);

        Initialized = true;
        return true;
    }

    public static void Enable() {
        Native.Sti();
    }

    public static void Disable() {
        Native.Cli();
    }

    public static unsafe void AllowUserSoftwareInterrupt(byte vector) {
        if (!Initialized) return;
        // Set DPL=3 for the given vector gate to allow int from ring3
        fixed (IDTEntry* p = idt) {
            IDTEntry* e = &p[vector];
            // Type 0xEE? preserve type, set DPL bits (bits 5-6) to 3 and Present bit
            e->Type_Attributes = (byte)((e->Type_Attributes & 0x9F) | (3 << 5) | 0x80);
        }
        Native.Load_IDT(ref idtr);
    }

    public struct RegistersStack {
        public ulong rax;
        public ulong rcx;
        public ulong rdx;
        public ulong rbx;
        public ulong rsi;
        public ulong rdi;
        public ulong r8;
        public ulong r9;
        public ulong r10;
        public ulong r11;
        public ulong r12;
        public ulong r13;
        public ulong r14;
        public ulong r15;
    }

    //https://os.phil-opp.com/returning-from-exceptions/
    public struct InterruptReturnStack {
        public ulong rip;
        public ulong cs;
        public ulong rflags;
        public ulong rsp;
        public ulong ss;
    }

    public struct IDTStackGeneric {
        public RegistersStack rs;
        public ulong errorCode;
        public InterruptReturnStack irs;
    }

    [RuntimeExport("intr_handler")]
    public static unsafe void intr_handler(int irq, IDTStackGeneric* stack) {
        if (irq < 0x20) {
            Panic.Error($"CPU{SMP.ThisCPU} KERNEL PANIC!!!", true);

            // Compute correct location of InterruptReturnStack depending on whether the CPU pushed an error code
            InterruptReturnStack* irs;
            bool hasErrorCode = false;
            switch (irq) {
                case 8:
                case 10:
                case 11:
                case 12:
                case 13:
                case 14:
                case 17:
                case 21:
                case 29:
                case 30:
                    // Exceptions that push an error code: irs follows RegistersStack + errorCode
                    irs = (InterruptReturnStack*)(((byte*)stack) + sizeof(RegistersStack) + sizeof(ulong));
                    hasErrorCode = true;
                    break;
                default:
                    // No error code pushed: irs follows only RegistersStack
                    irs = (InterruptReturnStack*)(((byte*)stack) + sizeof(RegistersStack));
                    hasErrorCode = false;
                    break;
            }

            Console.WriteLine("================== EXCEPTION ==================");
            Console.WriteLine($"Vector: 0x{((uint)irq).ToString("x2")}  CPU: {SMP.ThisCPU}");

            // Print using correctly computed frame
            Console.WriteLine($"RIP: 0x{irs->rip.ToString("x2")}  CS: 0x{irs->cs.ToString("x2")}  CPL: {((int)(irs->cs & 3))}");
            Console.WriteLine($"RFLAGS: 0x{irs->rflags.ToString("x2")}  RSP: 0x{irs->rsp.ToString("x2")}  SS: 0x{irs->ss.ToString("x2")} ");
            if (hasErrorCode) Console.WriteLine($"ERROR CODE: 0x{stack->errorCode.ToString("x2")}");

            // Registers
            Console.WriteLine($"RAX={stack->rs.rax.ToString("x2")} RBX={stack->rs.rbx.ToString("x2")} RCX={stack->rs.rcx.ToString("x2")} RDX={stack->rs.rdx.ToString("x2")} ");
            Console.WriteLine($"RSI={stack->rs.rsi.ToString("x2")} RDI={stack->rs.rdi.ToString("x2")} R8 ={stack->rs.r8.ToString("x2")} R9 ={stack->rs.r9.ToString("x2")} ");
            Console.WriteLine($"R10={stack->rs.r10.ToString("x2")} R11={stack->rs.r11.ToString("x2")} R12={stack->rs.r12.ToString("x2")} R13={stack->rs.r13.ToString("x2")} ");
            Console.WriteLine($"R14={stack->rs.r14.ToString("x2")} R15={stack->rs.r15.ToString("x2")} ");

            // Control and tables
            ulong cr2 = Native.ReadCR2();
            Console.WriteLine($"CR2: 0x{cr2.ToString("x2")}  IDTR: base=0x{((ulong)idtr.Base).ToString("x2")} limit=0x{((uint)idtr.Limit).ToString("x2")}  GDTR: base=0x{GDT.gdtr.Base.ToString("x2")} limit=0x{GDT.gdtr.Limit.ToString("x2")} ");

            // Decode exception type
            switch (irq) {
                case 0: Console.WriteLine("DIVIDE BY ZERO"); break;
                case 1: Console.WriteLine("SINGLE STEP"); break;
                case 2: Console.WriteLine("NMI"); break;
                case 3: Console.WriteLine("BREAKPOINT"); break;
                case 4: Console.WriteLine("OVERFLOW"); break;
                case 5: Console.WriteLine("BOUNDS CHECK"); break;
                case 6: Console.WriteLine("INVALID OPCODE"); break;
                case 7: Console.WriteLine("COPR UNAVAILABLE"); break;
                case 8: Console.WriteLine("DOUBLE FAULT"); break;
                case 9: Console.WriteLine("COPR SEGMENT OVERRUN"); break;
                case 10: Console.WriteLine("INVALID TSS"); break;
                case 11: Console.WriteLine("SEGMENT NOT FOUND"); break;
                case 12: Console.WriteLine("STACK EXCEPTION"); break;
                case 13:
                    Console.WriteLine("GENERAL PROTECTION");
                    if (hasErrorCode) Console.WriteLine($"GP ERROR CODE: 0x{stack->errorCode.ToString("x2")} ");
                    break;
                case 14: {
                        if (cr2 < 0x1000) {
                            Console.WriteLine("NULL POINTER");
                        } else {
                            Console.WriteLine("PAGE FAULT");
                        }
                        if (hasErrorCode) {
                            ulong ec = stack->errorCode;
                            // PF EC bits: P(0) W/R(1) U/S(2) RSVD(3) I/D(4) PK(5)
                            Console.WriteLine($"PF EC: P={(ec & 1UL)!=0} WR={((ec>>1)&1UL)!=0} US={((ec>>2)&1UL)!=0} RSVD={((ec>>3)&1UL)!=0} ID={((ec>>4)&1UL)!=0} PK={((ec>>5)&1UL)!=0}");
                        }
                        Console.WriteLine($"Fault VA: 0x{cr2.ToString("x2")} (CPL {((int)(irs->cs & 3))})");
                        break;
                    }
                case 16: Console.WriteLine("COPR ERROR"); break;
                default: Console.WriteLine("UNKNOWN EXCEPTION"); break;
            }
            Console.WriteLine("===============================================");
            Framebuffer.Update();
            for (; ; ) ;
        }

        //DEAD
        if (irq == 0xFD) {
            Native.Cli();
            Native.Hlt();
            for (; ; ) Native.Hlt();
        }

        //For main processor
        if (SMP.ThisCPU == 0) {
            //System calls
            if (irq == 0x80) {
                var pCell = (MethodFixupCell*)stack->rs.rcx;
                string name = string.FromASCII(pCell->Module->ModuleName, StringHelper.StringLength((byte*)pCell->Module->ModuleName));
                stack->rs.rax = (ulong)API.HandleSystemCall(name);
                name.Dispose();
            }
            switch (irq) {
                case 0x20:
                    //misc.asm Schedule_Next
                    if (stack->rs.rdx != 0x61666E6166696E)
                        Timer.OnInterrupt();
                    break;
            }
            Interrupts.HandleInterrupt(irq);
        }

        if (irq == 0x20) {
            ThreadPool.Schedule(stack);
        }

        Interrupts.EndOfInterrupt((byte)irq);
    }
}