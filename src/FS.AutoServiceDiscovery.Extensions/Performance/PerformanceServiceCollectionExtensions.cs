using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using FS.AutoServiceDiscovery.Extensions.Caching;
using FS.AutoServiceDiscovery.Extensions.Configuration;
using FS.AutoServiceDiscovery.Extensions.Diagnostics;
using FS.AutoServiceDiscovery.Extensions.Validation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace FS.AutoServiceDiscovery.Extensions.Performance;

/// <summary>
/// High-performance extension methods for service collection that implement advanced optimization strategies.
/// These methods are designed for production scenarios where startup performance is critical.
/// </summary>
public static class PerformanceServiceCollectionExtensions
{
    private static readonly Lazy<IAssemblyScanCache> DefaultCache =
        new(() => new MemoryAssemblyScanCache(), LazyThreadSafetyMode.ExecutionAndPublication);

    /// <summary>
    /// Adds auto services with performance optimizations enabled.
    /// </summary>
    [RequiresUnreferencedCode("Auto service discovery uses reflection to scan assemblies and types.")]
    public static IServiceCollection AddAutoServicesWithPerformanceOptimizations(
        this IServiceCollection services,
        Action<AutoServiceOptions>? configureOptions = null,
        params Assembly[] assemblies)
    {
        var options = new AutoServiceOptions();
        configureOptions?.Invoke(options);

        var cache = DefaultCache.Value;
        var scanner = new OptimizedTypeScanner();

        return AddAutoServicesOptimized(services, options, cache, scanner, assemblies);
    }

    /// <summary>
    /// Adds auto services with custom cache and scanner implementations.
    /// </summary>
    [RequiresUnreferencedCode("Auto service discovery uses reflection to scan assemblies and types.")]
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
    /// </summary>
    [RequiresUnreferencedCode("Auto service discovery uses reflection to scan assemblies and types.")]
    private static IServiceCollection AddAutoServicesOptimized(
        IServiceCollection services,
        AutoServiceOptions options,
        IAssemblyScanCache cache,
        OptimizedTypeScanner scanner,
        Assembly[] assemblies)
    {
        using var activity = AutoServiceDiscoveryMetrics.ActivitySource.StartActivity("OptimizedServiceDiscovery");
        var stopwatch = Stopwatch.StartNew();

        var logger = ResolveLogger(services);

        if (assemblies.Length == 0)
        {
            assemblies = [Assembly.GetCallingAssembly()];
        }

        activity?.SetTag("assembly.count", assemblies.Length);

        var allServiceRegistrations = new List<ServiceRegistrationInfo>(capacity: 128);

        logger.LogDebug("Starting optimized service discovery for {AssemblyCount} assemblies.", assemblies.Length);

        foreach (var assembly in assemblies)
        {
            AutoServiceDiscoveryMetrics.AssembliesScanned.Add(1);

            // Try to get results from cache first
            if (cache.TryGetCachedResults(assembly, out var cachedResults) && cachedResults != null)
            {
                AutoServiceDiscoveryMetrics.CacheHits.Add(1);
                var cachedList = cachedResults as IList<ServiceRegistrationInfo> ?? cachedResults.ToList();

                logger.LogDebug("Using cached results for assembly: {AssemblyName} ({Count} services)",
                    assembly.GetName().Name, cachedList.Count);

                allServiceRegistrations.AddRange(cachedList);
                continue;
            }

            AutoServiceDiscoveryMetrics.CacheMisses.Add(1);

            logger.LogDebug("Scanning assembly: {AssemblyName}", assembly.GetName().Name);

            var assemblySw = Stopwatch.StartNew();
            var assemblyResults = scanner.ScanAssemblies(new[] { assembly }).ToList();
            assemblySw.Stop();

            AutoServiceDiscoveryMetrics.AssemblyScanDuration.Record(assemblySw.Elapsed.TotalMilliseconds,
                new KeyValuePair<string, object?>("assembly", assembly.GetName().Name ?? "unknown"));

            logger.LogDebug("Scanned {AssemblyName} in {Duration:F1}ms, found {Count} services",
                assembly.GetName().Name, assemblySw.Elapsed.TotalMilliseconds, assemblyResults.Count);

            cache.CacheResults(assembly, assemblyResults);
            allServiceRegistrations.AddRange(assemblyResults);
        }

        // Apply filtering based on options
        var filteredServices = ApplyFiltering(allServiceRegistrations, options).ToList();

        // Register services in order
        RegisterServicesOptimized(services, filteredServices, options, logger);

        stopwatch.Stop();
        AutoServiceDiscoveryMetrics.DiscoveryDuration.Record(stopwatch.Elapsed.TotalMilliseconds);
        activity?.SetTag("services.registered", filteredServices.Count);

        logger.LogInformation(
            "Optimized service discovery completed in {Duration:F1}ms. Registered {Count} services from {AssemblyCount} assemblies.",
            stopwatch.Elapsed.TotalMilliseconds, filteredServices.Count, assemblies.Length);

        return services;
    }

