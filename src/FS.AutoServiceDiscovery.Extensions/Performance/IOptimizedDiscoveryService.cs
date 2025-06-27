using System.Reflection;
using FS.AutoServiceDiscovery.Extensions.Configuration;

namespace FS.AutoServiceDiscovery.Extensions.Performance;

/// <summary>
/// Defines a contract for high-performance service discovery operations that integrate all optimization strategies.
/// 
/// This interface represents the "premium version" of our discovery system - it coordinates all the performance
/// optimizations, caching strategies, and advanced features to provide the fastest possible service discovery
/// experience. Think of it as the difference between a regular car and a Formula 1 race car - both get you
/// from point A to point B, but one is specifically engineered for maximum performance.
/// 
/// The optimized discovery service addresses several performance challenges that arise in large-scale applications:
/// 1. **Cold Start Performance**: Minimizing the impact of the first discovery operation
/// 2. **Repeated Discovery**: Leveraging caching for subsequent operations
/// 3. **Parallel Processing**: Utilizing multi-core systems effectively
/// 4. **Memory Efficiency**: Minimizing memory allocations and garbage collection pressure
/// 5. **Incremental Discovery**: Supporting scenarios where only some assemblies need re-scanning
/// 
/// This service is particularly valuable in scenarios where discovery performance directly impacts
/// user experience, such as application startup time, test execution speed, or dynamic module loading.
/// </summary>
public interface IOptimizedDiscoveryService
{
    /// <summary>
    /// Performs optimized service discovery across multiple assemblies with full performance monitoring.
    /// 
    /// This method represents the culmination of all our optimization efforts. It orchestrates caching,
    /// parallel processing, plugin coordination, and performance monitoring to deliver the fastest
    /// possible discovery experience while maintaining comprehensive observability.
    /// 
    /// The optimization process follows a sophisticated workflow:
    /// 1. **Preprocessing**: Analyze assemblies to determine optimal processing strategy
    /// 2. **Cache Lookup**: Check for cached results to avoid redundant work
    /// 3. **Parallel Execution**: Process uncached assemblies in parallel when beneficial
    /// 4. **Plugin Coordination**: Execute relevant plugins against appropriate assemblies
    /// 5. **Result Validation**: Ensure all discovered services are valid and conflict-free
    /// 6. **Performance Recording**: Capture detailed metrics for monitoring and optimization
    /// 7. **Result Caching**: Store results for future use
    /// 8. **Final Assembly**: Combine all results into a unified, ordered collection
    /// 
    /// This comprehensive approach ensures maximum performance while maintaining reliability and observability.
    /// </summary>
    /// <param name="assemblies">
    /// The assemblies to discover services from. The service will automatically determine the optimal
    /// processing strategy based on assembly characteristics and current cache state.
    /// </param>
    /// <param name="options">
    /// Configuration options that control discovery behavior, including performance settings,
    /// plugin configuration, and monitoring preferences.
    /// </param>
    /// <returns>
    /// A comprehensive discovery result that includes all discovered services, performance metrics,
    /// validation results, and detailed execution information for analysis and monitoring.
    /// </returns>
    Task<OptimizedDiscoveryResult> DiscoverServicesAsync(IEnumerable<Assembly> assemblies, AutoServiceOptions options);

    /// <summary>
    /// Performs incremental service discovery, processing only assemblies that have changed since the last discovery.
    /// 
    /// This method addresses a common scenario in development and continuous integration environments
    /// where only a subset of assemblies change between discovery operations. By intelligently detecting
    /// changes and processing only what's necessary, this method can dramatically reduce discovery time
    /// in iterative scenarios.
    /// 
    /// The incremental discovery process involves:
    /// 1. **Change Detection**: Compare current assemblies with cached metadata to identify changes
    /// 2. **Dependency Analysis**: Determine if changes in one assembly affect discovery in others
    /// 3. **Selective Processing**: Process only assemblies that require re-scanning
    /// 4. **Result Merging**: Combine new results with existing cached results
    /// 5. **Cache Update**: Update the cache with new results while preserving valid existing data
    /// 
    /// This approach is particularly valuable in development environments where rapid iteration
    /// cycles require frequent discovery operations.
    /// </summary>
    /// <param name="assemblies">The complete set of assemblies to consider for discovery.</param>
    /// <param name="options">Configuration options for the discovery process.</param>
    /// <param name="lastDiscoveryTime">
    /// The timestamp of the last discovery operation, used as a baseline for change detection.
    /// Assemblies modified after this time will be considered for re-processing.
    /// </param>
    /// <returns>
    /// An incremental discovery result that indicates which assemblies were processed and
    /// provides both the newly discovered services and the complete merged result set.
    /// </returns>
    Task<IncrementalDiscoveryResult> DiscoverServicesIncrementalAsync(
        IEnumerable<Assembly> assemblies, 
        AutoServiceOptions options, 
        DateTime lastDiscoveryTime);

    /// <summary>
    /// Preloads and caches discovery results for the specified assemblies to improve future discovery performance.
    /// 
    /// This method implements a "warming" strategy where discovery work is performed proactively
    /// during application startup or idle periods to ensure that subsequent discovery operations
    /// are as fast as possible. This is particularly valuable in scenarios where predictable
    /// discovery performance is more important than immediate startup time.
    /// 
    /// The preloading process operates independently of immediate discovery needs, allowing it
    /// to use background processing and more aggressive optimization strategies without impacting
    /// the application's primary workflows.
    /// </summary>
    /// <param name="assemblies">The assemblies to preload into the cache.</param>
    /// <param name="options">Configuration options for the preloading process.</param>
    /// <returns>
    /// A task that completes when preloading is finished, providing information about
    /// what was cached and any issues encountered during the process.
    /// </returns>
    Task<PreloadResult> PreloadAssembliesAsync(IEnumerable<Assembly> assemblies, AutoServiceOptions options);

    /// <summary>
    /// Gets comprehensive performance statistics for all discovery operations performed by this service.
    /// 
    /// These statistics provide deep insights into the performance characteristics of the discovery
    /// system, enabling data-driven optimization decisions and proactive performance monitoring.
    /// </summary>
    /// <returns>
    /// Detailed performance statistics covering all aspects of the optimized discovery process.
    /// </returns>
    PerformanceMetricsSummary GetPerformanceStatistics();

    /// <summary>
    /// Clears all caches and resets performance statistics, forcing fresh discovery operations.
    /// 
    /// This method is primarily useful for testing scenarios, debugging cache-related issues,
    /// or when significant changes to the application structure require a complete reset of
    /// the discovery system's state.
    /// </summary>
    void Reset();
}