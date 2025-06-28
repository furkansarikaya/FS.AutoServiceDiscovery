namespace FS.AutoServiceDiscovery.Extensions.Performance;

/// <summary>
/// Represents the result of a preloading operation that proactively caches discovery results for future use.
/// 
/// Preloading is a strategic optimization technique that addresses the "cold start" problem in service discovery.
/// By performing discovery work proactively during application startup or idle periods, preloading ensures
/// that subsequent discovery operations can benefit from cached results immediately.
/// 
/// Think of preloading like pre-heating an oven before cooking - you invest some time upfront to ensure
/// that when you actually need to use the system, it's ready to perform at optimal speed immediately.
/// 
/// This result class provides comprehensive information about what was preloaded, how long it took,
/// and what benefits future operations can expect from the preloading effort.
/// </summary>
public class PreloadResult
{
    /// <summary>
    /// Gets or sets whether the preloading operation completed successfully.
    /// 
    /// A successful preload means that all specified assemblies were processed and their
    /// results were cached for future use. Partial failures (some assemblies failed) are
    /// still considered successful if the majority of the preloading completed.
    /// </summary>
    public bool IsSuccessful { get; set; } = false;

    /// <summary>
    /// Gets or sets the timestamp when the preloading operation began.
    /// </summary>
    public DateTime StartTime { get; set; }

    /// <summary>
    /// Gets or sets the total execution time for the preloading operation.
    /// 
    /// This represents the investment in time made upfront to improve future performance.
    /// The effectiveness of preloading can be measured by comparing this time to the
    /// cumulative time savings in subsequent discovery operations.
    /// </summary>
    public TimeSpan TotalExecutionTime { get; set; }

    /// <summary>
    /// Gets the collection of assemblies that were successfully preloaded and cached.
    /// 
    /// These assemblies will provide immediate cache hits in future discovery operations,
    /// resulting in significant performance improvements.
    /// </summary>
    public List<System.Reflection.Assembly> PreloadedAssemblies { get; } = new();

    /// <summary>
    /// Gets the collection of assemblies that failed to preload due to errors.
    /// 
    /// Failed assemblies will still require fresh discovery in future operations.
    /// Understanding which assemblies consistently fail to preload can help
    /// identify problematic assemblies or configuration issues.
    /// </summary>
    public List<System.Reflection.Assembly> FailedAssemblies { get; } = new();

    /// <summary>
    /// Gets the collection of assemblies that were skipped during preloading.
    /// 
    /// Assemblies might be skipped if they were already cached, if they don't contain
    /// discoverable services, or if they were excluded by preloading filters.
    /// </summary>
    public List<System.Reflection.Assembly> SkippedAssemblies { get; } = new();

    /// <summary>
    /// Gets or sets the total number of services discovered and cached during preloading.
    /// 
    /// This represents the "inventory" of services that are now available for immediate
    /// use in future discovery operations without additional processing time.
    /// </summary>
    public int TotalServicesPreloaded { get; set; }

    /// <summary>
    /// Gets detailed information about the preloading process for each assembly.
    /// 
    /// This granular information helps understand the performance characteristics
    /// of preloading different assemblies and can guide optimization efforts.
    /// </summary>
    public List<AssemblyPreloadDetails> AssemblyDetails { get; } = new();

    /// <summary>
    /// Gets or sets an error message if the preloading operation failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets the exception that caused the operation to fail, if applicable.
    /// </summary>
    public Exception? Exception { get; set; }

    /// <summary>
    /// Calculates the success ratio as a percentage of assemblies that were successfully preloaded.
    /// 
    /// A high success ratio indicates effective preloading, while a low ratio might suggest
    /// configuration issues or problematic assemblies that need attention.
    /// </summary>
    public double SuccessRatio
    {
        get
        {
            var totalProcessed = PreloadedAssemblies.Count + FailedAssemblies.Count;
            return totalProcessed > 0 ? (double)PreloadedAssemblies.Count / totalProcessed * 100 : 0;
        }
    }

    /// <summary>
    /// Calculates the average preloading time per assembly for successfully preloaded assemblies.
    /// 
    /// This metric helps estimate the cost of preloading and can be used to predict
    /// how long future preloading operations might take.
    /// </summary>
    public double AveragePreloadTimePerAssemblyMs
    {
        get
        {
            return PreloadedAssemblies.Count > 0 
                ? TotalExecutionTime.TotalMilliseconds / PreloadedAssemblies.Count 
                : 0;
        }
    }

    /// <summary>
    /// Gets performance statistics for this preloading operation.
    /// </summary>
    public PreloadPerformanceStatistics PerformanceStatistics => new()
    {
        TotalExecutionTimeMs = (long)TotalExecutionTime.TotalMilliseconds,
        PreloadedAssembliesCount = PreloadedAssemblies.Count,
        FailedAssembliesCount = FailedAssemblies.Count,
        SkippedAssembliesCount = SkippedAssemblies.Count,
        TotalServicesPreloaded = TotalServicesPreloaded,
        SuccessRatio = SuccessRatio,
        AveragePreloadTimePerAssemblyMs = AveragePreloadTimePerAssemblyMs
    };

