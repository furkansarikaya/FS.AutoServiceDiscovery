namespace FS.AutoServiceDiscovery.Extensions.Performance;

/// <summary>
/// Provides a comprehensive summary of performance metrics collected across all aspects of the auto-discovery system.
/// 
/// This class serves as the "executive dashboard" for performance monitoring. Just like a CEO needs a
/// high-level overview of company performance with the ability to drill down into specific departments,
/// this summary provides both high-level system performance and detailed breakdowns by component.
/// 
/// The summary is designed to answer key performance questions:
/// - How is the system performing overall?
/// - Which components are the performance bottlenecks?
/// - Are there patterns or trends that need attention?
/// - How effective are our caching and optimization strategies?
/// 
/// This structured approach to performance data makes it easy to integrate with monitoring systems,
/// generate reports, and provide actionable insights for system optimization.
/// </summary>
public class PerformanceMetricsSummary
{
    /// <summary>
    /// Gets or sets the time when metric collection began.
    /// This provides the start boundary for understanding the time period covered by these metrics.
    /// </summary>
    public DateTime CollectionStartTime { get; set; }

    /// <summary>
    /// Gets or sets the time when this summary was generated.
    /// This provides the end boundary for the metrics collection period.
    /// </summary>
    public DateTime CollectionEndTime { get; set; }

    /// <summary>
    /// Gets or sets the collection of assembly scanning performance metrics.
    /// 
    /// Assembly scanning is often the most computationally expensive part of service discovery,
    /// so these metrics are crucial for understanding and optimizing system performance.
    /// </summary>
    public List<AssemblySummary> AssemblyMetrics { get; set; } = new();

    /// <summary>
    /// Gets or sets the collection of cache operation performance metrics.
    /// 
    /// Cache performance directly impacts overall system performance, making these metrics
    /// essential for validating caching strategies and identifying optimization opportunities.
    /// </summary>
    public List<CacheSummary> CacheMetrics { get; set; } = new();

    /// <summary>
    /// Gets or sets the collection of plugin execution performance metrics.
    /// 
    /// Plugin metrics help identify which plugins are performing well and which might
    /// need optimization or replacement.
    /// </summary>
    public List<PluginSummary> PluginMetrics { get; set; } = new();

    /// <summary>
    /// Gets or sets the total number of services registered across all discovery operations.
    /// This represents the total "productivity" of the discovery system.
    /// </summary>
    public long TotalServiceRegistrations { get; set; }

    /// <summary>
    /// Gets or sets the average time spent registering services with the dependency injection container.
    /// This metric helps understand the performance characteristics of the final registration step.
    /// </summary>
    public double AverageRegistrationTimeMs { get; set; }

    /// <summary>
    /// Gets or sets the percentage of service registrations that failed due to errors or conflicts.
    /// A high failure rate indicates potential configuration or code issues that need attention.
    /// </summary>
    public double RegistrationFailureRate { get; set; }

    /// <summary>
    /// Calculates the total duration covered by this metrics collection.
    /// This provides context for interpreting rate-based and time-based metrics.
    /// </summary>
    public TimeSpan CollectionDuration => CollectionEndTime - CollectionStartTime;

    /// <summary>
    /// Gets the assembly with the longest average scan time, which may indicate a performance bottleneck.
    /// This information is valuable for targeted optimization efforts.
    /// </summary>
    public AssemblySummary? SlowestAssembly => AssemblyMetrics
        .OrderByDescending(a => a.AverageScanTimeMs)
        .FirstOrDefault();

    /// <summary>
    /// Gets the plugin with the longest average execution time, which may need performance optimization.
    /// </summary>
    public PluginSummary? SlowestPlugin => PluginMetrics
        .OrderByDescending(p => p.AverageExecutionTimeMs)
        .FirstOrDefault();

