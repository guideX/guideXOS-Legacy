# Detailed Build Instructions for GXM Apps

Step-by-step guide to building Native AOT GXM applications.

## Prerequisites Checklist

Before starting, ensure you have:

- [ ] .NET SDK 9.0 or later installed
- [ ] Visual Studio 2022 or VS Code
- [ ] PowerShell 5.1 or later
- [ ] Git (for cloning guideXOS)
- [ ] Administrator access (for some PowerShell scripts)

## Part 1: Setting Up the Environment

### Step 1: Verify .NET Installation

```powershell
# Check .NET SDK version
dotnet --version
# Should show 9.0.0 or later

# List installed SDKs
dotnet --list-sdks

# List installed runtimes
dotnet --list-runtimes
```

If .NET 9.0 is not installed:
1. Download from: https://dotnet.microsoft.com/download/dotnet/9.0
2. Install the SDK (not just runtime)
3. Restart your terminal/IDE

### Step 2: Clone or Update guideXOS

```powershell
# If you don't have guideXOS yet
cd D:\devgitlab
git clone https://gitlab.com/guideX/guideXOS
cd guideXOS

# If you already have it, update it
cd D:\devgitlab\guideXOS
git pull origin main
```

### Step 3: Build guideXOS Kernel

The kernel must be built first because GXM apps reference it:

```powershell
cd guideXOS
dotnet restore
dotnet build -c Release
```

This creates the guideXOS kernel APIs that your GXM apps will use.

## Part 2: Building the GXM Packager

### Step 1: Navigate to GXM.Apps

```powershell
cd D:\devgitlab\guideXOS\GXM.Apps
```

### Step 2: Build the Packager Tool

```powershell
cd GXMPackager
dotnet restore
dotnet build -c Release

# Verify it built successfully
dir bin\Release\net9.0\GXMPackager.exe
```

### Step 3: Test the Packager

```powershell
# Package the sample GUI script
.\bin\Release\net9.0\GXMPackager.exe sample-gui.txt test.gxm --script sample-gui.txt

# Check the output
dir test.gxm
# Should show a small file (~100 bytes)

# Clean up test file
del test.gxm
```

## Part 3: Creating Your First GUI Script GXM

### Step 1: Create a Simple Script

Create `hello.txt`:

```
WINDOW|Hello World|400|300
LABEL|Hello from my first GXM app!|20|50
BUTTON|1|Click Me|20|100|120|30
BUTTON|2|Close|160|100|120|30
ONCLICK|1|NOTIFY|Hello!
ONCLICK|2|CLOSE|
```

### Step 2: Package It

```powershell
cd D:\devgitlab\guideXOS\GXM.Apps

.\GXMPackager\bin\Release\net9.0\GXMPackager.exe hello.txt hello.gxm --script hello.txt
```

### Step 3: Deploy to guideXOS

```powershell
copy hello.gxm ..\guideXOS\Ramdisk\Programs\
```

### Step 4: Test in guideXOS

1. Rebuild guideXOS to include the new GXM in the ISO
2. Boot guideXOS in QEMU or real hardware
3. Open Console
4. Run: `cd Programs`
5. Run: `hello`
6. The window should appear!

## Part 4: Creating a Native AOT GXM App

### Step 1: Create Project Directory

```powershell
cd D:\devgitlab\guideXOS\GXM.Apps
mkdir SimpleApp.GXM
cd SimpleApp.GXM
```

### Step 2: Create Project File

Create `SimpleApp.GXM.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net9.0</TargetFramework>
    <AllowUnsafeBlocks>true</AllowUnsafeBlocks>
    <Nullable>enable</Nullable>
    
    <!-- NativeAOT compilation -->
    <PublishAot>true</PublishAot>
    <IlcOptimizationPreference>Size</IlcOptimizationPreference>
    <IlcGenerateStackTraceData>false</IlcGenerateStackTraceData>
    
    <!-- guideXOS-specific settings -->
    <IlcSystemModule>guideXOS</IlcSystemModule>
    <LinkerSubsystem>NATIVE</LinkerSubsystem>
    <EntryPointSymbol>SimpleAppMain</EntryPointSymbol>
  </PropertyGroup>

  <ItemGroup>
    <!-- Reference to guideXOS for APIs -->
    <ProjectReference Include="..\..\guideXOS\guideXOS.csproj" />
  </ItemGroup>
</Project>
```

