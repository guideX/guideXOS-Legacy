using System;
using System.Drawing;
using System.Windows.Forms;

namespace GXMPackager.GUI
{
    public partial class ScriptEditorForm : Form
    {
        private TextBox editorTextBox = null!;
        private ToolStripStatusLabel lineColumnLabel = null!;
        private ToolStripStatusLabel charCountLabel = null!;
        private string? filePath;
        private bool hasUnsavedChanges = false;

        public string ScriptContent => editorTextBox?.Text ?? string.Empty;
        public string? FilePath => filePath;

        public ScriptEditorForm(string? existingFilePath = null)
        {
            InitializeComponent();
            this.Text = "GXM Script Editor";
            this.Size = new Size(800, 600);
            this.StartPosition = FormStartPosition.CenterParent;
            this.MinimumSize = new Size(600, 400);

            if (existingFilePath != null && System.IO.File.Exists(existingFilePath))
            {
                filePath = existingFilePath;
                editorTextBox.Text = System.IO.File.ReadAllText(existingFilePath);
                this.Text = $"GXM Script Editor - {System.IO.Path.GetFileName(existingFilePath)}";
                hasUnsavedChanges = false;
            }
        }

        private void InitializeComponent()
        {
            // Toolbar
            var toolbar = new ToolStrip
            {
                Dock = DockStyle.Top,
                GripStyle = ToolStripGripStyle.Hidden,
                BackColor = Color.FromArgb(240, 240, 240)
            };

            var newButton = new ToolStripButton("New", null, (s, e) => NewFile())
            {
                DisplayStyle = ToolStripItemDisplayStyle.ImageAndText,
                ToolTipText = "New script (Ctrl+N)"
            };

            var openButton = new ToolStripButton("Open", null, (s, e) => OpenFile())
            {
                DisplayStyle = ToolStripItemDisplayStyle.ImageAndText,
                ToolTipText = "Open script (Ctrl+O)"
            };

            var saveButton = new ToolStripButton("Save", null, (s, e) => SaveFile())
            {
                DisplayStyle = ToolStripItemDisplayStyle.ImageAndText,
                ToolTipText = "Save script (Ctrl+S)"
            };

            var saveAsButton = new ToolStripButton("Save As", null, (s, e) => SaveFileAs())
            {
                DisplayStyle = ToolStripItemDisplayStyle.ImageAndText,
                ToolTipText = "Save script as..."
            };

            toolbar.Items.Add(newButton);
            toolbar.Items.Add(openButton);
            toolbar.Items.Add(new ToolStripSeparator());
            toolbar.Items.Add(saveButton);
            toolbar.Items.Add(saveAsButton);
            toolbar.Items.Add(new ToolStripSeparator());

            var insertButton = new ToolStripDropDownButton("Insert")
            {
                DisplayStyle = ToolStripItemDisplayStyle.ImageAndText,
                ToolTipText = "Insert template"
            };

            insertButton.DropDownItems.Add("Window", null, (s, e) => InsertTemplate("WINDOW|My Window|400|300\n"));
            insertButton.DropDownItems.Add(new ToolStripSeparator());
            insertButton.DropDownItems.Add("Window Properties...", null, (s, e) => InsertTemplate(
                "RESIZABLE|true\nTASKBAR|true\nMAXIMIZE|true\nMINIMIZE|true\nTOMBSTONE|true\nSTARTMENU|true\n"));
            insertButton.DropDownItems.Add(new ToolStripSeparator());
            insertButton.DropDownItems.Add("Label", null, (s, e) => InsertTemplate("LABEL|Text here|20|50\n"));
            insertButton.DropDownItems.Add("TextBox", null, (s, e) => InsertTemplate("TEXTBOX|1|20|50|400|200|\n"));
            insertButton.DropDownItems.Add("Button", null, (s, e) => InsertTemplate("BUTTON|1|Click Me|20|100|120|30\n"));
            insertButton.DropDownItems.Add("List", null, (s, e) => InsertTemplate("LIST|1|20|150|200|100|Item1;Item2;Item3\n"));
            insertButton.DropDownItems.Add("Dropdown", null, (s, e) => InsertTemplate("DROPDOWN|1|20|200|150|25|Option1;Option2;Option3\n"));
            insertButton.DropDownItems.Add("OnClick", null, (s, e) => InsertTemplate("ONCLICK|1|NOTIFY|Message here\n"));
            insertButton.DropDownItems.Add("OnTextChange", null, (s, e) => InsertTemplate("ONTEXTCHANGE|1|MSG|Text changed: $VALUE\n"));
            insertButton.DropDownItems.Add(new ToolStripSeparator());
            insertButton.DropDownItems.Add("Complete Template", null, (s, e) => InsertTemplate(GetCompleteTemplate()));

            toolbar.Items.Add(insertButton);

            var helpButton = new ToolStripButton("Help", null, (s, e) => ShowHelp())
            {
                Alignment = ToolStripItemAlignment.Right,
                ToolTipText = "Show command reference"
            };

            toolbar.Items.Add(helpButton);

            // Editor TextBox
            editorTextBox = new TextBox
            {
                Multiline = true,
                Dock = DockStyle.Fill,
                ScrollBars = ScrollBars.Both,
                Font = new Font("Consolas", 10),
                AcceptsTab = true,
                WordWrap = false,
                BackColor = Color.FromArgb(30, 30, 30),
                ForeColor = Color.FromArgb(220, 220, 220)
            };

            editorTextBox.TextChanged += (s, e) =>
            {
                if (!this.Text.EndsWith("*"))
                {
                    hasUnsavedChanges = true;
                    this.Text += " *";
                }

                // Update character count
                if (charCountLabel != null)
                {
                    charCountLabel.Text = $"{editorTextBox.Text.Length} characters";
                }
            };

            // Status Bar
            var statusBar = new StatusStrip
            {
                BackColor = Color.FromArgb(240, 240, 240)
            };

            lineColumnLabel = new ToolStripStatusLabel
            {
                Name = "lineColumnLabel",
                Text = "Line 1, Col 1"
            };

            charCountLabel = new ToolStripStatusLabel
            {
                Name = "charCountLabel",
                Text = "0 characters",
                Spring = true,
                TextAlign = ContentAlignment.MiddleRight
            };

            statusBar.Items.Add(lineColumnLabel);
            statusBar.Items.Add(charCountLabel);

            // Update status on click and key press
            editorTextBox.Click += (s, e) => UpdateStatusBar();
            editorTextBox.KeyUp += (s, e) => UpdateStatusBar();

            // Bottom buttons panel
            var buttonPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 50,
                Padding = new Padding(10)
            };