    /// <summary>
    /// Contains detailed information about preloading a specific assembly.
    /// </summary>
    public class AssemblyPreloadDetails
    {
        /// <summary>
        /// Gets or sets the assembly that was preloaded.
        /// </summary>
        public System.Reflection.Assembly Assembly { get; set; } = null!;

        /// <summary>
        /// Gets or sets whether this assembly was successfully preloaded.
        /// </summary>
        public bool WasSuccessful { get; set; }

        /// <summary>
        /// Gets or sets the time spent preloading this specific assembly.
        /// </summary>
        public TimeSpan PreloadTime { get; set; }

        /// <summary>
        /// Gets or sets the number of services discovered in this assembly.
        /// </summary>
        public int ServicesDiscovered { get; set; }

        /// <summary>
        /// Gets or sets an error message if preloading this assembly failed.
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// Gets or sets additional metadata about the preloading process for this assembly.
        /// </summary>
        public Dictionary<string, object> Metadata { get; } = new();
    }

    /// <summary>
    /// Performance statistics specific to preloading operations.
    /// </summary>
    public class PreloadPerformanceStatistics
    {
        /// <summary>
        /// Gets or sets the total execution time of the preloading operation in milliseconds.
        /// </summary>
        public long TotalExecutionTimeMs { get; set; }
        
        /// <summary>
        /// Gets or sets the number of assemblies that were successfully preloaded.
        /// </summary>
        public int PreloadedAssembliesCount { get; set; }
        
        /// <summary>
        /// Gets or sets the number of assemblies that failed to be preloaded.
        /// </summary>
        public int FailedAssembliesCount { get; set; }
        
        /// <summary>
        /// Gets or sets the number of assemblies that were skipped during preloading.
        /// </summary>
        public int SkippedAssembliesCount { get; set; }
        
        /// <summary>
        /// Gets or sets the total number of services that were preloaded.
        /// </summary>
        public int TotalServicesPreloaded { get; set; }
        
        /// <summary>
        /// Gets or sets the success ratio as a percentage of assemblies that were successfully preloaded.
        /// </summary>
        public double SuccessRatio { get; set; }
        
        /// <summary>
        /// Gets or sets the average preloading time per assembly in milliseconds.
        /// </summary>
        public double AveragePreloadTimePerAssemblyMs { get; set; }
    }

    /// <summary>
    /// Generates a human-readable summary of the preloading operation.
    /// </summary>
    /// <returns>A formatted string summarizing the preloading results and effectiveness.</returns>
    public string GenerateSummaryReport()
    {
        var report = new System.Text.StringBuilder();
        
        report.AppendLine("=== Assembly Preloading Operation Summary ===");
        report.AppendLine($"Operation Status: {(IsSuccessful ? "Successful" : "Failed")}");
        report.AppendLine($"Start Time: {StartTime:yyyy-MM-dd HH:mm:ss.fff}");
        report.AppendLine($"Total Execution Time: {TotalExecutionTime.TotalMilliseconds:F2} ms");
        report.AppendLine();
        
        report.AppendLine("Preloading Results:");
        report.AppendLine($"  Successfully Preloaded: {PreloadedAssemblies.Count} assemblies");
        report.AppendLine($"  Failed to Preload: {FailedAssemblies.Count} assemblies");
        report.AppendLine($"  Skipped: {SkippedAssemblies.Count} assemblies");
        report.AppendLine($"  Success Ratio: {SuccessRatio:F1}%");
        report.AppendLine();
        
        report.AppendLine("Service Discovery:");
        report.AppendLine($"  Total Services Preloaded: {TotalServicesPreloaded}");
        report.AppendLine($"  Average Services per Assembly: {(PreloadedAssemblies.Count > 0 ? (double)TotalServicesPreloaded / PreloadedAssemblies.Count : 0):F1}");
        report.AppendLine();
        
        report.AppendLine("Performance Metrics:");
        report.AppendLine($"  Average Preload Time per Assembly: {AveragePreloadTimePerAssemblyMs:F2} ms");
        
        if (FailedAssemblies.Count != 0)
        {
            report.AppendLine();
            report.AppendLine("Failed Assemblies:");
            foreach (var failedAssembly in FailedAssemblies.Take(5)) // Show first 5 failures
            {
                report.AppendLine($"  - {failedAssembly.GetName().Name}");
            }
            if (FailedAssemblies.Count > 5)
            {
                report.AppendLine($"  ... and {FailedAssemblies.Count - 5} more");
            }
        }

        if (IsSuccessful || string.IsNullOrEmpty(ErrorMessage)) 
            return report.ToString();
        report.AppendLine();
        report.AppendLine("Error Information:");
        report.AppendLine($"  Error Message: {ErrorMessage}");

        return report.ToString();
    }
}