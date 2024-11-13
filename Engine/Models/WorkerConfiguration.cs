using Engine.DAL.Entities;

public class WorkerConfiguration
{
    public required string WorkerId { get; set; }
    public TimeSpan ImgWatchdogGraceTime { get; set; }
    public TimeSpan ImgWatchdogInterval { get; set; }
    public bool ImgWatchdogEnabled { get; set; }
    public required string GstCommand { get; set; }
    public bool IsEnabled { get; set; }

    // Statisk metode til at oprette WorkerConfiguration fra WorkerEntity
    public static WorkerConfiguration FromEntity(WorkerEntity entity)
    {
        return new WorkerConfiguration
        {
            WorkerId = entity.WorkerId,
            ImgWatchdogGraceTime = entity.ImgWatchdogGraceTime,
            ImgWatchdogInterval = entity.ImgWatchdogInterval,
            ImgWatchdogEnabled = entity.ImgWatchdogEnabled,
            GstCommand = entity.Command,
            IsEnabled = entity.IsEnabled
        };
    }
}