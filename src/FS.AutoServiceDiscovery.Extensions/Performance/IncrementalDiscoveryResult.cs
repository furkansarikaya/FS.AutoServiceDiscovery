using FS.AutoServiceDiscovery.Extensions.Configuration;

namespace FS.AutoServiceDiscovery.Extensions.Performance;

/// <summary>
/// Represents the result of an incremental service discovery operation that processes only changed assemblies.
/// 
/// Incremental discovery is a sophisticated optimization strategy that addresses a common problem in development
/// and CI/CD environments: most of the time, only a small subset of assemblies change between discovery operations.
/// By intelligently detecting changes and processing only what's necessary, incremental discovery can provide
/// dramatic performance improvements in iterative scenarios.
/// 
/// Think of incremental discovery like a smart backup system that only backs up files that have changed since
/// the last backup. This approach provides most of the benefits of a full backup while requiring only a fraction
/// of the time and resources.
/// 
/// This result class provides detailed information about what was analyzed, what changed, and what was processed,
/// enabling developers to understand and optimize their incremental discovery strategies.
/// </summary>
public class IncrementalDiscoveryResult
{
    /// <summary>
    /// Gets or sets whether the incremental discovery operation completed successfully.
    /// 
    /// Success in incremental discovery means that the change detection worked correctly,
    /// necessary assemblies were processed, and the results were properly merged with
    /// existing cached data.
    /// </summary>
    public bool IsSuccessful { get; set; } = false;

    /// <summary>
    /// Gets or sets the timestamp when the incremental discovery operation began.
    /// </summary>
    public DateTime StartTime { get; set; }

    /// <summary>
    /// Gets or sets the total execution time for the incremental discovery operation.
    /// 
    /// This time should typically be much shorter than a full discovery operation,
    /// as incremental discovery only processes changed assemblies.
    /// </summary>
    public TimeSpan TotalExecutionTime { get; set; }

    /// <summary>
    /// Gets or sets the baseline timestamp used for change detection.
    /// 
    /// Assemblies modified after this timestamp were considered for reprocessing.
    /// This timestamp helps correlate the incremental discovery results with
    /// the specific point in time used as the baseline.
    /// </summary>
    public DateTime BaselineTimestamp { get; set; }

    /// <summary>
    /// Gets the collection of all assemblies that were analyzed for changes.
    /// 
    /// This represents the complete set of assemblies that were considered
    /// during the incremental discovery operation, regardless of whether
    /// they were actually processed.
    /// </summary>
    public List<System.Reflection.Assembly> AnalyzedAssemblies { get; } = new();

    /// <summary>
    /// Gets the collection of assemblies that were detected as changed and processed.
    /// 
    /// These assemblies required fresh discovery because they were modified after
    /// the baseline timestamp. The ratio of changed to analyzed assemblies indicates
    /// the effectiveness of the incremental strategy.
    /// </summary>
    public List<System.Reflection.Assembly> ChangedAssemblies { get; } = new();

    /// <summary>
    /// Gets the collection of assemblies that were unchanged and skipped.
    /// 
    /// These assemblies had their results retrieved from cache, providing the
    /// performance benefit of incremental discovery. A high ratio of unchanged
    /// assemblies indicates effective incremental discovery.
    /// </summary>
    public List<System.Reflection.Assembly> UnchangedAssemblies { get; } = new();

    /// <summary>
    /// Gets the collection of new services discovered from changed assemblies.
    /// 
    /// These are the services that were discovered by processing the changed assemblies.
    /// This collection represents the "delta" from the incremental discovery operation.
    /// </summary>
    public List<ServiceRegistrationInfo> NewlyDiscoveredServices { get; } = new();

    /// <summary>
    /// Gets the collection of all services after merging new discoveries with cached results.
    /// 
    /// This represents the complete, up-to-date set of all discovered services,
    /// combining both newly discovered services and previously cached services
    /// from unchanged assemblies.
    /// </summary>
    public List<ServiceRegistrationInfo> MergedServiceResults { get; } = new();

    /// <summary>
    /// Gets or sets detailed information about the change detection process.
    /// 
    /// This information helps understand how the change detection algorithm
    /// determined which assemblies needed reprocessing.
    /// </summary>
    public ChangeDetectionDetails ChangeDetection { get; set; } = new();

    /// <summary>
    /// Gets or sets an error message if the incremental discovery operation failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets the exception that caused the operation to fail, if applicable.
    /// </summary>
    public Exception? Exception { get; set; }

    /// <summary>
    /// Calculates the change ratio as a percentage of assemblies that required processing.
    /// 
    /// A lower change ratio indicates more effective incremental discovery, as fewer
    /// assemblies needed to be processed fresh.
    /// </summary>
    public double ChangeRatio
    {
        get
        {
            var totalAssemblies = AnalyzedAssemblies.Count;
            return totalAssemblies > 0 ? (double)ChangedAssemblies.Count / totalAssemblies * 100 : 0;
        }
    }

    /// <summary>
    /// Calculates the time savings compared to a hypothetical full discovery operation.
    /// 
    /// This metric estimates how much time was saved by using incremental discovery
    /// instead of processing all assemblies fresh.
    /// </summary>
    public TimeSpan EstimatedTimeSavings
    {
        get
        {
            if (UnchangedAssemblies.Count == 0) return TimeSpan.Zero;
            
            // Estimate time per assembly based on processed assemblies
            var avgTimePerAssembly = ChangedAssemblies.Count > 0 
                ? TotalExecutionTime.TotalMilliseconds / ChangedAssemblies.Count 
                : 0;
            
            var estimatedSavedTime = UnchangedAssemblies.Count * avgTimePerAssembly;
            return TimeSpan.FromMilliseconds(estimatedSavedTime);
        }
    }