### Step 3: Create Entry Point

Create `Program.cs`:

```csharp
using System.Runtime;
using guideXOS.GUI;

namespace SimpleApp.GXM
{
    public static class Program
    {
        /// <summary>
        /// Entry point - must be marked with RuntimeExport
        /// Symbol name must match EntryPointSymbol in project file
        /// </summary>
        [RuntimeExport("SimpleAppMain")]
        public static void Main()
        {
            // Create a simple window
            var window = new MessageBox(
                x: 300,
                y: 200,
                title: "Simple App",
                message: "Hello from a native GXM app!\nBuilt with NativeAOT."
            );
            
            WindowManager.MoveToEnd(window);
            window.Visible = true;
        }
    }
}
```

### Step 4: Create Build Script

Create `build.ps1`:

```powershell
param([string]$Configuration = "Release")

Write-Host "Building SimpleApp.GXM..." -ForegroundColor Cyan

# Step 1: Clean
Write-Host "  [1/4] Cleaning..." -ForegroundColor Yellow
if (Test-Path bin) { Remove-Item bin -Recurse -Force }
if (Test-Path obj) { Remove-Item obj -Recurse -Force }

# Step 2: Restore
Write-Host "  [2/4] Restoring packages..." -ForegroundColor Yellow
dotnet restore

# Step 3: Publish with NativeAOT
Write-Host "  [3/4] Publishing (this may take a few minutes)..." -ForegroundColor Yellow
dotnet publish -c $Configuration

# Check if build succeeded
$binary = "bin\$Configuration\net9.0\win-x64\publish\SimpleApp.GXM.exe"
if (!(Test-Path $binary)) {
    Write-Host "  Build failed!" -ForegroundColor Red
    exit 1
}

$size = (Get-Item $binary).Length
Write-Host "  Binary created: $size bytes" -ForegroundColor Green

# Step 4: Package as GXM
Write-Host "  [4/4] Packaging..." -ForegroundColor Yellow
$packager = "..\GXMPackager\bin\$Configuration\net9.0\GXMPackager.exe"
$output = "simpleapp.gxm"

& $packager $binary $output --entry 0 --version 1

if ($LASTEXITCODE -eq 0) {
    Write-Host "  Success! Created: $output" -ForegroundColor Green
    
    # Copy to Ramdisk
    $dest = "..\..\guideXOS\Ramdisk\Programs\simpleapp.gxm"
    copy $output $dest -Force
    Write-Host "  Copied to: $dest" -ForegroundColor Green
}

Write-Host ""
Write-Host "Done! To test:" -ForegroundColor Cyan
Write-Host "  1. Rebuild guideXOS ISO" -ForegroundColor White
Write-Host "  2. Boot guideXOS" -ForegroundColor White
Write-Host "  3. Run: cd Programs" -ForegroundColor White
Write-Host "  4. Run: simpleapp" -ForegroundColor White
```

### Step 5: Build It

```powershell
# Make sure GXMPackager is built first
cd ..\GXMPackager
dotnet build -c Release
cd ..\SimpleApp.GXM

# Run the build script
.\build.ps1
```

This will:
1. Clean previous builds
2. Restore NuGet packages
3. Compile with NativeAOT (takes 1-5 minutes first time)
4. Package as GXM
5. Copy to Ramdisk

### Step 6: Add to Solution (Optional)

Edit `GXM.Apps.sln` and add your project:

```xml
Project("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = "SimpleApp.GXM", "SimpleApp.GXM\SimpleApp.GXM.csproj", "{GUID-HERE}"
EndProject
```

## Part 5: Using guideXOS APIs

### File I/O Example

```csharp
using guideXOS.FS;
using System;

[RuntimeExport("FileAppMain")]
public static void Main()
{
    // Read file
    byte[] data = File.ReadAllBytes("/path/to/file.txt");
    if (data != null)
    {
        // Convert bytes to string
        char[] chars = new char[data.Length];
        for (int i = 0; i < data.Length; i++)
        {
            chars[i] = (char)data[i];
        }
        string content = new string(chars);
        
        // Show in message box
        var msg = new MessageBox(300, 200, "File Content", content);
        msg.Visible = true;
        
        data.Dispose();
    }
}
```

