# LiveMode and Configuration System Implementation

## Overview

A LiveMode detection and configuration persistence system has been implemented for guideXOS. This system automatically detects whether the OS is running from read-only media (USB/CD) or from an installed hard drive, and enables/disables persistent configuration saving accordingly.

## Components

### 1. SystemMode.cs (`guideXOS\OS\SystemMode.cs`)

**Purpose**: Detects and tracks the system's execution mode.

**Key Features**:
- `IsLiveMode` property: Returns `true` if running from USB/CD (no persistence), `false` if installed
- `BootDevice` property: Returns the boot device identifier ("ramdisk" or "/dev/sda2")
- `DetectMode()`: Automatically detects mode by checking for `/boot/config.txt`
- `CanWriteSettings()`: Tests if storage is writable
- `ForceLiveMode()`: Allows manual mode override for testing

**Detection Logic**:
```csharp
if (File.Exists("/boot/config.txt")) {
    // Installed mode - boot config exists
    _isLiveMode = false;
    _bootDevice = "/dev/sda2";
} else {
    // LiveMode - running from ramdisk
    _isLiveMode = true;
    _bootDevice = "ramdisk";
}
```

### 2. Configuration.cs (`guideXOS\OS\Configuration.cs`)

**Purpose**: Manages persistent configuration saving and loading.

**Key Features**:
- `Initialize()`: Sets up configuration system at boot
- `LoadConfiguration()`: Loads saved settings from disk
- `SaveConfiguration()`: Saves current settings to disk
- Automatic LiveMode detection - no saves in LiveMode
- Simple INI-style configuration format

**Saved Settings**:
- Desktop icon size
- Widget visibility on startup
- Animation settings (fade, slide)
- Background rotation settings

**Configuration File Location**: `/etc/guidexos/config.ini`

**Configuration Format**:
```ini
# guideXOS Configuration
icon_size=48
show_widgets_on_startup=false
enable_fade_animations=false
enable_window_slide_animations=false
enable_auto_background_rotation=false
background_rotation_interval_minutes=5
```

### 3. Visual Indicators

