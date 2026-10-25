using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Markdig;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;

namespace MarkPad;

/// <summary>
/// Read-only rendered markdown view: the preview half of the toggle, never a
/// split screen. Markdig renders to HTML; WebView2 (Edge/Chromium) shows it.
/// Snapshot semantics: it renders the editor text at toggle time. Toggle back
/// to edit, change text, toggle again for a fresh render. Links open in the
/// default browser so a click never navigates the preview away.
/// </summary>
public sealed class PreviewPane
{
    private static readonly MarkdownPipeline Pipeline =
        new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();

    public WebView2 Control { get; } = new();

    public PreviewPane()
    {
        Control.NavigationStarting += (_, e) =>
        {
            // Let the initial about:blank load and in-page anchors through;
            // send real web links out to the default browser instead.
            if (e.Uri.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
                || e.Uri.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                e.Cancel = true;
                Process.Start(new ProcessStartInfo(e.Uri) { UseShellExecute = true });
            }
        };
    }

    public async Task RenderAsync(string markdown, string theme)
    {
        await Control.EnsureCoreWebView2Async();
        var body = Markdown.ToHtml(markdown, Pipeline);
        Control.NavigateToString(BuildPage(body, theme));
    }

    private static string BuildPage(string body, string theme)
    {
        var dark = theme == "Dark";
        var bg = dark ? "#1E1E1E" : "#FFFFFF";
        var fg = dark ? "#D4D4D4" : "#1E1E1E";
        var link = dark ? "#4EA6FF" : "#0563C1";
        var codeBg = dark ? "#2D2D2D" : "#F2F2F2";
        var border = dark ? "#3E3E3E" : "#DDDDDD";
        return "<!DOCTYPE html><html><head><meta charset=\"utf-8\"><style>"
            + $"body{{background:{bg};color:{fg};font-family:'Segoe UI',sans-serif;font-size:15px;line-height:1.6;max-width:800px;margin:24px auto;padding:0 24px;}}"
            + $"a{{color:{link};}}"
            + $"pre{{background:{codeBg};padding:12px;overflow:auto;}}"
            + $"code{{background:{codeBg};padding:2px 4px;}}"
            + $"pre code{{background:none;padding:0;}}"
            + $"blockquote{{border-left:4px solid {border};margin-left:0;padding-left:16px;}}"
            + $"table{{border-collapse:collapse;}}"
            + $"th,td{{border:1px solid {border};padding:6px 12px;}}"
            + "</style></head><body>" + body + "</body></html>";
    }
}
