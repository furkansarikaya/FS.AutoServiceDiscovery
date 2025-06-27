using FS.AutoServiceDiscovery.Extensions.Architecture;

namespace FS.AutoServiceDiscovery.Extensions.Performance;

/// <summary>
/// A no-operation implementation of the performance metrics collector that discards all metrics.
/// 
/// This implementation follows the "Null Object Pattern" - it provides the same interface as the
/// real metrics collector but performs no actual work. This approach has several important benefits:
/// 
/// 1. **Zero Performance Overhead**: When metrics are disabled, there's absolutely no performance cost
/// 2. **Code Simplicity**: Consumers don't need to check if metrics are enabled before calling methods
/// 3. **Easy Configuration**: Metrics can be enabled/disabled simply by changing the registered implementation
/// 4. **Testing**: Provides a clean way to disable metrics during testing when they're not relevant
/// 
/// Think of this class as a "silent observer" - it appears to be watching and recording, but actually
/// does nothing. This allows the rest of the system to operate as if metrics are being collected
/// without any actual overhead.
/// </summary>
public class NoOpPerformanceMetricsCollector : IPerformanceMetricsCollector
{
    /// <summary>
    /// Does nothing - assembly scan metrics are silently discarded.
    /// </summary>
    public void RecordAssemblyScan(string assemblyName, TimeSpan scanDuration, int servicesFound, bool wasSuccessful)
    {
        // Intentionally empty - this is a no-op implementation
    }

    /// <summary>
    /// Does nothing - cache operation metrics are silently discarded.
    /// </summary>
    public void RecordCacheOperation(string operation, bool wasHit, TimeSpan operationDuration)
    {
        // Intentionally empty - this is a no-op implementation
    }

    /// <summary>
    /// Does nothing - plugin execution metrics are silently discarded.
    /// </summary>
    public void RecordPluginExecution(string pluginName, TimeSpan executionDuration, int servicesDiscovered, PluginValidationResult validationResult)
    {
        // Intentionally empty - this is a no-op implementation
    }

    /// <summary>
    /// Does nothing - service registration metrics are silently discarded.
    /// </summary>
    public void RecordServiceRegistration(int serviceCount, TimeSpan registrationDuration, int failedRegistrations)
    {
        // Intentionally empty - this is a no-op implementation
    }

    /// <summary>
    /// Does nothing - custom metrics are silently discarded.
    /// </summary>
    public void RecordCustomMetric(string metricName, double value, Dictionary<string, string>? tags = null)
    {
        // Intentionally empty - this is a no-op implementation
    }

    /// <summary>
    /// Returns an empty metrics summary since no metrics are being collected.
    /// </summary>
    public PerformanceMetricsSummary GetMetricsSummary()
    {
        return new PerformanceMetricsSummary
        {
            CollectionStartTime = DateTime.UtcNow,
            CollectionEndTime = DateTime.UtcNow,
            AssemblyMetrics = new List<PerformanceMetricsSummary.AssemblySummary>(),
            CacheMetrics = new List<PerformanceMetricsSummary.CacheSummary>(),
            PluginMetrics = new List<PerformanceMetricsSummary.PluginSummary>(),
            TotalServiceRegistrations = 0,
            AverageRegistrationTimeMs = 0,
            RegistrationFailureRate = 0
        };
    }

    /// <summary>
    /// Does nothing - there are no metrics to reset.
    /// </summary>
    public void ResetMetrics()
    {
        // Intentionally empty - this is a no-op implementation
    }
}