using Common.DTOs.Commands;
using Engine.DAL.Entities;

namespace Engine.Utils;

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

        // Detect changes in Streamer type
        if (original.StreamerType != updated.StreamerType)
            changes.Add(new WorkerChangeLog
            {
                WorkerId = original.WorkerId,
                ChangeDescription = "Streamer type changed",
                ChangeDetails = $"From:\n'{original.StreamerType}'\nTo:\n'{updated.StreamerType}'"
            });

        return changes;
    }
}