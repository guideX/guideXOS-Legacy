using guideXOS.FS;
using guideXOS.Kernel.Helpers;
using guideXOS.Misc;
using System.Runtime;
using System.Runtime.InteropServices;
namespace guideXOS.Kernel.Libraries {
    /// <summary>
    /// The stdio.h header defines three variable types, several macros, and various functions for performing input and output.
    /// </summary>
#pragma warning disable CS8981
    internal unsafe class stdio {
#pragma warning restore CS8981
        /// <summary>
        /// Put Char
        /// </summary>
        /// <param name="chr"></param>
        [RuntimeExport("_putchar")]
        public static void _putchar(byte chr) {
            if (chr == '\n') {
                Console.WriteLine();
            } else {
                Console.Write((char)chr);
            }
        }
        /// <summary>
        /// File
        /// </summary>
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct FILE {
            /// <summary>
            /// Data
            /// </summary>
            public byte* DATA;
            /// <summary>
            /// Offset
            /// </summary>
            public long OFFSET;
            /// <summary>
            /// Length
            /// </summary>
            public long LENGTH;
            /// <summary>
            /// File name (pointer to allocated ASCII string)
            /// </summary>
            public byte* NAME;
            /// <summary>
            /// Whether the file is open for writing
            /// </summary>
            public byte WRITABLE;
        }
        /// <summary>
        /// Seek
        /// </summary>
        public enum SEEK {
            SET,
            CUR,
            END
        }

        [RuntimeExport("fopen")]
        public static FILE* fopen(byte* name, byte* mode) {
            string sname = string.FromASCII((System.IntPtr)name, StringHelper.StringLength(name));
            string smode = string.FromASCII((System.IntPtr)mode, StringHelper.StringLength(mode));
            FILE file = new FILE();

            bool reading = smode == "r" || smode == "rb" || smode == "r+";
            bool writing = smode == "w" || smode == "wb" || smode == "w+" || smode == "a" || smode == "ab";
            bool appending = smode == "a" || smode == "ab";
            bool readWrite = smode == "r+" || smode == "w+";

            if (writing && !appending && !readWrite) {
                // "w" / "wb": create or truncate — start with empty buffer
                file.DATA = (byte*)Allocator.Allocate(1);
                file.LENGTH = 0;
                file.OFFSET = 0;
            } else if (appending) {
                // "a" / "ab": open existing (if any) and position at end
                if (File.Exists(sname)) {
                    byte[] buffer = File.ReadAllBytes(sname);
                    file.DATA = (byte*)Allocator.Allocate((ulong)buffer.Length);
                    fixed (byte* p = buffer)
                        Native.Movsb(file.DATA, p, (ulong)buffer.Length);
                    file.LENGTH = buffer.Length;
                    file.OFFSET = buffer.Length;
                    buffer.Dispose();
                } else {
                    file.DATA = (byte*)Allocator.Allocate(1);
                    file.LENGTH = 0;
                    file.OFFSET = 0;
                }
            } else {
                // "r" / "rb" / "r+" / "w+": read existing file
                byte[] buffer = File.ReadAllBytes(sname);
                if (buffer == null) {
                    if (reading && !readWrite) {
                        Panic.Error("fopen: file not found!");
                    }
                    // w+ on non-existent: create empty
                    file.DATA = (byte*)Allocator.Allocate(1);
                    file.LENGTH = 0;
                    file.OFFSET = 0;
                } else {
                    file.DATA = (byte*)Allocator.Allocate((ulong)buffer.Length);
                    fixed (byte* p = buffer)
                        Native.Movsb(file.DATA, p, (ulong)buffer.Length);
                    file.LENGTH = buffer.Length;
                    buffer.Dispose();
                }
            }

            // Store the file name so fclose/fflush can write back
            int nameLen = StringHelper.StringLength(name);
            file.NAME = (byte*)Allocator.Allocate((ulong)(nameLen + 1));
            Native.Movsb(file.NAME, name, (ulong)nameLen);
            file.NAME[nameLen] = 0;

            file.WRITABLE = (byte)(writing || readWrite ? 1 : 0);

            smode.Dispose();
            sname.Dispose();

            FILE* result = (FILE*)Allocator.Allocate((ulong)sizeof(FILE));
            *result = file;
            return result;
        }

        [RuntimeExport("fseek")]
        public static void fseek(FILE* handle, long offset, SEEK seek) {
            if (seek == SEEK.SET) {
                handle->OFFSET = offset;
            } else if (seek == SEEK.CUR) {
                handle->OFFSET += offset;
            } else if (seek == SEEK.END) {
                handle->OFFSET = handle->LENGTH + offset;
            } else {
                Panic.Error("Unknown seek");
            }
        }

        /// <summary>
        /// Returns the current file offset
        /// </summary>
        [RuntimeExport("ftell")]
        public static long ftell(FILE* handle) {
            return handle->OFFSET;
        }

        [RuntimeExport("fread")]
        public static void fread(byte* buffer, long elementSize, long elementCount, FILE* handle) {
            Native.Movsb(buffer, handle->DATA + handle->OFFSET, (ulong)elementSize);
        }

        /// <summary>
        /// Writes data from buffer into the file at the current offset
        /// </summary>
        [RuntimeExport("fwrite")]
        public static long fwrite(byte* buffer, long elementSize, long elementCount, FILE* handle) {
            long totalBytes = elementSize * elementCount;
            long requiredLen = handle->OFFSET + totalBytes;

            // Grow the internal buffer if needed
            if (requiredLen > handle->LENGTH) {
                byte* newData = (byte*)Allocator.Allocate((ulong)requiredLen);
                if (handle->LENGTH > 0) {
                    Native.Movsb(newData, handle->DATA, (ulong)handle->LENGTH);
                }
                // Zero-fill any gap between old length and current offset
                if (handle->OFFSET > handle->LENGTH) {
                    Native.Stosb(newData + handle->LENGTH, 0, (ulong)(handle->OFFSET - handle->LENGTH));
                }
                Allocator.Free((System.IntPtr)handle->DATA);
                handle->DATA = newData;
                handle->LENGTH = requiredLen;
            }

            // Copy user data into the buffer at the current offset
            Native.Movsb(handle->DATA + handle->OFFSET, buffer, (ulong)totalBytes);
            handle->OFFSET += totalBytes;

            return elementCount;
        }

        /// <summary>
        /// Flushes the in-memory buffer to disk for writable files
        /// </summary>
        [RuntimeExport("fflush")]
        public static void fflush(FILE* handle) {
            if (handle == null) return;
            if (handle->WRITABLE == 0) return;
            if (handle->NAME == null) return;

            string sname = string.FromASCII((System.IntPtr)handle->NAME, StringHelper.StringLength(handle->NAME));
            byte[] content = new byte[handle->LENGTH];
            fixed (byte* p = content)
                Native.Movsb(p, handle->DATA, (ulong)handle->LENGTH);
            File.WriteAllBytes(sname, content);
            content.Dispose();
            sname.Dispose();
        }

        /// <summary>
        /// Flushes writable files to disk and frees all allocated memory
        /// </summary>
        [RuntimeExport("fclose")]
        public static void fclose(FILE* handle) {
            if (handle == null) return;

            // Flush writable files to disk before closing
            if (handle->WRITABLE != 0) {
                fflush(handle);
            }

            // Free allocated memory
            if (handle->DATA != null) {
                Allocator.Free((System.IntPtr)handle->DATA);
            }
            if (handle->NAME != null) {
                Allocator.Free((System.IntPtr)handle->NAME);
            }
            Allocator.Free((System.IntPtr)handle);
        }
    }
}