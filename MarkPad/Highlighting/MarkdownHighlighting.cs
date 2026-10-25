using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Xml;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;

namespace MarkPad.Highlighting;

/// <summary>
/// Loads the embedded markdown highlighting definition for the current theme.
/// Highlighting must never take the editor down: any load failure falls back
/// to plain text, and the failure is swallowed here on purpose.
/// </summary>
public static class MarkdownHighlighting
{
    private static IHighlightingDefinition? _light;
    private static IHighlightingDefinition? _dark;
    private static readonly HashSet<string> _reportedFailures = new(StringComparer.Ordinal);

    public static IHighlightingDefinition? GetDefinition(string theme)
    {
        try
        {
            if (theme == "Dark")
                return _dark ??= Load("Markdown-Dark.xshd");
            return _light ??= Load("Markdown-Light.xshd");
        }
        catch (Exception ex)
        {
            // DIAGNOSTIC, temporary: the silent fallback hid the real load error
            // for two rounds. Surface it once per theme so the cause can be fixed,
            // then remove this popup. The editor still runs, just unhighlighted.
            if (_reportedFailures.Add(theme))
            {
                MessageBox.Show(
                    $"MarkPad highlighting diagnostic ({theme}):\n{ex.GetType().Name}: {ex.Message}",
                    "MarkPad highlighting diagnostic",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            return null;
        }
    }

    private static IHighlightingDefinition Load(string fileName)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var stream = assembly.GetManifestResourceStream("MarkPad.Highlighting." + fileName);
        if (stream == null)
        {
            // Fall back to any embedded resource ending with the file name, so a
            // different root namespace or folder layout cannot silently kill highlighting.
            var match = assembly.GetManifestResourceNames()
                .FirstOrDefault(n => n.EndsWith(fileName, StringComparison.OrdinalIgnoreCase));
            if (match != null)
                stream = assembly.GetManifestResourceStream(match);
        }
        using var owned = stream ?? throw new InvalidOperationException($"Embedded highlighting not found: {fileName}");
        using var reader = XmlReader.Create(owned);
        return HighlightingLoader.Load(reader, HighlightingManager.Instance);
    }
}
