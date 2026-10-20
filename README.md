# Scripter

A lightweight Windows macro/script creator. Build scripts manually in a GUI — click at coordinates, press keys, wait, move the mouse — then save them as JSON files and run with repeat/loop support.

## Requirements

- Windows 10/11
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)

## Build and Run

```powershell
dotnet build Scripter.sln
dotnet run --project Scripter
```

### Publish (optional)

Single executable (requires .NET 8 runtime on the target machine):

```powershell
dotnet publish Scripter -c Release -r win-x64 --self-contained false
```

Output: `Scripter\bin\Release\net8.0-windows\win-x64\publish\Scripter.exe`

## Usage

1. **New** — create a script from scratch
2. Add actions: **Mouse Click**, **Move Mouse**, **Key Press**, **Wait**, plus **If** / **Else** / **EndIf**, **Loop** / **EndLoop**, **Break**
3. For mouse actions, click **Position (x, y)** to set coordinates — choose **Type coordinates** or **Pick with mouse**
4. For image If/Loop conditions: **Snip image** (Snipping Tool-style), **Select region** (click-drag), set **Certainty** (default 70%)
5. Use **Insert If block**, **Insert Loop**, or **Wait until image** to add nested structure quickly
6. Double-click any action in the list to edit it
7. Set **Repeat** count or enable **Repeat until stopped**
8. **Save** — writes to `%USERPROFILE%\Documents\Scripter\`
9. **Run** / **Stop** — playback uses Win32 `SendInput` on a background thread. Press **F8** at any time to stop a running script (works even when another window is focused)

From the main window you can list, edit, delete, and run saved scripts.

## Script File Format

Scripts are saved as JSON in `Documents\Scripter\{name}.json`:

```json
{
  "name": "Auto Click",
  "repeatCount": 10,
  "repeatUntilStopped": false,
  "actions": [
    { "type": "mouseClick", "param1": 500, "param2": 300, "extra": "Left" },
    { "type": "wait", "param1": 100 },
    { "type": "keyPress", "extra": "Space" },
    { "type": "moveMouse", "param1": 100, "param2": 200 }
  ]
}
```

### Action types

| Type | param1 | param2 | extra |
|------|--------|--------|-------|
| `mouseClick` | X | Y | `"Left"` or `"Right"` |
| `moveMouse` | X | Y | — |
| `keyPress` | — | — | Chord (e.g. `"Space"`, `"Ctrl+C"`, `"Shift+Ctrl+S"`) |
| `wait` | Milliseconds | — | — |
| `if` / `else` / `endIf` | Control flow | `condition` object on `if` | Nested If/Else allowed |
| `loop` / `endLoop` / `break` | Loop until break, condition false, timeout, or F8 | `timeoutMs` on `loop` | `break` exits innermost loop |

### Image conditions

`if` and `loop` can use `condition.kind`: `always`, `imageFound`, `imageNotFound`.

- Snip a reference PNG (saved under `Documents\Scripter\images\`)
- Select a screen region (click-drag); defaults to the snip bounds if you only snip
- `threshold` is match certainty, default **70**

Wait-until-image pattern: **Wait until image** inserts `Loop` → `Wait` → `If imageFound` → `Break` → `EndIf` → `EndLoop`.

### Repeat behavior

- `repeatCount` — run the full action list this many times (default: 1)
- `repeatUntilStopped` — when `true`, loops until you click **Stop** (ignores `repeatCount`)

## Project Layout

```
scripter/
├── Scripter.sln
├── Scripter/
│   ├── Forms/           MainForm, ScriptEditorForm
│   ├── Models/          Script, MacroAction
│   └── Services/        Storage, playback, input simulation
└── src/                 Original C skeleton (reference only)
```

## Notes

- Playback simulates input globally — test carefully and avoid running untrusted scripts.
- **F8** is a global hotkey that stops whatever script is currently running.
- **Pick with mouse** shows a live coordinate HUD. Click the target (the click is not sent to the app underneath), press Enter, or use **Lock Position** on the HUD. Esc cancels.
- Record mode is not included in v1.
