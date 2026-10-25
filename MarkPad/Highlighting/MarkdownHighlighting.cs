using System;
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
                return _dark ??= Load("MarkPad.Highlighting.Markdown-Dark.xshd");
            return _light ??= Load("MarkPad.Highlighting.Markdown-Light.xshd");
        }
        catch
        {
            return null;
        }
    }

    private static IHighlightingDefinition Load(string resourceName)
    {
        var assembly = Assembly.GetExecutingAssembly();
        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Embedded highlighting not found: {resourceName}");
        using var reader = XmlReader.Create(stream);
        return HighlightingLoader.Load(reader, HighlightingManager.Instance);
    }
}
