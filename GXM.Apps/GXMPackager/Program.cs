using System;
using System.IO;
using System.Text;

namespace GXMPackager
{
    /// <summary>
    /// Core GXM packaging logic
    /// </summary>
    public static class GXMCore
    {
        /// <summary>
        /// Create a GXM file from binary data
        /// </summary>
        public static byte[] CreateGXM(byte[] binaryData, uint entryRva, uint version, byte[]? scriptData)
        {
            uint imageSize = (uint)binaryData.Length;
            int totalSize = 16 + binaryData.Length;

            // Add space for GUI script if present
            if (scriptData != null)
            {
                totalSize = 16 + 4 + scriptData.Length + 1 + binaryData.Length; // +4 for "GUI\0", +1 for null terminator after script
            }

            byte[] gxm = new byte[totalSize];
            int offset = 0;

            // Magic: 'G', 'X', 'M', '\0'
            gxm[offset++] = (byte)'G';
            gxm[offset++] = (byte)'X';
            gxm[offset++] = (byte)'M';
            gxm[offset++] = 0;

            // Version (u32, little-endian)
            WriteU32(gxm, offset, version);
            offset += 4;

            // Entry RVA (u32, little-endian)
            WriteU32(gxm, offset, entryRva);
            offset += 4;

            // Image size (u32, little-endian)
            uint declaredSize = scriptData != null 
                ? (uint)(4 + scriptData.Length + 1 + binaryData.Length) // +1 for null terminator after script
                : imageSize;
            WriteU32(gxm, offset, declaredSize);
            offset += 4;

            // Add GUI script if present
            if (scriptData != null)
            {
                gxm[offset++] = (byte)'G';
                gxm[offset++] = (byte)'U';
                gxm[offset++] = (byte)'I';
                gxm[offset++] = 0;

                Array.Copy(scriptData, 0, gxm, offset, scriptData.Length);
                offset += scriptData.Length;
                
                // CRITICAL FIX: Add null terminator after script to prevent infinite loop in GXMLoader
                gxm[offset++] = 0;
            }

            // Binary image data
            Array.Copy(binaryData, 0, gxm, offset, binaryData.Length);

            return gxm;
        }

        /// <summary>
        /// Package a GXM file from files
        /// </summary>
        public static void PackageGXM(string inputFile, string outputFile, uint entryRva, uint version, string? scriptFile, Action<string>? logger = null)
        {
            if (!File.Exists(inputFile))
            {
                throw new FileNotFoundException($"Input file not found: {inputFile}");
            }

            byte[] inputData = File.ReadAllBytes(inputFile);
            byte[]? scriptData = null;

            if (scriptFile != null)
            {
                if (!File.Exists(scriptFile))
                {
                    logger?.Invoke($"Warning: Script file not found: {scriptFile}");
                }
                else
                {
                    scriptData = File.ReadAllBytes(scriptFile);
                    logger?.Invoke($"Including GUI script from: {scriptFile}");
                }
            }

            byte[] gxmData = CreateGXM(inputData, entryRva, version, scriptData);
            File.WriteAllBytes(outputFile, gxmData);

            logger?.Invoke($"Successfully packaged:");
            logger?.Invoke($"  Input:  {inputFile} ({inputData.Length} bytes)");
            logger?.Invoke($"  Output: {outputFile} ({gxmData.Length} bytes)");
            logger?.Invoke($"  Entry:  0x{entryRva:X8}");
            logger?.Invoke($"  Version: {version}");
            if (scriptData != null)
            {
                logger?.Invoke($"  Script: {scriptData.Length} bytes");
            }
        }

        private static void WriteU32(byte[] buffer, int offset, uint value)
        {
            buffer[offset + 0] = (byte)(value & 0xFF);
            buffer[offset + 1] = (byte)((value >> 8) & 0xFF);
            buffer[offset + 2] = (byte)((value >> 16) & 0xFF);
            buffer[offset + 3] = (byte)((value >> 24) & 0xFF);
        }
    }

    /// <summary>
    /// GXM Packager - Creates GXM executable files with proper header
    /// 
    /// GXM Format:
    /// [0..3]   Magic: 'G', 'X', 'M', '\0'
    /// [4..7]   Version (u32, little-endian)
    /// [8..11]  Entry RVA (u32, little-endian) - offset to entry point
    /// [12..15] Image size (u32, little-endian) - total size of binary
    /// [16..]   Raw binary image data
    /// 
    /// Optional GUI Script Format (at offset 16):
    /// [16..19] 'G', 'U', 'I', '\0' - GUI script marker
    /// [20..]   UTF-8 script data (lines separated by \n, terminated by double \0)
    /// 
    /// Usage:
    ///   GXMPackager <input.bin> <output.gxm> [options]
    ///   
    /// Options:
    ///   --entry <offset>    Entry point RVA (default: 0)
    ///   --version <number>  Version number (default: 1)
    ///   --script <file>     Add GUI script from file
    /// </summary>
    class Program
    {
        static int Main(string[] args)
        {
            Console.WriteLine("GXM Packager v1.0");
            Console.WriteLine("==================");
            Console.WriteLine();

            if (args.Length < 2)
            {
                ShowUsage();
                return 1;
            }

            string inputFile = args[0];
            string outputFile = args[1];
            uint entryRva = 0;
            uint version = 1;
            string? scriptFile = null;

            // Parse options
            for (int i = 2; i < args.Length; i++)
            {
                switch (args[i])
                {
                    case "--entry":
                        if (i + 1 < args.Length)
                        {
                            if (!uint.TryParse(args[++i], out entryRva))
                            {
                                Console.WriteLine($"Error: Invalid entry point: {args[i]}");
                                return 1;
                            }
                        }
                        break;
                    case "--version":
                        if (i + 1 < args.Length)
                        {
                            if (!uint.TryParse(args[++i], out version))
                            {
                                Console.WriteLine($"Error: Invalid version: {args[i]}");
                                return 1;
                            }
                        }
                        break;
                    case "--script":
                        if (i + 1 < args.Length)
                        {
                            scriptFile = args[++i];
                        }
                        break;
                }
            }

            try
            {
                GXMCore.PackageGXM(inputFile, outputFile, entryRva, version, scriptFile, Console.WriteLine);
                return 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                return 1;
            }
        }

        static void ShowUsage()
        {
            Console.WriteLine("Usage: GXMPackager <input.bin> <output.gxm> [options]");
            Console.WriteLine();
            Console.WriteLine("Options:");
            Console.WriteLine("  --entry <offset>    Entry point RVA (default: 0)");
            Console.WriteLine("  --version <number>  Version number (default: 1)");
            Console.WriteLine("  --script <file>     Add GUI script from file");
            Console.WriteLine();
            Console.WriteLine("Examples:");
            Console.WriteLine("  GXMPackager app.bin app.gxm");
            Console.WriteLine("  GXMPackager app.bin app.gxm --entry 0x1000 --version 2");
            Console.WriteLine("  GXMPackager gui.txt demo.gxm --script gui.txt");
            Console.WriteLine();
            Console.WriteLine("GXM Format:");
            Console.WriteLine("  [0..3]   'G', 'X', 'M', '\\0'");
            Console.WriteLine("  [4..7]   Version (u32)");
            Console.WriteLine("  [8..11]  Entry RVA (u32)");
            Console.WriteLine("  [12..15] Image size (u32)");
            Console.WriteLine("  [16..]   Binary data or GUI script");
        }
    }
}
