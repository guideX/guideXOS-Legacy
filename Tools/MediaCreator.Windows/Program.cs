// THIS THING IS DANGEROUS! BE CAREFUL WHEN EDITING AND TESTING IT.
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace GuideXOS.MediaCreator.Windows {
    internal static class Program {
        [STAThread]
        static void Main() {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            var form = new Form() { Text = "GuideXOS Media Creator (Windows)", Width = 760, Height = 540, StartPosition = FormStartPosition.CenterScreen };

            var lbl = new Label(){ Left=12, Top=12, Width=720, Text="Create boot media for GuideXOS: USB, ISO, or burn to CD/DVD." };

            var rbUsb = new RadioButton(){ Left=12, Top=42, Text="USB Flash Drive", Checked=true };
            var rbIso = new RadioButton(){ Left=160, Top=42, Text="ISO Image" };
            var rbCd  = new RadioButton(){ Left=260, Top=42, Text="Burn to CD/DVD" };

            // USB controls
            var lblUsb = new Label(){ Left=12, Top=72, Width=120, Text="USB Drive:" };
            var comboUsb = new ComboBox(){ Left=120, Top=68, Width=200, DropDownStyle=ComboBoxStyle.DropDownList };
            var btnRefreshUsb = new Button(){ Left=330, Top=68, Width=80, Text="Refresh" };
            var chkFormat = new CheckBox(){ Left=420, Top=70, Width=180, Text="Quick format FAT32" , Checked = true};

            // ISO controls
            var lblIso = new Label(){ Left=12, Top=110, Width=120, Text="ISO Output:" };
            var txtIso = new TextBox(){ Left=120, Top=106, Width=450 };
            var btnBrowseIso = new Button(){ Left=580, Top=104, Width=80, Text="Browse" };

            // CD/DVD controls
            var lblCd = new Label(){ Left=12, Top=148, Width=120, Text="CD/DVD Drive:" };
            var comboCd = new ComboBox(){ Left=120, Top=144, Width=200, DropDownStyle=ComboBoxStyle.DropDownList };
            var btnRefreshCd = new Button(){ Left=330, Top=144, Width=80, Text="Refresh" };

            var btnCreate = new Button(){ Left=12, Top=186, Width=180, Height=32, Text="Create" };
            var output = new TextBox(){ Left=12, Top=230, Width=720, Height=260, Multiline=true, ScrollBars=ScrollBars.Vertical, ReadOnly=true };

            form.Controls.AddRange(new Control[]{ lbl, rbUsb, rbIso, rbCd, lblUsb, comboUsb, btnRefreshUsb, chkFormat, lblIso, txtIso, btnBrowseIso, lblCd, comboCd, btnRefreshCd, btnCreate, output });

            void Log(string s){ output.AppendText(s + Environment.NewLine); }

            string FindRoot(){
                // Walk up from executable until we find a Tools folder; return that parent (solution root)
                var dir = new DirectoryInfo(AppContext.BaseDirectory);
                for (int i=0;i<8 && dir != null; i++, dir = dir.Parent){
                    if (dir.GetDirectories("Tools").Any()) return dir.FullName;
                }
                return AppContext.BaseDirectory;
            }

            string Root = FindRoot();
            string ToolsDir = Path.Combine(Root, "Tools");
            string GrubDir = Path.Combine(ToolsDir, "grub2");
            string SevenZip = Path.Combine(ToolsDir, "7-Zip", "7z.exe");
            string MkIsoFs = Path.Combine(ToolsDir, "mkisofs.exe");
            string RamdiskDir = Path.Combine(Root, "Ramdisk");

            void RefreshUsb(){
                comboUsb.Items.Clear();
                foreach (var di in DriveInfo.GetDrives().Where(d=>d.DriveType==DriveType.Removable)) {
                    comboUsb.Items.Add(di.Name);
                }
                if (comboUsb.Items.Count>0) comboUsb.SelectedIndex = 0;
            }
            void RefreshCd(){
                comboCd.Items.Clear();
                foreach (var di in DriveInfo.GetDrives().Where(d=>d.DriveType==DriveType.CDRom)) {
                    comboCd.Items.Add(di.Name);
                }
                if (comboCd.Items.Count>0) comboCd.SelectedIndex = 0;
            }

            btnRefreshUsb.Click += (s,e)=> RefreshUsb();
            btnRefreshCd.Click += (s,e)=> RefreshCd();

            btnBrowseIso.Click += (s,e)=>{
                using var sfd = new SaveFileDialog(){ Filter = "ISO Image (*.iso)|*.iso", FileName = "guideXOS.iso" };
                if (sfd.ShowDialog(form) == DialogResult.OK) txtIso.Text = sfd.FileName;
            };

            RefreshUsb(); RefreshCd();

            btnCreate.Click += (s,e)=>{
                try {
                    output.Clear();
                    if (!Directory.Exists(GrubDir)) { MessageBox.Show("Tools\\grub2 not found. Run from repository root."); return; }
                    if (!File.Exists(SevenZip)) { MessageBox.Show("Tools\\7-Zip\\7z.exe not found."); return; }
                    if (!File.Exists(MkIsoFs)) { MessageBox.Show("Tools\\mkisofs.exe not found."); return; }

                    // Prepare a temp working grub tree with fresh ramdisk
                    string tempRoot = Path.Combine(Path.GetTempPath(), "guidexos-media-" + Guid.NewGuid().ToString("N"));
                    string tempGrub = Path.Combine(tempRoot, "grub2");
                    Directory.CreateDirectory(tempRoot);
                    Log("Copying grub template...");
                    CopyRecursive(GrubDir, tempGrub);

                    // Repack Ramdisk to boot/ramdisk.tar
                    string ramdiskTar = Path.Combine(tempGrub, "boot", "ramdisk.tar");
                    if (!Directory.Exists(RamdiskDir)) { MessageBox.Show("Ramdisk folder not found: " + RamdiskDir); return; }
                    Log("Packing Ramdisk to tar...");
                    Run(SevenZip, $"a \"{ramdiskTar}\" \"{RamdiskDir}\\*\"", output);

                    if (rbUsb.Checked) {
                        if (comboUsb.SelectedItem==null) { MessageBox.Show("Select a USB drive"); return; }
                        var drive = comboUsb.SelectedItem.ToString();
                        if (chkFormat.Checked) {
                            Log("Formatting " + drive + " to FAT32 (quick)...");
                            Run("cmd.exe", "/c format " + drive + " /FS:FAT32 /Q /Y", output);
                        }
                        Log("Copying files to USB...");
                        CopyRecursive(tempGrub, drive);
                        Log("Done.");
                        MessageBox.Show("USB media created.");
                    }
                    else if (rbIso.Checked) {
                        if (string.IsNullOrWhiteSpace(txtIso.Text)) { MessageBox.Show("Choose ISO output path"); return; }
                        string isoPath = txtIso.Text;
                        Directory.CreateDirectory(Path.GetDirectoryName(isoPath)!);
                        Log("Generating ISO...");
                        Run(MkIsoFs, $"-relaxed-filenames -J -R -o \"{isoPath}\" -b boot/grub/i386-pc/eltorito.img -no-emul-boot -boot-load-size 4 -boot-info-table \"{tempGrub}\"", output);
                        Log("ISO created: " + isoPath);
                        MessageBox.Show("ISO created.");
                    }
                    else if (rbCd.Checked) {
                        if (comboCd.SelectedItem==null) { MessageBox.Show("Select a CD/DVD drive"); return; }
                        // Build ISO to temp then launch isoburn to burn it
                        string isoPath = Path.Combine(tempRoot, "guideXOS.iso");
                        Log("Generating ISO for burning...");
                        Run(MkIsoFs, $"-relaxed-filenames -J -R -o \"{isoPath}\" -b boot/grub/i386-pc/eltorito.img -no-emul-boot -boot-load-size 4 -boot-info-table \"{tempGrub}\"", output);
                        Log("Launching Windows Disc Image Burner...");
                        Run("isoburn.exe", $"\"{isoPath}\"", output);
                        MessageBox.Show("isoburn launched. Follow the prompts to burn the disc.");
                    }
                } catch (Exception ex) {
                    MessageBox.Show(ex.ToString());
                }
            };

            Application.Run(form);
        }

        static void Run(string exe, string args, TextBox output){
            var psi = new ProcessStartInfo(exe, args){ UseShellExecute=false, RedirectStandardOutput=true, RedirectStandardError=true, CreateNoWindow=true };
            using var p = Process.Start(psi);
            if (p == null) return;
            p.OutputDataReceived += (s,e)=> { if (e.Data!=null) output.BeginInvoke(new Action(()=> output.AppendText(e.Data+Environment.NewLine))); };
            p.BeginOutputReadLine();
            p.ErrorDataReceived += (s,e)=> { if (e.Data!=null) output.BeginInvoke(new Action(()=> output.AppendText(e.Data+Environment.NewLine))); };
            p.BeginErrorReadLine();
            p.WaitForExit();
        }

        static void CopyRecursive(string src, string dst){
            Directory.CreateDirectory(dst);
            foreach (var d in Directory.GetDirectories(src)) { var name = Path.GetFileName(d); CopyRecursive(Path.Combine(src, name), Path.Combine(dst, name)); }
            foreach (var f in Directory.GetFiles(src)) { var name = Path.GetFileName(f); File.Copy(Path.Combine(src, name), Path.Combine(dst, name), true); }
        }
    }
}
