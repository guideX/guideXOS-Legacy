using System;
using System.IO;
using System.Windows.Forms;
using GXMPackager;

namespace GXMPackager.GUI
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();
            this.Text = "GXM Packager GUI v1.0";
            this.Size = new System.Drawing.Size(700, 600);  // Increased height for new button
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
        }

        private void InitializeComponent()
        {
            // Title Label
            var titleLabel = new Label
            {
                Text = "GXM Packager",
                Font = new System.Drawing.Font("Segoe UI", 16, System.Drawing.FontStyle.Bold),
                Location = new System.Drawing.Point(20, 20),
                Size = new System.Drawing.Size(660, 35),
                TextAlign = System.Drawing.ContentAlignment.MiddleCenter
            };

            var subtitleLabel = new Label
            {
                Text = "Package binary files and GUI scripts into GXM executables",
                Font = new System.Drawing.Font("Segoe UI", 9),
                ForeColor = System.Drawing.Color.Gray,
                Location = new System.Drawing.Point(20, 55),
                Size = new System.Drawing.Size(660, 20),
                TextAlign = System.Drawing.ContentAlignment.MiddleCenter
            };

            // Input File
            var inputLabel = new Label
            {
                Text = "Input File (Binary or Script):",
                Location = new System.Drawing.Point(20, 95),
                Size = new System.Drawing.Size(200, 20)
            };

            var inputTextBox = new TextBox
            {
                Name = "inputTextBox",
                Location = new System.Drawing.Point(20, 120),
                Size = new System.Drawing.Size(550, 25)
            };

            var inputButton = new Button
            {
                Text = "Browse...",
                Location = new System.Drawing.Point(580, 118),
                Size = new System.Drawing.Size(90, 27)
            };
            inputButton.Click += (s, e) =>
            {
                using var dialog = new OpenFileDialog
                {
                    Title = "Select Input File",
                    Filter = "All Files (*.*)|*.*|Binary Files (*.bin;*.exe)|*.bin;*.exe|Text Files (*.txt)|*.txt"
                };
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    inputTextBox.Text = dialog.FileName;
                }
            };

            // Output File
            var outputLabel = new Label
            {
                Text = "Output GXM File:",
                Location = new System.Drawing.Point(20, 160),
                Size = new System.Drawing.Size(200, 20)
            };

            var outputTextBox = new TextBox
            {
                Name = "outputTextBox",
                Location = new System.Drawing.Point(20, 185),
                Size = new System.Drawing.Size(550, 25)
            };

            var outputButton = new Button
            {
                Text = "Browse...",
                Location = new System.Drawing.Point(580, 183),
                Size = new System.Drawing.Size(90, 27)
            };
            outputButton.Click += (s, e) =>
            {
                using var dialog = new SaveFileDialog
                {
                    Title = "Save GXM File",
                    Filter = "GXM Files (*.gxm)|*.gxm|All Files (*.*)|*.*",
                    DefaultExt = "gxm"
                };
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    outputTextBox.Text = dialog.FileName;
                }
            };

            // Options Group
            var optionsGroup = new GroupBox
            {
                Text = "Options",
                Location = new System.Drawing.Point(20, 225),
                Size = new System.Drawing.Size(650, 150)  // Increased height for editor button
            };

            // Entry Point
            var entryLabel = new Label
            {
                Text = "Entry Point RVA (hex):",
                Location = new System.Drawing.Point(15, 30),
                Size = new System.Drawing.Size(150, 20)
            };

            var entryTextBox = new TextBox
            {
                Name = "entryTextBox",
                Text = "0",
                Location = new System.Drawing.Point(170, 27),
                Size = new System.Drawing.Size(100, 25)
            };

            // Version
            var versionLabel = new Label
            {
                Text = "Version:",
                Location = new System.Drawing.Point(290, 30),
                Size = new System.Drawing.Size(60, 20)
            };

            var versionTextBox = new TextBox
            {
                Name = "versionTextBox",
                Text = "1",
                Location = new System.Drawing.Point(355, 27),
                Size = new System.Drawing.Size(100, 25)
            };

            // Script File
            var scriptCheckBox = new CheckBox
            {
                Name = "scriptCheckBox",
                Text = "Include GUI Script:",
                Location = new System.Drawing.Point(15, 65),
                Size = new System.Drawing.Size(150, 20)
            };

            var scriptTextBox = new TextBox
            {
                Name = "scriptTextBox",
                Enabled = false,
                Location = new System.Drawing.Point(170, 62),
                Size = new System.Drawing.Size(315, 25)
            };

            var scriptButton = new Button
            {
                Name = "scriptButton",
                Text = "Browse...",
                Enabled = false,
                Location = new System.Drawing.Point(495, 60),
                Size = new System.Drawing.Size(70, 27)
            };

            // NEW: Script Editor Button
            var editScriptButton = new Button
            {
                Name = "editScriptButton",
                Text = "Edit...",
                Enabled = false,
                Location = new System.Drawing.Point(570, 60),
                Size = new System.Drawing.Size(70, 27),
                BackColor = System.Drawing.Color.FromArgb(0, 120, 215),
                ForeColor = System.Drawing.Color.White,
                FlatStyle = FlatStyle.Flat
            };

            editScriptButton.Click += (s, e) =>
            {
                string? existingFile = !string.IsNullOrWhiteSpace(scriptTextBox.Text) && File.Exists(scriptTextBox.Text)
                    ? scriptTextBox.Text
                    : null;

                using var editorForm = new ScriptEditorForm(existingFile);
                if (editorForm.ShowDialog() == DialogResult.OK)
                {
                    // If editor has a file path, use it
                    if (!string.IsNullOrEmpty(editorForm.FilePath))
                    {
                        scriptTextBox.Text = editorForm.FilePath;
                        
                        // Auto-set input file if empty
                        if (string.IsNullOrWhiteSpace(inputTextBox.Text))
                        {
                            inputTextBox.Text = editorForm.FilePath;
                        }
                    }
                    else if (!string.IsNullOrEmpty(editorForm.ScriptContent))
                    {
                        // Content but no file - prompt to save
                        using var saveDialog = new SaveFileDialog
                        {
                            Title = "Save Script",
                            Filter = "Text Files (*.txt)|*.txt|All Files (*.*)|*.*",
                            DefaultExt = "txt",
                            FileName = "script.txt"
                        };

                        if (saveDialog.ShowDialog() == DialogResult.OK)
                        {
                            try
                            {
                                File.WriteAllText(saveDialog.FileName, editorForm.ScriptContent);
                                scriptTextBox.Text = saveDialog.FileName;
                                
                                if (string.IsNullOrWhiteSpace(inputTextBox.Text))
                                {
                                    inputTextBox.Text = saveDialog.FileName;
                                }
                            }
                            catch (Exception ex)
                            {
                                MessageBox.Show($"Error saving script: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            }
                        }
                    }
                }
            };

            scriptCheckBox.CheckedChanged += (s, e) =>
            {
                scriptTextBox.Enabled = scriptCheckBox.Checked;
                scriptButton.Enabled = scriptCheckBox.Checked;
                editScriptButton.Enabled = scriptCheckBox.Checked;
            };

            scriptButton.Click += (s, e) =>
            {
                using var dialog = new OpenFileDialog
                {
                    Title = "Select GUI Script File",
                    Filter = "Text Files (*.txt)|*.txt|All Files (*.*)|*.*"
                };
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    scriptTextBox.Text = dialog.FileName;
                }
            };

            // NEW: Create New Script Button
            var newScriptButton = new Button
            {
                Text = "?? Create New Script",
                Font = new System.Drawing.Font("Segoe UI", 9, System.Drawing.FontStyle.Bold),
                Location = new System.Drawing.Point(15, 100),
                Size = new System.Drawing.Size(625, 35),
                BackColor = System.Drawing.Color.FromArgb(40, 167, 69),
                ForeColor = System.Drawing.Color.White,
                FlatStyle = FlatStyle.Flat
            };

            newScriptButton.Click += (s, e) =>
            {
                using var editorForm = new ScriptEditorForm();
                if (editorForm.ShowDialog() == DialogResult.OK)
                {
                    if (!string.IsNullOrEmpty(editorForm.FilePath))
                    {
                        // Enable script checkbox if not already
                        if (!scriptCheckBox.Checked)
                        {
                            scriptCheckBox.Checked = true;
                        }

                        scriptTextBox.Text = editorForm.FilePath;
                        
                        if (string.IsNullOrWhiteSpace(inputTextBox.Text))
                        {
                            inputTextBox.Text = editorForm.FilePath;
                        }

                        // Auto-suggest output filename
                        if (string.IsNullOrWhiteSpace(outputTextBox.Text))
                        {
                            string dir = Path.GetDirectoryName(editorForm.FilePath) ?? "";
                            string nameWithoutExt = Path.GetFileNameWithoutExtension(editorForm.FilePath);
                            outputTextBox.Text = Path.Combine(dir, nameWithoutExt + ".gxm");
                        }
                    }
                }
            };

            optionsGroup.Controls.AddRange(new Control[] {
                entryLabel, entryTextBox, versionLabel, versionTextBox,
                scriptCheckBox, scriptTextBox, scriptButton, editScriptButton,
                newScriptButton
            });

            // Log
            var logLabel = new Label
            {
                Text = "Log:",
                Location = new System.Drawing.Point(20, 385),
                Size = new System.Drawing.Size(100, 20)
            };

            var logTextBox = new TextBox
            {
                Name = "logTextBox",
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                Location = new System.Drawing.Point(20, 410),
                Size = new System.Drawing.Size(650, 80),
                BackColor = System.Drawing.Color.Black,
                ForeColor = System.Drawing.Color.LightGreen,
                Font = new System.Drawing.Font("Consolas", 9)
            };

            // Package Button
            var packageButton = new Button
            {
                Text = "Package GXM",
                Font = new System.Drawing.Font("Segoe UI", 10, System.Drawing.FontStyle.Bold),
                Location = new System.Drawing.Point(540, 500),
                Size = new System.Drawing.Size(130, 35),
                BackColor = System.Drawing.Color.FromArgb(0, 120, 215),
                ForeColor = System.Drawing.Color.White,
                FlatStyle = FlatStyle.Flat
            };

            packageButton.Click += (s, e) =>
            {
                logTextBox.Clear();
                try
                {
                    // Validate inputs
                    if (string.IsNullOrWhiteSpace(inputTextBox.Text))
                    {
                        MessageBox.Show("Please select an input file.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }

                    if (string.IsNullOrWhiteSpace(outputTextBox.Text))
                    {
                        MessageBox.Show("Please select an output file.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }

                    // Parse entry point
                    uint entryRva = 0;
                    string entryText = entryTextBox.Text.Trim();
                    if (entryText.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                    {
                        entryText = entryText.Substring(2);
                    }
                    if (!uint.TryParse(entryText, System.Globalization.NumberStyles.HexNumber, null, out entryRva))
                    {
                        MessageBox.Show("Invalid entry point. Please enter a valid hexadecimal value.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }

                    // Parse version
                    if (!uint.TryParse(versionTextBox.Text, out uint version))
                    {
                        MessageBox.Show("Invalid version number.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }

                    // Get script file if enabled
                    string? scriptFile = null;
                    if (scriptCheckBox.Checked && !string.IsNullOrWhiteSpace(scriptTextBox.Text))
                    {
                        scriptFile = scriptTextBox.Text;
                    }

                    // Package
                    logTextBox.AppendText("Starting GXM packaging...\r\n");
                    logTextBox.AppendText("=====================================\r\n\r\n");

                    GXMCore.PackageGXM(
                        inputTextBox.Text,
                        outputTextBox.Text,
                        entryRva,
                        version,
                        scriptFile,
                        message =>
                        {
                            logTextBox.AppendText(message + "\r\n");
                            Application.DoEvents();
                        }
                    );

                    logTextBox.AppendText("\r\n=====================================\r\n");
                    logTextBox.AppendText("Packaging complete!\r\n");

                    MessageBox.Show("GXM file created successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    logTextBox.AppendText($"\r\nERROR: {ex.Message}\r\n");
                    MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };

            // Help Button
            var helpButton = new Button
            {
                Text = "Help",
                Location = new System.Drawing.Point(20, 500),
                Size = new System.Drawing.Size(80, 35)
            };

            helpButton.Click += (s, e) =>
            {
                string helpText = @"GXM Packager GUI - Help

GXM Format:
A GXM file is a custom executable format for guideXOS.

Structure:
[0..3]   Magic: 'G', 'X', 'M', '\0'
[4..7]   Version (u32)
[8..11]  Entry RVA (u32) - Entry point offset
[12..15] Image size (u32) - Total binary size
[16..]   Binary data or GUI script

GUI Scripts:
If you enable 'Include GUI Script', the packager will
add a GUI script marker at offset 16:
[16..19] 'G', 'U', 'I', '\0'
[20..]   Script data

GUI Script Format:
WINDOW|Title|Width|Height
LABEL|Text|X|Y
BUTTON|ID|Text|X|Y|W|H
ONCLICK|ID|Action|Arg

Example:
WINDOW|My App|400|300
LABEL|Hello World!|20|50
BUTTON|1|Click Me|20|100|120|30
ONCLICK|1|NOTIFY|Hello!

Built-in Editor:
Click 'Create New Script' to open the built-in
script editor with templates and syntax help.

Entry Point:
For binary executables, set the entry point RVA (offset)
where execution should begin. For GUI scripts, use 0.

Version:
GXM format version number (default: 1).";

                MessageBox.Show(helpText, "Help", MessageBoxButtons.OK, MessageBoxIcon.Information);
            };

            // Clear Button
            var clearButton = new Button
            {
                Text = "Clear",
                Location = new System.Drawing.Point(110, 500),
                Size = new System.Drawing.Size(80, 35)
            };

            clearButton.Click += (s, e) =>
            {
                inputTextBox.Clear();
                outputTextBox.Clear();
                entryTextBox.Text = "0";
                versionTextBox.Text = "1";
                scriptCheckBox.Checked = false;
                scriptTextBox.Clear();
                logTextBox.Clear();
            };

            // Add all controls to form
            this.Controls.AddRange(new Control[] {
                titleLabel, subtitleLabel,
                inputLabel, inputTextBox, inputButton,
                outputLabel, outputTextBox, outputButton,
                optionsGroup,
                logLabel, logTextBox,
                packageButton, helpButton, clearButton
            });
        }
    }
}
