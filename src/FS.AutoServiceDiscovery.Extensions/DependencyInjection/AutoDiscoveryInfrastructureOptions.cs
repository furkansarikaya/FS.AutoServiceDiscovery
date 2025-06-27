using FS.AutoServiceDiscovery.Extensions.Architecture;
using FS.AutoServiceDiscovery.Extensions.Architecture.Conventions;
using FS.AutoServiceDiscovery.Extensions.Configuration;

namespace FS.AutoServiceDiscovery.Extensions.DependencyInjection;

/// <summary>
/// Configuration options for setting up the auto-discovery infrastructure components.
/// 
/// This class represents the "control panel" for the entire auto-discovery system, allowing users
/// to configure every aspect of how the discovery process works. Think of it as the dashboard in
/// a car - it gives you control over all the important systems and settings.
/// 
/// The options pattern is a best practice in .NET for providing flexible configuration while
/// maintaining backward compatibility. By using this pattern, we can add new configuration
/// options in the future without breaking existing code.
/// 
/// The configuration is divided into logical sections:
/// 1. Caching behavior and performance settings
/// 2. Plugin registration and management
/// 3. Naming convention customization
/// 4. Performance monitoring and metrics
/// 5. Logging and debugging options
/// </summary>
public class AutoDiscoveryInfrastructureOptions
{
    /// <summary>
    /// Gets or sets whether to enable performance metrics collection throughout the discovery process.
    /// 
    /// When enabled, the system will collect detailed metrics about:
    /// - Cache hit rates and performance
    /// - Plugin execution times and success rates
    /// - Assembly scanning performance
    /// - Service registration statistics
    /// 
    /// This option adds some overhead but provides valuable insights for optimization and monitoring.
    /// In production, you might want to enable this selectively or sample the data to balance
    /// observability with performance.
    /// </summary>
    public bool EnablePerformanceMetrics { get; set; } = false;

    /// <summary>
    /// Gets or sets the configuration object for cache behavior and performance tuning.
    /// 
    /// This allows fine-grained control over how the assembly scan cache operates:
    /// - Memory usage limits
    /// - Eviction policies
    /// - Cache invalidation strategies
    /// - Performance thresholds
    /// 
    /// If not provided, the system will use sensible defaults that work well for most applications.
    /// </summary>
    public CacheConfiguration? CacheConfiguration { get; set; }

    /// <summary>
    /// Gets the collection of custom naming convention types to register with the system.
    /// 
    /// Naming conventions are registered as types rather than instances to enable proper
    /// dependency injection. This allows naming conventions to inject services they need
    /// for their operation, such as configuration or logging services.
    /// 
    /// The conventions will be registered in the order they appear in this collection,
    /// but their actual execution order is determined by their Priority property.
    /// </summary>
    public List<Type> CustomNamingConventionTypes { get; } = new();

    /// <summary>
    /// Gets the collection of custom naming convention instances to register with the system.
    /// 
    /// This collection is for pre-configured naming convention instances that don't require
    /// dependency injection. Use this when you have naming conventions that are fully
    /// self-contained or when you want to provide specific configuration to the convention
    /// at registration time.
    /// </summary>
    public List<INamingConvention> CustomNamingConventions { get; } = new();

    /// <summary>
    /// Gets the collection of service discovery plugin types to register with the system.
    /// 
    /// Plugins are registered as types to enable dependency injection, allowing them to
    /// access any services they need for their discovery logic. This is particularly
    /// important for plugins that need to access databases, configuration systems,
    /// or external APIs during the discovery process.
    /// </summary>
    public List<Type> PluginTypes { get; } = new();

    /// <summary>
    /// Gets the collection of service discovery plugin instances to register with the system.
    /// 
    /// This collection is for pre-configured plugin instances. Use this when you have
    /// plugins that don't require dependency injection or when you want to provide
    /// specific configuration to the plugin at registration time.
    /// </summary>
    public List<IServiceDiscoveryPlugin> PluginInstances { get; } = new();

    /// <summary>
    /// Adds a custom naming convention type to be registered with dependency injection.
    /// This method provides a fluent interface for configuring naming conventions.
    /// </summary>
    /// <typeparam name="TNamingConvention">The type of naming convention to add.</typeparam>
    /// <returns>This options instance for method chaining.</returns>
    public AutoDiscoveryInfrastructureOptions AddNamingConvention<TNamingConvention>()
        where TNamingConvention : class, INamingConvention
    {
        CustomNamingConventionTypes.Add(typeof(TNamingConvention));
        return this;
    }

    /// <summary>
    /// Adds a custom naming convention instance to the system.
    /// This method provides a fluent interface for configuring naming conventions.
    /// </summary>
    /// <param name="convention">The naming convention instance to add.</param>
    /// <returns>This options instance for method chaining.</returns>
    public AutoDiscoveryInfrastructureOptions AddNamingConvention(INamingConvention convention)
    {
        CustomNamingConventions.Add(convention);
        return this;
    }

    /// <summary>
    /// Adds a service discovery plugin type to be registered with dependency injection.
    /// This method provides a fluent interface for configuring plugins.
    /// </summary>
    /// <typeparam name="TPlugin">The type of plugin to add.</typeparam>
    /// <returns>This options instance for method chaining.</returns>
    public AutoDiscoveryInfrastructureOptions AddPlugin<TPlugin>()
        where TPlugin : class, IServiceDiscoveryPlugin
    {
        PluginTypes.Add(typeof(TPlugin));
        return this;
    }

    /// <summary>
    /// Adds a service discovery plugin instance to the system.
    /// This method provides a fluent interface for configuring plugins.
    /// </summary>
    /// <param name="plugin">The plugin instance to add.</param>
    /// <returns>This options instance for method chaining.</returns>
    public AutoDiscoveryInfrastructureOptions AddPlugin(IServiceDiscoveryPlugin plugin)
    {
        PluginInstances.Add(plugin);
        return this;
    }
}