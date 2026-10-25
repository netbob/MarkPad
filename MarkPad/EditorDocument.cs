using System;
using System.IO;
using System.Linq;
using System.Text;
using ICSharpCode.AvalonEdit;

namespace MarkPad;

/// <summary>
/// One open file: its bytes on disk, and the editor showing it.
///
/// The editor never reformats: what you type is what gets saved, with the
/// file's original line endings and encoding (including BOM) preserved.
/// That matters for Jekyll front matter, where a rewritten file can change
/// meaning (a bare numeric tag, for instance, parses as a YAML integer).
/// </summary>
public sealed class EditorDocument
{
    public string? FilePath { get; private set; }
    public string NewLine { get; private set; } = "\r\n";
    public Encoding Encoding { get; private set; } = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
    public bool HasBom { get; private set; }
    public bool IsDirty { get; set; }

    /// <summary>True while a file is being loaded into the editor, so the
    /// load itself is not mistaken for an edit.</summary>
    public bool Loading { get; set; }

    public string DisplayName => FilePath == null ? "Untitled" : Path.GetFileName(FilePath);

    public string? Directory => FilePath == null ? null : Path.GetDirectoryName(FilePath);

    public void LoadFromFile(string path, TextEditor editor)
    {
        var bytes = File.ReadAllBytes(path);
        var (encoding, hasBom, bodyStart) = DetectEncoding(bytes);
        Encoding = encoding;
        HasBom = hasBom;

        var text = encoding.GetString(bytes, bodyStart, bytes.Length - bodyStart);
        NewLine = DetectNewLine(text);
        FilePath = path;

        Loading = true;
        try
        {
            editor.Text = text;
        }
        finally
        {
            Loading = false;
        }

        editor.Document.UndoStack.ClearAll();
        IsDirty = false;
    }

    public void SaveToFile(TextEditor editor, string? path = null)
    {
        var target = path ?? FilePath ?? throw new InvalidOperationException("No file path to save to.");

        // AvalonEdit hands text back with \n line endings; convert to the
        // file's own line-ending style before writing.
        var text = editor.Text.Replace("\n", NewLine);
        var body = Encoding.GetBytes(text);

        byte[] bytes;
        if (HasBom)
        {
            var preamble = Encoding.GetPreamble();
            bytes = new byte[preamble.Length + body.Length];
            Buffer.BlockCopy(preamble, 0, bytes, 0, preamble.Length);
            Buffer.BlockCopy(body, 0, bytes, preamble.Length, body.Length);
        }
        else
        {
            bytes = body;
        }

        File.WriteAllBytes(target, bytes);
        FilePath = target;
        IsDirty = false;
    }

    private static (Encoding Encoding, bool HasBom, int BodyStart) DetectEncoding(byte[] bytes)
    {
        // Longest BOM first: UTF-32 LE starts with the same two bytes as UTF-16 LE.
        if (bytes.Length >= 4 && bytes[0] == 0xFF && bytes[1] == 0xFE && bytes[2] == 0x00 && bytes[3] == 0x00)
            return (new UTF32Encoding(bigEndian: false, byteOrderMark: true), true, 4);
        if (bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF)
            return (new UTF8Encoding(encoderShouldEmitUTF8Identifier: true), true, 3);
        if (bytes.Length >= 2 && bytes[0] == 0xFF && bytes[1] == 0xFE)
            return (new UnicodeEncoding(bigEndian: false, byteOrderMark: true), true, 2);
        if (bytes.Length >= 2 && bytes[0] == 0xFE && bytes[1] == 0xFF)
            return (new UnicodeEncoding(bigEndian: true, byteOrderMark: true), true, 2);

        return (new UTF8Encoding(encoderShouldEmitUTF8Identifier: false), false, 0);
    }

    private static string DetectNewLine(string text)
    {
        var crlf = CountOccurrences(text, "\r\n");
        var lf = CountOccurrences(text, "\n") - crlf;
        return crlf >= lf ? "\r\n" : "\n";
    }

    private static int CountOccurrences(string text, string value)
    {
        var count = 0;
        var index = 0;
        while ((index = text.IndexOf(value, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += value.Length;
        }

        return count;
    }
}
