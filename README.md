# MarkPad

A minimal markdown editor for Windows, built because Markdown Monster is full
of features and clunky to use. MarkPad does three things and stays out of the
way:

1. **GitHub-friendly git workflow** — open a file inside a repo and the status
   bar shows the branch and how many files changed. Commit (with a file
   checklist), push, and pull from the Git menu. It shells out to the `git`
   you already have installed, so it reuses your existing GitHub auth
   (GitHub Desktop / Git Credential Manager). No new login, no token.
2. **One view, like Notepad++** — the markdown source, with syntax
   highlighting coloring headers, bold, italic, code, links, lists, quotes,
   and rules in place. Tabs for multiple files, line numbers, Ctrl+F find.
   No preview pane, no mode switching. That is the design, not a missing
   feature.
3. **Dark mode switch** — View menu, Light/Dark, remembered between runs.

## The one hard rule

The editor never reformats a file. What you type is what gets saved, with
the file's original line endings and encoding (including BOM) preserved.
That matters for Jekyll front matter, where a rewritten file can change
meaning — a bare numeric tag parses as a YAML integer, and a colon-space
in a description can break a build. MarkPad is a text editor; it has
nothing to "fix."

## Build

Requires Visual Studio 2026 (or any recent Visual Studio with .NET 8) and
an internet connection for the first NuGet restore.

1. Open `MarkPad.sln`.
2. Let NuGet restore packages (one dependency: AvalonEdit, the WPF text
   editor control).
3. Build (Ctrl+Shift+B) and run (F5).

If NuGet restore reports the pinned AvalonEdit version (6.3.0) as
unavailable, open Manage NuGet Packages and take the latest 6.x — no code
changes are needed.

## Use

- File: New (Ctrl+N), Open (Ctrl+O), Save (Ctrl+S), Save As
  (Ctrl+Shift+S), Close Tab (Ctrl+W). Drag .md/.markdown/.txt files onto
  the window to open them.
- Edit: Undo/Redo/Cut/Copy/Paste/Select All; Ctrl+F finds.
- View: Dark Mode toggle, Word Wrap toggle.
- Git: Refresh Status, Commit (message + file checklist), Push, Pull.
  Git commands run against the repo containing the current file. If a
  push or pull waits on credentials, it uses your credential manager's own
  window; if git ever waits on a terminal prompt instead, the command
  times out with an explanation rather than hanging.

Settings (theme, last folder) live in `%APPDATA%\MarkPad\settings.json`.

## Add a feature

The deal is: start minimal, add a feature when you actually miss it. The
map, so nothing has to be hunted for:

- Highlighting colors or rules: `MarkPad/Highlighting/Markdown-Light.xshd`
  and `Markdown-Dark.xshd`. Same rules in both files; only colors differ.
  If a highlighting file fails to load, the editor falls back to plain
  text rather than crashing — check the file against AvalonEdit's XSHD
  format if colors ever go missing.
- Theme colors (window, menu, status bar): `MarkPad/Themes/Light.xaml`
  and `Dark.xaml`. Editor colors come from the same files.
- Git behavior: `MarkPad/Git/GitService.cs` — every git command is one
  method there.
- Menus, tabs, shortcuts, status bar: `MarkPad/MainWindow.xaml` and
  `MainWindow.xaml.cs` (code-behind on purpose; the app is small).
- File loading/saving rules: `MarkPad/EditorDocument.cs`.

Deliberately not in v1: preview pane, WYSIWYG, spellcheck, a Jekyll
new-post wizard, diff view, and anything from the GitHub API (PRs,
issues). Those arrive when one of them is actually missed.

## Name

MarkPad is the name — confirmed, and it travels well: "Pad" does half the
iPad marketing on its own, and nothing in it is Windows-specific.

## Platforms

v1 is Windows (WPF). That is a deliberate starting point, not the
ceiling — but be clear-eyed about the boundary:

- **Ports as-is:** the non-UI code is plain .NET and already
  platform-agnostic — `Git/GitService.cs` (git CLI exists on Linux and
  macOS too), `EditorDocument.cs` (bytes, encoding, line endings),
  `Settings.cs` (on Linux, `SpecialFolder.ApplicationData` maps to
  `~/.config`). Those files carry over to any future version unchanged.
- **Gets rebuilt:** the UI layer. WPF is Windows-only, so an iPadOS
  version or a Linux .deb means rebuilding the window, menus, and editor
  pane on Avalonia (the WPF-like XAML framework that targets Windows,
  macOS, Linux, iOS, and Android, with its own AvaloniaEdit control).
  The XAML concepts transfer; the files do not copy across verbatim.

So the path is: ship WPF v1 on Windows, prove the editor, and if iPadOS
or Linux become real goals, the next major version moves the UI to
Avalonia while the core comes along for free. Do not try to be
cross-platform in v1 — that is how a small editor becomes a big project
before it ever ships.

## License

MIT. Free forever, no accounts, no telemetry.
