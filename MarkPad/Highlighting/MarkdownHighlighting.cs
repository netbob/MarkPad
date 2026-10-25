using System;
using System.Linq;
using System.Reflection;
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

    public static IHighlightingDefinition? GetDefinition(string theme)
    {
        try
        {
            if (theme == "Dark")
                return _dark ??= Load("Markdown-Dark.xshd");
            return _light ??= Load("Markdown-Light.xshd");
        }
        catch
        {
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
