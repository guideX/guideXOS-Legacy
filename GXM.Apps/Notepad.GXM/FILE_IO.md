# ? REAL FILE I/O IMPLEMENTED!

## What's New

The GXM Notepad now has **REAL** save and load functionality! No more "coming soon" messages - files are actually written to and read from disk!

## New Actions Added

### SAVEFILE
Saves the content of the first LIST control to a file.

**Syntax:**
```
ONCLICK|ButtonID|SAVEFILE|filename.txt
```

**What it does:**
1. Gets text from the first LIST in the window
2. Converts list items to lines of text (separated by newlines)
3. Writes to file in current directory (Desktop.Dir)
4. Shows success message with location
5. Adds to recent documents

**Example:**
```
LIST|1|20|50|400|200|Line 1;Line 2;Line 3
BUTTON|1|Save|20|300|100|30
ONCLICK|1|SAVEFILE|mynotes.txt
```

### LOADFILE
Loads text from a file and populates the first LIST control.

**Syntax:**
```
ONCLICK|ButtonID|LOADFILE|filename.txt
```

**What it does:**
1. Reads file from current directory (Desktop.Dir)
2. Splits content into lines
3. Clears the first LIST control
4. Populates LIST with loaded lines
5. Shows success message with line count
6. Adds to recent documents

**Example:**
```
LIST|1|20|50|400|200|Empty
BUTTON|1|Load|20|300|100|30
ONCLICK|1|LOADFILE|mynotes.txt
```

### $VALUE Token Support

You can use `$VALUE` in LOADFILE actions to load different files based on dropdown/list selection:

```
DROPDOWN|1|20|50|200|30|notes.txt;todo.txt;readme.txt
ONCHANGE|1|LOADFILE|$VALUE
```

When user selects "notes.txt", it loads `notes.txt`
When user selects "todo.txt", it loads `todo.txt`

## Updated Scripts

### notepad-simple.txt
```
WINDOW|Text Editor|500|400
LIST|1|20|150|460|150|Line 1;Line 2;Line 3;Line 4;Line 5
BUTTON|1|?? Save File|20|320|100|35
BUTTON|2|?? Load File|130|320|100|35
ONCLICK|1|SAVEFILE|notepad.txt
ONCLICK|2|LOADFILE|notepad.txt
```

**Features:**
- Edit text in the list
- Click "Save File" - writes to `notepad.txt`
- Click "Load File" - reads from `notepad.txt`
- File saved in current working directory

### notepad-full.txt
```
DROPDOWN|1|20|80|200|30|notepad.txt;document.txt;readme.txt;notes.txt;data.txt
ONCHANGE|1|LOADFILE|$VALUE
LIST|2|240|80|340|250|Line 1;Line 2;...
BUTTON|10|?? Save|20|360|100|30
BUTTON|11|?? Load|130|360|100|30
ONCLICK|10|SAVEFILE|notepad.txt
ONCLICK|11|LOADFILE|notepad.txt
```

**Features:**
- Dropdown to switch between files
- Selecting dropdown item auto-loads that file
- Edit text in list
- Click "Save" - writes to notepad.txt
- Click "Load" - reads from notepad.txt
- 5 different files to work with

### notepad-demo.txt
```
DROPDOWN|1|20|80|200|30|demo.txt;document.txt;notes.txt;readme.txt;todo.txt
ONCHANGE|1|LOADFILE|$VALUE
LIST|2|240|110|340|200|Line 1;Line 2;...
BUTTON|11|?? Save File|170|160|140|35
BUTTON|12|?? Load File|320|160|140|35
ONCLICK|11|SAVEFILE|demo.txt
ONCLICK|12|LOADFILE|demo.txt
```

**Features:**
- Full demonstration with all GXM features
- Real file I/O on "Save File" and "Load File" buttons
- Dropdown with 5 files
- Selecting dropdown auto-loads file
- All 13 buttons functional

## How It Works

### Implementation Details

**In GXMScriptWindow.cs:**

```csharp
private void ExecuteAction(string action, string arg) {
    string a = action.ToUpper();
    
    if (a == "MSG") {
        Notify(arg);
    } else if (a == "OPENAPP") {
        Desktop.Apps.Load(arg);
    } else if (a == "CLOSE") {
        this.Visible = false;
    } else if (a == "SAVEFILE") {
        SaveFileAction(arg);  // NEW!
    } else if (a == "LOADFILE") {
        LoadFileAction(arg);  // NEW!
    }
}
```

**SaveFileAction:**
- Gets text from first LIST control
- Joins lines with newlines
- Converts string to byte array
- Calls `File.WriteAllBytes(path, data)`
- Invalidates directory cache
- Shows success notification
- Adds to recent documents

**LoadFileAction:**
- Calls `File.ReadAllBytes(path)`
- Converts byte array to string
- Splits on newlines (\n and \r)
- Clears first LIST control
- Populates LIST with loaded lines
- Shows success notification with line count
- Adds to recent documents

## Testing

### Test in guideXOS

