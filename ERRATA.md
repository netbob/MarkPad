# MarkPad Errata

Corrections, known issues, and what's coming — by version. If you're
looking for help with a feature, find it below first: it may belong to
a version newer than the one you have installed.

## Current version: 1.0.0 (Windows)

Single-pane markdown editing with syntax highlighting, preview toggle,
change markers, dark mode, and a git commit/push/pull workflow through
your own git install. The editor never reformats a file. Installed via
Setup.exe from GitHub Releases, with automatic updates.

Confirmed issue in 1.0.0: double-clicking a .md file in Explorer
launches MarkPad to a blank Untitled tab instead of opening the file,
even when the file is passed on the command line. File > Open and
drag-and-drop into the editor window both work correctly. Cause: the
app does not yet read the file path passed on launch. Workaround: open
files from inside MarkPad (File > Open) or drag them into the window.
Fix queued for v1.1. This file is updated as
issues are confirmed and fixed.

## v1.1 — Spellcheck (queued, not yet released)

Spellcheck was excluded from v1 by design and is the queued next item.
AvalonEdit has no built-in spellchecker, so this means a dictionary
plus a squiggle renderer — a real feature, not a toggle. If you're
looking for spellcheck in 1.0.0, it isn't there yet; it arrives in
v1.1.

## v2 — Depth on Windows (ideas, not built)

- GitHub explorer (browse a repo's files, beyond the current
  status/commit/push/pull workflow).
- Config settings page — e.g. a line-numbers on/off toggle (line
  numbers are currently always on).
- Foreign languages for the help files.

These are recorded ideas, not promises and not scheduled. If you're
looking for one of them, it is not in any released version yet.

## v3 — Other operating systems (horizon)

Debian, Apple platforms, and beyond, via the Avalonia path: the
plain-.NET core ports as-is while the WPF UI gets rebuilt on
Avalonia, one platform at a time. No dates, no promises.

## Looking for assistance?

- If the feature you want is listed under a version newer than yours,
  update from GitHub Releases (or wait for that version — see above).
- If it isn't listed anywhere, or something isn't working as
  described, open an issue at
  https://github.com/netbob/MarkPad/issues for bugs. For questions
  about using MarkPad, post in Discussions, in the Q&A category:
  https://github.com/netbob/MarkPad/discussions. Questions are also
  welcome in the comments on the Field Report announcement at
  https://mjmcshane.com.

MarkPad is free forever, MIT, no accounts, no telemetry.
