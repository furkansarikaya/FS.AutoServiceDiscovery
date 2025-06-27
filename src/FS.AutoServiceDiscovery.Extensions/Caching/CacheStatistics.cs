namespace FS.AutoServiceDiscovery.Extensions.Caching;

/// <summary>
/// Provides detailed statistics about cache performance for monitoring and optimization purposes.
/// These metrics help developers understand cache effectiveness and identify potential performance issues.
/// </summary>
public class CacheStatistics
{
    /// <summary>
    /// Gets or sets the total number of cache lookup attempts.
    /// This includes both hits and misses.
    /// </summary>
    public long TotalRequests { get; set; }
    
    /// <summary>
    /// Gets or sets the number of successful cache hits.
    /// A higher hit rate indicates better performance.
    /// </summary>
    public long CacheHits { get; set; }
    
    /// <summary>
    /// Gets or sets the number of cache misses (lookups that didn't find cached data).
    /// High miss rates might indicate cache invalidation issues or insufficient cache warming.
    /// </summary>
    public long CacheMisses { get; set; }
    
    /// <summary>
    /// Gets or sets the number of assemblies currently cached.
    /// This helps monitor memory usage.
    /// </summary>
    public int CachedAssembliesCount { get; set; }
    
    /// <summary>
    /// Gets or sets the total number of cached service registrations across all assemblies.
    /// This metric helps understand the cache's memory footprint.
    /// </summary>
    public int TotalCachedServices { get; set; }
    
    /// <summary>
    /// Calculates the cache hit ratio as a percentage.
    /// Values closer to 100% indicate excellent cache performance.
    /// </summary>
    public double HitRatio => TotalRequests == 0 ? 0 : (double)CacheHits / TotalRequests * 100;
    
    /// <summary>
    /// Gets the average number of services per cached assembly.
    /// This metric helps identify assemblies with unusual service counts.
    /// </summary>
    public double AverageServicesPerAssembly => CachedAssembliesCount == 0 ? 0 : (double)TotalCachedServices / CachedAssembliesCount;
}