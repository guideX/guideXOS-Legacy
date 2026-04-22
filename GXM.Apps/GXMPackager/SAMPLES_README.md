# GXM Sample Scripts

This directory contains various sample GXM script files demonstrating different features and use cases.

## Basic Samples

### sample-gui.txt
A basic example showing all default window properties enabled.
- Demonstrates: WINDOW, LABEL, BUTTON, ONCLICK with NOTIFY and CLOSE actions
- Window Properties: All defaults (everything enabled)
- Use Case: Starting template for new applications

## Window Properties Samples

### sample-window-props.txt
Comprehensive demonstration of customized window properties.
- Window Properties: Mixed configuration showing each property's effect
- Use Case: Learning how each property affects window appearance
- Features: Detailed labels explaining each property setting

### sample-minimal-chrome.txt
A minimal window with all chrome features disabled.
- Window Properties: All set to `false`
- Use Case: Splash screens, simple notifications, minimal dialogs
- Features: Clean window with no resize, no buttons, no taskbar

### sample-chrome-showcase.txt
Interactive showcase of all chrome options.
- Window Properties: All enabled (full featured)
- Use Case: Testing and demonstrating window chrome capabilities
- Features: Buttons to simulate different window styles

### sample-about-dialog.txt
A typical "About" dialog box.
- Window Properties: Fixed size, no maximize/minimize/tombstone
- Use Case: About boxes, copyright notices, version info dialogs
- Features: Classic dialog styling with single OK button

## How to Use These Samples

### In GXM Packager GUI
1. Launch `GXMPackager.GUI.exe`
2. Click "Browse..." next to **Input File**
3. Select one of these sample files
4. Check **"Include GUI Script"** and select the same file
5. Choose an output location
6. Click **"Package GXM"**

### In GXM Packager Console
```bash
GXMPackager.exe sample-gui.txt output.gxm --script sample-gui.txt
```

### Testing in guideXOS
1. Copy the generated `.gxm` file to `guideXOS\Ramdisk\Programs\`
2. Rebuild the guideXOS ISO
3. Boot guideXOS
4. Run the app from console:
   ```
   cd Programs
   your-app-name
   ```

## Creating Your Own Scripts

### Starting from Scratch
1. Use `sample-gui.txt` as a template
2. Modify the WINDOW title and size
3. Customize window properties to match your needs
4. Add your UI elements (LABEL, BUTTON, LIST, DROPDOWN)
5. Wire up events with ONCLICK and ONCHANGE

### Using the GUI Editor
1. In GXM Packager GUI, click **"?? Create New Script"**
2. Use the **"Insert"** menu to add commands
3. Try **"Insert"** ? **"Complete Template"** for a full example
4. Customize as needed
5. Save and package

## Sample Comparison

| Sample | Resizable | Taskbar | Maximize | Minimize | Tombstone | Start Menu | Best For |
|--------|-----------|---------|----------|----------|-----------|------------|----------|
| sample-gui.txt | ? | ? | ? | ? | ? | ? | Full applications |
| sample-window-props.txt | ? | ? | ? | ? | ? | ? | Demonstrations |
| sample-minimal-chrome.txt | ? | ? | ? | ? | ? | ? | Splash screens |
| sample-chrome-showcase.txt | ? | ? | ? | ? | ? | ? | Testing/Demo |
| sample-about-dialog.txt | ? | ? | ? | ? | ? | ? | About boxes |

## Command Reference
For complete documentation of available commands, see:
- `guideXOS\Docs\GXM_GUI_Scripting.md` - Complete GXM scripting reference
- `guideXOS\Docs\GXM_Window_Properties.md` - Detailed window properties guide
- `guideXOS\Docs\GXM_Window_Properties_Quick_Reference.md` - Quick command lookup

## Common Patterns

### Fixed-Size Dialog
```
WINDOW|Settings|400|300
RESIZABLE|false
MAXIMIZE|false
MINIMIZE|false
```

### Background Utility
```
WINDOW|Monitor|250|300
TASKBAR|false
STARTMENU|false
MAXIMIZE|false
```

### Full Application
```
WINDOW|My App|600|400
RESIZABLE|true
TASKBAR|true
MAXIMIZE|true
MINIMIZE|true
```

### Popup/Toast
```
WINDOW|Notification|300|100
RESIZABLE|false
TASKBAR|false
MAXIMIZE|false
MINIMIZE|false
TOMBSTONE|false
STARTMENU|false
```

## Tips
- Start with a sample that's close to your desired result
- Window properties should come right after the WINDOW command
- Test your script by packaging and running in guideXOS
- Use the GXM Packager GUI editor for syntax highlighting and templates
- Keep controls within window bounds (X + Width < Window Width)

## Troubleshooting

**Issue:** My script doesn't work
- Check syntax: Commands must be UPPERCASE, fields separated by `|`
- Verify window properties use valid boolean values
- Ensure WINDOW command is first

**Issue:** Window looks wrong
- Verify width/height are reasonable (minimum 160x120)
- Check that property values are true/false (not True/False)
- Make sure there are no typos in property names

**Issue:** Can't find the app in guideXOS
- Verify the .gxm file is in `Ramdisk\Programs\`
- Rebuild the ISO after copying the file
- Check that the file has execute permissions
