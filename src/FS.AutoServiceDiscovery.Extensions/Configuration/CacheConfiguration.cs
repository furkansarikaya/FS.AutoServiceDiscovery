namespace FS.AutoServiceDiscovery.Extensions.Configuration;

/// <summary>
/// Configuration settings for assembly scan cache behavior, allowing fine-tuned control over
/// cache performance characteristics and memory usage patterns.
/// 
/// This configuration class addresses the reality that different applications have different
/// performance and memory constraints. A microservice might prioritize low memory usage,
/// while a large monolithic application might prioritize maximum cache hit rates regardless
/// of memory consumption.
/// 
/// The configuration options are designed based on common caching patterns and real-world
/// performance considerations that arise in production applications.
/// </summary>
public class CacheConfiguration
{
    /// <summary>
    /// Gets or sets the maximum number of assemblies to keep cached simultaneously.
    /// 
    /// This setting prevents the cache from growing unbounded in applications that
    /// dynamically load many assemblies. When this limit is reached, the least recently
    /// used assemblies will be evicted from the cache.
    /// 
    /// Setting this too low will reduce cache effectiveness, while setting it too high
    /// might consume excessive memory. The optimal value depends on your application's
    /// assembly loading patterns and available memory.
    /// 
    /// Default value of 50 should be sufficient for most applications, including those
    /// with moderate plugin architectures or microservice compositions.
    /// </summary>
    public int MaxCachedAssemblies { get; set; } = 50;

    /// <summary>
    /// Gets or sets the time after which cached assembly data is considered stale and
    /// should be refreshed, even if the assembly file hasn't changed.
    /// 
    /// This setting provides a safety net against cache corruption or scenarios where
    /// file system metadata might not accurately reflect assembly changes. It also
    /// helps in development environments where assemblies might be modified in ways
    /// that don't update file timestamps.
    /// 
    /// Setting this too low will reduce cache effectiveness, while setting it too high
    /// might cause the system to use stale data in edge cases. The default of 1 hour
    /// balances safety with performance for most scenarios.
    /// </summary>
    public TimeSpan MaxCacheAge { get; set; } = TimeSpan.FromHours(1);

    /// <summary>
    /// Gets or sets whether to enable detailed cache logging for debugging and monitoring purposes.
    /// 
    /// When enabled, the cache will log detailed information about:
    /// - Cache hits and misses with reasons
    /// - Assembly invalidation events and triggers
    /// - Performance statistics and trends
    /// - Memory usage patterns
    /// 
    /// This logging is valuable for understanding cache behavior but adds overhead.
    /// Consider enabling this in development and staging environments, or selectively
    /// in production when investigating performance issues.
    /// </summary>
    public bool EnableDetailedLogging { get; set; } = false;

    /// <summary>
    /// Gets or sets whether to preload commonly used assemblies into the cache during application startup.
    /// 
    /// Preloading can improve the user experience by ensuring that the first requests don't
    /// experience cache miss penalties. However, it increases startup time and memory usage.
    /// 
    /// This feature is most beneficial in applications where:
    /// - Startup time is less critical than first-request performance
    /// - You have predictable assembly usage patterns
    /// - Memory usage is not a primary constraint
    /// </summary>
    public bool EnablePreloading { get; set; } = false;

    /// <summary>
    /// Gets or sets the collection of assembly name patterns to preload when preloading is enabled.
    /// 
    /// This allows selective preloading of only the assemblies that are most likely to be used,
    /// balancing the benefits of preloading with the costs of increased startup time and memory usage.
    /// 
    /// Patterns support wildcards and can include:
    /// - Exact names: "MyApp.Services"
    /// - Prefix patterns: "MyApp.*"
    /// - Suffix patterns: "*.Services"
    /// 
    /// If this collection is empty when preloading is enabled, all assemblies in the current
    /// application domain will be considered for preloading.
    /// </summary>
    public List<string> PreloadPatterns { get; set; } = new();

    /// <summary>
    /// Creates a configuration optimized for development environments where assemblies change frequently
    /// and cache invalidation needs to be responsive to changes.
    /// </summary>
    /// <returns>A cache configuration suitable for development scenarios.</returns>
    public static CacheConfiguration ForDevelopment() => new()
    {
        MaxCachedAssemblies = 20, // Lower memory usage for development machines
        MaxCacheAge = TimeSpan.FromMinutes(10), // More frequent refresh for rapidly changing code
        EnableDetailedLogging = true, // More logging for debugging
        EnablePreloading = false // Faster startup for development iterations
    };

    /// <summary>
    /// Creates a configuration optimized for production environments where performance and
    /// memory efficiency are primary concerns.
    /// </summary>
    /// <returns>A cache configuration suitable for production scenarios.</returns>
    public static CacheConfiguration ForProduction() => new()
    {
        MaxCachedAssemblies = 100, // Higher cache capacity for better hit rates
        MaxCacheAge = TimeSpan.FromHours(4), // Longer cache retention for stable assemblies
        EnableDetailedLogging = false, // Minimal logging overhead
        EnablePreloading = true // Better first-request performance
    };

    /// <summary>
    /// Creates a configuration optimized for testing environments where cache behavior
    /// should be predictable and not interfere with test isolation.
    /// </summary>
    /// <returns>A cache configuration suitable for testing scenarios.</returns>
    public static CacheConfiguration ForTesting() => new()
    {
        MaxCachedAssemblies = 10, // Minimal caching for test isolation
        MaxCacheAge = TimeSpan.FromMinutes(1), // Short cache duration for fresh test runs
        EnableDetailedLogging = false, // Reduce test output noise
        EnablePreloading = false // Faster test startup
    };
}