    /// <summary>
    /// Gets performance statistics for this incremental discovery operation.
    /// </summary>
    public IncrementalPerformanceStatistics PerformanceStatistics => new()
    {
        TotalExecutionTimeMs = (long)TotalExecutionTime.TotalMilliseconds,
        AnalyzedAssembliesCount = AnalyzedAssemblies.Count,
        ChangedAssembliesCount = ChangedAssemblies.Count,
        UnchangedAssembliesCount = UnchangedAssemblies.Count,
        ChangeRatio = ChangeRatio,
        NewServicesDiscovered = NewlyDiscoveredServices.Count,
        TotalServicesAfterMerge = MergedServiceResults.Count,
        EstimatedTimeSavingsMs = (long)EstimatedTimeSavings.TotalMilliseconds
    };

    /// <summary>
    /// Contains detailed information about the change detection process.
    /// </summary>
    public class ChangeDetectionDetails
    {
        /// <summary>
        /// Gets or sets the algorithm used for change detection (e.g., "FileTimestamp", "HashComparison").
        /// </summary>
        public string DetectionMethod { get; set; } = "FileTimestamp";

        /// <summary>
        /// Gets or sets the time spent performing change detection analysis.
        /// </summary>
        public TimeSpan ChangeDetectionTime { get; set; }

        /// <summary>
        /// Gets the collection of assemblies that could not be analyzed for changes.
        /// 
        /// This might include dynamic assemblies or assemblies with inaccessible file metadata.
        /// </summary>
        public List<string> UnanalyzableAssemblies { get; } = new();

        /// <summary>
        /// Gets additional metadata about the change detection process.
        /// </summary>
        public Dictionary<string, object> AdditionalMetadata { get; } = new();
    }

    /// <summary>
    /// Performance statistics specific to incremental discovery operations.
    /// </summary>
    public class IncrementalPerformanceStatistics
    {
        /// <summary>
        /// Gets the total execution time of the incremental discovery operation in milliseconds.
        /// </summary>
        public long TotalExecutionTimeMs { get; set; }
        
        /// <summary>
        /// Gets the number of assemblies analyzed for changes.
        /// </summary>
        public int AnalyzedAssembliesCount { get; set; }
        
        /// <summary>
        /// Gets the number of assemblies that required processing.
        /// </summary>
        public int ChangedAssembliesCount { get; set; }
        
        /// <summary>
        /// Gets the number of assemblies that did not require processing.
        /// </summary>
        public int UnchangedAssembliesCount { get; set; }
        
        /// <summary>
        /// Gets the change ratio as a percentage of assemblies that required processing.
        /// </summary>
        public double ChangeRatio { get; set; }
        
        /// <summary>
        /// Gets the number of new services discovered during the incremental discovery operation.
        /// </summary>
        public int NewServicesDiscovered { get; set; }
        
        /// <summary>
        /// Gets the total number of services after merging the results.
        /// </summary>
        public int TotalServicesAfterMerge { get; set; }
        
        /// <summary>
        /// Gets the estimated time savings in milliseconds compared to a hypothetical full discovery operation.
        /// </summary>
        public long EstimatedTimeSavingsMs { get; set; }
    }

    /// <summary>
    /// Generates a human-readable summary of the incremental discovery operation.
    /// </summary>
    /// <returns>A formatted string summarizing the operation and its efficiency gains.</returns>
    public string GenerateSummaryReport()
    {
        var report = new System.Text.StringBuilder();
        
        report.AppendLine("=== Incremental Discovery Operation Summary ===");
        report.AppendLine($"Operation Status: {(IsSuccessful ? "Successful" : "Failed")}");
        report.AppendLine($"Start Time: {StartTime:yyyy-MM-dd HH:mm:ss.fff}");
        report.AppendLine($"Total Execution Time: {TotalExecutionTime.TotalMilliseconds:F2} ms");
        report.AppendLine($"Baseline Timestamp: {BaselineTimestamp:yyyy-MM-dd HH:mm:ss.fff}");
        report.AppendLine();
        
        report.AppendLine("Change Analysis:");
        report.AppendLine($"  Total Assemblies Analyzed: {AnalyzedAssemblies.Count}");
        report.AppendLine($"  Changed Assemblies: {ChangedAssemblies.Count}");
        report.AppendLine($"  Unchanged Assemblies: {UnchangedAssemblies.Count}");
        report.AppendLine($"  Change Ratio: {ChangeRatio:F1}%");
        report.AppendLine();
        
        report.AppendLine("Discovery Results:");
        report.AppendLine($"  Newly Discovered Services: {NewlyDiscoveredServices.Count}");
        report.AppendLine($"  Total Services (after merge): {MergedServiceResults.Count}");
        report.AppendLine();
        
        report.AppendLine("Performance Impact:");
        report.AppendLine($"  Estimated Time Savings: {EstimatedTimeSavings.TotalMilliseconds:F2} ms");
        report.AppendLine($"  Change Detection Time: {ChangeDetection.ChangeDetectionTime.TotalMilliseconds:F2} ms");

        if (IsSuccessful || string.IsNullOrEmpty(ErrorMessage)) 
            return report.ToString();
        report.AppendLine();
        report.AppendLine("Error Information:");
        report.AppendLine($"  Error Message: {ErrorMessage}");

        return report.ToString();
    }
}