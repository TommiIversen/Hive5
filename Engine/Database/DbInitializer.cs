using Engine.DAL.Entities;
using Microsoft.EntityFrameworkCore;

namespace Engine.Database;

public static class DbInitializer
{
    public static void Seed(ApplicationDbContext context)
    {
        var engineEntity = context.EngineEntities.Include(e => e.HubUrls).FirstOrDefault();
        if (engineEntity == null)
        {
            engineEntity = new EngineEntities
            {
                EngineId = Guid.NewGuid(),
                Name = "Default Engine",
                Version = "1.0",
                Description = "Default Description",
                InstallDate = DateTime.Now,
                HubUrls = new List<HubUrlEntity>
                {
                    new() { HubUrl = "http://127.0.0.1:9000/streamhub" },
                    new() { HubUrl = "http://127.0.0.1:8999/streamhub" }
                }
            };

            context.EngineEntities.Add(engineEntity);
            context.SaveChanges();
        }
        else if (!engineEntity.HubUrls.Any())
        {
            engineEntity.HubUrls.Add(new HubUrlEntity { HubUrl = "http://127.0.0.1:9000/streamhub" });
            engineEntity.HubUrls.Add(new HubUrlEntity { HubUrl = "http://127.0.0.1:8999/streamhub" });
            context.SaveChanges();
        }

        // Update WorkerEntities with missing StreamerType to "FakeStreamer"
        var workersWithMissingStreamerType = context.Workers
            .Where(w => string.IsNullOrEmpty(w.StreamerType))
            .ToList();

        foreach (var worker in workersWithMissingStreamerType) worker.StreamerType = "FakeStreamer";

        if (workersWithMissingStreamerType.Any()) context.SaveChanges();
        
        
        
        // Sikrer, at der er to Workere
        if (!context.Workers.Any())
        {
            var defaultWorkers = new List<WorkerEntity>
            {
                new WorkerEntity
                {
                    WorkerId = "Worker01",
                    Name = "Fake Worker 1",
                    Description = "First default worker",
                    Command =
                        "--gst-debug=3 videotestsrc is-live=true pattern=ball ! video/x-raw, framerate=25/1, width=300, height=168 ! timeoverlay ! videoconvert ! videorate ! video/x-raw, framerate=5/1 ! jpegenc ! fdsink fd=2",
                    IsEnabled = true,
                    StreamerType = "FakeStreamer",
                    ImgWatchdogGraceTime = TimeSpan.FromSeconds(5),
                    ImgWatchdogInterval = TimeSpan.FromSeconds(2),
                    ImgWatchdogEnabled = true
                },
                new WorkerEntity
                {
                    WorkerId = "Worker02",
                    Name = "Fake Worker 2",
                    Description = "Second default worker",
                    Command =
                        "--gst-debug=3 videotestsrc is-live=true pattern=smpte ! video/x-raw, framerate=30/1, width=400, height=300 ! videoconvert ! jpegenc ! fdsink fd=2",
                    IsEnabled = true,
                    StreamerType = "FakeStreamer",
                    ImgWatchdogGraceTime = TimeSpan.FromSeconds(5),
                    ImgWatchdogInterval = TimeSpan.FromSeconds(2),
                    ImgWatchdogEnabled = true
                }
            };

            context.Workers.AddRange(defaultWorkers);
            context.SaveChanges();
        }
    }
}