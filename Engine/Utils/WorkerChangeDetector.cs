using System.Text;
using Common.DTOs;
using Common.DTOs.Commands;
using Engine.DAL.Entities;

namespace Engine.Metrics;


public static class TextDiffHelper
{
    public static string HighlightChanges(string original, string updated)
    {
        var sb = new StringBuilder();
        int originalIndex = 0, updatedIndex = 0;

        while (originalIndex < original.Length || updatedIndex < updated.Length)
        {
            if (originalIndex < original.Length && updatedIndex < updated.Length && original[originalIndex] == updated[updatedIndex])
            {
                sb.Append(original[originalIndex]); // Ingen ændring
                originalIndex++;
                updatedIndex++;
            }
            else if (originalIndex < original.Length && updatedIndex < updated.Length && original[originalIndex] != updated[updatedIndex])
            {
                // Opdag ændring
                sb.Append("<span class='changed'>").Append(updated[updatedIndex]).Append("</span>");
                originalIndex++;
                updatedIndex++;
            }
            else if (originalIndex < original.Length)
            {
                // Tegn fjernet
                sb.Append("<span class='removed'>").Append(original[originalIndex]).Append("</span>");
                originalIndex++;
            }
            else if (updatedIndex < updated.Length)
            {
                // Tegn tilføjet
                sb.Append("<span class='added'>").Append(updated[updatedIndex]).Append("</span>");
                updatedIndex++;
            }
        }

        return sb.ToString();
    }
}


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
                ChangeDetails = $"From:\n'{original.Name}'\nTo:\n'{updated.Name}'"
            });
        }

        if (original.Description != updated.Description)
        {
            changes.Add(new WorkerChangeLog
            {
                WorkerId = original.WorkerId,
                ChangeDescription = "Description changed",
                ChangeDetails = $"From:\n'{original.Description}'\nTo:\n'{updated.Description}'"
            });
        }

        if (original.Command != updated.Command)
        {
            changes.Add(new WorkerChangeLog
            {
                WorkerId = original.WorkerId,
                ChangeDescription = "Command changed",
                ChangeDetails = $"From:\n{original.Command}\n\nTo:\n{TextDiffHelper.HighlightChanges(original.Command, updated.Command)}"
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
                ChangeDetails = $"Enabled:\nFrom '{original.ImgWatchdogEnabled}'\nTo '{updated.ImgWatchdogEnabled}'\n\n" +
                                $"Interval:\nFrom '{original.ImgWatchdogInterval}'\nTo '{updated.ImgWatchdogInterval}'\n\n" +
                                $"Grace Time:\nFrom '{original.ImgWatchdogGraceTime}'\nTo '{updated.ImgWatchdogGraceTime}'"
            });
        }

        return changes;
    }
}