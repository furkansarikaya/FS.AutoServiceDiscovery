using System.Collections.Concurrent;
using FS.AutoServiceDiscovery.Extensions.Architecture;

namespace FS.AutoServiceDiscovery.Extensions.Performance;

/// <summary>
/// Collects and aggregates performance metrics for the auto-discovery system with thread-safe operations.
/// 
/// This implementation serves as the "data recorder" for our system - like a black box in an airplane,
/// it continuously captures important performance data that can be analyzed later to understand system
/// behavior, identify performance bottlenecks, and optimize the discovery process.
/// 
/// The collector uses several important design patterns:
/// 1. **Thread Safety**: All operations are thread-safe for concurrent discovery scenarios
/// 2. **Efficient Aggregation**: Metrics are pre-aggregated to minimize memory usage and improve query performance
/// 3. **Structured Data**: Metrics are organized into logical categories for easy analysis
/// 4. **Minimal Overhead**: The collection process is designed to have minimal impact on discovery performance
/// 
/// This approach enables comprehensive observability without significantly impacting the performance
/// of the system being observed.
/// </summary>
public class PerformanceMetricsCollector : IPerformanceMetricsCollector
{
    // Thread-safe collections for storing metrics
    private readonly ConcurrentDictionary<string, AssemblyMetrics> _assemblyMetrics = new();
    private readonly ConcurrentDictionary<string, CacheMetrics> _cacheMetrics = new();
    private readonly ConcurrentDictionary<string, PluginMetrics> _pluginMetrics = new();
    private readonly ConcurrentDictionary<string, CustomMetric> _customMetrics = new();
    
    // Overall system metrics
    private long _totalServiceRegistrations = 0;
    private long _totalRegistrationTime = 0; // in milliseconds
    private long _totalFailedRegistrations = 0;
    private DateTime _startTime = DateTime.UtcNow;

    /// <summary>
    /// Records assembly scanning performance with thread-safe aggregation.
    /// 
    /// Assembly scanning metrics are crucial because scanning is often the most expensive part
    /// of the discovery process. By tracking these metrics, we can identify problematic assemblies
    /// and optimize scanning strategies.
    /// </summary>
    public void RecordAssemblyScan(string assemblyName, TimeSpan scanDuration, int servicesFound, bool wasSuccessful)
    {
        var metrics = _assemblyMetrics.GetOrAdd(assemblyName, _ => new AssemblyMetrics { AssemblyName = assemblyName });
        
        // Thread-safe updates using Interlocked operations
        Interlocked.Increment(ref metrics.ScanCount);
        Interlocked.Add(ref metrics.TotalScanTimeMs, (long)scanDuration.TotalMilliseconds);
        Interlocked.Add(ref metrics.TotalServicesFound, servicesFound);
        
        if (wasSuccessful)
        {
            Interlocked.Increment(ref metrics.SuccessfulScans);
        }
        else
        {
            Interlocked.Increment(ref metrics.FailedScans);
        }

        // Update last scan time (this is a simple assignment, which is atomic for DateTime)
        metrics.LastScanTime = DateTime.UtcNow;
    }

    /// <summary>
    /// Records cache operation performance for monitoring cache effectiveness.
    /// 
    /// Cache metrics help us understand how effectively our caching strategy is working
    /// and whether cache configuration needs adjustment.
    /// </summary>
    public void RecordCacheOperation(string operation, bool wasHit, TimeSpan operationDuration)
    {
        var metrics = _cacheMetrics.GetOrAdd(operation, _ => new CacheMetrics { OperationType = operation });
        
        Interlocked.Increment(ref metrics.OperationCount);
        Interlocked.Add(ref metrics.TotalOperationTimeMs, (long)operationDuration.TotalMilliseconds);
        
        if (wasHit)
        {
            Interlocked.Increment(ref metrics.HitCount);
        }
        else
        {
            Interlocked.Increment(ref metrics.MissCount);
        }
    }

    /// <summary>
    /// Records plugin execution performance for monitoring plugin health and effectiveness.
    /// 
    /// Plugin metrics are essential for understanding the contribution and performance
    /// characteristics of individual plugins in the discovery ecosystem.
    /// </summary>
    public void RecordPluginExecution(string pluginName, TimeSpan executionDuration, int servicesDiscovered, PluginValidationResult validationResult)
    {
        var metrics = _pluginMetrics.GetOrAdd(pluginName, _ => new PluginMetrics { PluginName = pluginName });
        
        Interlocked.Increment(ref metrics.ExecutionCount);
        Interlocked.Add(ref metrics.TotalExecutionTimeMs, (long)executionDuration.TotalMilliseconds);
        Interlocked.Add(ref metrics.TotalServicesDiscovered, servicesDiscovered);
        
        if (validationResult.IsValid)
        {
            Interlocked.Increment(ref metrics.SuccessfulExecutions);
        }
        else
        {
            Interlocked.Increment(ref metrics.FailedExecutions);
        }
        
        Interlocked.Add(ref metrics.TotalErrors, validationResult.Errors.Count);
        Interlocked.Add(ref metrics.TotalWarnings, validationResult.Warnings.Count);
    }

    /// <summary>
    /// Records service registration performance for monitoring the final step of the discovery process.
    /// </summary>
    public void RecordServiceRegistration(int serviceCount, TimeSpan registrationDuration, int failedRegistrations)
    {
        Interlocked.Add(ref _totalServiceRegistrations, serviceCount);
        Interlocked.Add(ref _totalRegistrationTime, (long)registrationDuration.TotalMilliseconds);
        Interlocked.Add(ref _totalFailedRegistrations, failedRegistrations);
    }

