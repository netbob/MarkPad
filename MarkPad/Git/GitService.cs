using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace MarkPad.Git;

public sealed record GitResult(int ExitCode, string Stdout, string Stderr)
{
    public bool Success => ExitCode == 0;
}

public sealed record GitStatusEntry(string Status, string Path);

/// <summary>
/// Git through the installed git CLI — never through a new login or token.
/// That means MarkPad reuses whatever auth you already have (GitHub Desktop,
/// Git Credential Manager, an existing terminal session). Push/pull can pop
/// the credential manager's own window; if git ever waits on a terminal
/// prompt instead, the timeout below fails the command rather than hanging.
/// </summary>
public static class GitService
{
    public static async Task<GitResult> RunAsync(
        string workingDirectory,
        IEnumerable<string> args,
        int timeoutMs = 60000)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "git",
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        foreach (var arg in args)
            psi.ArgumentList.Add(arg);

        try
        {
            using var process = new Process { StartInfo = psi };
            process.Start();
            var stdoutTask = process.StandardOutput.ReadToEndAsync();
            var stderrTask = process.StandardError.ReadToEndAsync();
            var exited = await Task.Run(() => process.WaitForExit(timeoutMs));
            if (!exited)
            {
                try { process.Kill(entireProcessTree: true); } catch { }
                return new GitResult(-1, "",
                    "git timed out. If it was waiting on credentials, sign in once via GitHub Desktop or a terminal, then retry.");
            }

            return new GitResult(process.ExitCode, await stdoutTask, await stderrTask);
        }
        catch (Exception ex)
        {
            return new GitResult(-1, "", $"Could not run git: {ex.Message}");
        }
    }

    public static async Task<string?> GetRepoRootAsync(string directory)
    {
        var result = await RunAsync(directory, new[] { "rev-parse", "--show-toplevel" });
        if (!result.Success)
            return null;
        var root = result.Stdout.Trim();
        return string.IsNullOrEmpty(root) ? null : root;
    }

    public static async Task<string> GetBranchAsync(string repoRoot)
    {
        var result = await RunAsync(repoRoot, new[] { "branch", "--show-current" });
        return result.Success ? result.Stdout.Trim() : string.Empty;
    }

    public static async Task<List<GitStatusEntry>> GetStatusAsync(string repoRoot)
    {
        var result = await RunAsync(repoRoot, new[] { "status", "--porcelain=v1" });
        var entries = new List<GitStatusEntry>();
        if (!result.Success)
            return entries;

        foreach (var rawLine in result.Stdout.Split('\n'))
        {
            var line = rawLine.TrimEnd('\r');
            if (line.Length < 4)
                continue;
            var status = line[..2];
            var path = line[3..];
            var arrow = path.IndexOf(" -> ", StringComparison.Ordinal);
            if (arrow >= 0)
                path = path[(arrow + 4)..];
            entries.Add(new GitStatusEntry(status, path));
        }

        return entries;
    }

    public static Task<GitResult> StageAsync(string repoRoot, IEnumerable<string> paths)
    {
        var args = new List<string> { "add", "--" };
        args.AddRange(paths);
        return RunAsync(repoRoot, args);
    }

    public static Task<GitResult> CommitAsync(string repoRoot, string message) =>
        RunAsync(repoRoot, new[] { "commit", "-m", message });

    public static Task<GitResult> PushAsync(string repoRoot) =>
        RunAsync(repoRoot, new[] { "push" }, timeoutMs: 120000);

    public static Task<GitResult> PullAsync(string repoRoot) =>
        RunAsync(repoRoot, new[] { "pull", "--ff-only" }, timeoutMs: 120000);
}
