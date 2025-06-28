using System.Reflection;
using FS.AutoServiceDiscovery.Extensions.Caching;
using FS.AutoServiceDiscovery.Extensions.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FS.AutoServiceDiscovery.Extensions.Performance;

/// <summary>
/// High-performance extension methods for service collection that implement advanced optimization strategies.
/// These methods are designed for production scenarios where startup performance is critical.
/// </summary>
public static class PerformanceServiceCollectionExtensions
{
    // Singleton cache instance shared across all service collection operations
    // This ensures cache benefits persist across multiple registrations in the same application
    private static readonly Lazy<IAssemblyScanCache> DefaultCache =
        new(() => new MemoryAssemblyScanCache(), LazyThreadSafetyMode.ExecutionAndPublication);

    /// <summary>
    /// Adds auto services with performance optimizations enabled.
    /// This method implements caching, parallel scanning, and other performance enhancements
    /// that can significantly reduce startup time in large applications.
    /// </summary>
    /// <param name="services">The service collection to add services to</param>
    /// <param name="configureOptions">Optional configuration for the discovery process</param>
    /// <param name="assemblies">Assemblies to scan for services</param>
    /// <returns>The service collection for method chaining</returns>
    public static IServiceCollection AddAutoServicesWithPerformanceOptimizations(
        this IServiceCollection services,
        Action<AutoServiceOptions>? configureOptions = null,
        params Assembly[] assemblies)
    {
        var options = new AutoServiceOptions();
        configureOptions?.Invoke(options);

        // Use default cache if none provided
        var cache = DefaultCache.Value;
        var scanner = new OptimizedTypeScanner();

        return AddAutoServicesOptimized(services, options, cache, scanner, assemblies);
    }

    /// <summary>
    /// Adds auto services with custom cache and scanner implementations.
    /// This overload provides maximum flexibility for advanced scenarios.
    /// </summary>
    public static IServiceCollection AddAutoServicesWithCustomOptimizations(
        this IServiceCollection services,
        IAssemblyScanCache cache,
        OptimizedTypeScanner scanner,
        Action<AutoServiceOptions>? configureOptions = null,
        params Assembly[] assemblies)
    {
        var options = new AutoServiceOptions();
        configureOptions?.Invoke(options);

        return AddAutoServicesOptimized(services, options, cache, scanner, assemblies);
    }

    /// <summary>
    /// Core optimized service registration implementation.
    /// This method orchestrates all optimization strategies for maximum performance.
    /// </summary>
    private static IServiceCollection AddAutoServicesOptimized(
        IServiceCollection services,
        AutoServiceOptions options,
        IAssemblyScanCache cache,
        OptimizedTypeScanner scanner,
        Assembly[] assemblies)
    {
        // Default to calling assembly if none specified
        if (assemblies.Length == 0)
        {
            assemblies = [Assembly.GetCallingAssembly()];
        }

        var allServiceRegistrations = new List<ServiceRegistrationInfo>();
        var cacheStats = cache.GetStatistics();
        var initialCacheHits = cacheStats.CacheHits;

        if (options.EnableLogging)
        {
            Console.WriteLine($"Starting optimized service discovery for {assemblies.Length} assemblies...");
            Console.WriteLine($"Cache stats - Hits: {cacheStats.CacheHits}, Misses: {cacheStats.CacheMisses}, Hit Ratio: {cacheStats.HitRatio:F1}%");
        }

        foreach (var assembly in assemblies)
        {
            // Try to get results from cache first
            if (cache.TryGetCachedResults(assembly, out var cachedResults) && cachedResults != null)
            {
                if (options.EnableLogging)
                {
                    Console.WriteLine($"Using cached results for assembly: {assembly.GetName().Name} ({cachedResults.Count()} services)");
                }

                allServiceRegistrations.AddRange(cachedResults); // Null check eklendi
                continue;
            }

            // Cache miss - scan the assembly
            if (options.EnableLogging)
            {
                Console.WriteLine($"Scanning assembly: {assembly.GetName().Name}");
            }

            var startTime = DateTime.UtcNow;
            var assemblyResults = scanner.ScanAssemblies(new[] { assembly }).ToList();
            var scanDuration = DateTime.UtcNow - startTime;

            if (options.EnableLogging)
            {
                Console.WriteLine($"Scanned {assembly.GetName().Name} in {scanDuration.TotalMilliseconds:F1}ms, found {assemblyResults.Count} services");
            }

            // Cache the results for future use
            cache.CacheResults(assembly, assemblyResults);
            allServiceRegistrations.AddRange(assemblyResults);
        }

        // Apply filtering based on options (profile, conditional, etc.)
        var filteredServices = ApplyFiltering(allServiceRegistrations, options);

        // Register services in order
        RegisterServicesOptimized(services, filteredServices, options);

        // Log final statistics
        if (!options.EnableLogging) 
            return services;
        var finalStats = cache.GetStatistics();
        var newCacheHits = finalStats.CacheHits - initialCacheHits;
        Console.WriteLine($"Service discovery completed. Cache performance - New hits: {newCacheHits}, Total services registered: {filteredServices.Count()}");

        return services;
    }

