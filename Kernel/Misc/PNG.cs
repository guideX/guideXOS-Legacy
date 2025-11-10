using System.Drawing;
using System.Runtime.InteropServices;
using guideXOS.Kernel.Drivers;

namespace guideXOS.Misc {
    public unsafe class PNG : Image {
        public enum LodePNGColorType {
            LCT_GREY = 0,
            LCT_RGB = 2,
            LCT_PALETTE = 3,
            LCT_GREY_ALPHA = 4,
            LCT_RGBA = 6
        }

        private static void BuildFallback(string reason, out int[] data, out int w, out int h) {
            w = 8; h = 8; data = new int[w * h];
            // simple checker pattern (magenta/black) to indicate failure
            for (int y = 0; y < h; y++) {
                for (int x = 0; x < w; x++) {
                    bool c = ((x ^ y) & 1) == 0;
                    data[y * w + x] = c ? unchecked((int)0xFFFF00FF) : unchecked((int)0xFF000000);
                }
            }
            Console.WriteLine("[PNG] Fallback image used: " + reason);
        }

        public PNG(byte[] file, LodePNGColorType type = LodePNGColorType.LCT_RGBA, uint bitDepth = 8) {
            fixed (byte* p = file) {
                uint* decoded;
                uint w, h;
                uint err = lodepng_decode_memory(out decoded, out w, out h, p, file.Length, type, bitDepth);

                if (err != 0 || decoded == null) {
                    // Known lodepng error codes: 83 alloc fail, 85 too many pixels
                    BuildFallback("decode error code=" + err.ToString(), out RawData, out int fw, out int fh);
                    Width = fw; Height = fh; Bpp = 4; return;
                }

                // Guard against absurd dimensions / overflow
                if (w == 0 || h == 0 || w > 8192 || h > 8192) { // clamp maximum size
                    Allocator.Free((System.IntPtr)decoded);
                    BuildFallback("invalid dimensions w=" + w + " h=" + h, out RawData, out int fw, out int fh);
                    Width = fw; Height = fh; Bpp = 4; return;
                }
                ulong pixelCount = (ulong)w * (ulong)h;
                if (pixelCount > (ulong)int.MaxValue || pixelCount * 4UL > Allocator.MemorySize / 2) { // refuse > half memory
                    Allocator.Free((System.IntPtr)decoded);
                    BuildFallback("excessive pixel count=" + pixelCount.ToString(), out RawData, out int fw, out int fh);
                    Width = fw; Height = fh; Bpp = 4; return;
                }

                RawData = new int[pixelCount];
                for (uint y2 = 0; y2 < h; y2++) {
                    uint rowOff = y2 * w;
                    for (uint x2 = 0; x2 < w; x2++) {
                        uint px = decoded[rowOff + x2];
                        // convert RGBA -> expected order (keeping alpha, swapping RGB if needed)
                        RawData[(int)(rowOff + x2)] = (int)((px & 0xFF000000) | (NETv4.SwapLeftRight(px & 0x00FFFFFF)) >> 8);
                    }
                }
                Allocator.Free((System.IntPtr)decoded);
                Width = (int)w; Height = (int)h; Bpp = 4;
            }
        }

        [DllImport("*")]
        public static extern uint lodepng_decode_memory(out uint* _out, out uint w, out uint h, byte* _in, int insize, LodePNGColorType colortype, uint bitdepth);
    }
}