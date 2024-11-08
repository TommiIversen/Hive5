using Common.DTOs.Events;

namespace StreamHub.Models;

public class MetricSimpleViewModel
{
    // Constructor to map values from a EngineMetric object
    public MetricSimpleViewModel(EngineMetric engineMetric)
    {
        CPUUsage = engineMetric.CpuUsage;
        RxMbps = engineMetric.RxMbps;
        TxMbps = engineMetric.TxMbps;
        RxUsagePercent = engineMetric.RxUsagePercent;
        TxUsagePercent = engineMetric.TxUsagePercent;
        Timestamp = engineMetric.MeasureTimestamp;
    }

    public double CPUUsage { get; set; }
    public double RxMbps { get; set; }
    public double TxMbps { get; set; }
    public double RxUsagePercent { get; set; }
    public double TxUsagePercent { get; set; }
    public DateTime Timestamp { get; set; }
}