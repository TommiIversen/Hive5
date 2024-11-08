using System.Text;
using Common.DTOs.Commands;
using Engine.DAL.Entities;

namespace Engine.Utils;

public static class TextDiffHelper
{
    public static string HighlightChanges(string original, string updated)
    {
        // Normaliser linjeskift til \n
        original = original.Replace("\r\n", "\n").Replace("\r", "\n");
        updated = updated.Replace("\r\n", "\n").Replace("\r", "\n");

        var originalLines = original.Split('\n');
        var updatedLines = updated.Split('\n');

        var diff = GetLineDiff(originalLines, updatedLines);

        var sb = new StringBuilder();

        foreach (var change in diff)
            switch (change.Type)
            {
                case DiffType.Unchanged:
                    sb.Append(change.Content + "\n");
                    break;
                case DiffType.Added:
                    sb.Append($"<span class='added'>{change.Content}</span>\n");
                    break;
                case DiffType.Removed:
                    sb.Append($"<span class='removed'>{change.Content}</span>\n");
                    break;
                case DiffType.Changed:
                    var highlightedLine = HighlightWordChanges(change.OriginalContent, change.Content);
                    sb.Append(highlightedLine + "\n");
                    break;
            }

        // Fjern den sidste \n, hvis den findes
        if (sb.Length > 0 && sb[sb.Length - 1] == '\n') sb.Length--;

        return sb.ToString();
    }

    private static string HighlightWordChanges(string original, string updated)
    {
        var originalWords = original.Split(' ');
        var updatedWords = updated.Split(' ');

        var diff = GetWordDiff(originalWords, updatedWords);

        var sb = new StringBuilder();

        foreach (var change in diff)
            switch (change.Type)
            {
                case DiffType.Unchanged:
                    sb.Append(change.Content + " ");
                    break;
                case DiffType.Added:
                    sb.Append($"<span class='added'>{change.Content}</span> ");
                    break;
                case DiffType.Removed:
                    sb.Append($"<span class='removed'>{change.Content}</span> ");
                    break;
                case DiffType.Changed:
                    sb.Append($"<span class='changed'>{change.Content}</span> ");
                    break;
            }

        // Fjern det sidste mellemrum
        if (sb.Length > 0 && sb[sb.Length - 1] == ' ') sb.Length--;

        return sb.ToString();
    }

    private static List<DiffResult> GetLineDiff(string[] original, string[] updated)
    {
        return GetDiff(original, updated);
    }

    private static List<DiffResult> GetWordDiff(string[] original, string[] updated)
    {
        return GetDiff(original, updated);
    }

