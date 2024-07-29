using static Native;
using System.Runtime.InteropServices;
using guideXOS.Misc;
namespace guideXOS.Kernel.Drivers {
    /// <summary>
    /// AC'97 (Audio Codec '97; also MC'97 for Modem Codec '97) is an audio codec standard developed by Intel Architecture Labs and various codec manufacturers in 1997. The standard was used in motherboards, modems, and sound cards.
    /// </summary>
    public unsafe class AC97 {
        /// <summary>
        /// NAM, NABM
        /// </summary>
        private static uint NAM, NABM;
        /// <summary>
        /// Num Descriptors
        /// </summary>
        public static int NumDescriptors;
        /// <summary>
        /// Buffer Descriptors
        /// </summary>
        static BufferDescriptor* BufferDescriptors;
        /// <summary>
        /// Initialize
        /// </summary>
        public static unsafe void Initialize() {
            var device = PCI.GetDevice(0x8086, 0x2415);
            if (device == null) return;
            //Console.WriteLine("[AC97] Intel 82801AA AC97 Audio Controller Found");
            device.WriteRegister(0x04, 0x04 | 0x02 | 0x01);
            NumDescriptors = 31;
            NAM = device.Bar0 & ~(0xFU);
            NABM = device.Bar1 & ~(0xFU);
            Out8((ushort)(NABM + 0x2C), 0x2);
            Out32((ushort)(NAM + 0x00), 0x6166696E);
            Out16((ushort)(NAM + 0x02), 0x0F0F);
            Out16((ushort)(NAM + 0x018), 0x0F0F);
            BufferDescriptors = (BufferDescriptor*)Allocator.Allocate((ulong)(sizeof(BufferDescriptor) * 32));
            for (int i = 0; i < NumDescriptors; i++) {
                ulong ptr = (ulong)Allocator.Allocate(Audio.SizePerPacket);
                if (ptr > 0xFFFFFFFF) Panic.Error("Invalid buf");
                BufferDescriptors[i].Address = (uint)ptr;
                BufferDescriptors[i].Size = Audio.SizePerPacket / 2;
                BufferDescriptors[i].Arribute = 1 << 15;
            }
            Interrupts.EnableInterrupt(device.IRQ, &OnInterrupt);
            Index = 0;
            Out16((ushort)(NAM + 0x2C), Audio.SampleRate);
            Out16((ushort)(NAM + 0x32), Audio.SampleRate);
            Out8((ushort)(NABM + 0x1B), 0x02);
            Out32((ushort)(NABM + 0x10), (uint)BufferDescriptors);
            Out8((ushort)(NABM + 0x15), Index);
            Out8((ushort)(NABM + 0x1B), 0x19);
            Audio.HasAudioDevice = true;
        }
        /// <summary>
        /// Index
        /// </summary>
        public static byte Index = 0;
        /// <summary>
        /// On Interupt
        /// </summary>
        public static void OnInterrupt() {
            ushort Status = In16((ushort)(NABM + 0x16));
            if ((Status & (1 << 3)) != 0) {
                //Clear last buffer
                int LastIndex = Index;
                Native.Stosb((void*)BufferDescriptors[Index].Address, 0, Audio.SampleRate * 2);
                Index++;
                Index %= (byte)NumDescriptors;
                Audio.require((byte*)BufferDescriptors[Index].Address);
                Out8((ushort)(NABM + 0x15), Index);
            }
            //Ack
            Out16((ushort)(NABM + 0x16), 0x1C);
        }
        /// <summary>
        /// Buffer Descriptor
        /// </summary>
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        struct BufferDescriptor {
            public uint Address;
            public ushort Size;
            public ushort Arribute;
        }
    }
}