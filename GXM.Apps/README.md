# GXM Apps Build System

Complete guide to building external GXM applications for guideXOS.

## Table of Contents

1. [Overview](#overview)
2. [Directory Structure](#directory-structure)
3. [Prerequisites](#prerequisites)
4. [Quick Start](#quick-start)
5. [GUI Script GXM Apps](#gui-script-gxm-apps)
6. [Native AOT GXM Apps](#native-aot-gxm-apps)
7. [GXM Packager Tool](#gxm-packager-tool)
8. [Troubleshooting](#troubleshooting)

---

## Overview

The GXM Apps build system allows you to create external applications for guideXOS in two ways:

### 1. **GUI Script GXM Apps** (Easiest)
- Simple text-based script format
- No compilation required
- Perfect for simple UI demos
- **Size:** ~50-200 bytes
- **Build time:** Instant

### 2. **Native AOT GXM Apps** (Advanced)
- Full C# applications compiled to native code
- Complete access to guideXOS APIs
- Production-quality applications
- **Size:** 50KB-500KB (depending on features)
- **Build time:** 1-5 minutes

---

## Directory Structure

```
GXM.Apps/
??? GXM.Apps.sln              # Solution file
??? build-all.ps1             # Master build script
??? README.md                 # This file
??? BUILD_INSTRUCTIONS.md     # Detailed build guide
?
??? GXMPackager/              # Packaging tool
?   ??? GXMPackager.csproj
?   ??? Program.cs
?   ??? sample-gui.txt        # Example GUI script
?
??? Notepad.GXM/              # Example app
    ??? Notepad.GXM.csproj
    ??? Program.cs
    ??? build.ps1             # App-specific build script
    ??? notepad-simple.txt    # GUI script version
```

---

## Prerequisites

### For GUI Script Apps (Minimal)
- **GXM Packager tool** (built automatically)
- Text editor

### For Native AOT Apps (Full Setup)
- **.NET SDK 9.0** or later
- **Visual Studio 2022** (optional, VS Code works too)
- **PowerShell 5.1** or later
- **guideXOS kernel** (for API reference)

---

## Quick Start

### Step 1: Build the GXM Packager

```powershell
cd GXM.Apps/GXMPackager
dotnet build -c Release
```

### Step 2: Create a Simple GUI Script App

Create `myapp.txt`:

```
WINDOW|My First App|400|300
LABEL|Hello, guideXOS!|20|50
BUTTON|1|Click Me|20|100|120|30
ONCLICK|1|NOTIFY|Button was clicked!
```

### Step 3: Package as GXM

```powershell
cd GXM.Apps
.\GXMPackager\bin\Release\net9.0\GXMPackager.exe myapp.txt myapp.gxm --script myapp.txt
```

### Step 4: Deploy to guideXOS

```powershell
copy myapp.gxm ..\guideXOS\Ramdisk\Programs\
```

### Step 5: Rebuild guideXOS and Test

```powershell
cd ..\guideXOS
# Rebuild your ISO with the new GXM file
# Boot guideXOS
# Run: launch myapp
```

---

## GUI Script GXM Apps

### Supported Commands

| Command | Format | Example |
|---------|--------|---------|
| **WINDOW** | `WINDOW\|title\|width\|height` | `WINDOW\|My App\|400\|300` |
| **LABEL** | `LABEL\|text\|x\|y` | `LABEL\|Hello!\|20\|50` |
| **BUTTON** | `BUTTON\|id\|text\|x\|y\|w\|h` | `BUTTON\|1\|Click\|20\|80\|100\|30` |
| **LIST** | `LIST\|id\|x\|y\|w\|h\|items` | `LIST\|1\|20\|50\|200\|100\|Item1;Item2;Item3` |
| **DROPDOWN** | `DROPDOWN\|id\|x\|y\|w\|h\|items` | `DROPDOWN\|1\|20\|50\|150\|25\|Opt1;Opt2;Opt3` |
| **ONCLICK** | `ONCLICK\|id\|action\|arg` | `ONCLICK\|1\|NOTIFY\|Clicked!` |
| **ONCHANGE** | `ONCHANGE\|id\|action\|arg` | `ONCHANGE\|1\|NOTIFY\|Changed!` |

### Available Actions

- **NOTIFY** - Show notification message
- **CLOSE** - Close the window
- **EXEC** - Execute a command (future)
- **LOADFILE** - Load a file (future)

### Example: Calculator UI

```
WINDOW|Calculator|300|400
LABEL|Result: 0|20|30
BUTTON|1|7|20|80|60|50
BUTTON|2|8|90|80|60|50
BUTTON|3|9|160|80|60|50
BUTTON|4|÷|230|80|60|50
BUTTON|5|4|20|140|60|50
BUTTON|6|5|90|140|60|50
BUTTON|7|6|160|140|60|50
BUTTON|8|×|230|140|60|50
ONCLICK|1|NOTIFY|7 pressed
ONCLICK|2|NOTIFY|8 pressed
```

---

## Native AOT GXM Apps

### Creating a New Native App

#### 1. Create Project Structure

```powershell
cd GXM.Apps
mkdir MyApp.GXM
cd MyApp.GXM
```

#### 2. Create Project File

`MyApp.GXM.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net9.0</TargetFramework>
    <AllowUnsafeBlocks>true</AllowUnsafeBlocks>
    
    <!-- NativeAOT Settings -->
    <PublishAot>true</PublishAot>
    <IlcOptimizationPreference>Size</IlcOptimizationPreference>
    <IlcSystemModule>guideXOS</IlcSystemModule>
    <LinkerSubsystem>NATIVE</LinkerSubsystem>
    <EntryPointSymbol>MyAppMain</EntryPointSymbol>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\guideXOS\guideXOS.csproj" />
  </ItemGroup>
</Project>
```

#### 3. Create Entry Point

`Program.cs`:

```csharp
using System.Runtime;
using guideXOS.GUI;
using guideXOS.DefaultApps;

namespace MyApp.GXM
{
    public static class Program
    {
        [RuntimeExport("MyAppMain")]
        public static void Main()
        {
            // Your app code here
            var window = new Window(200, 150, 400, 300)
            {
                Title = "My App",
                Visible = true
            };
            WindowManager.MoveToEnd(window);
        }
    }
}
```

#### 4. Build Script

`build.ps1`:

```powershell
# Build and package script
$appName = "myapp"

Write-Host "Building $appName..." -ForegroundColor Cyan

# Build with NativeAOT
dotnet publish -c Release

# Package as GXM
$binary = "bin\Release\net9.0\win-x64\publish\MyApp.GXM.exe"
$output = "$appName.gxm"
..\GXMPackager\bin\Release\net9.0\GXMPackager.exe $binary $output

# Copy to Ramdisk
copy $output ..\..\guideXOS\Ramdisk\Programs\

Write-Host "Done! GXM created at: $output" -ForegroundColor Green
```

#### 5. Build and Deploy

```powershell
.\build.ps1
```

### Using guideXOS APIs

Your native app can use all guideXOS APIs:

```csharp
// GUI System
using guideXOS.GUI;
var window = new Window(x, y, width, height);
var button = new Button(x, y, width, height, "Click Me");

// File System
using guideXOS.FS;
byte[] data = File.ReadAllBytes("/path/to/file");
File.WriteAllBytes("/path/to/file", data);

// Networking
using guideXOS.Compat;
var ip = NETv4.DNSQuery("example.com");
NETv4.ICMPPing(ip);

// Graphics
using guideXOS.Graph;
Framebuffer.Graphics.FillRectangle(x, y, w, h, color);
```

---

## GXM Packager Tool

### Usage

```powershell
GXMPackager <input> <output> [options]
```

### Options

| Option | Description | Example |
|--------|-------------|---------|
| `--entry <offset>` | Entry point RVA | `--entry 0x1000` |
| `--version <number>` | Version number | `--version 2` |
| `--script <file>` | Add GUI script | `--script app.txt` |

### Examples

```powershell
# Package a native binary
GXMPackager app.exe app.gxm --entry 0 --version 1

# Package a GUI script
GXMPackager demo.txt demo.gxm --script demo.txt

# Package with custom entry point
GXMPackager kernel.bin kernel.gxm --entry 0x8000 --version 3
```

### GXM File Format

```
Offset  | Size | Description
--------|------|-------------
0x00    | 4    | Magic: 'G', 'X', 'M', '\0'
0x04    | 4    | Version (u32, little-endian)
0x08    | 4    | Entry RVA (u32, little-endian)
0x0C    | 4    | Image size (u32, little-endian)
0x10    | 4    | Optional: 'G', 'U', 'I', '\0' (script marker)
0x14    | N    | Script data or binary image
```

---

## Troubleshooting

### "GXMPackager.exe not found"

```powershell
cd GXM.Apps/GXMPackager
dotnet build -c Release
```

### "NativeAOT build fails"

1. Ensure .NET SDK 9.0+ is installed:
   ```powershell
   dotnet --version
   ```

2. Check project reference to guideXOS:
   ```xml
   <ProjectReference Include="..\..\guideXOS\guideXOS.csproj" />
   ```

3. Verify IlcSystemModule is set correctly

### "GXM file doesn't launch"

1. Check the file was copied to `Ramdisk/Programs/`
2. Rebuild the guideXOS ISO to include new files
3. Use `gxminfo` command in guideXOS console to inspect:
   ```
   gxminfo Programs/myapp.gxm
   ```

### "App crashes on launch"

1. Check entry point symbol matches:
   ```xml
   <EntryPointSymbol>MyAppMain</EntryPointSymbol>
   ```
   ```csharp
   [RuntimeExport("MyAppMain")]
   public static void Main() { }
   ```

2. Ensure all dependencies are included in build

3. Test with simple GUI script first

---

## Next Steps

- See `BUILD_INSTRUCTIONS.md` for detailed NativeAOT setup
- Check example apps in `Notepad.GXM/`
- Read guideXOS API documentation
- Join the community for support

**Happy coding!** ??
