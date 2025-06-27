namespace FS.AutoServiceDiscovery.Extensions.Architecture.Conventions;

/// <summary>
/// Contains statistical information about naming convention resolver performance and usage patterns.
/// 
/// This class provides insights into how effectively the naming convention system is working
/// in your application. Understanding these metrics helps you optimize the system configuration
/// and identify potential improvements.
/// 
/// Think of this class as a "performance report card" for your naming convention system.
/// Just like you might track website analytics to understand user behavior, these statistics
/// help you understand how your service resolution patterns are working in practice.
/// </summary>
public class NamingConventionStatistics
{
    /// <summary>
    /// Gets or sets the total number of service type resolution attempts made by the resolver.
    /// 
    /// This metric provides context for all other statistics - it represents the total
    /// "workload" that the naming convention system has handled.
    /// </summary>
    public long TotalResolutionAttempts { get; set; }
    
    /// <summary>
    /// Gets or sets the number of successful resolutions where a naming convention was able
    /// to determine an appropriate service interface.
    /// 
    /// A high success rate indicates that your naming conventions are well-aligned with
    /// your codebase's actual naming patterns.
    /// </summary>
    public long SuccessfulResolutions { get; set; }
    
    /// <summary>
    /// Gets or sets the number of resolution attempts that failed because no naming convention
    /// could determine an appropriate service interface.
    /// 
    /// A high failure rate might indicate inconsistent naming patterns in your codebase,
    /// missing naming conventions for patterns you actually use, or types that genuinely 
    /// shouldn't be auto-registered.
    /// </summary>
    public long FailedResolutions { get; set; }
    
    /// <summary>
    /// Gets or sets performance statistics for each registered naming convention, keyed by convention name.
    /// 
    /// This detailed breakdown helps you understand which conventions are most frequently successful,
    /// whether convention priority ordering is optimal, and performance characteristics of individual conventions.
    /// </summary>
    public Dictionary<string, ConventionPerformanceMetrics> ConventionMetrics { get; set; } = new();
    
    /// <summary>
    /// Calculates the overall success rate as a percentage of successful resolutions.
    /// </summary>
    public double SuccessRate => TotalResolutionAttempts == 0 ? 0 : (double)SuccessfulResolutions / TotalResolutionAttempts * 100;
    
    /// <summary>
    /// Gets the convention with the highest number of successful resolutions.
    /// This indicates which naming pattern is most common in your codebase.
    /// </summary>
    public string? MostSuccessfulConvention => ConventionMetrics
        .OrderByDescending(kvp => kvp.Value.SuccessfulResolutions)
        .FirstOrDefault().Key;
}

/// <summary>
/// Performance metrics for an individual naming convention.
/// 
/// IMPORTANT: These are defined as fields (not properties) to support Interlocked operations
/// for thread-safe updates in high-concurrency scenarios.
/// </summary>
public class ConventionPerformanceMetrics
{
    /// <summary>
    /// The number of times this convention was consulted for resolution.
    /// This field is accessed directly for thread-safe Interlocked operations.
    /// </summary>
    public long ConsultationCount;
    
    /// <summary>
    /// The number of times this convention successfully resolved a service type.
    /// This field is accessed directly for thread-safe Interlocked operations.
    /// </summary>
    public long SuccessfulResolutions;
    
    /// <summary>
    /// The total time spent executing this convention across all consultations.
    /// Note: TimeSpan updates require special handling since Interlocked doesn't support TimeSpan directly.
    /// </summary>
    public TimeSpan TotalExecutionTime;
    
    /// <summary>
    /// Object used for locking during TimeSpan updates since TimeSpan cannot be used with Interlocked operations.
    /// </summary>
    internal readonly object TimeLock = new object();
    
    /// <summary>
    /// Calculates the success rate for this specific convention.
    /// </summary>
    public double SuccessRate => ConsultationCount == 0 ? 0 : (double)SuccessfulResolutions / ConsultationCount * 100;
    
    /// <summary>
    /// Calculates the average execution time for this convention.
    /// </summary>
    public double AverageExecutionTimeMs => ConsultationCount == 0 ? 0 : TotalExecutionTime.TotalMilliseconds / ConsultationCount;
}