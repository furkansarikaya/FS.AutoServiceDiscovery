using FS.AutoServiceDiscovery.Extensions.Architecture;
using FS.AutoServiceDiscovery.Extensions.Architecture.Conventions;
using FS.AutoServiceDiscovery.Extensions.Caching;
using FS.AutoServiceDiscovery.Extensions.Configuration;
using FS.AutoServiceDiscovery.Extensions.Performance;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace FS.AutoServiceDiscovery.Extensions.DependencyInjection;

/// <summary>
/// Provides extension methods for registering auto-discovery infrastructure components with the dependency injection container.
/// 
/// This class represents the "master configuration center" for our entire auto-discovery ecosystem. Think of it as
/// the main electrical panel in a building - it connects all the different systems (lighting, heating, security)
/// to the main power source and ensures they all work together harmoniously.
/// 
/// The registration strategy here follows several important dependency injection principles:
/// 1. **Interface Segregation**: Each component is registered by its interface, allowing for easy replacement
/// 2. **Dependency Inversion**: High-level modules depend on abstractions, not concretions
/// 3. **Single Responsibility**: Each registration method focuses on one aspect of the system
/// 4. **Open/Closed Principle**: The system is open for extension (new implementations) but closed for modification
/// 
/// This comprehensive registration approach ensures that every component in our system can be independently
/// tested, replaced, or enhanced without affecting other components.
/// </summary>
public static class AutoDiscoveryServiceCollectionExtensions
{
    /// <summary>
    /// Registers all the auto-discovery infrastructure components with the dependency injection container.
    /// 
    /// This method serves as the "one-stop shop" for setting up the complete auto-discovery system. It orchestrates
    /// the registration of all subsystems in the correct order, ensuring that dependencies are satisfied and
    /// the system is ready for use.
    /// 
    /// The registration process follows a carefully designed sequence:
    /// 1. Core configuration and options
    /// 2. Fundamental services (caching, performance monitoring)
    /// 3. Discovery mechanisms (naming conventions, plugins)
    /// 4. Coordination services (plugin coordinator, convention resolver)
    /// 5. Integration services (the main discovery extensions)
    /// 
    /// This ordered approach prevents circular dependencies and ensures that each component has access
    /// to the services it needs during its own initialization.
    /// </summary>
    public static IServiceCollection AddAutoDiscoveryInfrastructure(
        this IServiceCollection services,
        Action<AutoDiscoveryInfrastructureOptions>? configureOptions = null)
    {
        var options = new AutoDiscoveryInfrastructureOptions();
        configureOptions?.Invoke(options);

        // Register the configuration options themselves so components can access them
        services.TryAddSingleton(options);

        // Register core caching infrastructure - the foundation for performance optimization
        RegisterCachingServices(services, options);

        // Register performance monitoring - essential for production observability
        RegisterPerformanceMonitoring(services, options);

        // Register naming convention system - enables flexible service type resolution
        RegisterNamingConventions(services, options);

        // Register plugin infrastructure - provides extensibility for custom discovery logic
        RegisterPluginInfrastructure(services, options);

        // Register coordination services - orchestrates all the moving parts
        RegisterCoordinationServices(services, options);

        return services;
    }

