using Common.Models;

namespace StreamHub.Models;

public class MetricSimpleViewModel
{
    public double CPUUsage { get; set; }
    public double RxMbps { get; set; }
    public double TxMbps { get; set; }
    public double RxUsagePercent { get; set; }
    public double TxUsagePercent { get; set; }
    public DateTime Timestamp { get; set; }

    // Constructor to map values from a Metric object
    public MetricSimpleViewModel(Metric metric)
    {
        CPUUsage = metric.CPUUsage;
        RxMbps = metric.RxMbps;
        TxMbps = metric.TxMbps;
        RxUsagePercent = metric.RxUsagePercent;
        TxUsagePercent = metric.TxUsagePercent;
        Timestamp = metric.Timestamp;
    }
}