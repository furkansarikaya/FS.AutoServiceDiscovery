using FS.AutoServiceDiscovery.Extensions.Architecture;
using FS.AutoServiceDiscovery.Extensions.Configuration;

namespace FS.AutoServiceDiscovery.Extensions.Performance;

/// <summary>
/// Represents the comprehensive result of an optimized service discovery operation, including performance metrics,
/// cache utilization statistics, and detailed execution information.
/// 
/// This class serves as the "comprehensive report card" for an optimized discovery operation. Just like a
/// performance review provides not just the final results but also details about how those results were
/// achieved, this class provides both the discovered services and extensive metadata about the discovery process.
/// 
/// The richness of information in this result enables several important scenarios:
/// 1. **Performance Analysis**: Understanding which optimizations were effective
/// 2. **Debugging**: Diagnosing issues when discovery doesn't work as expected
/// 3. **Monitoring**: Tracking discovery performance over time in production systems
/// 4. **Optimization**: Identifying opportunities for further performance improvements
/// 5. **Reporting**: Providing detailed information for stakeholders about system behavior
/// 
/// This comprehensive approach transforms service discovery from a "black box" operation into
/// a fully observable and analyzable process.
/// </summary>
public class OptimizedDiscoveryResult
{
    /// <summary>
    /// Gets or sets whether the overall discovery operation completed successfully.
    /// 
    /// This represents the high-level success status. A successful operation means that discovery
    /// completed without critical errors, though there might have been warnings or minor issues
    /// that didn't prevent the operation from completing.
    /// </summary>
    public bool IsSuccessful { get; set; } = false;

    /// <summary>
    /// Gets or sets the timestamp when the discovery operation began.
    /// This provides the starting point for understanding the timeline of the operation.
    /// </summary>
    public DateTime StartTime { get; set; }

    /// <summary>
    /// Gets or sets the total execution time for the entire discovery operation.
    /// 
    /// This is the primary performance metric - it represents the total time from when
    /// discovery started until all results were available. This includes all phases:
    /// cache lookups, fresh discovery, plugin execution, and result validation.
    /// </summary>
    public TimeSpan TotalExecutionTime { get; set; }

    /// <summary>
    /// Gets or sets the configuration options that were used for this discovery operation.
    /// 
    /// Storing the options with the result enables correlation between configuration
    /// choices and performance outcomes, which is valuable for optimization analysis.
    /// </summary>
    public AutoServiceOptions Options { get; set; } = new();

    /// <summary>
    /// Gets the collection of all services discovered during this operation.
    /// 
    /// This is the primary output of the discovery process - the collection of services
    /// that are ready to be registered with the dependency injection container.
    /// </summary>
    public List<ServiceRegistrationInfo> AllDiscoveredServices { get; set; } = [];

    /// <summary>
    /// Gets the collection of assemblies that had their results retrieved from cache.
    /// 
    /// This metric is crucial for understanding cache effectiveness. A high ratio of
    /// cached to processed assemblies indicates that the caching strategy is working
    /// effectively and providing significant performance benefits.
    /// </summary>
    public List<System.Reflection.Assembly> CachedAssemblies { get; set; } = new();

    /// <summary>
    /// Gets the collection of assemblies that were processed fresh (not from cache).
    /// 
    /// These assemblies required the full discovery process, including reflection operations,
    /// attribute reading, and service candidate identification. Understanding which assemblies
    /// consistently require fresh processing can help optimize caching strategies.
    /// </summary>
    public List<System.Reflection.Assembly> ProcessedAssemblies { get; set; } = new();

    /// <summary>
    /// Gets or sets the result of plugin execution, if plugins were enabled for this operation.
    /// 
    /// Plugin execution represents additional discovery logic beyond the standard attribute-based
    /// approach. The plugin results provide insights into how well custom discovery strategies
    /// are performing and what they're contributing to the overall discovery process.
    /// </summary>
    public PluginExecutionResult? PluginExecutionResult { get; set; }

    /// <summary>
    /// Gets or sets an error message if the discovery operation failed.
    /// 
    /// When IsSuccessful is false, this property provides human-readable information
    /// about what went wrong during the discovery process.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets the exception that caused the discovery operation to fail, if applicable.
    /// 
    /// This provides detailed technical information about failures, which is particularly
    /// valuable for debugging and error reporting in development environments.
    /// </summary>
    public Exception? Exception { get; set; }

    /// <summary>
    /// Calculates the cache hit ratio as a percentage of assemblies that were served from cache.
    /// 
    /// This is a key performance indicator for the discovery system. Higher cache hit ratios
    /// indicate better performance and more effective caching strategies.
    /// </summary>
    public double CacheHitRatio
    {
        get
        {
            var totalAssemblies = CachedAssemblies.Count + ProcessedAssemblies.Count;
            return totalAssemblies > 0 ? (double)CachedAssemblies.Count / totalAssemblies * 100 : 0;
        }
    }

