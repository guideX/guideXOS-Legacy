# Notepad GXM Application

A simple text editor for guideXOS packaged as a GXM (guideX Module) executable.

## Overview

This is a GUI script-based GXM application that provides a basic notepad interface with working buttons and demonstrations of all supported GXM features.

## What's Included

- **notepad-simple.txt** - Simple notepad with working buttons ?
- **notepad-full.txt** - Extended version with list and dropdown controls ?
- **notepad-demo.txt** - Full-featured demo with all GXM capabilities ?
- **build.ps1** - PowerShell build script to package and deploy the GXM
- **notepad.gxm** - Compiled GXM file (generated)
- **notepad-full.gxm** - Full version (generated)
- **notepad-demo.gxm** - Demo version (generated)
- **Program.cs** - Minimal C# entry point stub (for future native code integration)
- **Notepad.GXM.csproj** - Project file (for future native AOT compilation)

## Working Features ?

All buttons now work correctly! The GXM script system supports:

### Supported Actions

1. **MSG** - Shows a message box with custom text
2. **OPENAPP** - Opens a built-in guideXOS application (e.g., `OPENAPP|Notepad`)
3. **CLOSE** - Closes the current window

### Supported Controls

1. **WINDOW** - Creates the main window
2. **LABEL** - Displays static text
3. **BUTTON** - Clickable buttons with ONCLICK handlers
4. **LIST** - Selectable list of items with ONCHANGE handlers
5. **DROPDOWN** - Combo box with selectable options and ONCHANGE handlers

### What Works Now

- ? **All buttons respond to clicks**
- ? **Message boxes display when buttons are clicked**
- ? **Close button actually closes the window**
- ? **"Open Built-in" button launches the full C# Notepad**
- ? **List items are selectable and show messages**
- ? **Dropdowns work with item selection**
- ? **$VALUE token replacement in ONCHANGE handlers**

## Quick Start

### Build All Versions

```powershell
cd GXM.Apps\Notepad.GXM
.\build.ps1                # Builds notepad-simple.txt
```

Or build specific versions:

```powershell
cd GXM.Apps\GXMPackager

# Simple version
dotnet run -- ..\Notepad.GXM\notepad-simple.txt ..\Notepad.GXM\notepad.gxm --script ..\Notepad.GXM\notepad-simple.txt

# Full version with controls
dotnet run -- ..\Notepad.GXM\notepad-full.txt ..\Notepad.GXM\notepad-full.gxm --script ..\Notepad.GXM\notepad-full.txt

# Demo version with all features
dotnet run -- ..\Notepad.GXM\notepad-demo.txt ..\Notepad.GXM\notepad-demo.gxm --script ..\Notepad.GXM\notepad-demo.txt
```

### Deploy to guideXOS

After building, rebuild the guideXOS ISO:

```powershell
cd ..\..\guideXOS
dotnet build guideXOS.csproj
```

### Run in guideXOS

Boot guideXOS and run:

```
cd Programs
notepad           # Simple version
notepad-full      # Full version with list
notepad-demo      # Demo with all features
```

Or use direct paths:

```
Programs/notepad.gxm
Programs/notepad-full.gxm
Programs/notepad-demo.gxm
```

## GUI Script Format

The notepad GUI uses text-based script commands:

### Simple Version (notepad-simple.txt)

```
WINDOW|Text Editor|500|400
LABEL|Simple Notepad Demo|20|20
BUTTON|1|Save|20|350|100|30
BUTTON|4|Close|380|350|100|30
ONCLICK|1|MSG|Save functionality coming soon!
ONCLICK|4|CLOSE|
```

### Command Reference

- `WINDOW|Title|Width|Height` - Creates the main window
- `LABEL|Text|X|Y` - Adds a text label
- `BUTTON|ID|Text|X|Y|Width|Height` - Adds a clickable button
- `LIST|ID|X|Y|Width|Height|Item1;Item2;Item3` - Adds a selectable list
- `DROPDOWN|ID|X|Y|Width|Height|Item1;Item2` - Adds a dropdown menu
- `ONCLICK|ButtonID|ACTION|Argument` - Defines button click behavior
- `ONCHANGE|ControlID|ACTION|Argument` - Defines list/dropdown selection behavior

### Available Actions

- `MSG|Message` - Show a notification/message box
- `CLOSE|` - Close the window
- `OPENAPP|AppName` - Launch a built-in application (e.g., Notepad, Calculator)

### Special Tokens

- `$VALUE` - In ONCHANGE handlers, gets replaced with the selected item text

Example:
```
LIST|1|20|50|200|100|Red;Green;Blue
ONCHANGE|1|MSG|You selected: $VALUE
```