    private static List<DiffResult> GetDiff(string[] original, string[] updated)
    {
        var m = original.Length;
        var n = updated.Length;

        // Compute LCS matrix
        var lcs = new int[m + 1, n + 1];
        for (var i = 0; i < m; i++)
        for (var j = 0; j < n; j++)
            if (original[i] == updated[j])
                lcs[i + 1, j + 1] = lcs[i, j] + 1;
            else
                lcs[i + 1, j + 1] = Math.Max(lcs[i + 1, j], lcs[i, j + 1]);

        // Backtrack to find the diff
        var diff = new List<DiffResult>();
        int x = m, y = n;
        while (x > 0 && y > 0)
            if (original[x - 1] == updated[y - 1])
            {
                diff.Add(new DiffResult
                    { Type = DiffType.Unchanged, Content = original[x - 1], OriginalContent = original[x - 1] });
                // Console.WriteLine($"Unchanged: {original[x - 1]}"); // Debugging
                x--;
                y--;
            }
            else if (lcs[x - 1, y] >= lcs[x, y - 1])
            {
                diff.Add(new DiffResult
                    { Type = DiffType.Removed, Content = original[x - 1], OriginalContent = original[x - 1] });
                // Console.WriteLine($"Removed: {original[x - 1]}"); // Debugging
                x--;
            }
            else
            {
                diff.Add(new DiffResult { Type = DiffType.Added, Content = updated[y - 1], OriginalContent = null });
                // Console.WriteLine($"Added: {updated[y - 1]}"); // Debugging
                y--;
            }

        while (x > 0)
        {
            diff.Add(new DiffResult
                { Type = DiffType.Removed, Content = original[x - 1], OriginalContent = original[x - 1] });
            // Console.WriteLine($"Removed: {original[x - 1]}"); // Debugging
            x--;
        }

        while (y > 0)
        {
            diff.Add(new DiffResult { Type = DiffType.Added, Content = updated[y - 1], OriginalContent = null });
            // Console.WriteLine($"Added: {updated[y - 1]}"); // Debugging
            y--;
        }

        diff.Reverse();

        // Post-process to identify changed
        var finalDiff = new List<DiffResult>();
        var iFinal = 0;
        while (iFinal < diff.Count)
            if (diff[iFinal].Type == DiffType.Removed)
            {
                if (iFinal + 1 < diff.Count && diff[iFinal + 1].Type == DiffType.Added)
                {
                    // Treat as changed
                    finalDiff.Add(new DiffResult
                    {
                        Type = DiffType.Changed,
                        Content = diff[iFinal + 1].Content,
                        OriginalContent = diff[iFinal].Content
                    });
                    // Console.WriteLine($"Changed: {diff[iFinal].Content} -> {diff[iFinal + 1].Content}"); // Debugging
                    iFinal += 2;
                }
                else
                {
                    finalDiff.Add(diff[iFinal]);
                    iFinal++;
                }
            }
            else
            {
                finalDiff.Add(diff[iFinal]);
                iFinal++;
            }

        return finalDiff;
    }

    private class DiffResult
    {
        public DiffType Type { get; set; }
        public string Content { get; set; }
        public string OriginalContent { get; set; }
    }

    private enum DiffType
    {
        Unchanged,
        Added,
        Removed,
        Changed
    }
}

public class WorkerChangeDetector
{
    public List<WorkerChangeLog> DetectChanges(WorkerEntity original, WorkerCreateAndEdit updated)
    {
        var changes = new List<WorkerChangeLog>();

        if (original.Name != updated.Name)
            changes.Add(new WorkerChangeLog
            {
                WorkerId = original.WorkerId,
                ChangeDescription = "Name changed",
                ChangeDetails = $"From:\n'{original.Name}'\nTo:\n'{updated.Name}'"
            });

        if (original.Description != updated.Description)
            changes.Add(new WorkerChangeLog
            {
                WorkerId = original.WorkerId,
                ChangeDescription = "Description changed",
                ChangeDetails = $"From:\n'{original.Description}'\nTo:\n'{updated.Description}'"
            });

        if (original.Command != updated.Command)
            changes.Add(new WorkerChangeLog
            {
                WorkerId = original.WorkerId,
                ChangeDescription = "Command changed",
                ChangeDetails =
                    $"From:\n{original.Command}\n\nTo:\n{TextDiffHelper.HighlightChanges(original.Command, updated.Command)}"
            });

        if (original.ImgWatchdogEnabled != updated.ImgWatchdogEnabled ||
            original.ImgWatchdogInterval != updated.ImgWatchdogInterval ||
            original.ImgWatchdogGraceTime != updated.ImgWatchdogGraceTime)
            changes.Add(new WorkerChangeLog
            {
                WorkerId = original.WorkerId,
                ChangeDescription = "Watchdog settings changed",
                ChangeDetails =
                    $"Enabled:\nFrom '{original.ImgWatchdogEnabled}'\nTo '{updated.ImgWatchdogEnabled}'\n\n" +
                    $"Interval:\nFrom '{original.ImgWatchdogInterval}'\nTo '{updated.ImgWatchdogInterval}'\n\n" +
                    $"Grace Time:\nFrom '{original.ImgWatchdogGraceTime}'\nTo '{updated.ImgWatchdogGraceTime}'"
            });

        return changes;
    }
}