    /// <summary>
    /// Calculates the average discovery time per assembly for assemblies that were processed fresh.
    /// 
    /// This metric helps understand the computational cost of discovery operations and can
    /// guide optimization efforts for particularly expensive assemblies.
    /// </summary>
    public double AverageProcessingTimePerAssemblyMs
    {
        get
        {
            if (ProcessedAssemblies.Count == 0) return 0;
            
            // Estimate processing time by subtracting plugin execution time from total time
            var processingTime = TotalExecutionTime;
            if (PluginExecutionResult != null)
            {
                processingTime = processingTime.Subtract(PluginExecutionResult.TotalExecutionTime);
            }
            
            return processingTime.TotalMilliseconds / ProcessedAssemblies.Count;
        }
    }

    /// <summary>
    /// Gets performance statistics derived from this discovery operation.
    /// 
    /// This computed property provides structured performance data that can be easily
    /// consumed by monitoring systems or performance analysis tools.
    /// </summary>
    public DiscoveryPerformanceStatistics PerformanceStatistics => new()
    {
        TotalExecutionTimeMs = (long)TotalExecutionTime.TotalMilliseconds,
        TotalServicesDiscovered = AllDiscoveredServices.Count,
        CacheHitRatio = CacheHitRatio,
        AssembliesFromCache = CachedAssemblies.Count,
        AssembliesProcessedFresh = ProcessedAssemblies.Count,
        PluginsExecuted = PluginExecutionResult?.PluginResults.Count ?? 0,
        AverageProcessingTimePerAssemblyMs = AverageProcessingTimePerAssemblyMs
    };

    /// <summary>
    /// Generates a human-readable summary of the discovery operation for logging or debugging purposes.
    /// 
    /// This method creates a formatted text report that can be easily read by developers
    /// or included in log files for troubleshooting purposes.
    /// </summary>
    /// <returns>A formatted string summarizing the discovery operation and its results.</returns>
    public string GenerateSummaryReport()
    {
        var report = new System.Text.StringBuilder();
        
        report.AppendLine("=== Optimized Discovery Operation Summary ===");
        report.AppendLine($"Operation Status: {(IsSuccessful ? "Successful" : "Failed")}");
        report.AppendLine($"Start Time: {StartTime:yyyy-MM-dd HH:mm:ss.fff}");
        report.AppendLine($"Total Execution Time: {TotalExecutionTime.TotalMilliseconds:F2} ms");
        report.AppendLine();
        
        report.AppendLine("Service Discovery Results:");
        report.AppendLine($"  Total Services Discovered: {AllDiscoveredServices.Count}");
        report.AppendLine($"  Services by Lifetime:");
        
        var servicesByLifetime = AllDiscoveredServices.GroupBy(s => s.Lifetime);
        foreach (var group in servicesByLifetime)
        {
            report.AppendLine($"    {group.Key}: {group.Count()} services");
        }
        report.AppendLine();
        
        report.AppendLine("Cache Performance:");
        report.AppendLine($"  Cache Hit Ratio: {CacheHitRatio:F1}%");
        report.AppendLine($"  Assemblies from Cache: {CachedAssemblies.Count}");
        report.AppendLine($"  Assemblies Processed Fresh: {ProcessedAssemblies.Count}");
        report.AppendLine($"  Average Processing Time per Assembly: {AverageProcessingTimePerAssemblyMs:F2} ms");
        report.AppendLine();
        
        if (PluginExecutionResult != null)
        {
            report.AppendLine("Plugin Execution:");
            report.AppendLine($"  Plugins Executed: {PluginExecutionResult.PluginResults.Count}");
            report.AppendLine($"  Plugin Execution Time: {PluginExecutionResult.TotalExecutionTime.TotalMilliseconds:F2} ms");
            report.AppendLine($"  Services from Plugins: {PluginExecutionResult.AllDiscoveredServices.Count}");
        }
        
        if (!IsSuccessful && !string.IsNullOrEmpty(ErrorMessage))
        {
            report.AppendLine();
            report.AppendLine("Error Information:");
            report.AppendLine($"  Error Message: {ErrorMessage}");
        }
        
        return report.ToString();
    }

    /// <summary>
    /// Performance statistics structure for easy consumption by monitoring systems.
    /// </summary>
    public class DiscoveryPerformanceStatistics
    {
        /// <summary>
        /// Total execution time in milliseconds.
        /// </summary>
        public long TotalExecutionTimeMs { get; set; }
        
        /// <summary>
        /// Total number of services discovered.
        /// </summary>
        public int TotalServicesDiscovered { get; set; }
        
        /// <summary>
        /// Cache hit ratio as a percentage.
        /// </summary>
        public double CacheHitRatio { get; set; }
        
        /// <summary>
        /// Number of assemblies retrieved from cache.
        /// </summary>
        public int AssembliesFromCache { get; set; }
        
        /// <summary>
        /// Number of assemblies processed fresh.
        /// </summary>
        public int AssembliesProcessedFresh { get; set; }
        
        /// <summary>
        /// Number of plugins executed.
        /// </summary>
        public int PluginsExecuted { get; set; }
        
        /// <summary>
        /// Average processing time per assembly in milliseconds.
        /// </summary>
        public double AverageProcessingTimePerAssemblyMs { get; set; }
    }
}