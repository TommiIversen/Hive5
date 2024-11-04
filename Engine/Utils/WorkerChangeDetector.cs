using Common.DTOs;
using Engine.DAL.Entities;

namespace Engine.Utils;

public class WorkerChangeDetector
{
    public List<WorkerChangeLog> DetectChanges(WorkerEntity original, WorkerCreateAndEdit updated)
    {
        var changes = new List<WorkerChangeLog>();

        if (original.Name != updated.Name)
        {
            changes.Add(new WorkerChangeLog
            {
                WorkerId = original.WorkerId,
                ChangeDescription = "Name changed",
                ChangeDetails = $"From '{original.Name}' to '{updated.Name}'"
            });
        }

        if (original.Description != updated.Description)
        {
            changes.Add(new WorkerChangeLog
            {
                WorkerId = original.WorkerId,
                ChangeDescription = "Description changed",
                ChangeDetails = $"From '{original.Description}' to '{updated.Description}'"
            });
        }

        if (original.Command != updated.Command)
        {
            changes.Add(new WorkerChangeLog
            {
                WorkerId = original.WorkerId,
                ChangeDescription = "Command changed",
                ChangeDetails = $"From '{original.Command}' to '{updated.Command}'"
            });
        }

        if (original.ImgWatchdogEnabled != updated.ImgWatchdogEnabled ||
            original.ImgWatchdogInterval != updated.ImgWatchdogInterval ||
            original.ImgWatchdogGraceTime != updated.ImgWatchdogGraceTime)
        {
            changes.Add(new WorkerChangeLog
            {
                WorkerId = original.WorkerId,
                ChangeDescription = "Watchdog settings changed",
                ChangeDetails = $"Enabled: From '{original.ImgWatchdogEnabled}' to '{updated.ImgWatchdogEnabled}', " +
                                $"Interval: From '{original.ImgWatchdogInterval}' to '{updated.ImgWatchdogInterval}', " +
                                $"Grace Time: From '{original.ImgWatchdogGraceTime}' to '{updated.ImgWatchdogGraceTime}'"
            });
        }

        return changes;
    }
}
