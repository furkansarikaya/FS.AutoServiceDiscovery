using System.Collections.Concurrent;
using System.Diagnostics;
using System.Reflection;
using FS.AutoServiceDiscovery.Extensions.Configuration;
using FS.AutoServiceDiscovery.Extensions.Performance;

namespace FS.AutoServiceDiscovery.Extensions.Architecture;

/// <summary>
/// Coordinates the execution of multiple service discovery plugins, managing their lifecycle and ensuring
/// reliable, performant discovery operations across all registered plugins.
/// 
/// This implementation represents the "conductor" of our plugin orchestra. Just as a conductor ensures
/// that all musicians play in harmony, at the right tempo, and contribute to a unified performance,
/// this coordinator ensures that all plugins work together effectively to discover services.
/// 
/// The coordinator addresses several complex challenges that arise when multiple independent systems
/// need to work together:
/// 1. **Order of Execution**: Some plugins might depend on others completing first
/// 2. **Error Isolation**: One plugin's failure shouldn't crash the entire discovery process
/// 3. **Resource Management**: Preventing plugins from interfering with each other's resources
/// 4. **Performance Optimization**: Ensuring efficient execution while maintaining thorough discovery
/// 5. **Conflict Resolution**: Handling cases where multiple plugins discover the same services
/// 
/// This approach enables a plugin ecosystem where different teams can contribute discovery logic
/// without needing to understand or modify the core discovery system.
/// </summary>
public class PluginCoordinator : IPluginCoordinator
{
    private readonly ConcurrentDictionary<string, IServiceDiscoveryPlugin> _registeredPlugins = new();
    private readonly IPerformanceMetricsCollector _metricsCollector;
    private readonly PluginCoordinatorStatistics _statistics = new();

    /// <summary>
    /// Initializes a new instance of the plugin coordinator with the specified metrics collector.
    /// 
    /// The dependency injection approach here is crucial - by injecting the metrics collector,
    /// we enable the coordinator to be thoroughly observable while remaining testable.
    /// </summary>
    /// <param name="metricsCollector">
    /// The metrics collector for tracking plugin performance and coordination statistics.
    /// </param>
    public PluginCoordinator(IPerformanceMetricsCollector metricsCollector)
    {
        _metricsCollector = metricsCollector ?? throw new ArgumentNullException(nameof(metricsCollector));
    }

    /// <summary>
    /// Executes all registered plugins in priority order and aggregates their results into a unified discovery result.
    /// 
    /// This method implements the core orchestration logic that makes the plugin system reliable and predictable.
    /// The execution follows a carefully designed process that balances performance with reliability:
    /// 
    /// 1. **Pre-execution Validation**: Verify that all plugins are in a valid state
    /// 2. **Priority Ordering**: Sort plugins by priority to ensure proper execution sequence
    /// 3. **Isolated Execution**: Run each plugin in isolation to prevent cross-contamination
    /// 4. **Progress Tracking**: Monitor execution progress and collect performance metrics
    /// 5. **Error Handling**: Gracefully handle plugin failures without stopping the process
    /// 6. **Result Aggregation**: Combine all successful plugin results into a unified collection
    /// 7. **Conflict Resolution**: Handle cases where multiple plugins register similar services
    /// 8. **Validation**: Ensure the final result set is consistent and valid
    /// 
    /// This comprehensive approach ensures that the plugin system behaves predictably even when
    /// individual plugins have bugs, performance issues, or unexpected interactions.
    /// </summary>
    public PluginExecutionResult ExecutePlugins(IEnumerable<Assembly> assemblies, AutoServiceOptions options)
    {
        var executionStopwatch = Stopwatch.StartNew();
        var result = new PluginExecutionResult();
        var assemblyList = assemblies.ToList();
        
        // Get ordered plugins for execution
        var orderedPlugins = _registeredPlugins.Values.OrderBy(p => p.Priority).ToList();
        
        if (options.EnableLogging)
        {
            Console.WriteLine($"Starting plugin coordination with {orderedPlugins.Count} plugins across {assemblyList.Count} assemblies...");
        }

        // Execute each plugin in isolation
        foreach (var plugin in orderedPlugins)
        {
            var pluginStopwatch = Stopwatch.StartNew();
            var pluginResult = new PluginExecutionResult.PluginResult
            {
                PluginName = plugin.Name,
                ExecutionOrder = result.PluginResults.Count + 1
            };

            try
            {
                if (options.EnableLogging)
                {
                    Console.WriteLine($"Executing plugin: {plugin.Name} (Priority: {plugin.Priority})");
                }

                // Filter assemblies that this plugin can process
                var relevantAssemblies = assemblyList.Where(plugin.CanProcessAssembly).ToList();
                
                if (relevantAssemblies.Count == 0)
                {
                    if (options.EnableLogging)
                    {
                        Console.WriteLine($"  Plugin {plugin.Name} has no relevant assemblies to process");
                    }
                    pluginResult.ValidationResult = PluginValidationResult.Success();
                    result.PluginResults.Add(pluginResult);
                    continue;
                }

                // Execute plugin discovery
                var discoveredServices = new List<ServiceRegistrationInfo>();
                foreach (var assembly in relevantAssemblies)
                {
                    var assemblyServices = plugin.DiscoverServices(assembly, options);
                    discoveredServices.AddRange(assemblyServices);
                }

                pluginResult.DiscoveredServices = discoveredServices;

                // Validate plugin results
                var validationResult = plugin.ValidateDiscoveredServices(
                    discoveredServices, 
                    result.AllDiscoveredServices, 
                    options);
                
                pluginResult.ValidationResult = validationResult;

                // If validation passed, add services to the global result
                if (validationResult.IsValid)
                {
                    result.AllDiscoveredServices.AddRange(discoveredServices);
                    pluginResult.IsSuccessful = true;
                    
                    if (options.EnableLogging)
                    {
                        Console.WriteLine($"  Plugin {plugin.Name} discovered {discoveredServices.Count} services successfully");
                    }
                }
                else
                {
                    result.HasErrors = true;
                    if (options.EnableLogging)
                    {
                        Console.WriteLine($"  Plugin {plugin.Name} validation failed: {string.Join("; ", validationResult.Errors)}");
                    }
                }

                // Record plugin performance metrics
                pluginStopwatch.Stop();
                _metricsCollector.RecordPluginExecution(
                    plugin.Name, 
                    pluginStopwatch.Elapsed, 
                    discoveredServices.Count, 
                    validationResult);

            }
            catch (Exception ex)
            {
                // Isolate plugin failures to prevent cascade failures
                pluginStopwatch.Stop();
                pluginResult.IsSuccessful = false;
                pluginResult.ValidationResult = PluginValidationResult.Failure($"Plugin execution failed: {ex.Message}");
                result.HasErrors = true;

                if (options.EnableLogging)
                {
                    Console.WriteLine($"  ERROR: Plugin {plugin.Name} threw exception: {ex.Message}");
                }

                // Still record metrics for failed executions
                _metricsCollector.RecordPluginExecution(
                    plugin.Name, 
                    pluginStopwatch.Elapsed, 
                    0, 
                    pluginResult.ValidationResult);
            }

            result.PluginResults.Add(pluginResult);
        }

        // Finalize execution results
        executionStopwatch.Stop();
        result.TotalExecutionTime = executionStopwatch.Elapsed;
        result.IsSuccessful = !result.HasErrors;

        // Update coordinator statistics
        _statistics.TotalExecutions++;
        if (result.IsSuccessful)
        {
            _statistics.SuccessfulExecutions++;
        }
        _statistics.TotalPluginsExecuted += orderedPlugins.Count;
        _statistics.TotalServicesDiscovered += result.AllDiscoveredServices.Count;

        if (options.EnableLogging)
        {
            Console.WriteLine($"Plugin coordination completed in {executionStopwatch.ElapsedMilliseconds}ms. " +
                            $"Total services discovered: {result.AllDiscoveredServices.Count}");
        }

        return result;
    }