    /// <summary>
    /// Gets the cache operation with the highest hit rate, indicating the most effective caching strategy.
    /// </summary>
    public CacheSummary? MostEffectiveCache => CacheMetrics
        .OrderByDescending(c => c.HitRate)
        .FirstOrDefault();

    /// <summary>
    /// Performance summary for individual assembly scanning operations.
    /// </summary>
    public class AssemblySummary
    {
        /// <summary>
        /// Gets or sets the name of the assembly that was scanned.
        /// </summary>
        public string AssemblyName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the total number of times this assembly was scanned.
        /// Multiple scans might occur during development or in scenarios with cache misses.
        /// </summary>
        public long ScanCount { get; set; }

        /// <summary>
        /// Gets or sets the average time spent scanning this assembly.
        /// This metric helps identify assemblies that are expensive to process.
        /// </summary>
        public double AverageScanTimeMs { get; set; }

        /// <summary>
        /// Gets or sets the total number of services discovered in this assembly across all scans.
        /// This represents the "productivity" of scanning this particular assembly.
        /// </summary>
        public long TotalServicesFound { get; set; }

        /// <summary>
        /// Gets or sets the percentage of scan attempts that completed successfully.
        /// A success rate significantly below 100% indicates potential issues with the assembly.
        /// </summary>
        public double SuccessRate { get; set; }

        /// <summary>
        /// Gets or sets the timestamp of the most recent scan of this assembly.
        /// This helps understand usage patterns and identify stale data.
        /// </summary>
        public DateTime LastScanTime { get; set; }

        /// <summary>
        /// Calculates the average number of services found per scan for this assembly.
        /// This normalized metric helps compare productivity across assemblies of different sizes.
        /// </summary>
        public double AverageServicesPerScan => ScanCount > 0 ? (double)TotalServicesFound / ScanCount : 0;
    }

    /// <summary>
    /// Performance summary for cache operations.
    /// </summary>
    public class CacheSummary
    {
        /// <summary>
        /// Gets or sets the type of cache operation (e.g., "Get", "Set", "Invalidate").
        /// </summary>
        public string OperationType { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the total number of this type of cache operation performed.
        /// </summary>
        public long OperationCount { get; set; }

        /// <summary>
        /// Gets or sets the cache hit rate as a percentage for this operation type.
        /// Higher hit rates indicate more effective caching for this operation.
        /// </summary>
        public double HitRate { get; set; }

        /// <summary>
        /// Gets or sets the average time spent performing this type of cache operation.
        /// This helps identify cache operations that might be performance bottlenecks.
        /// </summary>
        public double AverageOperationTimeMs { get; set; }

        /// <summary>
        /// Calculates the cache miss rate as the inverse of the hit rate.
        /// </summary>
        public double MissRate => 100.0 - HitRate;

        /// <summary>
        /// Evaluates the effectiveness of this cache operation based on hit rate and performance.
        /// </summary>
        public string EffectivenessRating
        {
            get
            {
                if (HitRate > 80 && AverageOperationTimeMs < 10) return "Excellent";
                if (HitRate > 60 && AverageOperationTimeMs < 50) return "Good";
                if (HitRate > 40) return "Fair";
                return "Poor";
            }
        }
    }

    /// <summary>
    /// Performance summary for plugin execution.
    /// </summary>
    public class PluginSummary
    {
        /// <summary>
        /// Gets or sets the name of the plugin.
        /// </summary>
        public string PluginName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the total number of times this plugin has been executed.
        /// </summary>
        public long ExecutionCount { get; set; }

        /// <summary>
        /// Gets or sets the average time spent executing this plugin.
        /// Plugins with high execution times may need optimization.
        /// </summary>
        public double AverageExecutionTimeMs { get; set; }

        /// <summary>
        /// Gets or sets the total number of services discovered by this plugin across all executions.
        /// This represents the plugin's total contribution to the discovery process.
        /// </summary>
        public long TotalServicesDiscovered { get; set; }

