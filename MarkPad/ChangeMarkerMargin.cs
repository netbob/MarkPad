using System;
using System.Windows;
using System.Windows.Media;
using ICSharpCode.AvalonEdit.Editing;
using ICSharpCode.AvalonEdit.Rendering;

namespace MarkPad;

/// <summary>
/// Notepad++-style change bar in the left margin: orange for lines changed
/// since the last save, green for lines changed since open and saved.
/// Sits right of the line numbers, like Notepad++ (numbers | bar | text).
/// </summary>
public sealed class ChangeMarkerMargin : AbstractMargin
{
    private static readonly Brush UnsavedBrush = new SolidColorBrush(Color.FromRgb(0xFF, 0xA5, 0x00));
    private static readonly Brush SavedBrush = new SolidColorBrush(Color.FromRgb(0x00, 0xB0, 0x50));

    private readonly TextView _textView;
    private readonly ChangeTracker _tracker;

    public ChangeMarkerMargin(TextView textView, ChangeTracker tracker)
    {
        _textView = textView;
        _tracker = tracker;
        Width = 6;
        _tracker.MarkersChanged += (_, _) => InvalidateVisual();
        _textView.VisualLinesChanged += (_, _) => InvalidateVisual();
    }

    protected override void OnRender(DrawingContext drawingContext)
    {
        base.OnRender(drawingContext);
        foreach (var visualLine in _textView.VisualLines)
        {
            var state = _tracker.GetState(visualLine.FirstDocumentLine.LineNumber);
            if (state == MarkerState.None)
                continue;
            var brush = state == MarkerState.Unsaved ? UnsavedBrush : SavedBrush;
            var y = visualLine.VisualTop - _textView.VerticalOffset;
            drawingContext.DrawRectangle(brush, null, new Rect(1, y, Width - 2, visualLine.Height));
        }
    }
}
