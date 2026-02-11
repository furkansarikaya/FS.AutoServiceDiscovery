using System.Reflection;
using FS.AutoServiceDiscovery.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace FS.AutoServiceDiscovery.Extensions.Architecture;

/// <summary>
/// Extension methods for integrating service discovery plugins into the dependency injection container.
/// These methods provide a clean, fluent API for registering and configuring plugins while maintaining
/// backward compatibility with the existing auto-discovery functionality.
/// 
/// The plugin system architecture follows the Open/Closed Principle - the core discovery system
/// is closed for modification but open for extension through plugins. This allows new discovery
/// strategies to be added without changing the fundamental behavior of the library.
/// </summary>
public static class PluginServiceCollectionExtensions
{
    /// <summary>
    /// Adds auto service discovery with support for custom plugins, providing extensibility
    /// for specialized discovery scenarios that go beyond the standard attribute-based approach.
    /// 
    /// This method represents the "full power" version of auto discovery, where you can:
    /// 1. Use all standard auto-discovery features (attributes, profiles, conditions)
    /// 2. Add custom plugins for specialized discovery logic
    /// 3. Control the order in which different discovery strategies are applied
    /// 4. Get detailed validation feedback about the entire discovery process
    /// 
    /// The plugin integration works by running each plugin against appropriate assemblies,
    /// collecting all discovered services, validating the results for conflicts or issues,
    /// and then registering everything in the proper order.
    /// 
    /// Think of this method as orchestrating a team of specialists (plugins) who each
    /// contribute their expertise to the overall service discovery process.
    /// </summary>
    /// <param name="services">The service collection to register discovered services with.</param>
    /// <param name="plugins">
    /// The collection of plugins to use for service discovery. Plugins will be executed
    /// in priority order (lower Priority values first), allowing for controlled layering
    /// of discovery strategies.
    /// </param>
    /// <param name="configureOptions">
    /// Optional configuration for the discovery process. This configuration applies to
    /// both the core discovery and all plugins, ensuring consistent behavior across
    /// all discovery strategies.
    /// </param>
    /// <param name="assemblies">
    /// The assemblies to scan for services. Each plugin will have the opportunity to
    /// process assemblies that pass its CanProcessAssembly filter.
    /// </param>
    /// <returns>The service collection for method chaining.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when plugin validation fails with critical errors that prevent safe registration.
    /// The exception will include details about which plugins failed and why.
    /// </exception>
    public static IServiceCollection AddAutoServicesWithPlugins(
        this IServiceCollection services,
        IEnumerable<IServiceDiscoveryPlugin> plugins,
        Action<AutoServiceOptions>? configureOptions = null,
        params Assembly[] assemblies)
    {
        var options = new AutoServiceOptions();
        configureOptions?.Invoke(options);

        // Default to calling assembly if none specified
        if (assemblies.Length == 0)
        {
            assemblies = [Assembly.GetCallingAssembly()];
        }

        var logger = ResolveLogger(services);
        var orderedPlugins = plugins.OrderBy(p => p.Priority).ToList();
        var allDiscoveredServices = new List<ServiceRegistrationInfo>();
        var validationResults = new List<(IServiceDiscoveryPlugin Plugin, PluginValidationResult Result)>();

        if (options.EnableLogging)
        {
            logger.LogInformation("Starting plugin-based service discovery with {PluginCount} plugins across {AssemblyCount} assemblies...",
                orderedPlugins.Count, assemblies.Length);
        }

        // Execute each plugin in priority order
        foreach (var plugin in orderedPlugins)
        {
            if (options.EnableLogging)
            {
                logger.LogInformation("Executing plugin: {PluginName} (Priority: {Priority})", plugin.Name, plugin.Priority);
            }

            var pluginServices = new List<ServiceRegistrationInfo>();

            try
            {
                // Process each assembly through the plugin
                foreach (var assembly in assemblies.Where(plugin.CanProcessAssembly))
                {
                    var discoveredInAssembly = plugin.DiscoverServices(assembly, options);
                    pluginServices.AddRange(discoveredInAssembly);

                    if (options.EnableLogging)
                    {
                        var count = discoveredInAssembly.Count();
                        if (count > 0)
                        {
                            logger.LogInformation("  Plugin {PluginName} discovered {Count} services in {AssemblyName}",
                                plugin.Name, count, assembly.GetName().Name);
                        }
                    }
                }

                // Validate the plugin's discoveries
                var validationResult = plugin.ValidateDiscoveredServices(pluginServices, allDiscoveredServices, options);
                validationResults.Add((plugin, validationResult));

                // If validation failed, stop processing
                if (!validationResult.IsValid)
                {
                    var errorMessage = $"Plugin '{plugin.Name}' validation failed: {string.Join("; ", validationResult.Errors)}";
                    throw new InvalidOperationException(errorMessage);
                }

                // Add valid services to the collection
                allDiscoveredServices.AddRange(pluginServices);

                // Log validation results
                if (options.EnableLogging && validationResult.HasMessages)
                {
                    LogValidationResults(logger, plugin.Name, validationResult);
                }
            }
            catch (Exception ex) when (ex is not InvalidOperationException)
            {
                // Handle plugin exceptions gracefully
                var errorMessage = $"Plugin '{plugin.Name}' encountered an error during discovery: {ex.Message}";
                if (options.EnableLogging)
                {
                    logger.LogError(ex, "Plugin {PluginName} encountered an error during discovery", plugin.Name);
                }

                // Depending on configuration, either throw or continue with other plugins
                throw new InvalidOperationException(errorMessage, ex);
            }
        }

        // Register all discovered services
        RegisterDiscoveredServices(services, allDiscoveredServices, options, logger);

        // Provide summary information
        if (!options.EnableLogging)
            return services;
        logger.LogInformation("Plugin discovery completed. Total services registered: {Count}", allDiscoveredServices.Count);
        LogDiscoverySummary(logger, validationResults);

        return services;
    }

