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
   Change markers in the line-number margin: orange for edits since the
   last save, green once saved. View → Preview swaps the whole tab to a
   rendered read-only view — never a split pane. It renders a snapshot,
   so toggle off and back on after editing for a fresh render, and links
   in preview open in your browser.
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
2. Let NuGet restore packages (AvalonEdit, the WPF text editor control;
   Markdig for preview; WebView2 for rendering; Velopack for the
   installer and updates).
3. Build (Ctrl+Shift+B) and run (F5).

The dependencies are AvalonEdit 6.3.1.120 (note the package ID:
"AvalonEdit", not "ICSharpCode.AvalonEdit" — the project moved to the
shorter ID; the assembly and namespaces are still ICSharpCode.AvalonEdit),
Markdig 0.40.0, Microsoft.Web.WebView2 1.0.3595.46, and Velopack 1.2.161.
If restore ever reports a version as unavailable, take the latest stable
in Manage NuGet Packages.

## Use

- File: New (Ctrl+N), Open (Ctrl+O), Save (Ctrl+S), Save As
  (Ctrl+Shift+S), Close Tab (Ctrl+W). Drag .md/.markdown/.txt files onto
  the window to open them.
- Edit: Undo/Redo/Cut/Copy/Paste/Select All; Ctrl+F finds.
- View: Dark Mode toggle, Word Wrap toggle, Preview toggle (whole-tab rendered view).
- Change markers: an orange bar means edited since save, green means saved. Display-only; markers never touch the file.
- Git: Refresh Status, Commit (message + file checklist), Push, Pull.
  Git commands run against the repo containing the current file. If a
  push or pull waits on credentials, it uses your credential manager's own
  window; if git ever waits on a terminal prompt instead, the command
  times out with an explanation rather than hanging.

Settings (theme, last folder) live in `%APPDATA%\MarkPad\settings.json`.

## Install

Download `Setup.exe` from the latest release on GitHub
(https://github.com/netbob/MarkPad/releases) and run it. It installs
per-user with Start Menu shortcuts — no admin rights needed — and it
checks GitHub Releases on every launch. A new version downloads in the
background and installs itself when you quit normally; it never
interrupts an editing session and never relaunches the app after you
close it. The installer is unsigned, so Windows SmartScreen may ask once
on first install; code-signing is a later option, not v1.

## Releasing (for the maintainer)

1. The release version is the `--packVersion` you pass to `vpk pack`
   (semver, e.g. `1.0.0` — never four parts). Keep the Velopack NuGet
   package and the `vpk` tool on the same version.
2. `dotnet tool install -g vpk` (once).
3. From `MarkPad/`: `dotnet publish -c Release -r win-x64 --self-contained true -o publish`
4. `vpk pack --packId netbob.MarkPad --packVersion 1.0.0 --packDir publish --mainExe MarkPad.exe --packTitle "MarkPad"`
5. Create a GitHub Release and upload everything `vpk pack` produced
   (Setup.exe, the full and delta .nupkg files, RELEASES). Updates read
   the release assets, so all of them must be attached — Setup.exe alone
   installs but never updates.

## Support

MarkPad is free forever. If it earns a coffee:
https://buymeacoffee.com/Netbob — the tip jar, and the only money door.
The same link is in Help → About.

Support: open an issue on GitHub
(https://github.com/netbob/MarkPad/issues) for bugs; questions are
welcome in the comments on the Field Report announcement at
https://mjmcshane.com.

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

Deliberately not in v1: WYSIWYG, a Jekyll new-post wizard, diff view,
and anything from the GitHub API (PRs, issues). Spellcheck is queued
for v1.1. Those arrive when one of them is actually missed.

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
