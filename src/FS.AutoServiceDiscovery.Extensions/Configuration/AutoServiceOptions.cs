using FS.AutoServiceDiscovery.Extensions.Caching;
using Microsoft.Extensions.Configuration;

namespace FS.AutoServiceDiscovery.Extensions.Configuration;

/// <summary>
/// Configuration options for automatic service discovery and registration with performance optimization settings.
/// </summary>
public class AutoServiceOptions
{
    /// <summary>
    /// Gets or sets the active profile for profile-based service registration.
    /// Services marked with specific profiles will only be registered when this profile matches.
    /// </summary>
    public string? Profile { get; set; }
    
    /// <summary>
    /// Gets or sets whether the application is running in a test environment.
    /// Services marked with IgnoreInTests = true will be skipped during registration.
    /// Default is false.
    /// </summary>
    public bool IsTestEnvironment { get; set; } = false;
    
    /// <summary>
    /// Gets or sets whether to enable logging of service registration operations.
    /// When enabled, each registered service will be logged to the console.
    /// Default is true.
    /// </summary>
    public bool EnableLogging { get; set; } = true;
    
    /// <summary>
    /// Gets or sets the configuration instance used for conditional service registration.
    /// Required for services that use <see cref="Attributes.ConditionalServiceAttribute"/>.
    /// </summary>
    public IConfiguration? Configuration { get; set; }
    
    /// <summary>
    /// Gets or sets whether to enable performance optimizations such as caching and parallel processing.
    /// When enabled, the discovery process will use assembly caching and optimized scanning algorithms.
    /// Default is true.
    /// </summary>
    public bool EnablePerformanceOptimizations { get; set; } = true;
    
    /// <summary>
    /// Gets or sets whether to enable parallel processing when scanning multiple assemblies.
    /// This can significantly improve performance when scanning many large assemblies.
    /// Default is true.
    /// </summary>
    public bool EnableParallelProcessing { get; set; } = true;
    
    /// <summary>
    /// Gets or sets the custom assembly scan cache implementation.
    /// If not provided, a default in-memory cache will be used.
    /// </summary>
    public IAssemblyScanCache? CustomCache { get; set; }
    
    /// <summary>
    /// Gets or sets the maximum degree of parallelism for assembly scanning.
    /// If not specified, defaults to the number of processor cores.
    /// Setting this to 1 effectively disables parallel processing.
    /// </summary>
    public int? MaxDegreeOfParallelism { get; set; }
    
    /// <summary>
    /// Gets or sets whether to enable detailed performance metrics collection.
    /// When enabled, detailed timing and cache statistics will be collected and can be accessed
    /// through the GetCacheStatistics method.
    /// Default is false (to minimize overhead in production).
    /// </summary>
    public bool EnablePerformanceMetrics { get; set; } = false;
    
    /// <summary>
    /// Gets or sets whether to enable plugin registration.
    /// When enabled, plugins can be registered and discovered.
    /// Default is false.
    /// </summary>
    public bool EnablePlugins { get; set; } = false;
}