## Versions

### Simple (notepad-simple.txt)
- Basic layout with labels and buttons
- Demonstrates MSG and CLOSE actions
- File size: ~844 bytes

### Full (notepad-full.txt)
- Includes a LIST control for displaying text lines
- Dropdown for file type selection
- Button to open built-in C# Notepad
- File size: ~1092 bytes

### Demo (notepad-demo.txt)
- Comprehensive demonstration of all GXM features
- Multiple buttons organized by category
- Lists and dropdowns with working handlers
- Shows all available emoji icons
- Opens built-in Notepad for comparison
- File size: ~3312 bytes

## Customization

### Modify the Interface

Edit any `.txt` file to change:
- Window size and title
- Button positions, sizes, and labels
- Button actions and messages
- List items and dropdown options

After editing, rebuild:

```powershell
.\build.ps1
```

### Create a New Version

1. Copy an existing script file
2. Modify the commands as desired
3. Package it:
   ```powershell
   cd ..\GXMPackager
   dotnet run -- yourscript.txt yourapp.gxm --script yourscript.txt
   ```
4. Copy to Ramdisk and rebuild guideXOS

## Technical Details

### Why Buttons Now Work

The original script used `NOTIFY` action, which is **not supported** by the GXM script system. The correct actions are:

- ? `ONCLICK|1|NOTIFY|Message` - **Doesn't work**
- ? `ONCLICK|1|MSG|Message` - **Works correctly**

### Current Limitations

The GXM script system doesn't yet support:
- **TEXTBOX** control (text input field)
- **CHECKBOX** control
- **RADIO** buttons
- **SLIDER** control
- Scrollable lists
- Custom fonts or colors
- File dialogs

For full text editing, use the `OPENAPP|Notepad` action to launch the built-in C# Notepad application.

### Future Enhancements

When textbox support is added to `GXMScriptWindow.cs`, you'll be able to:

1. **Add Editable Text** - TEXTBOX control for typing
2. **Real File Operations** - Save/Load with filesystem access
3. **Text Manipulation** - Cut, copy, paste functionality
4. **Search Features** - Find and replace text

## Architecture

### Current: GUI Script Approach

```
notepad-simple.txt ? GXMPackager ? notepad.gxm ? Ramdisk ? guideXOS
```

- **Pros**: Simple, no compilation, easy to modify, small file size
- **Cons**: Limited to script features (no text input yet)

### Future: Hybrid Approach

When textbox support is added to GXMScriptWindow, the script-based notepad will become fully functional without needing compiled C# code!

## Troubleshooting

### Buttons Don't Respond

- ? **Fixed!** Make sure actions are: `MSG`, `OPENAPP`, or `CLOSE`
- ? Don't use `NOTIFY` - it's not a valid action
- Verify button IDs match between `BUTTON` and `ONCLICK` lines

### Wrong Action Used

If you see `NOTIFY` in any script file, change it to `MSG`:

```diff
- ONCLICK|1|NOTIFY|Hello!
+ ONCLICK|1|MSG|Hello!
```

### Message Box Doesn't Show

- Ensure the action is `MSG` (uppercase or lowercase both work)
- Check that the message text is provided after the pipe `|`

### Close Button Doesn't Work

Make sure it uses the `CLOSE` action:

```
BUTTON|99|Close|400|450|80|30
ONCLICK|99|CLOSE|
```

The argument after `CLOSE|` can be empty.

### Can't Open Built-in Notepad

Use the exact app name:

```
ONCLICK|10|OPENAPP|Notepad
```

Valid app names: `Notepad`, `Calculator`, `Paint`, etc. (check `App.cs` in guideXOS)

## Related Documentation

- [GXM GUI Scripting Guide](../../guideXOS/Docs/GXM_GUI_Scripting.md) - Official command reference
- [GXMScriptWindow.cs](../../guideXOS/guideXOS/GUI/GXMScriptWindow.cs) - Implementation source code
- [GXM Format Specification](../../guideXOS.com/Views/Wiki/GXMFormat.cshtml)
- [GXM Packager Tool](../../guideXOS.com/Views/Wiki/GXMPackager.cshtml)

## Testing Checklist

- [x] Simple notepad buttons work
- [x] Message boxes display correctly
- [x] Close button closes the window
- [x] Full version list is selectable
- [x] Dropdown shows items
- [x] ONCHANGE handlers trigger
- [x] $VALUE token replacement works
- [x] OPENAPP launches built-in Notepad
- [x] All three versions package successfully
- [x] File sizes are reasonable (<4KB)

## License

Part of guideXOS - see main project license.
