using System;
using System.IO;
using System.Runtime.InteropServices;

namespace GuideXOS.Installer {
    internal static class Program {
        static int Main(string[] args) {
            Console.WriteLine("GuideXOS Universal Installer");
            Console.WriteLine("This tool prepares a target disk/partition and copies GuideXOS files.");
            if (args.Length == 0) { PrintUsage(); return 1; }

            string mode = null; string target = null; string fs = null; bool auto = false; string subdir = "GuideXOS"; long sizeMB = 4096;
            for (int i=0;i<args.Length;i++) {
                switch (args[i]) {
                    case "--mode": mode = Next(args, ref i); break; // windows|linux
                    case "--target": target = Next(args, ref i); break; // Windows: \\?\PhysicalDriveN or drive letter; Linux: /dev/sdX
                    case "--fs": fs = Next(args, ref i); break; // fat|ext
                    case "--auto": auto = true; break;
                    case "--manual": auto = false; break;
                    case "--dir": subdir = Next(args, ref i); break;
                    case "--sizeMB": long.TryParse(Next(args, ref i), out sizeMB); break;
                    case "--help": PrintUsage(); return 0;
                }
            }
            if (string.IsNullOrEmpty(mode) || string.IsNullOrEmpty(target)) { PrintUsage(); return 1; }

            try {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) && mode == "windows") {
                    return InstallOnWindows(target, fs, auto, subdir, sizeMB);
                } else if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows) && mode == "linux") {
                    return InstallOnLinux(target, fs, auto, subdir, sizeMB);
                } else {
                    Console.WriteLine("Mode/OS mismatch. Run with --mode windows on Windows, --mode linux on Linux.");
                    return 2;
                }
            } catch (Exception ex) {
                Console.WriteLine("Error: " + ex.Message);
                return 3;
            }
        }

        private static void PrintUsage() {
            Console.WriteLine("Usage:");
            Console.WriteLine("  GuideXOS.Installer --mode <windows|linux> --target <disk> [--fs <fat|ext>] [--auto|--manual] [--dir <folder>] [--sizeMB <n>]");
            Console.WriteLine();
            Console.WriteLine("Examples:");
            Console.WriteLine("  Windows auto FAT on Drive D: copy to D:\\GuideXOS");
            Console.WriteLine("    GuideXOS.Installer --mode windows --target D: --fs fat --auto --dir GuideXOS");
            Console.WriteLine("  Linux manual EXT on /dev/sdb");
            Console.WriteLine("    sudo ./GuideXOS.Installer --mode linux --target /dev/sdb --fs ext --manual --dir GuideXOS --sizeMB 8192");
        }

        private static int InstallOnWindows(string target, string fs, bool auto, string subdir, long sizeMB) {
            Console.WriteLine("Windows mode");
            if (target.EndsWith(":") && Directory.Exists(target + "\\")) {
                string root = target + "\\"; string dest = Path.Combine(root, subdir);
                Directory.CreateDirectory(dest); CopyRamdisk(dest); Console.WriteLine("Files copied to: " + dest); return 0;
            }
            Console.WriteLine("Raw disk/partitioning not implemented in this managed helper. Use the in-OS Disk Manager to partition.");
            return 4;
        }

        private static int InstallOnLinux(string device, string fs, bool auto, string subdir, long sizeMB) {
            Console.WriteLine("Linux mode");
            // Requires external tools: parted/mkfs.vfat/mkfs.ext4 and mount, run via shell.
            if (!auto) {
                Console.WriteLine("Manual mode selected. Please partition device yourself; this tool will only copy files to a mounted path.");
                return 0;
            }
            Console.WriteLine("Auto mode: will attempt to create a single partition and format it.");
            if (string.IsNullOrEmpty(fs)) fs = "fat";
            string part = device + "1";
            int rc = 0;
            rc = Sh("sudo parted -s " + device + " mklabel msdos"); if (rc!=0) return rc;
            rc = Sh($"sudo parted -s {device} mkpart primary 1MiB {sizeMB}MiB"); if (rc!=0) return rc;
            if (fs == "ext") rc = Sh($"sudo mkfs.ext4 -F {part}"); else rc = Sh($"sudo mkfs.vfat -F 32 {part}"); if (rc!=0) return rc;
            Directory.CreateDirectory("/mnt/guidexos");
            rc = Sh($"sudo mount {part} /mnt/guidexos"); if (rc!=0) return rc;
            Directory.CreateDirectory(Path.Combine("/mnt/guidexos", subdir));
            CopyRamdisk(Path.Combine("/mnt/guidexos", subdir));
            Sh("sudo sync");
            Sh("sudo umount /mnt/guidexos");
            Console.WriteLine("Installed to " + part);
            return 0;
        }

        private static int Sh(string cmd) {
            try {
                var psi = new System.Diagnostics.ProcessStartInfo("/bin/bash", "-c \"" + cmd.Replace("\"","\\\"") + "\"") {
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                };
                var p = System.Diagnostics.Process.Start(psi);
                p.WaitForExit();
                Console.WriteLine(p.StandardOutput.ReadToEnd());
                string err = p.StandardError.ReadToEnd(); if (!string.IsNullOrEmpty(err)) Console.WriteLine(err);
                return p.ExitCode;
            } catch (Exception ex) { Console.WriteLine(ex.ToString()); return -1; }
        }

        private static void CopyRamdisk(string dest) {
            // Copies the workspace Ramdisk contents (boot files) into dest.
            string baseDir = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "Ramdisk");
            if (!Directory.Exists(baseDir)) {
                Console.WriteLine("Ramdisk folder not found: " + baseDir);
                return;
            }
            CopyRecursive(baseDir, dest);
        }

        private static void CopyRecursive(string src, string dst) {
            Directory.CreateDirectory(dst);
            foreach (var d in Directory.GetDirectories(src)) {
                string name = Path.GetFileName(d);
                CopyRecursive(Path.Combine(src, name), Path.Combine(dst, name));
            }
            foreach (var f in Directory.GetFiles(src)) {
                string name = Path.GetFileName(f);
                File.Copy(Path.Combine(src, name), Path.Combine(dst, name), overwrite:true);
            }
        }

        private static string Next(string[] a, ref int i) { if (i+1 < a.Length) return a[++i]; return null; }
    }
}