### Networking Example

```csharp
using guideXOS.Compat;
using System;

[RuntimeExport("NetAppMain")]
public static void Main()
{
    // DNS query
    var ip = NETv4.DNSQuery("example.com");
    
    // Ping
    NETv4.ICMPPing(ip);
    
    // Wait for response (in real app, use timer/callback)
    // Show result in UI
}
```

### GUI Example

```csharp
using guideXOS.GUI;
using guideXOS.DefaultApps;

[RuntimeExport("NotepadAppMain")]
public static void Main()
{
    // Use built-in Notepad class
    var notepad = new Notepad(300, 220);
    notepad.Visible = true;
    WindowManager.MoveToEnd(notepad);
}
```

## Part 6: Debugging

### Build Errors

#### "IlcSystemModule not found"

Your `guideXOS.csproj` needs this property:

```xml
<PropertyGroup>
  <IlcSystemModule>guideXOS</IlcSystemModule>
</PropertyGroup>
```

#### "Entry point not found"

Make sure your entry point:
1. Has `[RuntimeExport("YourSymbol")]` attribute
2. Matches `<EntryPointSymbol>YourSymbol</EntryPointSymbol>` in .csproj
3. Is a public static void method with no parameters

#### "NativeAOT compilation failed"

Common causes:
- Missing .NET 9.0 SDK
- Project reference to guideXOS is incorrect
- Build configuration mismatch (Debug vs Release)

Try:
```powershell
dotnet clean
dotnet restore --force
dotnet publish -c Release -v detailed
```

### Runtime Errors

#### "GXM fails to load"

1. Check GXM file format:
   ```powershell
   cd guideXOS
   # In console: gxminfo Programs/yourapp.gxm
   ```

2. Verify entry point RVA is correct (usually 0 for simple apps)

3. Check file permissions and size

#### "App crashes immediately"

1. Simplify entry point to minimal code:
   ```csharp
   [RuntimeExport("TestMain")]
   public static void Main()
   {
       // Just show a message box
       var msg = new MessageBox(300, 200, "Test", "If you see this, entry point works!");
       msg.Visible = true;
   }
   ```

2. Gradually add functionality back

3. Check for null references or uninitialized objects

## Part 7: Advanced Topics

### Memory Management

guideXOS uses manual memory management:

```csharp
// Allocate
byte[] data = new byte[1024];

// Use it
// ...

// IMPORTANT: Explicitly dispose when done
data.Dispose();
```

Always dispose:
- Byte arrays
- Images
- File data
- Network buffers

### Performance Optimization

1. **Size Optimization:**
   ```xml
   <IlcOptimizationPreference>Size</IlcOptimizationPreference>
   <IlcGenerateStackTraceData>false</IlcGenerateStackTraceData>
   ```

2. **Speed Optimization:**
   ```xml
   <IlcOptimizationPreference>Speed</IlcOptimizationPreference>
   ```

3. **Link-Time Optimization:**
   ```xml
   <IlcInvariantGlobalization>true</IlcInvariantGlobalization>
   ```

### Sharing Code Between Apps

Create a shared library:

```
GXM.Apps/
??? Shared/
?   ??? Shared.csproj
?   ??? Utils.cs
??? App1.GXM/
?   ??? (references Shared)
??? App2.GXM/
    ??? (references Shared)
```

## Troubleshooting Checklist

- [ ] .NET 9.0 SDK installed and in PATH
- [ ] guideXOS kernel built successfully
- [ ] GXMPackager built and executable
- [ ] Project references correct (relative paths)
- [ ] Entry point symbol matches
- [ ] RuntimeExport attribute present
- [ ] Build configuration matches (Debug/Release)
- [ ] GXM file copied to Ramdisk/Programs/
- [ ] guideXOS ISO rebuilt to include new GXM
- [ ] Tested in fresh boot (not stale cache)

## Getting Help

1. Check build output for specific error messages
2. Review example apps in `Notepad.GXM/`
3. Read guideXOS kernel source for API usage
4. Search GitLab issues
5. Ask in community forums

---

**Success!** You're now ready to build GXM apps for guideXOS! ??
