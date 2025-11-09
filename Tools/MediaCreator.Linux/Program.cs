// THIS THING IS DANGEROUS! BE CAREFUL WHEN EDITING AND TESTING IT.

using System;
using System.Diagnostics;
using System.IO;

namespace GuideXOS.MediaCreator.Linux {
    internal static class Program {
        static int Main(string[] args) {
            Console.WriteLine("GuideXOS Media Creator (Linux)");
            if (args.Length < 1) { Console.WriteLine("Usage: sudo dotnet run -- <device>  (e.g. /dev/sdb)"); return 1; }
            string dev = args[0];
            try {
                Sh($"sudo parted -s {dev} mklabel msdos");
                Sh($"sudo parted -s {dev} mkpart primary fat32 1MiB 100%");
                Sh($"sudo mkfs.vfat -F 32 {dev}1");
                Directory.CreateDirectory("/mnt/guidexos-usb");
                Sh($"sudo mount {dev}1 /mnt/guidexos-usb");
                string src = Path.Combine(AppContext.BaseDirectory, "..","..","..","..","Ramdisk");
                CopyRecursive(src, "/mnt/guidexos-usb");
                Sh("sudo sync");
                Sh("sudo umount /mnt/guidexos-usb");
                Console.WriteLine("Done");
                return 0;
            } catch (Exception ex) { Console.WriteLine(ex); return 2; }
        }

        static void Sh(string cmd){
            var psi = new ProcessStartInfo("/bin/bash", "-c \""+cmd+"\"") { UseShellExecute=false };
            var p = Process.Start(psi); p.WaitForExit(); if (p.ExitCode!=0) throw new Exception("Command failed: "+cmd);
        }

        static void CopyRecursive(string src, string dst){
            Directory.CreateDirectory(dst);
            foreach (var d in Directory.GetDirectories(src)) { var name = Path.GetFileName(d); CopyRecursive(Path.Combine(src, name), Path.Combine(dst, name)); }
            foreach (var f in Directory.GetFiles(src)) { var name = Path.GetFileName(f); File.Copy(Path.Combine(src, name), Path.Combine(dst, name), true); }
        }
    }
}