            var okButton = new Button
            {
                Text = "OK",
                DialogResult = DialogResult.OK,
                Location = new Point(10, 10),
                Size = new Size(100, 30)
            };

            var cancelButton = new Button
            {
                Text = "Cancel",
                DialogResult = DialogResult.Cancel,
                Location = new Point(120, 10),
                Size = new Size(100, 30)
            };

            buttonPanel.Controls.Add(cancelButton);
            buttonPanel.Controls.Add(okButton);

            // Add all controls
            this.Controls.Add(editorTextBox);
            this.Controls.Add(toolbar);
            this.Controls.Add(statusBar);
            this.Controls.Add(buttonPanel);

            // Keyboard shortcuts
            this.KeyPreview = true;
            this.KeyDown += (s, e) =>
            {
                if (e.Control)
                {
                    switch (e.KeyCode)
                    {
                        case Keys.N:
                            NewFile();
                            e.Handled = true;
                            break;
                        case Keys.O:
                            OpenFile();
                            e.Handled = true;
                            break;
                        case Keys.S:
                            if (e.Shift)
                                SaveFileAs();
                            else
                                SaveFile();
                            e.Handled = true;
                            break;
                    }
                }
            };

            // Handle closing
            this.FormClosing += (s, e) =>
            {
                if (hasUnsavedChanges)
                {
                    var result = MessageBox.Show(
                        "You have unsaved changes. Do you want to save before closing?",
                        "Unsaved Changes",
                        MessageBoxButtons.YesNoCancel,
                        MessageBoxIcon.Warning);

                    if (result == DialogResult.Yes)
                    {
                        if (!SaveFile())
                        {
                            e.Cancel = true;
                        }
                    }
                    else if (result == DialogResult.Cancel)
                    {
                        e.Cancel = true;
                    }
                }
            };
        }

        private void UpdateStatusBar()
        {
            if (editorTextBox == null || lineColumnLabel == null) return;

            int line = editorTextBox.GetLineFromCharIndex(editorTextBox.SelectionStart) + 1;
            int col = editorTextBox.SelectionStart - editorTextBox.GetFirstCharIndexFromLine(line - 1) + 1;
            lineColumnLabel.Text = $"Line {line}, Col {col}";
        }

        private void NewFile()
        {
            if (hasUnsavedChanges)
            {
                var result = MessageBox.Show(
                    "You have unsaved changes. Create new file anyway?",
                    "Unsaved Changes",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning);

                if (result != DialogResult.Yes)
                    return;
            }

            editorTextBox.Clear();
            filePath = null;
            hasUnsavedChanges = false;
            this.Text = "GXM Script Editor - New File";
        }