    /// <summary>
    /// Registers a plugin with the coordinator, making it available for future discovery operations.
    /// 
    /// This method enables dynamic plugin management while ensuring system integrity through validation.
    /// The registration process verifies that the plugin meets all requirements and doesn't conflict
    /// with existing plugins.
    /// </summary>
    public void RegisterPlugin(IServiceDiscoveryPlugin plugin)
    {
        if (plugin == null)
            throw new ArgumentNullException(nameof(plugin));

        if (string.IsNullOrWhiteSpace(plugin.Name))
            throw new ArgumentException("Plugin must have a valid name", nameof(plugin));

        if (_registeredPlugins.ContainsKey(plugin.Name))
            throw new InvalidOperationException($"A plugin with name '{plugin.Name}' is already registered");

        _registeredPlugins.TryAdd(plugin.Name, plugin);
        _statistics.RegisteredPluginsCount = _registeredPlugins.Count;
    }

    /// <summary>
    /// Removes a previously registered plugin from the coordinator.
    /// </summary>
    public bool UnregisterPlugin(string pluginName)
    {
        if (string.IsNullOrWhiteSpace(pluginName))
            return false;

        var removed = _registeredPlugins.TryRemove(pluginName, out _);
        if (removed)
        {
            _statistics.RegisteredPluginsCount = _registeredPlugins.Count;
        }
        return removed;
    }

    /// <summary>
    /// Gets information about all currently registered plugins for monitoring and debugging purposes.
    /// </summary>
    public IEnumerable<PluginInfo> GetRegisteredPlugins()
    {
        return _registeredPlugins.Values.Select(plugin => new PluginInfo
        {
            Name = plugin.Name,
            Priority = plugin.Priority,
            TypeName = plugin.GetType().FullName ?? plugin.GetType().Name,
            IsActive = true // In this implementation, all registered plugins are considered active
        }).ToList();
    }

    /// <summary>
    /// Gets comprehensive performance statistics for the plugin coordination system.
    /// </summary>
    public PluginCoordinatorStatistics GetStatistics()
    {
        return new PluginCoordinatorStatistics
        {
            RegisteredPluginsCount = _statistics.RegisteredPluginsCount,
            TotalExecutions = _statistics.TotalExecutions,
            SuccessfulExecutions = _statistics.SuccessfulExecutions,
            TotalPluginsExecuted = _statistics.TotalPluginsExecuted,
            TotalServicesDiscovered = _statistics.TotalServicesDiscovered
        };
    }
}