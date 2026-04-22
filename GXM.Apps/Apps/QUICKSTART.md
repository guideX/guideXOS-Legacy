# Quick Start Guide - GXM Native Apps

Get your GXM apps running in guideXOS in 5 minutes!

## Prerequisites

- .NET 9.0 SDK installed
- guideXOS repository cloned
- PowerShell (for .ps1 scripts) or Command Prompt (for .bat files)

## Step 1: Navigate to Apps Directory

```powershell
cd D:\devgitlab\guideXOS\GXM.Apps\Apps
```

## Step 2: Build All Apps

### Option A: PowerShell (Recommended)
```powershell
.\build-all.ps1
```

### Option B: Batch File
```cmd
build-all.bat
```

### Option C: Manual Build
```powershell
dotnet run --project ..\GXMPackager\GXMPackager.csproj notepad.txt notepad.gxm --script notepad.txt
dotnet run --project ..\GXMPackager\GXMPackager.csproj paint.txt paint.gxm --script paint.txt
dotnet run --project ..\GXMPackager\GXMPackager.csproj calculator.txt calculator.gxm --script calculator.txt
dotnet run --project ..\GXMPackager\GXMPackager.csproj imageviewer.txt imageviewer.gxm --script imageviewer.txt
dotnet run --project ..\GXMPackager\GXMPackager.csproj welcome.txt welcome.gxm --script welcome.txt
```

**Expected Output:**
```
Building GXM Native Applications...
=====================================

Building notepad.gxm...
  SUCCESS: notepad.gxm created

Building paint.gxm...
  SUCCESS: paint.gxm created

...
```

## Step 3: Deploy to guideXOS

### Option A: Automated Deployment (Recommended)
```powershell
.\deploy.ps1
```

This will:
1. Build all apps
2. Copy `.gxm` files to `guideXOS\Ramdisk\Programs\`
3. Rebuild the guideXOS ISO

### Option B: Manual Deployment
```powershell
# Copy files
Copy-Item *.gxm ..\..\..\guideXOS\Ramdisk\Programs\

# Rebuild ISO
cd ..\..\..\guideXOS
.\build.ps1
```

## Step 4: Test in guideXOS

### Boot guideXOS
```powershell
# If using QEMU
cd guideXOS
.\run.ps1
```

### Launch Apps

**From Console:**
```
cd Programs
notepad
```

**From Start Menu:**
1. Click Start button (bottom-left)
2. Navigate to application category
3. Click "Notepad" (or other app name)

**Expected Result:** App window opens with UI as defined in the script!

---

## Troubleshooting

### Error: "dotnet: command not found"
**Solution:** Install .NET 9.0 SDK from https://dotnet.microsoft.com/download

### Error: "File not found: notepad.txt"
**Solution:** Ensure you're in the `GXM.Apps\Apps\` directory

### Error: "GXMPackager.csproj not found"
**Solution:** Check that GXMPackager project exists in `GXM.Apps\GXMPackager\`

### Apps don't appear in guideXOS
**Solution:** 
1. Verify `.gxm` files are in `guideXOS\Ramdisk\Programs\`
2. Rebuild the ISO: `cd guideXOS && .\build.ps1`
3. Reboot guideXOS

### App window is blank
**Solution:**
1. Check `.txt` script syntax
2. Ensure all commands are UPPERCASE
3. Verify field separators are `|` (pipe character)

---

## Next Steps

### Customize Apps
1. Edit `.txt` files with your favorite text editor
2. Modify window sizes, button positions, labels
3. Rebuild: `.\build-all.ps1`
4. Redeploy: `.\deploy.ps1`

### Create New Apps
1. Copy an existing `.txt` as template
2. Modify WINDOW, BUTTON, LABEL commands
3. Add to `build-all.ps1` script
4. Build and deploy

### Advanced Features
- Add TEXTBOX for input fields
- Use LIST/DROPDOWN for selections
- Wire ONCLICK events to actions
- Customize window properties (RESIZABLE, TASKBAR, etc.)

---

## Command Reference

### Window Commands
```
WINDOW|Title|Width|Height          - Create main window
RESIZABLE|true/false                - Allow resizing
TASKBAR|true/false                  - Show in taskbar
MAXIMIZE|true/false                 - Show maximize button
MINIMIZE|true/false                 - Show minimize button
TOMBSTONE|true/false                - Enable tombstone mode
STARTMENU|true/false                - Show in start menu
```

### Control Commands
```
LABEL|Text|X|Y                                    - Static text
BUTTON|Id|Text|X|Y|Width|Height                   - Clickable button
TEXTBOX|Id|X|Y|Width|Height|InitialText           - Text input
LIST|Id|X|Y|Width|Height|Item1;Item2;Item3        - List box
DROPDOWN|Id|X|Y|Width|Height|Item1;Item2;Item3    - Dropdown menu
```

### Event Commands
```
ONCLICK|ButtonId|Action|Argument          - Button click handler
ONCHANGE|ControlId|Action|Argument        - Selection change handler
ONTEXTCHANGE|TextBoxId|Action|Argument    - Text change handler
```

### Actions
```
NOTIFY|Message          - Show notification
CLOSE|                  - Close window
OPENAPP|AppName         - Launch another app
SAVEDIALOG|DefaultFile  - Open save dialog
OPENDIALOG|             - Open file dialog
CLEAR|ControlId         - Clear control content
```

---

## Example: Hello World App

Create `hello.txt`:
```
WINDOW|Hello World|400|200
LABEL|Hello from GXM!|100|50
BUTTON|1|Click Me|100|90|120|30
BUTTON|2|Exit|230|90|120|30
ONCLICK|1|NOTIFY|Button clicked!
ONCLICK|2|CLOSE|
```

Build and deploy:
```powershell
dotnet run --project ..\GXMPackager\GXMPackager.csproj hello.txt hello.gxm --script hello.txt
Copy-Item hello.gxm ..\..\..\guideXOS\Ramdisk\Programs\
```

---

## Support

- Documentation: `README.md` in this directory
- Sample Scripts: `..\GXMPackager\sample-*.txt`
- GXM Loader Source: `..\..\guideXOS\Kernel\Misc\GXMLoader.cs`
- Wiki: `https://guidexos.com/Wiki/GXMPackager`

---

## Tips

? **Start simple** - Begin with basic WINDOW, LABEL, BUTTON
? **Test frequently** - Build and test after each change
? **Use samples** - Copy from existing sample scripts
? **Check syntax** - Commands must be UPPERCASE, use `|` separators
? **Plan layout** - Sketch UI on paper before coding
? **Consistent spacing** - Use 10-20px padding for clean look

---

Ready to build your first GXM app? Run `.\build-all.ps1` and get started! ??