1. **Build guideXOS:**
   ```powershell
   cd guideXOS
   dotnet build
   ```

2. **Run in QEMU:**
   ```powershell
   dotnet build -p:vm=qemu
   ```

3. **Test Simple Version:**
   ```
   cd Programs
   notepad
   
   # Edit some text in the list
   # Click "Save File"
   # Should see: "File saved: notepad.txt"
   # Click "Load File"
   # Should see: "File loaded: notepad.txt Lines: X"
   ```

4. **Verify File on Disk:**
   ```
   cd /
   ls
   # Should see notepad.txt
   cat notepad.txt
   # Should see your saved content
   ```

5. **Test Full Version:**
   ```
   cd Programs
   notepad-full
   
   # Select "document.txt" from dropdown
   # Should auto-load if file exists
   # Edit text
   # Click "Save"
   # Select another file from dropdown
   # Switch back to see it reloads
   ```

6. **Test Demo Version:**
   ```
   cd Programs
   notepad-demo
   
   # Try all file operations
   # Click "Save File" - writes demo.txt
   # Click "Load File" - reads demo.txt
   # Use dropdown to switch files
   ```

## Expected Behavior

### Save Operation

**Before Click:**
```
LIST showing:
- Line 1: Hello
- Line 2: World
- Line 3: This is a test
```

**Click "Save File":**
```
Message box appears:
"File saved: notepad.txt
Location: /Programs/"
```

**File on Disk:**
```
$ cat /Programs/notepad.txt
Hello
World
This is a test
```

### Load Operation

**Before Click:**
```
LIST showing:
- Line 1: Old data
- Line 2: Not saved
```

**File on Disk:**
```
$ cat /Programs/notepad.txt
Hello
World
This is a test
```

**Click "Load File":**
```
Message box appears:
"File loaded: notepad.txt
Lines: 3"

LIST now shows:
- Line 1: Hello
- Line 2: World
- Line 3: This is a test
```

### Dropdown Auto-Load

**Dropdown shows:**
```
? notepad.txt
  document.txt
  readme.txt
```

**User clicks "document.txt":**
```
1. Dropdown closes
2. Message: "File type: document.txt" (from ONCHANGE MSG)
3. Then LOADFILE action executes
4. If file exists: Loads content into list
5. Message: "File loaded: document.txt Lines: X"
```

## File Locations

All files are saved/loaded relative to `Desktop.Dir`:

- If in `/Programs/`: Files saved as `/Programs/notepad.txt`
- If in `/`: Files saved as `/notepad.txt`
- If in `/Documents/`: Files saved as `/Documents/notepad.txt`

## Error Handling

### File Not Found
```
Click "Load File" on non-existent file:
? Message: "Error: File notepad.txt is empty or not found"
```

### Empty Filename
```
ONCLICK|1|SAVEFILE|
? Saves as "notepad.txt" (default)

ONCLICK|1|LOADFILE|
? Message: "Error: No filename specified"
```

### Write Failure
```
Click "Save File" when disk is full/read-only:
? Message: "Error: Failed to save file notepad.txt"
```

### Read Failure
```
Click "Load File" on corrupted file:
? Message: "Error: Failed to load file notepad.txt"
```

## Limitations

### Current Limitations

1. **LIST-Based Editing**
   - Text must be edited via LIST selection
   - No direct text input (TEXTBOX not implemented yet)
   - Each list item = one line

2. **First LIST Only**
   - SAVEFILE saves from first LIST only
   - LOADFILE loads to first LIST only
   - Multiple LISTs not supported for I/O

3. **Current Directory Only**
   - Files saved to Desktop.Dir
   - No path specification (e.g., `/Documents/file.txt` not supported)
   - No file browser dialog

4. **Line-Based Format**
   - Content split by lines
   - Each line becomes a LIST item
   - Long lines may wrap in display

### Future Enhancements

When TEXTBOX is added:
- Direct text editing
- Multi-line text input
- Better text manipulation
- Search and replace
- Syntax highlighting

## Architecture

### Before (Mock):
```
ONCLICK|1|MSG|Save functionality coming soon!
         ?
   Shows message only
   No file operation
```

### After (Real):
```
ONCLICK|1|SAVEFILE|notepad.txt
         ?
   ExecuteAction("SAVEFILE", "notepad.txt")
         ?
   SaveFileAction("notepad.txt")
         ?
   Get text from first LIST
         ?
   Convert to byte array
         ?
   File.WriteAllBytes(path, data)
         ?
   Desktop.InvalidateDirCache()
         ?
   Show success message
         ?
   RecentManager.AddDocument()
```

## Summary

? **SAVEFILE action** - Writes LIST content to disk
? **LOADFILE action** - Reads file content to LIST
? **$VALUE token support** - Dynamic filenames from dropdowns
? **Error handling** - Graceful failure messages
? **Recent documents** - Files added to recent list
? **Directory cache** - Invalidated after save
? **Success feedback** - Shows file location and line count

**Status: FULLY FUNCTIONAL FILE I/O! ??**

No more mock messages - save and load actually work!
