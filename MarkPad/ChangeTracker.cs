using System;
using System.Collections.Generic;
using ICSharpCode.AvalonEdit.Document;

namespace MarkPad;

/// <summary>Notepad++-style change state for one document line.</summary>
public enum MarkerState
{
    None,
    Unsaved,
    Saved
}

/// <summary>
/// Tracks which lines changed since the file was opened, and whether those
/// changes are saved. Display-only: it never touches the file.
/// Lines are tracked with document anchors, so insertions and deletions
/// above a marked line move its marker with it. Undo back to the pristine
/// state still shows the marker (v1 approximation; Notepad++ clears it).
/// </summary>
public sealed class ChangeTracker
{
    private readonly TextDocument _document;
    private readonly List<(ITextAnchor Anchor, MarkerState State)> _marks = new();

    public ChangeTracker(TextDocument document)
    {
        _document = document;
        _document.Changed += OnDocumentChanged;
    }

    public event EventHandler? MarkersChanged;

    public MarkerState GetState(int lineNumber)
    {
        var state = MarkerState.None;
        foreach (var (anchor, s) in _marks)
        {
            if (_document.GetLineByOffset(anchor.Offset).LineNumber != lineNumber)
                continue;
            if (s == MarkerState.Unsaved)
                return MarkerState.Unsaved;
            state = MarkerState.Saved;
        }
        return state;
    }

    public void MarkSaved()
    {
        var changed = false;
        for (var i = 0; i < _marks.Count; i++)
        {
            if (_marks[i].State == MarkerState.Unsaved)
            {
                _marks[i] = (_marks[i].Anchor, MarkerState.Saved);
                changed = true;
            }
        }
        if (changed)
            MarkersChanged?.Invoke(this, EventArgs.Empty);
    }

    public void Reset()
    {
        if (_marks.Count == 0)
            return;
        _marks.Clear();
        MarkersChanged?.Invoke(this, EventArgs.Empty);
    }

    private void OnDocumentChanged(object? sender, DocumentChangeEventArgs e)
    {
        // The inserted text now sits at [e.Offset, e.Offset + e.InsertionLength).
        // Every line it spans (plus the line it starts on) changed.
        var startLine = _document.GetLineByOffset(e.Offset).LineNumber;
        var insertedLines = 0;
        if (e.InsertionLength > 0)
        {
            var inserted = _document.GetText(e.Offset, e.InsertionLength);
            foreach (var ch in inserted)
            {
                if (ch == '\n')
                    insertedLines++;
            }
        }
        var touched = false;
        for (var line = startLine; line <= startLine + insertedLines; line++)
        {
            if (line > _document.LineCount)
                break;
            if (EnsureMark(line))
                touched = true;
        }
        if (touched)
            MarkersChanged?.Invoke(this, EventArgs.Empty);
    }

    private bool EnsureMark(int lineNumber)
    {
        foreach (var (anchor, _) in _marks)
        {
            if (_document.GetLineByOffset(anchor.Offset).LineNumber == lineNumber)
                return false;
        }
        var offset = _document.GetLineByNumber(lineNumber).Offset;
        _marks.Add((_document.CreateAnchor(offset), MarkerState.Unsaved));
        return true;
    }
}
