# GXM Packager GUI

A user-friendly Windows Forms application for packaging GXM (guideX Module) executables with a **built-in script editor**.

## Features

- **Built-in Script Editor** ? NEW
  - Syntax-highlighted editor (dark theme)
  - Template insertion (Window, Button, Label, etc.)
  - Auto-save and file management
  - Command reference help
  - Line/column tracking
  - Keyboard shortcuts (Ctrl+N, Ctrl+O, Ctrl+S)
- **Drag-and-drop file selection** via Browse buttons
- **Visual options** for entry point, version, and GUI scripts
- **Real-time log output** showing packaging progress
- **Input validation** to prevent errors
- **Help system** with format documentation
- **Clean, modern interface** with color-coded log output

## Quick Start

### Create Your First GXM App in 30 Seconds

1. **Launch GXM Packager GUI**
2. **Click "?? Create New Script"** (green button)
3. **Use the built-in editor**:
   - Click "Insert" ? "Complete Template" for a starter app
   - Or write your own GUI script
   - Click "Save" (Ctrl+S) to save the script
4. **Click "OK"** to close the editor
5. **Click "Package GXM"** - Done!

The editor automatically:
- ? Enables "Include GUI Script"
- ? Sets the input file path
- ? Suggests an output filename

## Built-in Script Editor

### Features

**Toolbar:**
- **New** - Create new script (Ctrl+N)
- **Open** - Open existing script (Ctrl+O)
- **Save** - Save current script (Ctrl+S)
- **Save As** - Save with new name
- **Insert** - Quick template insertion
  - Window
  - Label
  - Button
  - List
  - Dropdown
  - OnClick
  - Complete Template (full example)
- **Help** - Command reference

**Editor:**
- Dark theme for comfortable editing
- Monospace font (Consolas)
- Tab support for indentation
- Line and column tracking in status bar
- Character count display
- Auto-save prompts

**Keyboard Shortcuts:**
- `Ctrl+N` - New file
- `Ctrl+O` - Open file
- `Ctrl+S` - Save file
- `Ctrl+Shift+S` - Save as
- `Tab` - Insert tab character

### Usage Workflow

#### Method 1: Create From Scratch
1. Click "Create New Script" in main window
2. Use Insert menu to add commands
3. Save when done
4. Package automatically

#### Method 2: Edit Existing
1. Browse for script file
2. Check "Include GUI Script"
3. Click "Edit..." button
4. Modify in editor
5. Save and package

#### Method 3: Quick Templates
1. Create New Script
2. Insert ? Complete Template
3. Customize the template
4. Save and package

## Usage

### Basic Packaging

1. Click "Browse..." next to **Input File** and select your binary or script file
2. Click "Browse..." next to **Output GXM File** and choose where to save
3. Click **"Package GXM"**

### Advanced Options

- **Entry Point RVA**: Enter the hexadecimal offset where execution should begin (default: 0)
  - For GUI scripts: Use `0`
  - For binary executables: Set to your entry point offset (e.g., `1000`)

- **Version**: GXM format version number (default: 1)

- **Include GUI Script**: Check this box to add a GUI script
  - Browse for a `.txt` file containing GUI script commands
  - Or click **"Edit..."** to use the built-in editor
  - The packager will automatically add the GUI marker

## GUI Script Format

Create a text file with GUI commands:

```
WINDOW|Window Title|Width|Height
LABEL|Text to display|X|Y
BUTTON|ButtonID|Button Text|X|Y|Width|Height
ONCLICK|ButtonID|ACTION|Argument
```

### Supported Actions
- `NOTIFY` - Show notification message
- `CLOSE` - Close the window
- `EXEC` - Execute command (future)

### Example Script

```
WINDOW|Hello World|400|300
LABEL|Welcome to GXM Apps!|20|50
BUTTON|1|Say Hello|20|100|120|30
BUTTON|2|Exit|160|100|120|30
ONCLICK|1|NOTIFY|Hello from GXM!
ONCLICK|2|CLOSE|
```

## Building the GUI

### Prerequisites
- .NET 9.0 SDK or later
- Windows OS

### Build Steps

```powershell
cd GXM.Apps\GXMPackager.GUI
dotnet build -c Release
```

The executable will be at:
```
bin\Release\net9.0-windows\GXMPackager.GUI.exe
```

### Run from Visual Studio

1. Open `GXM.Apps.sln`
2. Set `GXMPackager.GUI` as startup project
3. Press F5 to run

## Command-Line Alternative

If you prefer command-line usage, the console version is still available:

```powershell
GXMPackager.exe input.bin output.gxm --entry 0x1000 --version 1
```

For GUI scripts:
```powershell
GXMPackager.exe script.txt output.gxm --script script.txt
```

## Tips

### Creating GUI Script Apps
1. Click "Create New Script" (easiest!)
2. Use Insert menu for templates
3. Edit your script
4. Save (Ctrl+S)
5. Package automatically!

### Creating Binary Apps
1. Compile your app with NativeAOT (see BUILD_INSTRUCTIONS.md)
2. Select the compiled `.exe` as **Input File**
3. Set the **Entry Point RVA** if needed
4. Uncheck "Include GUI Script"
5. Package it!

### Script Editor Tips
- Use **Insert ? Complete Template** for a working example
- Press **Tab** to indent (tabs are preserved)
- Check **Help** in editor for command reference
- **Ctrl+S** saves quickly without closing editor
- Editor remembers your last file location

### Testing Your GXM

1. Copy the generated `.gxm` file to `guideXOS\Ramdisk\Programs\`
2. Rebuild the guideXOS ISO
3. Boot guideXOS
4. Run: `cd Programs` then type your app name (without .gxm)

## Troubleshooting

### "Input file not found"
- Make sure the file path is correct
- Use the Browse button to avoid typos

### "Invalid entry point"
- Entry points must be in hexadecimal format
- You can use `0x` prefix or just the hex number
- Example: `1000` or `0x1000`

### "Script file not found"
- When "Include GUI Script" is checked, you must select a valid script file
- Use the built-in editor ("Create New Script") to create one

### Editor won't open
- Make sure you have .NET 9.0 runtime installed
- Try running from command line to see error messages

### GXM doesn't run in guideXOS
- Use the `gxminfo` command in guideXOS console to inspect the GXM file
- Check the signature shows `GXM\0`
- Verify the file size matches expected size

## What's New

### Version 1.1 - Built-in Editor
- ? **Built-in script editor** with syntax highlighting
- ?? **Template insertion** for quick script creation
- ?? **Auto-save prompts** to prevent data loss
- ?? **Keyboard shortcuts** for power users
- ?? **Line/column tracking** in status bar
- ?? **Dark theme** for comfortable editing
- ?? **Integrated help system** with command reference

### Version 1.0 - Initial Release
- GUI-based GXM packaging
- File browser dialogs
- Real-time log output
- Input validation
- Help system

## See Also

- `../README.md` - Complete GXM Apps documentation
- `../BUILD_INSTRUCTIONS.md` - Building native apps
- `QUICKSTART.md` - Step-by-step tutorial
- `../GXMPackager/sample-gui.txt` - Example GUI script
