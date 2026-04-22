# Built-in Script Editor Guide

## Overview

The GXM Packager GUI now includes a powerful built-in script editor that makes creating GUI scripts fast and easy!

## Opening the Editor

### Method 1: Create New Script (Recommended)
Click the **"?? Create New Script"** button (large green button in Options section)

### Method 2: Edit Existing Script
1. Check "Include GUI Script"
2. Browse for a script file (or leave blank)
3. Click **"Edit..."** button

## Editor Interface

```
????????????????????????????????????????????????????????????
? [New] [Open] ? [Save] [Save As] ? [Insert ?] ?    [Help] ?
????????????????????????????????????????????????????????????
?                                                          ?
?  WINDOW|My App|400|300                                  ?
?  LABEL|Hello World!|20|50                               ?
?  BUTTON|1|Click Me|20|100|120|30                        ?
?  ONCLICK|1|NOTIFY|Hello!                                ?
?                                                          ?
?  (Dark theme, monospace font)                           ?
?                                                          ?
????????????????????????????????????????????????????????????
? Line 4, Col 23                        156 characters     ?
????????????????????????????????????????????????????????????
?                                          [OK] [Cancel]    ?
????????????????????????????????????????????????????????????
```

## Quick Start Tutorial

### Create Your First Script in 1 Minute

1. **Click "Create New Script"**
2. **Click "Insert" ? "Complete Template"**
   
   This inserts:
   ```
   WINDOW|My Application|400|300
   LABEL|Welcome to my app!|20|40
   BUTTON|1|Click Me|20|100|120|30
   BUTTON|2|Show Info|160|100|120|30
   BUTTON|3|Close|300|100|120|30
   ONCLICK|1|NOTIFY|Button 1 clicked!
   ONCLICK|2|NOTIFY|Information message
   ONCLICK|3|CLOSE|
   ```

3. **Customize the text** (optional)
4. **Press Ctrl+S** (or click Save)
5. **Name your file** (e.g., `myapp.txt`)
6. **Click OK** - Done!

The main window will automatically:
- Set input file to your script
- Enable "Include GUI Script"
- Set script path
- Suggest output filename

## Insert Menu Templates

Click **"Insert"** in the toolbar to quickly add:

### Individual Commands

| Template | What It Inserts |
|----------|----------------|
| **Window** | `WINDOW\|My Window\|400\|300` |
| **Label** | `LABEL\|Text here\|20\|50` |
| **Button** | `BUTTON\|1\|Click Me\|20\|100\|120\|30` |
| **List** | `LIST\|1\|20\|150\|200\|100\|Item1;Item2;Item3` |
| **Dropdown** | `DROPDOWN\|1\|20\|200\|150\|25\|Option1;Option2;Option3` |
| **OnClick** | `ONCLICK\|1\|NOTIFY\|Message here` |
| **Complete Template** | Full working example (see above) |

### How to Use Templates

1. **Position cursor** where you want to insert
2. Click **"Insert"**
3. Choose a template
4. **Customize** the inserted text
5. Repeat for more commands

## Keyboard Shortcuts

| Shortcut | Action |
|----------|--------|
| **Ctrl+N** | New file (clears editor) |
| **Ctrl+O** | Open existing script |
| **Ctrl+S** | Save current script |
| **Ctrl+Shift+S** | Save as (new name) |
| **Tab** | Insert tab character |

## Features

### Auto-Save Protection
- Prompted when closing with unsaved changes
- Asterisk (*) appears in title when modified
- Never lose your work!

### Status Bar
Shows real-time information:
- **Line number** and **column** position
- **Character count**
- Updates as you type

### Dark Theme
- Easy on the eyes
- Professional coding environment
- Monospace font (Consolas)

### Help System
Click **"Help"** button in editor toolbar for:
- Complete command reference
- Syntax examples
- Tips and tricks

## Workflow Examples

### Example 1: Quick Hello World

1. Create New Script
2. Type:
   ```
   WINDOW|Hello|300|200
   LABEL|Hello World!|20|50
   BUTTON|1|OK|20|100|100|30
   ONCLICK|1|CLOSE|
   ```
3. Save (Ctrl+S) as `hello.txt`
4. Click OK
5. Package!

**Time: 30 seconds** ?

### Example 2: Calculator UI

1. Create New Script
2. Insert ? Complete Template
3. Modify to create calculator:
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
4. Save as `calc.txt`
5. Package!

**Time: 2 minutes** ?

### Example 3: Form with List

1. Create New Script
2. Build form step-by-step:
   - Insert ? Window
   - Insert ? Label (for title)
   - Insert ? List (for items)
   - Insert ? Button (for actions)
   - Insert ? OnClick (for each button)
3. Customize each command
4. Save as `form.txt`
5. Package!

**Time: 3 minutes** ?

## Tips & Tricks

### Tip 1: Copy & Paste Buttons
When creating multiple buttons:
1. Insert one button
2. Copy the line (Ctrl+C)
3. Paste it (Ctrl+V)
4. Change the ID and position

### Tip 2: Align Your Code
Use spaces to align the pipe separators:
```
BUTTON|1  |Save   |20 |100|100|30
BUTTON|2  |Load   |140|100|100|30
BUTTON|3  |Clear  |260|100|100|30
```
Makes it easier to read!

### Tip 3: Test Incrementally
1. Create basic script
2. Package and test
3. Open in editor again
4. Add more features
5. Repeat

### Tip 4: Save Often
Press **Ctrl+S** frequently while editing
- Quick and easy
- Doesn't close the editor
- Prevents data loss

### Tip 5: Use Comments
Lines starting with # are ignored:
```
# This is a comment
WINDOW|My App|400|300
# TODO: Add more buttons
BUTTON|1|OK|20|100|100|30
```

## Command Reference (Quick)

```
WINDOW|Title|Width|Height
LABEL|Text|X|Y
BUTTON|ID|Text|X|Y|Width|Height
LIST|ID|X|Y|Width|Height|Items
DROPDOWN|ID|X|Y|Width|Height|Options
ONCLICK|ID|Action|Argument
ONCHANGE|ID|Action|Argument
```

**Actions:** `NOTIFY`, `CLOSE`, `EXEC`

**Separators:**
- Use `|` between fields
- Use `;` between list/dropdown items

## Troubleshooting

### Editor won't open
**Solution:** Check .NET 9.0 is installed

### Can't save file
**Solution:** Check folder permissions, choose different location

### Lost unsaved work
**Solution:** Editor prompts on close - always answer the prompt!

### Script doesn't work in guideXOS
**Solution:** 
1. Click Help in editor for syntax reference
2. Check all pipes `|` are present
3. Verify button IDs match in ONCLICK commands
4. Test with Complete Template first

## Next Steps

1. **Experiment** - Try the Complete Template
2. **Customize** - Make it your own
3. **Test** - Package and run in guideXOS
4. **Iterate** - Edit, package, test, repeat!

**Happy script editing!** ?
