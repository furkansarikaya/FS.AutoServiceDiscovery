using FS.AutoServiceDiscovery.Extensions.Architecture;

namespace FS.AutoServiceDiscovery.Extensions.Performance;

/// <summary>
/// Defines a contract for collecting and reporting performance metrics throughout the auto-discovery process.
/// 
/// This interface represents a crucial aspect of production-ready software: observability. In complex systems,
/// understanding how your software performs in real-world conditions is essential for maintaining reliability,
/// diagnosing issues, and optimizing for better user experience.
/// 
/// Think of this interface as the "instrumentation panel" for your auto-discovery system - just like a car's
/// dashboard shows you engine temperature, fuel level, and speed, this interface provides visibility into
/// the health and performance of your service discovery process.
/// 
/// The metrics collection follows several important principles:
/// 1. **Low Overhead**: Metrics collection should not significantly impact the performance of the system being measured
/// 2. **Comprehensive Coverage**: Capture metrics from all important operations and decision points
/// 3. **Actionable Information**: Provide metrics that actually help with troubleshooting and optimization
/// 4. **Aggregation Friendly**: Design metrics that can be easily aggregated and analyzed over time
/// </summary>
public interface IPerformanceMetricsCollector
{
    /// <summary>
    /// Records the execution time and result of an assembly scanning operation.
    /// 
    /// Assembly scanning is often one of the most expensive operations in the discovery process,
    /// so monitoring its performance is crucial for understanding overall system behavior.
    /// 
    /// This method captures both the quantitative aspects (how long did it take?) and qualitative
    /// aspects (did it succeed? how many services were found?) of the scanning process.
    /// </summary>
    /// <param name="assemblyName">
    /// The name of the assembly that was scanned. This identifier helps correlate performance
    /// with specific assemblies to identify problematic assemblies or patterns.
    /// </param>
    /// <param name="scanDuration">
    /// The total time spent scanning the assembly, including all reflection operations,
    /// attribute reading, and service candidate identification.
    /// </param>
    /// <param name="servicesFound">
    /// The number of services discovered in this assembly. This metric helps understand
    /// the "productivity" of scanning different assemblies.
    /// </param>
    /// <param name="wasSuccessful">
    /// Whether the scanning operation completed successfully without errors.
    /// Failed scans indicate potential issues that need investigation.
    /// </param>
    void RecordAssemblyScan(string assemblyName, TimeSpan scanDuration, int servicesFound, bool wasSuccessful);
    
    /// <summary>
    /// Records cache performance metrics for monitoring cache effectiveness and optimization opportunities.
    /// 
    /// Cache performance directly impacts overall system performance, so monitoring cache behavior
    /// is essential for maintaining optimal performance and identifying when cache configuration
    /// needs adjustment.
    /// </summary>
    /// <param name="operation">
    /// The type of cache operation performed (e.g., "Get", "Set", "Invalidate").
    /// This helps understand which cache operations are most common and their relative performance.
    /// </param>
    /// <param name="wasHit">
    /// Whether the cache operation resulted in a hit (for Get operations) or was successful
    /// (for Set/Invalidate operations). This is the primary metric for cache effectiveness.
    /// </param>
    /// <param name="operationDuration">
    /// The time spent performing the cache operation. This helps identify performance
    /// issues with the cache implementation itself.
    /// </param>
    void RecordCacheOperation(string operation, bool wasHit, TimeSpan operationDuration);
    
    /// <summary>
    /// Records plugin execution metrics for monitoring plugin performance and reliability.
    /// 
    /// Since plugins are often developed by different teams or third parties, monitoring
    /// their performance is crucial for identifying issues and ensuring overall system stability.
    /// </summary>
    /// <param name="pluginName">
    /// The name of the plugin that was executed. This identifier helps track performance
    /// on a per-plugin basis and identify problematic plugins.
    /// </param>
    /// <param name="executionDuration">
    /// The total time spent executing the plugin, including all discovery operations
    /// and validation performed by the plugin.
    /// </param>
    /// <param name="servicesDiscovered">
    /// The number of services discovered by this plugin execution. This metric helps
    /// understand the contribution of each plugin to the overall discovery process.
    /// </param>
    /// <param name="validationResult">
    /// The result of plugin validation, indicating whether the plugin executed successfully
    /// and whether any issues were found with its discovered services.
    /// </param>
    void RecordPluginExecution(string pluginName, TimeSpan executionDuration, int servicesDiscovered, PluginValidationResult validationResult);
    
    /// <summary>
    /// Records service registration metrics for monitoring the final step of the discovery process.
    /// 
    /// Service registration is the culmination of the entire discovery process, so monitoring
    /// this step helps ensure that discovered services are being properly registered and that
    /// the registration process itself is performing well.
    /// </summary>
    /// <param name="serviceCount">
    /// The total number of services that were registered with the dependency injection container.
    /// </param>
    /// <param name="registrationDuration">
    /// The total time spent registering all services with the container.
    /// </param>
    /// <param name="failedRegistrations">
    /// The number of services that failed to register due to errors or conflicts.
    /// A high number of failed registrations indicates potential configuration issues.
    /// </param>
    void RecordServiceRegistration(int serviceCount, TimeSpan registrationDuration, int failedRegistrations);
    
    /// <summary>
    /// Records custom metrics that don't fit into the standard categories.
    /// 
    /// This method provides flexibility for plugins and extensions to record their own
    /// specialized metrics without requiring changes to the core interface.
    /// </summary>
    /// <param name="metricName">
    /// A descriptive name for the metric being recorded. Use consistent naming conventions
    /// to enable effective aggregation and analysis.
    /// </param>
    /// <param name="value">
    /// The numeric value of the metric. The meaning of this value depends on the metric type
    /// (e.g., count, duration in milliseconds, percentage, etc.).
    /// </param>
    /// <param name="tags">
    /// Optional key-value pairs that provide additional context for the metric.
    /// These tags enable filtering and grouping during analysis.
    /// </param>
    void RecordCustomMetric(string metricName, double value, Dictionary<string, string>? tags = null);
    
    /// <summary>
    /// Retrieves a summary of all collected performance metrics for analysis and reporting.
    /// 
    /// This method provides access to the aggregated performance data for use in monitoring
    /// dashboards, performance reports, and optimization analysis.
    /// </summary>
    /// <returns>
    /// A comprehensive summary of performance metrics collected since the last reset or
    /// since the collector was initialized.
    /// </returns>
    PerformanceMetricsSummary GetMetricsSummary();
    
    /// <summary>
    /// Resets all collected metrics, clearing the current data and starting fresh collection.
    /// 
    /// This method is useful for scenarios where you want to measure performance over
    /// specific time periods or after making configuration changes.
    /// </summary>
    void ResetMetrics();
}