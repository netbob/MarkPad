using System.Threading.Tasks;
using System.Windows;
using Velopack;
using Velopack.Sources;

namespace MarkPad;

public partial class App : Application
{
    // Set when a background update check downloaded a new release. MainWindow
    // applies it on a normal quit (see OnClosing), never mid-session.
    public static bool UpdateDownloaded { get; private set; }

    protected override void OnStartup(StartupEventArgs e)
    {
        // Velopack's install/update/uninstall hooks run from inside Run(), so it
        // must be the first code at startup; during a hook it exits the process
        // here and normal startup never runs.
        VelopackApp.Build().Run();
        base.OnStartup(e);
        _ = CheckForUpdatesAsync();
    }

    // Silent background update check against GitHub Releases. A new release
    // downloads in the background and waits for a normal quit to apply.
    // Updates must never break startup, so every failure (no network, no
    // release yet, plain VS build) is swallowed.
    private static async Task CheckForUpdatesAsync()
    {
        try
        {
            var mgr = new UpdateManager(new GithubSource("https://github.com/netbob/MarkPad", null, false));
            if (!mgr.IsInstalled)
                return;
            var update = await mgr.CheckForUpdatesAsync();
            if (update == null)
                return;
            await mgr.DownloadUpdatesAsync(update);
            UpdateDownloaded = true;
        }
        catch
        {
            // Deliberately silent: see above.
        }
    }
}