    /// <summary>
    /// Registers caching services with appropriate lifetimes and configurations.
    /// 
    /// Caching is fundamental to the performance characteristics of our discovery system. The registration
    /// strategy here ensures that caches live for the appropriate duration (singleton for shared state)
    /// while remaining replaceable for testing and customization scenarios.
    /// 
    /// The factory pattern used here allows the cache implementation to receive dependencies from
    /// the DI container, enabling features like configuration-driven cache behavior and logging integration.
    /// </summary>
    private static void RegisterCachingServices(IServiceCollection services, AutoDiscoveryInfrastructureOptions options)
    {
        // Register the default cache implementation with factory pattern for dependency injection
        services.TryAddSingleton<IAssemblyScanCache>(serviceProvider =>
        {
            var cache = new MemoryAssemblyScanCache();
            
            // In future versions, we could inject additional services here:
            // var logger = serviceProvider.GetService<ILogger<MemoryAssemblyScanCache>>();
            // var metricsCollector = serviceProvider.GetService<IPerformanceMetricsCollector>();
            // cache.SetLogger(logger);
            // cache.SetMetricsCollector(metricsCollector);
            
            return cache;
        });

        // Register cache statistics as a transient service for easy access by monitoring components
        services.TryAddTransient<CacheStatistics>(serviceProvider =>
        {
            var cache = serviceProvider.GetRequiredService<IAssemblyScanCache>();
            return cache.GetStatistics();
        });

        // Register cache configuration if provided
        if (options.CacheConfiguration != null)
        {
            services.TryAddSingleton(options.CacheConfiguration);
        }
        else
        {
            // Provide default cache configuration based on environment
            services.TryAddSingleton<CacheConfiguration>(serviceProvider =>
            {
                // In a real implementation, we might determine this based on environment variables
                // or other configuration sources
                return CacheConfiguration.ForProduction();
            });
        }
    }

    /// <summary>
    /// Registers performance monitoring components with conditional logic based on configuration.
    /// 
    /// Performance monitoring is crucial for production systems, but it should be lightweight and
    /// configurable. This registration approach allows the monitoring system to be completely
    /// disabled (using a no-op implementation) when performance overhead is a concern.
    /// </summary>
    private static void RegisterPerformanceMonitoring(IServiceCollection services, AutoDiscoveryInfrastructureOptions options)
    {
        // Register the optimized type scanner as a core component
        services.TryAddTransient<OptimizedTypeScanner>();

        // Conditional registration of performance metrics collector
        if (options.EnablePerformanceMetrics)
        {
            services.TryAddSingleton<IPerformanceMetricsCollector, PerformanceMetricsCollector>();
        }
        else
        {
            // Use a no-op implementation when metrics are disabled for zero performance overhead
            services.TryAddSingleton<IPerformanceMetricsCollector, NoOpPerformanceMetricsCollector>();
        }

        // Register a service for easy access to performance summaries
        services.TryAddTransient<PerformanceMetricsSummary>(serviceProvider =>
        {
            var collector = serviceProvider.GetRequiredService<IPerformanceMetricsCollector>();
            return collector.GetMetricsSummary();
        });
    }

    /// <summary>
    /// Registers naming convention services in priority order with resolver coordination.
    /// 
    /// The naming convention system demonstrates a powerful pattern: ordered service collections
    /// with a coordinator that manages their execution. This approach allows for flexible,
    /// extensible service type resolution while maintaining predictable behavior.
    /// </summary>
    private static void RegisterNamingConventions(IServiceCollection services, AutoDiscoveryInfrastructureOptions options)
    {
        // Register the standard naming convention as the default - this handles the majority of cases
        services.TryAddTransient<INamingConvention, StandardInterfacePrefixConvention>();

        // Register custom naming convention types provided by the user
        foreach (var conventionType in options.CustomNamingConventionTypes)
        {
            services.AddTransient(typeof(INamingConvention), conventionType);
        }

        // Register custom naming convention instances provided by the user
        foreach (var conventionInstance in options.CustomNamingConventions)
        {
            services.AddTransient<INamingConvention>(serviceProvider => conventionInstance);
        }

        // Register the convention resolver that coordinates all naming conventions
        services.TryAddTransient<INamingConventionResolver, NamingConventionResolver>();
    }

    /// <summary>
    /// Registers plugin infrastructure components with support for both type-based and instance-based plugins.
    /// 
    /// Plugin registration demonstrates the flexibility of dependency injection - we can register
    /// plugins either as types (allowing them to have their own dependencies injected) or as
    /// pre-configured instances (for scenarios where specific configuration is needed).
    /// </summary>
    private static void RegisterPluginInfrastructure(IServiceCollection services, AutoDiscoveryInfrastructureOptions options)
    {
        // Register plugin types - these will be instantiated with full DI support
        foreach (var pluginType in options.PluginTypes)
        {
            services.AddTransient(typeof(IServiceDiscoveryPlugin), pluginType);
        }

        // Register plugin instances - these are pre-configured and ready to use
        foreach (var pluginInstance in options.PluginInstances)
        {
            services.AddTransient<IServiceDiscoveryPlugin>(serviceProvider => pluginInstance);
        }
    }