#### Taskbar LiveMode Badge
A prominent orange "LIVE" badge is displayed in the taskbar when running in LiveMode:
- **Position**: Left of network indicator, right of clock
- **Color**: Orange background (#FF6B35) with brighter border (#FFAA00)
- **Visibility**: Only shown when `SystemMode.IsLiveMode == true`

#### Desktop Context Menu
A "Save Settings" option is added to the right-click desktop context menu:
- **Visibility**: Only shown when NOT in LiveMode
- **Function**: Manually triggers configuration save
- **Feedback**: Shows confirmation message via MessageBox

## Integration Points

### EntryPoint.cs
```csharp
// Detect system mode (LiveMode vs Installed)
guideXOS.OS.SystemMode.DetectMode();

// Initialize configuration system (only works when not in LiveMode)
guideXOS.OS.Configuration.Initialize();
```

### Program.cs
```csharp
// Load saved configuration (UI settings, window positions, recent files, etc.)
guideXOS.OS.Configuration.LoadConfiguration();
```

### Taskbar.cs
```csharp
// LiveMode indicator (left of network indicator)
int liveModeWidth = 0;
if (guideXOS.OS.SystemMode.IsLiveMode) {
    string liveText = "LIVE";
    // ... draw orange badge ...
}
```

### RightMenu.cs
```csharp
// Save Settings (only when not in LiveMode)
if (!guideXOS.OS.SystemMode.IsLiveMode) {
    if (Hit(currentItem, mx, my, itemH)) {
        guideXOS.OS.Configuration.SaveConfiguration();
        // Show confirmation message
    }
}
```

## Usage Scenarios

### Scenario 1: Boot from USB (LiveMode)
1. System boots from USB flash drive
2. No `/boot/config.txt` exists ? LiveMode detected
3. "LIVE" badge appears in taskbar
4. No "Save Settings" option in context menu
5. All settings changes are temporary (RAM only)
6. Settings are lost on reboot

### Scenario 2: Installed to Hard Drive
1. System boots from HDD (via HDInstaller)
2. `/boot/config.txt` exists ? Installed mode detected
3. No "LIVE" badge in taskbar
4. "Save Settings" option available in context menu
5. Configuration is loaded from `/etc/guidexos/config.ini` at startup
6. Settings can be saved to disk and persist across reboots

### Scenario 3: Manual Configuration Save
1. User right-clicks on desktop
2. Selects "Save Settings" (only visible when installed)
3. Current settings are written to `/etc/guidexos/config.ini`
4. Confirmation message appears: "Settings saved successfully!"
5. Settings will be restored on next boot

## Future Enhancements

### Phase 1: Additional Settings
- Window positions and sizes
- Recent files list
- Desktop wallpaper path
- Taskbar preferences
- Keyboard/mouse sensitivity

### Phase 2: Advanced Configuration
- Per-window state persistence
- Application-specific settings
- User profiles
- Multi-user support

### Phase 3: Auto-Save
- Periodic automatic saves (every 5 minutes)
- Save on clean shutdown
- Save on settings change
- Crash recovery

### Phase 4: Configuration UI
- Settings application
- Import/Export configuration
- Reset to defaults
- Configuration backup/restore

## Testing

### Test LiveMode Detection
```csharp
// Force LiveMode for testing
SystemMode.ForceLiveMode(true);
Console.WriteLine($"LiveMode: {SystemMode.IsLiveMode}");
Console.WriteLine($"Boot Device: {SystemMode.BootDevice}");
```

### Test Configuration Save/Load
```csharp
// Save current settings
Configuration.SaveConfiguration();

// Modify settings
UISettings.ShowWidgetsOnStartup = !UISettings.ShowWidgetsOnStartup;

// Reload from disk
Configuration.LoadConfiguration();

// Verify setting was restored
Console.WriteLine($"Widgets on startup: {UISettings.ShowWidgetsOnStartup}");
```

### Test Write Capability
```csharp
bool canWrite = SystemMode.CanWriteSettings();
Console.WriteLine($"Storage writable: {canWrite}");
```

## Implementation Notes

### Custom String API Compatibility
The configuration system is designed to work with guideXOS's custom string implementation, which has limited API compared to full .NET:
- Manual string searching (no `string.IndexOf(string)`)
- Manual integer parsing (no `int.TryParse`)
- Manual boolean parsing (no `bool.TryParse`)
- Simple line-by-line parsing (no robust INI library)

### Memory Management
The system carefully manages string disposal to prevent memory leaks:
```csharp
string content = GetStringFromBytes(data);
data.Dispose();

string value = FindValue(content, "key");
if (value != null) {
    // Use value
    value.Dispose();
}

content.Dispose();
```

### Error Handling
All operations are wrapped in try-catch blocks to prevent crashes:
- Failed detection defaults to LiveMode (safer)
- Failed loads use default values
- Failed saves log errors but don't crash
- Console messages provide debugging information

## Console Output

### LiveMode (USB Boot)
```
[BOOT] System Mode: LiveMode (USB/CD)
[CONFIG] Initializing configuration system...
[CONFIG] LiveMode detected - settings will not be saved
[CONFIG] Configuration system initialized
```

### Installed Mode (HDD Boot)
```
[BOOT] System Mode: Installed (/dev/sda2)
[CONFIG] Initializing configuration system...
[CONFIG] UI settings loaded
[CONFIG] Configuration loaded from disk
[CONFIG] Configuration system initialized
```

### Manual Save
```
[CONFIG] UI settings saved
[CONFIG] Configuration saved to disk
```

## Benefits

1. **User Clarity**: Visual indicator shows when settings can be saved
2. **Data Safety**: Prevents confusion about why settings aren't persisting
3. **Flexibility**: Works in both live and installed environments
4. **Simplicity**: Automatic detection requires no user configuration
5. **Extensibility**: Easy to add new settings to the system

## Conclusion

The LiveMode and Configuration system provides a foundation for persistent settings in guideXOS. It intelligently detects the boot environment and adjusts behavior accordingly, providing clear visual feedback to users about the system's capabilities.
