# GXM Native Applications

This directory contains GXM script-based native applications for guideXOS.

## Available Applications

### 1. Notepad (`notepad.txt` ? `notepad.gxm`)
A simple text editor with the following features:
- **Large text editing area** (680x370 pixels)
- **Save As...** - Save your notes to a file
- **Open...** - Load existing text files
- **New** - Clear the text area for a fresh start
- **Close** - Exit the application
- **Window Properties**: Resizable, maximizable, minimizable, shows in taskbar and start menu

**Usage:**
1. Type your text in the main text area
2. Click "Save As..." to save your work
3. Click "Open..." to load existing files
4. Click "New" to clear and start fresh

---

### 2. Paint (`paint.txt` ? `paint.gxm`)
A simple drawing application interface with:
- **Color Palette**: Black, Red, Green, Blue, Yellow, White
- **Drawing Tools**: Pen, Eraser, Fill
- **File Operations**: Save and Load drawings
- **Clear Canvas** - Start with a fresh canvas
- **Window Properties**: Resizable, maximizable, minimizable, shows in taskbar and start menu

**Note:** This is a UI mockup. Full drawing functionality requires native implementation with canvas rendering support.

---

### 3. Calculator (`calculator.txt` ? `calculator.gxm`)
A standard calculator application with:
- **Number Pad**: 0-9 with decimal point
- **Operations**: Addition (+), Subtraction (-), Multiplication (*), Division (/)
- **Functions**: 
  - **=** - Calculate result
  - **C** - Clear all
- **Fixed Size Window** - Non-resizable for consistent button layout
- **Window Properties**: Shows in taskbar, start menu; minimizable but not resizable/maximizable

**Note:** This version provides the UI. Full calculation logic requires native implementation.

---

### 4. Image Viewer (`imageviewer.txt` ? `imageviewer.gxm`)
An image viewing application with:
- **Open Image...** - Load image files (PNG, BMP, etc.)
- **Zoom Controls**: 
  - Zoom In (125%)
  - Zoom Out (80%)
  - Fit to Window
  - Actual Size (100%)
- **Rotation**: Rotate left/right by 90°
- **Navigation**: Previous/Next for browsing multiple images
- **Window Properties**: Resizable, maximizable, minimizable, shows in taskbar and start menu

**Features:**
- Supports multiple image formats
- Zoom level indicator
- Image navigation controls

---

### 5. Welcome (`welcome.txt` ? `welcome.gxm`)
A welcome screen/getting started guide with:
- **Introduction** to guideXOS features
- **Getting Started** tips and keyboard shortcuts
- **Quick Launch Buttons** for featured applications:
  - Notepad
  - Calculator
  - File Manager
  - Web Browser
  - Image Viewer
  - Settings (placeholder)
- **Window Properties**: Resizable, maximizable, minimizable, shows in taskbar and start menu

**Purpose:**
- First-run experience for new users
- Quick access to commonly used applications
- Keyboard shortcut reference

---

## Building GXM Apps

### Using GXM Packager GUI

1. **Launch GXM Packager GUI**:
   ```powershell
   cd ..\GXMPackager.GUI
   dotnet run
   ```

2. **For each app** (e.g., `notepad.txt`):
   - Click **"Browse..."** next to Input File
   - Select the `.txt` file (e.g., `notepad.txt`)
   - Check **"Include GUI Script"**
   - Click **"Browse..."** next to GUI Script and select the same `.txt` file
   - Click **"Browse..."** next to Output GXM File and save as `.gxm` (e.g., `notepad.gxm`)
   - Click **"Package GXM"**

### Using GXM Packager CLI

```powershell
cd ..\GXMPackager

# Build all apps
dotnet run notepad.txt notepad.gxm --script notepad.txt
dotnet run paint.txt paint.gxm --script paint.txt
dotnet run calculator.txt calculator.gxm --script calculator.txt
dotnet run imageviewer.txt imageviewer.gxm --script imageviewer.txt
dotnet run welcome.txt welcome.gxm --script welcome.txt
```

### Batch Build Script

Create a `build-all.ps1` file:

```powershell
# Build all GXM apps
$apps = @("notepad", "paint", "calculator", "imageviewer", "welcome")

foreach ($app in $apps) {
    Write-Host "Building $app.gxm..." -ForegroundColor Cyan
    dotnet run --project ..\GXMPackager\GXMPackager.csproj $app.txt $app.gxm --script $app.txt
}

Write-Host "All apps built successfully!" -ForegroundColor Green
```

Run with:
```powershell
.\build-all.ps1
```

---

## Deploying to guideXOS

### Method 1: Manual Copy

1. **Build the apps** (see above)
2. **Copy `.gxm` files** to guideXOS Ramdisk:
   ```powershell
   Copy-Item *.gxm ..\..\..\guideXOS\Ramdisk\Programs\
   ```
3. **Rebuild guideXOS ISO**:
   ```powershell
   cd ..\..\..\guideXOS
   .\build.ps1
   ```