    /// <summary>
    /// Applies filtering logic based on configuration options.
    /// This method consolidates all filtering logic in one place for better performance.
    /// </summary>
    private static IEnumerable<ServiceRegistrationInfo> ApplyFiltering(
        IEnumerable<ServiceRegistrationInfo> services,
        AutoServiceOptions options)
    {
        return services.Where(service =>
        {
            // Profile filtering
            if (!ShouldRegisterForProfile(service, options.Profile))
                return false;

            // Test environment filtering
            if (options.IsTestEnvironment && service.IgnoreInTests)
                return false;

            // Conditional filtering
            return ShouldRegisterConditional(service, options.Configuration);
        });
    }

    /// <summary>
    /// Profile filtering logic moved from main extension class for better performance.
    /// </summary>
    private static bool ShouldRegisterForProfile(ServiceRegistrationInfo serviceInfo, string? profile)
    {
        if (string.IsNullOrEmpty(profile) || string.IsNullOrEmpty(serviceInfo.Profile))
            return true;

        return string.Equals(serviceInfo.Profile, profile, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Conditional filtering logic for performance optimization.
    /// </summary>
    private static bool ShouldRegisterConditional(ServiceRegistrationInfo serviceInfo, IConfiguration? configuration)
    {
        if (configuration == null || serviceInfo.ConditionalAttributes.Length == 0)
            return true;

        // Tüm conditional'lar true olmalı (AND logic)
        foreach (var conditional in serviceInfo.ConditionalAttributes)
        {
            var configValue = configuration[conditional.ConfigurationKey];
            if (!string.Equals(configValue, conditional.ExpectedValue, StringComparison.OrdinalIgnoreCase))
                return false;
        }

        return true;
    }

    /// <summary>
    /// Optimized service registration that minimizes ServiceDescriptor allocations.
    /// </summary>
    private static void RegisterServicesOptimized(
        IServiceCollection services,
        IEnumerable<ServiceRegistrationInfo> serviceInfos,
        AutoServiceOptions options)
    {
        // Group by lifetime for more efficient registration
        var servicesByLifetime = serviceInfos
            .GroupBy(s => s.Lifetime)
            .ToList();

        foreach (var lifetimeGroup in servicesByLifetime)
        {
            var servicesInGroup = lifetimeGroup.OrderBy(s => s.Order).ToList();

            if (options.EnableLogging)
            {
                Console.WriteLine($"Registering {servicesInGroup.Count} {lifetimeGroup.Key} services...");
            }

            // Bulk registration for better performance
            foreach (var serviceInfo in servicesInGroup)
            {
                services.Add(new ServiceDescriptor(
                    serviceInfo.ServiceType,
                    serviceInfo.ImplementationType,
                    serviceInfo.Lifetime));

                if (options.EnableLogging)
                {
                    Console.WriteLine($"  {serviceInfo.ServiceType.Name} -> {serviceInfo.ImplementationType.Name} (Order: {serviceInfo.Order})");
                }
            }
        }
    }

    /// <summary>
    /// Gets cache statistics for the default cache instance.
    /// Useful for monitoring and debugging cache performance.
    /// </summary>
    public static CacheStatistics GetCacheStatistics()
    {
        return DefaultCache.Value.GetStatistics();
    }

    /// <summary>
    /// Clears all caches (assembly cache and type metadata cache).
    /// Primarily useful for testing scenarios.
    /// </summary>
    public static void ClearAllCaches()
    {
        DefaultCache.Value.ClearCache();
        OptimizedTypeScanner.ClearCache();
    }
}