    /// <summary>
    /// Applies filtering logic based on configuration options.
    /// </summary>
    private static IEnumerable<ServiceRegistrationInfo> ApplyFiltering(
        IEnumerable<ServiceRegistrationInfo> services,
        AutoServiceOptions options)
    {
        return services.Where(service =>
        {
            if (!ShouldRegisterForProfile(service, options.Profile))
            {
                AutoServiceDiscoveryMetrics.ServicesSkipped.Add(1,
                    new KeyValuePair<string, object?>("reason", "profile_mismatch"));
                return false;
            }

            if (options.IsTestEnvironment && service.IgnoreInTests)
            {
                AutoServiceDiscoveryMetrics.ServicesSkipped.Add(1,
                    new KeyValuePair<string, object?>("reason", "test_excluded"));
                return false;
            }

            return ShouldRegisterConditional(service, options.Configuration);
        });
    }

    private static bool ShouldRegisterForProfile(ServiceRegistrationInfo serviceInfo, string? profile)
    {
        if (string.IsNullOrEmpty(profile) || string.IsNullOrEmpty(serviceInfo.Profile))
            return true;

        return string.Equals(serviceInfo.Profile, profile, StringComparison.OrdinalIgnoreCase);
    }

    private static bool ShouldRegisterConditional(ServiceRegistrationInfo serviceInfo, IConfiguration? configuration)
    {
        if (configuration == null || serviceInfo.ConditionalAttributes.Length == 0)
            return true;

        foreach (var conditional in serviceInfo.ConditionalAttributes)
        {
            var configValue = configuration[conditional.ConfigurationKey ?? string.Empty];
            if (!string.Equals(configValue, conditional.ExpectedValue, StringComparison.OrdinalIgnoreCase))
                return false;
        }

        return true;
    }

    /// <summary>
    /// Optimized service registration with metrics, ILogger, keyed services, TryAdd, and scope validation.
    /// </summary>
    private static void RegisterServicesOptimized(
        IServiceCollection services,
        List<ServiceRegistrationInfo> serviceInfos,
        AutoServiceOptions options,
        ILogger logger)
    {
        var orderedServices = serviceInfos.OrderBy(s => s.Order);

        foreach (var serviceInfo in orderedServices)
        {
            var useTryAdd = serviceInfo.UseTryAdd || options.UseTryAddByDefault;

            ServiceDescriptor descriptor;

            if (serviceInfo.ServiceKey != null)
            {
                descriptor = new ServiceDescriptor(
                    serviceInfo.ServiceType,
                    serviceInfo.ServiceKey,
                    serviceInfo.ImplementationType,
                    serviceInfo.Lifetime);
                AutoServiceDiscoveryMetrics.KeyedRegistrations.Add(1);
            }
            else
            {
                descriptor = new ServiceDescriptor(
                    serviceInfo.ServiceType,
                    serviceInfo.ImplementationType,
                    serviceInfo.Lifetime);
            }

            if (useTryAdd)
            {
                var countBefore = services.Count;
                services.TryAdd(descriptor);
                if (services.Count == countBefore)
                {
                    AutoServiceDiscoveryMetrics.DuplicatesPrevented.Add(1);
                    continue;
                }
            }
            else
            {
                services.Add(descriptor);
            }

            AutoServiceDiscoveryMetrics.ServicesRegistered.Add(1,
                new KeyValuePair<string, object?>("lifetime", serviceInfo.Lifetime.ToString()));

            logger.LogDebug("  {ServiceType} -> {ImplementationType} (Order: {Order})",
                serviceInfo.ServiceType.Name, serviceInfo.ImplementationType.Name, serviceInfo.Order);
        }

        // Scope validation
        if (options.EnableScopeValidation)
        {
            var validationSw = Stopwatch.StartNew();
            var validationResult = ScopeValidator.ValidateScopes(services);
            validationSw.Stop();

            AutoServiceDiscoveryMetrics.ScopeValidationDuration.Record(validationSw.Elapsed.TotalMilliseconds);
            AutoServiceDiscoveryMetrics.ScopeViolations.Add(validationResult.Violations.Count);
            AutoServiceDiscoveryMetrics.ScopeWarnings.Add(validationResult.Warnings.Count);

            foreach (var warning in validationResult.Warnings)
                logger.LogWarning("Scope Warning: {Message}", warning.Message);
            foreach (var violation in validationResult.Violations)
                logger.LogError("Scope Violation: {Message}", violation.Message);

            if (!validationResult.IsValid && options.ThrowOnScopeViolation)
            {
                var messages = string.Join(Environment.NewLine, validationResult.Violations.Select(v => v.Message));
                throw new InvalidOperationException(
                    $"Scope validation failed with {validationResult.Violations.Count} violation(s):{Environment.NewLine}{messages}");
            }
        }
    }

    /// <summary>
    /// Gets cache statistics for the default cache instance.
    /// </summary>
    public static CacheStatistics GetCacheStatistics()
    {
        return DefaultCache.Value.GetStatistics();
    }

    /// <summary>
    /// Clears all caches (assembly cache and type metadata cache).
    /// </summary>
    public static void ClearAllCaches()
    {
        DefaultCache.Value.ClearCache();
        OptimizedTypeScanner.ClearCache();
    }

    private static ILogger ResolveLogger(IServiceCollection services)
    {
        var loggerFactoryDescriptor = services.LastOrDefault(d => d.ServiceType == typeof(ILoggerFactory));
        if (loggerFactoryDescriptor?.ImplementationInstance is ILoggerFactory factory)
        {
            return factory.CreateLogger("FS.AutoServiceDiscovery");
        }

        return NullLogger.Instance;
    }
}