        /// <summary>
        /// Gets or sets the percentage of plugin executions that completed successfully.
        /// Low success rates indicate potential issues with the plugin implementation.
        /// </summary>
        public double SuccessRate { get; set; }

        /// <summary>
        /// Gets or sets the average number of errors per execution for this plugin.
        /// This metric helps identify plugins that frequently encounter issues.
        /// </summary>
        public double AverageErrorsPerExecution { get; set; }

        /// <summary>
        /// Calculates the average number of services discovered per execution.
        /// This productivity metric helps evaluate the plugin's effectiveness.
        /// </summary>
        public double AverageServicesPerExecution => ExecutionCount > 0 
            ? (double)TotalServicesDiscovered / ExecutionCount 
            : 0;

        /// <summary>
        /// Evaluates the overall health of this plugin based on success rate and error frequency.
        /// </summary>
        public string HealthStatus
        {
            get
            {
                if (SuccessRate > 95 && AverageErrorsPerExecution < 0.1) return "Healthy";
                if (SuccessRate > 85 && AverageErrorsPerExecution < 0.5) return "Warning";
                return "Critical";
            }
        }
    }

    /// <summary>
    /// Generates a text-based performance report suitable for logging or console output.
    /// 
    /// This method creates a human-readable summary that can be used for debugging,
    /// logging, or quick performance assessments.
    /// </summary>
    /// <returns>A formatted string containing key performance insights.</returns>
    public string GenerateTextReport()
    {
        var report = new System.Text.StringBuilder();
        
        report.AppendLine("=== Auto-Discovery Performance Summary ===");
        report.AppendLine($"Collection Period: {CollectionStartTime:yyyy-MM-dd HH:mm:ss} to {CollectionEndTime:yyyy-MM-dd HH:mm:ss}");
        report.AppendLine($"Duration: {CollectionDuration.TotalMinutes:F1} minutes");
        report.AppendLine();
        
        report.AppendLine("Service Registration Overview:");
        report.AppendLine($"  Total Services Registered: {TotalServiceRegistrations:N0}");
        report.AppendLine($"  Average Registration Time: {AverageRegistrationTimeMs:F2} ms");
        report.AppendLine($"  Registration Failure Rate: {RegistrationFailureRate:F1}%");
        report.AppendLine();
        
        if (AssemblyMetrics.Count != 0)
        {
            report.AppendLine("Assembly Scanning Performance:");
            report.AppendLine($"  Assemblies Scanned: {AssemblyMetrics.Count}");
            report.AppendLine($"  Average Scan Time: {AssemblyMetrics.Average(a => a.AverageScanTimeMs):F2} ms");
            if (SlowestAssembly != null)
            {
                report.AppendLine($"  Slowest Assembly: {SlowestAssembly.AssemblyName} ({SlowestAssembly.AverageScanTimeMs:F2} ms)");
            }
            report.AppendLine();
        }
        
        if (PluginMetrics.Count != 0)
        {
            report.AppendLine("Plugin Performance:");
            report.AppendLine($"  Plugins Executed: {PluginMetrics.Count}");
            report.AppendLine($"  Average Success Rate: {PluginMetrics.Average(p => p.SuccessRate):F1}%");
            if (SlowestPlugin != null)
            {
                report.AppendLine($"  Slowest Plugin: {SlowestPlugin.PluginName} ({SlowestPlugin.AverageExecutionTimeMs:F2} ms)");
            }
            report.AppendLine();
        }

        if (CacheMetrics.Count == 0) return report.ToString();
        report.AppendLine("Cache Performance:");
        report.AppendLine($"  Average Hit Rate: {CacheMetrics.Average(c => c.HitRate):F1}%");
        if (MostEffectiveCache != null)
        {
            report.AppendLine($"  Most Effective Cache: {MostEffectiveCache.OperationType} ({MostEffectiveCache.HitRate:F1}% hit rate)");
        }

        return report.ToString();
    }
}