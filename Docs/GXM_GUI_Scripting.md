# GXM GUI Scripting (guideXOS)

Embed a simple GUI in a GXM executable by placing a GUI header after the standard 16?byte GXM header:

Signature: `G X M \0`
Header layout:
```
0..3  magic (GXM or MUE)
4..7  version (u32)
8..11 entry RVA (u32)
12..15 image size (u32)
16..19 optional 'G','U','I','\0' to enable script parsing
20..    script text (lines separated by \n, terminated by \0)
```
If 'GUI\0' is present the loader parses the script and does NOT enter user mode; it builds a window.

## Commands
Each line uses `|` to separate fields.

### WINDOW
`WINDOW|Title|Width|Height`
Sets window title and size. Size is clamped to minimum 160x120.

### LABEL
`LABEL|Text|X|Y`
Draws multiline capable text (width constrained to window minus padding).

### BUTTON
`BUTTON|Id|Text|X|Y|W|H`
Creates a button.

### LIST
`LIST|Id|X|Y|W|H|item1;item2;item3`
Creates a vertical list view with selectable rows.

### DROPDOWN
`DROPDOWN|Id|X|Y|W|H|item1;item2;item3`
Creates a dropdown (combo) control.

## Events
Define callbacks:
- `ONCLICK|Id|Action|Arg` for button click events
- `ONCHANGE|Id|Action|Arg` for list selection and dropdown change

Supported actions:
- `MSG` shows a message box. `Arg` can include `$VALUE` token which is replaced with selection text.
- `OPENAPP` opens a built-in app by name (e.g., Notepad, Calculator).
- `CLOSE` closes the script window.

Example:
```
WINDOW|Demo|480|320
LABEL|Pick a color|16|16
DROPDOWN|1|16|46|140|24|Red;Green;Blue
ONCHANGE|1|MSG|Selected $VALUE
BUTTON|2|Open Notepad|16|80|140|28
ONCLICK|2|OPENAPP|Notepad
LIST|3|200|46|200|140|Alpha;Beta;Gamma
ONCHANGE|3|MSG|List: $VALUE
BUTTON|4|Close|360|280|100|28
ONCLICK|4|CLOSE|
```

## Packaging
1. Build binary header (16 bytes GXM) with image size including script region.
2. Append `GUI\0` and script text, end with `\0`.

## Roadmap
- TextBox, CheckBox, Radio, Slider, Scrollable list.
- Callback to native code or user-mode for advanced scenarios.