    /// <summary>
    /// Adds a single plugin to the auto discovery process, providing a convenient method
    /// for scenarios where only one custom plugin is needed.
    /// 
    /// This method is perfect for applications that need just one specialized discovery
    /// strategy in addition to the standard attribute-based discovery. It provides
    /// the same power as the multi-plugin version but with a simpler API surface.
    /// </summary>
    /// <param name="services">The service collection to register discovered services with.</param>
    /// <param name="plugin">The single plugin to use for specialized service discovery.</param>
    /// <param name="configureOptions">Optional configuration for the discovery process.</param>
    /// <param name="assemblies">The assemblies to scan for services.</param>
    /// <returns>The service collection for method chaining.</returns>
    public static IServiceCollection AddAutoServicesWithPlugin(
        this IServiceCollection services,
        IServiceDiscoveryPlugin plugin,
        Action<AutoServiceOptions>? configureOptions = null,
        params Assembly[] assemblies)
    {
        return services.AddAutoServicesWithPlugins(new[] { plugin }, configureOptions, assemblies);
    }

    /// <summary>
    /// Registers a plugin type that will be instantiated and used for service discovery.
    /// This method provides a convenient way to register plugins using dependency injection,
    /// allowing plugins to have their own dependencies injected during construction.
    /// 
    /// This approach is particularly useful when plugins need access to configuration,
    /// logging, or other services during their operation. The plugin will be constructed
    /// using the service provider, allowing for full dependency injection support.
    /// </summary>
    /// <typeparam name="TPlugin">
    /// The type of plugin to register. Must implement IServiceDiscoveryPlugin and have
    /// a constructor that can be satisfied by the dependency injection container.
    /// </typeparam>
    /// <param name="services">The service collection to register the plugin with.</param>
    /// <param name="configureOptions">Optional configuration for the discovery process.</param>
    /// <param name="assemblies">The assemblies to scan for services.</param>
    /// <returns>The service collection for method chaining.</returns>
    public static IServiceCollection AddAutoServicesWithPlugin<TPlugin>(
        this IServiceCollection services,
        Action<AutoServiceOptions>? configureOptions = null,
        params Assembly[] assemblies)
        where TPlugin : class, IServiceDiscoveryPlugin
    {
        // Register the plugin type if not already registered
        if (services.All(s => s.ServiceType != typeof(TPlugin)))
        {
            services.AddTransient<TPlugin>();
        }

        // Build a temporary service provider to resolve the plugin
        var tempServiceProvider = services.BuildServiceProvider();
        var plugin = tempServiceProvider.GetRequiredService<TPlugin>();

        return services.AddAutoServicesWithPlugin(plugin, configureOptions, assemblies);
    }