### Method 2: Automated Deployment

Create a `deploy.ps1` file:

```powershell
# Build all apps
.\build-all.ps1

# Copy to Ramdisk
Write-Host "Deploying to Ramdisk..." -ForegroundColor Cyan
Copy-Item *.gxm ..\..\..\guideXOS\Ramdisk\Programs\

# Rebuild ISO
Write-Host "Rebuilding guideXOS ISO..." -ForegroundColor Cyan
cd ..\..\..\guideXOS
.\build.ps1

Write-Host "Deployment complete!" -ForegroundColor Green
```

---

## Testing in guideXOS

1. **Boot guideXOS** in QEMU, VirtualBox, or real hardware
2. **From Console**:
   ```
   cd Programs
   notepad
   ```
3. **From Start Menu**: 
   - Click Start button
   - Navigate to application category
   - Click app name
4. **From Desktop**:
   - Create shortcuts to `.gxm` files (if supported)

---

## Integration with Existing Apps

### Replacing Native Implementations

To replace the current C# native apps with GXM versions:

1. **Update App.cs** in `guideXOS\OS\App.cs`:
   - Replace native app instantiation with GXM loader calls
   - Example:
   ```csharp
   case "Notepad": 
       // Old: _apps[i].AppObject = new Notepad(300, 200); b = true;
       // New:
       b = LaunchGXMFromFile("Programs/notepad.gxm", _apps[i].Icon);
       break;
   ```

2. **Update Start Menu** entries to point to GXM versions

3. **Remove or deprecate** old native implementations in `DefaultApps\`

### Benefits of GXM Apps

- ? **No Recompilation** - Update apps without rebuilding OS
- ? **Smaller Binary** - OS binary is smaller without bundled apps
- ? **Easy Customization** - Edit `.txt` scripts and repackage
- ? **Rapid Development** - No C# knowledge required for UI changes
- ? **Modular** - Apps are separate, loadable modules

---

## Advanced Features

### Adding New Controls

GXM scripts support:
- `LABEL` - Static text
- `BUTTON` - Clickable buttons
- `TEXTBOX` - Text input/editing
- `LIST` - List selection
- `DROPDOWN` - Dropdown menus

### Window Properties

Customize window behavior:
- `RESIZABLE|true/false` - Allow window resizing
- `TASKBAR|true/false` - Show in taskbar
- `MAXIMIZE|true/false` - Show maximize button
- `MINIMIZE|true/false` - Show minimize button
- `TOMBSTONE|true/false` - Enable tombstone (minimize to icon)
- `STARTMENU|true/false` - Show in Start Menu

### Event Handlers

Wire up interactions:
- `ONCLICK|ButtonId|Action|Argument`
  - Actions: `NOTIFY`, `CLOSE`, `OPENAPP`, `SAVEDIALOG`, `OPENDIALOG`, `CLEAR`
- `ONCHANGE|ControlId|Action|Argument`
- `ONTEXTCHANGE|TextBoxId|Action|Argument`

---

## Extending GXMLoader

To add full functionality (calculation, drawing, etc.), enhance `GXMLoader.cs`:

1. **Add action handlers** for calculator operations
2. **Implement canvas rendering** for Paint
3. **Add image loading/display** for Image Viewer
4. **Extend command set** with new actions

Example additions needed:
```csharp
// In ApplyGuiLine or new handler
else if(StringEquals(cmd,"CANVAS")) {
    // Create drawable canvas area
}
else if(action == "CALCULATE") {
    // Perform calculator operation
}
```

---

## Troubleshooting

### App doesn't appear in Start Menu
- Verify `STARTMENU|true` is set
- Check that `.gxm` file is in `Ramdisk\Programs\`
- Rebuild ISO after copying files

### Buttons don't respond
- Verify `ONCLICK|ButtonId|Action|Arg` syntax
- Check ButtonId matches the BUTTON definition
- Ensure action is supported (NOTIFY, CLOSE, OPENAPP, etc.)

### Window is too small/large
- Adjust `WINDOW|Title|Width|Height` dimensions
- Minimum: 160x120 pixels
- Check `RESIZABLE|true` to allow user resize

### Text is cut off
- Increase LABEL X/Y positions
- Increase WINDOW height
- Use shorter text or abbreviations

---

## Contributing

To add new applications:

1. **Create a `.txt` script** following GXM format
2. **Test locally** by packaging and running
3. **Document features** in this README
4. **Submit** to the guideXOS repository

---

## License

These applications are part of the guideXOS project and follow the same license terms.

## See Also

- `../GXMPackager/README.md` - GXM Packager documentation
- `../GXMPackager/SAMPLES_README.md` - Sample scripts reference
- `../../guideXOS/Kernel/Misc/GXMLoader.cs` - GXM loader implementation
- `../../guideXOS.com/Views/Wiki/GXMPackager.cshtml` - Online documentation
