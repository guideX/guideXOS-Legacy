# GXM Packager GUI - Quick Start Guide

## ?? Getting Started

### Launch the GUI

**From command line:**
```powershell
cd D:\devgitlab\guideXOS\GXM.Apps\GXMPackager.GUI
.\bin\Release\net9.0-windows\GXMPackager.GUI.exe
```

**From Visual Studio:**
1. Open `GXM.Apps.sln`
2. Set `GXMPackager.GUI` as startup project
3. Press F5

## ?? Quick Tutorial: Create Your First GUI Script GXM

### Step 1: Create a Script File

Create a new text file called `myapp.txt`:

```
WINDOW|My First GXM App|400|300
LABEL|Welcome to GXM Apps!|20|40
BUTTON|1|Say Hello|20|100|120|30
BUTTON|2|Show Info|160|100|120|30
BUTTON|3|Close|300|100|120|30
ONCLICK|1|NOTIFY|Hello from your first GXM app!
ONCLICK|2|NOTIFY|GXM apps are easy to create!
ONCLICK|3|CLOSE|
```

### Step 2: Package in GUI

1. **Launch GXM Packager GUI**
2. **Input File**: Browse and select `myapp.txt`
3. **Output GXM File**: Browse and choose where to save (e.g., `myapp.gxm`)
4. **Options**:
   - Entry Point: Leave as `0`
   - Version: Leave as `1`
   - ? Check "Include GUI Script"
   - Browse and select the same `myapp.txt` file
5. **Click "Package GXM"**
6. Watch the log output - you should see "Packaging complete!"

### Step 3: Test in guideXOS

1. Copy `myapp.gxm` to `D:\devgitlab\guideXOS\guideXOS\Ramdisk\Programs\`
2. Rebuild guideXOS ISO
3. Boot guideXOS
4. In console, run:
   ```
   cd Programs
   myapp
   ```

Your window should appear with three buttons!

## ?? GUI Features Explained

### Main Window

```
???????????????????????????????????????????
?         GXM Packager                    ?
? Package binary files and GUI scripts... ?
???????????????????????????????????????????
? Input File: [_______________] [Browse] ?
? Output GXM: [_______________] [Browse] ?
???????????????????????????????????????????
? Options:                                ?
?   Entry Point RVA: [0]    Version: [1] ?
?   ? Include GUI Script: [___] [Browse] ?
???????????????????????????????????????????
? Log:                                    ?
? ??????????????????????????????????????? ?
? ? [Black terminal-style output]       ? ?
? ??????????????????????????????????????? ?
? [Help] [Clear]          [Package GXM]   ?
???????????????????????????????????????????
```

### Input File
Select the file to package:
- For **GUI scripts**: Select your `.txt` script file
- For **binaries**: Select your compiled `.exe` or `.bin` file

### Output GXM File
Where to save the packaged GXM file

### Entry Point RVA
- **GUI scripts**: Use `0`
- **Binary apps**: Set to entry point offset (hex)
  - Example: `1000` or `0x1000`

### Version
GXM format version (default: 1)

### Include GUI Script
- Check this box for GUI script apps
- Browse to select the script file
- The packager adds the GUI marker automatically

## ?? Tips & Tricks

### Tip 1: Quick Script Testing
1. Keep the GUI open
2. Edit your script in a text editor
3. Click "Package GXM" again
4. Copy to Ramdisk and test immediately

### Tip 2: Entry Point Conversion
The GUI accepts hex values with or without `0x`:
- `1000` ?
- `0x1000` ?
- Both work the same!

### Tip 3: Log Output
The log shows detailed information:
- ? Green text = success
- ? Red = errors
- Copy log text for troubleshooting

### Tip 4: Script Validation
Before packaging, test your script syntax:
```
WINDOW|Title|Width|Height       ? Correct
WINDOW Title Width Height       ? Missing pipes
BUTTON|1|Text|X|Y|W|H          ? Correct  
BUTTON 1 Text X Y W H          ? Missing pipes
```

## ?? GUI Script Commands Reference

### Window
```
WINDOW|Title|Width|Height
```
Creates the main window. **Required** - must be first line.

### Label
```
LABEL|Text|X|Y
```
Static text display.

### Button
```
BUTTON|ID|Text|X|Y|Width|Height
```
Clickable button. ID must be unique.

### List
```
LIST|ID|X|Y|Width|Height|Item1;Item2;Item3
```
List box with selectable items.

### Dropdown
```
DROPDOWN|ID|X|Y|Width|Height|Option1;Option2;Option3
```
Dropdown/combo box.

### OnClick
```
ONCLICK|ButtonID|Action|Argument
```
Define button click behavior.

**Actions:**
- `NOTIFY` - Show notification
- `CLOSE` - Close window
- `EXEC` - Execute command (future)

### OnChange
```
ONCHANGE|ControlID|Action|Argument
```
Handle list/dropdown selection change.

## ?? Troubleshooting

### Problem: "Input file not found"
**Solution**: Use the Browse button to select files - avoids typos in paths

### Problem: "Invalid entry point"
**Solution**: 
- For GUI scripts, use `0`
- For binaries, enter hex value (e.g., `1000`)
- Don't use special characters

### Problem: GUI app doesn't launch in guideXOS
**Solution**:
1. Check "Include GUI Script" was checked
2. Verify script file was selected
3. Use `gxminfo Programs/myapp.gxm` in guideXOS to inspect
4. Look for `GUI` marker in output

### Problem: Window appears but no controls
**Solution**:
- Verify WINDOW command is first line
- Check all commands use pipe `|` separators
- Validate X,Y coordinates are within window bounds

## ?? Example Scripts to Try

### Hello World (Minimal)
```
WINDOW|Hello|300|200
LABEL|Hello World!|20|50
BUTTON|1|OK|20|100|100|30
ONCLICK|1|CLOSE|
```

### Calculator UI
```
WINDOW|Calculator|300|400
LABEL|Result: 0|20|30
BUTTON|1|7|20|80|60|50
BUTTON|2|8|90|80|60|50
BUTTON|3|9|160|80|60|50
BUTTON|4|÷|230|80|60|50
ONCLICK|1|NOTIFY|7
ONCLICK|2|NOTIFY|8
ONCLICK|3|NOTIFY|9
ONCLICK|4|NOTIFY|÷
```

### Text Editor UI
```
WINDOW|Notepad|500|400
LABEL|Simple Editor|20|20
BUTTON|1|Save|20|350|100|30
BUTTON|2|Load|140|350|100|30
BUTTON|3|Clear|260|350|100|30
ONCLICK|1|NOTIFY|Save clicked
ONCLICK|2|NOTIFY|Load clicked
ONCLICK|3|NOTIFY|Clear clicked
```

## ?? Next Steps

1. **Create more complex scripts** - Add lists and dropdowns
2. **Try binary packaging** - Package compiled NativeAOT apps
3. **Read full documentation** - See `../README.md` and `../BUILD_INSTRUCTIONS.md`
4. **Join the community** - Share your GXM apps!

---

**Happy GXM packaging!** ??