    /// <summary>
    /// Logs validation results in a structured, readable format that helps developers
    /// understand what happened during the validation process and what actions might be needed.
    /// </summary>
    /// <param name="logger">The logger instance for structured logging.</param>
    /// <param name="pluginName">The name of the plugin that was validated.</param>
    /// <param name="result">The validation result containing messages to log.</param>
    private static void LogValidationResults(ILogger logger, string pluginName, PluginValidationResult result)
    {
        if (result.Errors.Count != 0)
        {
            foreach (var error in result.Errors)
            {
                logger.LogError("Plugin {PluginName} validation error: {Error}", pluginName, error);
            }
        }

        if (result.Warnings.Count != 0)
        {
            foreach (var warning in result.Warnings)
            {
                logger.LogWarning("Plugin {PluginName} validation warning: {Warning}", pluginName, warning);
            }
        }

        if (result.Information.Count == 0) return;
        foreach (var info in result.Information)
        {
            logger.LogInformation("Plugin {PluginName}: {Info}", pluginName, info);
        }
    }

    /// <summary>
    /// Provides a summary of the entire discovery process, highlighting key metrics
    /// and any important issues that were found across all plugins.
    /// </summary>
    /// <param name="logger">The logger instance for structured logging.</param>
    /// <param name="validationResults">The validation results from all plugins.</param>
    private static void LogDiscoverySummary(ILogger logger, List<(IServiceDiscoveryPlugin Plugin, PluginValidationResult Result)> validationResults)
    {
        var totalErrors = validationResults.Sum(r => r.Result.Errors.Count);
        var totalWarnings = validationResults.Sum(r => r.Result.Warnings.Count);
        var pluginsWithIssues = validationResults.Count(r => r.Result.TotalIssueCount > 0);

        logger.LogInformation("Discovery Summary: {PluginCount} plugins processed, {IssueCount} with issues, {ErrorCount} errors, {WarningCount} warnings",
            validationResults.Count, pluginsWithIssues, totalErrors, totalWarnings);

        if (totalErrors > 0)
        {
            logger.LogError("Plugin discovery completed with {ErrorCount} critical errors - review error messages above", totalErrors);
        }
        else if (totalWarnings > 0)
        {
            logger.LogWarning("Plugin discovery completed successfully with {WarningCount} warnings", totalWarnings);
        }
        else
        {
            logger.LogInformation("All plugins validated successfully with no issues");
        }
    }

    /// <summary>
    /// Registers the discovered services with the dependency injection container,
    /// applying proper ordering and lifetime management.
    /// </summary>
    /// <param name="services">The service collection to register services with.</param>
    /// <param name="discoveredServices">The services discovered by all plugins.</param>
    /// <param name="options">The configuration options for registration behavior.</param>
    /// <param name="logger">The logger instance for structured logging.</param>
    private static void RegisterDiscoveredServices(
        IServiceCollection services,
        IEnumerable<ServiceRegistrationInfo> discoveredServices,
        AutoServiceOptions options,
        ILogger logger)
    {
        // Order services by priority and register them
        var orderedServices = discoveredServices.OrderBy(s => s.Order).ToList();

        foreach (var serviceInfo in orderedServices)
        {
            services.Add(new ServiceDescriptor(
                serviceInfo.ServiceType,
                serviceInfo.ImplementationType,
                serviceInfo.Lifetime));

            if (options.EnableLogging)
            {
                logger.LogDebug("Registered: {ServiceType} -> {ImplementationType} ({Lifetime})",
                    serviceInfo.ServiceType.Name, serviceInfo.ImplementationType.Name, serviceInfo.Lifetime);
            }
        }
    }

    private static ILogger ResolveLogger(IServiceCollection services)
    {
        var factoryDescriptor = services.FirstOrDefault(d => d.ServiceType == typeof(ILoggerFactory));
        if (factoryDescriptor?.ImplementationInstance is ILoggerFactory factory)
            return factory.CreateLogger("FS.AutoServiceDiscovery.Plugins");
        return NullLogger.Instance;
    }
}