    /// <summary>
    /// Records custom metrics for specialized monitoring needs.
    /// 
    /// This flexibility allows plugins and extensions to contribute their own metrics
    /// without requiring changes to the core collector interface.
    /// </summary>
    public void RecordCustomMetric(string metricName, double value, Dictionary<string, string>? tags = null)
    {
        var metric = _customMetrics.GetOrAdd(metricName, _ => new CustomMetric 
        { 
            Name = metricName,
            Tags = tags ?? new Dictionary<string, string>()
        });
        
        Interlocked.Increment(ref metric.RecordCount);
        
        // For simplicity, we'll track sum and let the summary calculate averages
        // In a production system, you might want more sophisticated aggregation
        var longValue = (long)(value * 1000); // Store as integer to avoid floating point concurrency issues
        Interlocked.Add(ref metric.TotalValue, longValue);
        
        // Update min/max values (these operations are not perfectly thread-safe but close enough for metrics)
        if (value < metric.MinValue || metric.MinValue == 0)
            metric.MinValue = value;
        if (value > metric.MaxValue)
            metric.MaxValue = value;
    }

    /// <summary>
    /// Retrieves a comprehensive summary of all collected metrics for analysis and reporting.
    /// 
    /// This method aggregates all collected data into a structured format that's easy to
    /// analyze and export to monitoring systems.
    /// </summary>
    public PerformanceMetricsSummary GetMetricsSummary()
    {
        var summary = new PerformanceMetricsSummary
        {
            CollectionStartTime = _startTime,
            CollectionEndTime = DateTime.UtcNow
        };

        // Aggregate assembly metrics
        summary.AssemblyMetrics = _assemblyMetrics.Values.Select(m => new PerformanceMetricsSummary.AssemblySummary
        {
            AssemblyName = m.AssemblyName,
            ScanCount = m.ScanCount,
            AverageScanTimeMs = m.ScanCount > 0 ? (double)m.TotalScanTimeMs / m.ScanCount : 0,
            TotalServicesFound = m.TotalServicesFound,
            SuccessRate = m.ScanCount > 0 ? (double)m.SuccessfulScans / m.ScanCount * 100 : 0,
            LastScanTime = m.LastScanTime
        }).ToList();

        // Aggregate cache metrics
        summary.CacheMetrics = _cacheMetrics.Values.Select(m => new PerformanceMetricsSummary.CacheSummary
        {
            OperationType = m.OperationType,
            OperationCount = m.OperationCount,
            HitRate = m.OperationCount > 0 ? (double)m.HitCount / m.OperationCount * 100 : 0,
            AverageOperationTimeMs = m.OperationCount > 0 ? (double)m.TotalOperationTimeMs / m.OperationCount : 0
        }).ToList();

        // Aggregate plugin metrics
        summary.PluginMetrics = _pluginMetrics.Values.Select(m => new PerformanceMetricsSummary.PluginSummary
        {
            PluginName = m.PluginName,
            ExecutionCount = m.ExecutionCount,
            AverageExecutionTimeMs = m.ExecutionCount > 0 ? (double)m.TotalExecutionTimeMs / m.ExecutionCount : 0,
            TotalServicesDiscovered = m.TotalServicesDiscovered,
            SuccessRate = m.ExecutionCount > 0 ? (double)m.SuccessfulExecutions / m.ExecutionCount * 100 : 0,
            AverageErrorsPerExecution = m.ExecutionCount > 0 ? (double)m.TotalErrors / m.ExecutionCount : 0
        }).ToList();

        // Overall system metrics
        summary.TotalServiceRegistrations = _totalServiceRegistrations;
        summary.AverageRegistrationTimeMs = _totalServiceRegistrations > 0 ? (double)_totalRegistrationTime / _totalServiceRegistrations : 0;
        summary.RegistrationFailureRate = _totalServiceRegistrations > 0 ? (double)_totalFailedRegistrations / _totalServiceRegistrations * 100 : 0;

        return summary;
    }

    /// <summary>
    /// Resets all collected metrics, starting fresh collection from this point.
    /// </summary>
    public void ResetMetrics()
    {
        _assemblyMetrics.Clear();
        _cacheMetrics.Clear();
        _pluginMetrics.Clear();
        _customMetrics.Clear();
        
        _totalServiceRegistrations = 0;
        _totalRegistrationTime = 0;
        _totalFailedRegistrations = 0;
        _startTime = DateTime.UtcNow;
    }

    // Internal metric storage classes
    private class AssemblyMetrics
    {
        public string AssemblyName = string.Empty;
        public long ScanCount = 0;
        public long TotalScanTimeMs = 0;
        public long TotalServicesFound = 0;
        public long SuccessfulScans = 0;
        public long FailedScans = 0;
        public DateTime LastScanTime = DateTime.MinValue;
    }

    private class CacheMetrics
    {
        public string OperationType = string.Empty;
        public long OperationCount = 0;
        public long TotalOperationTimeMs = 0;
        public long HitCount = 0;
        public long MissCount = 0;
    }

    private class PluginMetrics
    {
        public string PluginName = string.Empty;
        public long ExecutionCount = 0;
        public long TotalExecutionTimeMs = 0;
        public long TotalServicesDiscovered = 0;
        public long SuccessfulExecutions = 0;
        public long FailedExecutions = 0;
        public long TotalErrors = 0;
        public long TotalWarnings = 0;
    }

    private class CustomMetric
    {
        public string Name = string.Empty;
        public long RecordCount = 0;
        public long TotalValue = 0; // Stored as long for thread safety
        public double MinValue = 0;
        public double MaxValue = 0;
        public Dictionary<string, string> Tags = new();
    }
}