        private void OpenFile()
        {
            using var dialog = new OpenFileDialog
            {
                Title = "Open Script",
                Filter = "Text Files (*.txt)|*.txt|All Files (*.*)|*.*",
                DefaultExt = "txt"
            };

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    editorTextBox.Text = System.IO.File.ReadAllText(dialog.FileName);
                    filePath = dialog.FileName;
                    hasUnsavedChanges = false;
                    this.Text = $"GXM Script Editor - {System.IO.Path.GetFileName(dialog.FileName)}";
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error opening file: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private bool SaveFile()
        {
            if (string.IsNullOrEmpty(filePath))
            {
                return SaveFileAs();
            }

            try
            {
                System.IO.File.WriteAllText(filePath, editorTextBox.Text);
                hasUnsavedChanges = false;
                this.Text = $"GXM Script Editor - {System.IO.Path.GetFileName(filePath)}";
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving file: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        private bool SaveFileAs()
        {
            using var dialog = new SaveFileDialog
            {
                Title = "Save Script As",
                Filter = "Text Files (*.txt)|*.txt|All Files (*.*)|*.*",
                DefaultExt = "txt",
                FileName = !string.IsNullOrEmpty(filePath) ? System.IO.Path.GetFileName(filePath) : "script.txt"
            };

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                filePath = dialog.FileName;
                return SaveFile();
            }

            return false;
        }

        private void InsertTemplate(string template)
        {
            int selectionStart = editorTextBox.SelectionStart;
            editorTextBox.Text = editorTextBox.Text.Insert(selectionStart, template);
            editorTextBox.SelectionStart = selectionStart + template.Length;
            editorTextBox.Focus();
        }

        private string GetCompleteTemplate()
        {
            return @"WINDOW|My Application|400|300
RESIZABLE|true
TASKBAR|true
MAXIMIZE|true
MINIMIZE|true
TOMBSTONE|true
STARTMENU|true
LABEL|Welcome to my app!|20|40
BUTTON|1|Click Me|20|100|120|30
BUTTON|2|Show Info|160|100|120|30
BUTTON|3|Close|300|100|120|30
ONCLICK|1|NOTIFY|Button 1 clicked!
ONCLICK|2|NOTIFY|Information message
ONCLICK|3|CLOSE|
";
        }

        private void ShowHelp()
        {
            string helpText = @"GXM Script Commands Reference

WINDOW|Title|Width|Height
  Creates the main window (required - must be first line)
  Example: WINDOW|My App|400|300

Window Properties:
RESIZABLE|true/false
  Controls if window can be resized
  Example: RESIZABLE|false

TASKBAR|true/false
  Controls if window shows in taskbar
  Example: TASKBAR|true

MAXIMIZE|true/false
  Controls if window has maximize button
  Example: MAXIMIZE|true

MINIMIZE|true/false
  Controls if window has minimize button
  Example: MINIMIZE|true

TOMBSTONE|true/false
  Controls if window has tombstone button
  Example: TOMBSTONE|true

STARTMENU|true/false
  Controls if window shows in start menu
  Example: STARTMENU|false

LABEL|Text|X|Y
  Static text display
  Example: LABEL|Hello World!|20|50

TEXTBOX|ID|X|Y|Width|Height|InitialText
  Multi-line editable text box (InitialText optional)
  Click to focus, supports typing, backspace, enter, tab
  Example: TEXTBOX|1|10|40|680|340|

BUTTON|ID|Text|X|Y|Width|Height
  Clickable button (ID must be unique)
  Example: BUTTON|1|Click Me|20|100|120|30

LIST|ID|X|Y|Width|Height|Items
  List box with selectable items (items separated by ;)
  Example: LIST|1|20|150|200|100|Item1;Item2;Item3

DROPDOWN|ID|X|Y|Width|Height|Options
  Dropdown/combo box (options separated by ;)
  Example: DROPDOWN|1|20|200|150|25|Opt1;Opt2;Opt3

ONCLICK|ID|Action|Argument
  Define button click behavior
  Actions: NOTIFY, CLOSE, OPENAPP, SAVETEXT, LOADTEXT
  Example: ONCLICK|1|NOTIFY|Hello!

ONCHANGE|ID|Action|Argument
  Handle list/dropdown selection change
  Example: ONCHANGE|1|NOTIFY|Selection changed

ONTEXTCHANGE|ID|Action|Argument
  Handle textbox text change (fires on every keystroke)
  Use $VALUE token to get current text
  Example: ONTEXTCHANGE|1|MSG|Text: $VALUE

Actions:
  MSG or NOTIFY - Show notification message
  CLOSE - Close the window
  OPENAPP - Launch built-in app (arg: app name)
  SAVETEXT - Save textbox content to file (arg: filename)
  LOADTEXT - Load file into textbox (arg: filename)

Tips:
• Use | (pipe) to separate fields
• Commands are case-insensitive
• Window properties accept: true/false, 1/0, yes/no, on/off
• Coordinates: X=horizontal, Y=vertical (top-left = 0,0)
• Keep controls within window bounds
• Place window properties right after WINDOW command
• TEXTBOX click to focus (blue border = focused)
• Use $VALUE in action arguments to get control values";

            using var helpForm = new Form
            {
                Text = "Command Reference",
                Size = new Size(700, 650),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false
            };

            var textBox = new TextBox
            {
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                Dock = DockStyle.Fill,
                Font = new Font("Consolas", 9),
                Text = helpText,
                BackColor = Color.White
            };

            var closeButton = new Button
            {
                Text = "Close",
                DialogResult = DialogResult.OK,
                Dock = DockStyle.Bottom,
                Height = 35
            };

            helpForm.Controls.Add(textBox);
            helpForm.Controls.Add(closeButton);
            helpForm.ShowDialog();
        }
    }
}