    /// <summary>
    /// Registers coordination services that orchestrate the interaction between all other components.
    /// 
    /// The coordination services represent the "control center" of our system. They don't perform
    /// the actual work of discovery, but they ensure that all the specialized components work
    /// together effectively and efficiently.
    /// </summary>
    private static void RegisterCoordinationServices(IServiceCollection services, AutoDiscoveryInfrastructureOptions options)
    {
        // Register the plugin coordinator that manages plugin execution lifecycle
        services.TryAddTransient<IPluginCoordinator, PluginCoordinator>();

        // Register a factory service for creating optimized discovery processes
        services.TryAddTransient<IOptimizedDiscoveryService, OptimizedDiscoveryService>();
    }

    /// <summary>
    /// Adds a custom naming convention to the auto-discovery system with fluent interface support.
    /// 
    /// This method provides a convenient, discoverable way for users to add their own naming
    /// conventions without needing to understand the internal registration mechanics.
    /// </summary>
    public static IServiceCollection AddNamingConvention<TNamingConvention>(this IServiceCollection services)
        where TNamingConvention : class, INamingConvention
    {
        services.AddTransient<INamingConvention, TNamingConvention>();
        return services;
    }

    /// <summary>
    /// Adds a custom service discovery plugin to the auto-discovery system with fluent interface support.
    /// 
    /// This method enables easy plugin registration while maintaining the flexibility of dependency injection.
    /// Plugins registered this way can have their own dependencies injected during construction.
    /// </summary>
    public static IServiceCollection AddServiceDiscoveryPlugin<TPlugin>(this IServiceCollection services)
        where TPlugin : class, IServiceDiscoveryPlugin
    {
        services.AddTransient<IServiceDiscoveryPlugin, TPlugin>();
        return services;
    }

    /// <summary>
    /// Replaces the default assembly scan cache with a custom implementation.
    /// 
    /// This method demonstrates the power of interface-based design - users can completely replace
    /// the caching strategy without affecting any other part of the system. This is particularly
    /// valuable for scenarios like distributed caching or specialized storage systems.
    /// </summary>
    public static IServiceCollection UseAssemblyScanCache<TCache>(this IServiceCollection services)
        where TCache : class, IAssemblyScanCache
    {
        // Remove any existing cache registration to ensure clean replacement
        var existingRegistration = services.FirstOrDefault(s => s.ServiceType == typeof(IAssemblyScanCache));
        if (existingRegistration != null)
        {
            services.Remove(existingRegistration);
        }

        services.AddSingleton<IAssemblyScanCache, TCache>();
        return services;
    }

    /// <summary>
    /// Configures the auto-discovery system to use a specific cache configuration.
    /// 
    /// This method provides a fluent way to customize cache behavior without needing to
    /// replace the entire cache implementation.
    /// </summary>
    public static IServiceCollection UseCacheConfiguration(this IServiceCollection services, CacheConfiguration configuration)
    {
        services.AddSingleton(configuration);
        return services;
    }

    /// <summary>
    /// Enables comprehensive performance monitoring with detailed metrics collection.
    /// 
    /// This method provides an easy way to enable the full monitoring capabilities of the
    /// auto-discovery system, which is particularly valuable in production environments.
    /// </summary>
    public static IServiceCollection EnablePerformanceMonitoring(this IServiceCollection services)
    {
        // Remove any existing no-op metrics collector
        var existingCollector = services.FirstOrDefault(s => s.ServiceType == typeof(IPerformanceMetricsCollector));
        if (existingCollector != null)
        {
            services.Remove(existingCollector);
        }

        services.AddSingleton<IPerformanceMetricsCollector, PerformanceMetricsCollector>();
        return services;
    }
}