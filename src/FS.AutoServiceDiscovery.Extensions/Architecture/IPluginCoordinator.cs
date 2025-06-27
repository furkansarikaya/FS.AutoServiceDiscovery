using System.Reflection;
using FS.AutoServiceDiscovery.Extensions.Configuration;

namespace FS.AutoServiceDiscovery.Extensions.Architecture;

/// <summary>
/// Defines a contract for coordinating multiple service discovery plugins and managing their execution lifecycle.
/// 
/// The plugin coordinator serves as the "conductor of an orchestra" - it ensures that all plugins
/// work together harmoniously, each contributing their part at the right time and in the right way.
/// Without coordination, plugins might conflict with each other, process the same assemblies multiple
/// times, or produce inconsistent results.
/// 
/// This interface addresses several critical challenges in plugin-based architectures:
/// 1. **Execution Order**: Ensuring plugins run in the correct sequence based on dependencies
/// 2. **Conflict Resolution**: Handling cases where multiple plugins want to register the same service
/// 3. **Error Isolation**: Preventing one plugin's failure from breaking the entire discovery process
/// 4. **Performance Optimization**: Avoiding duplicate work and optimizing resource usage
/// 5. **Lifecycle Management**: Properly initializing and cleaning up plugin resources
/// </summary>
public interface IPluginCoordinator
{
    /// <summary>
    /// Executes all registered plugins against the specified assemblies and returns the aggregated results.
    /// 
    /// This method implements the core orchestration logic that makes the plugin system work effectively.
    /// It handles the complex task of coordinating multiple independent plugins while ensuring consistent,
    /// reliable results.
    /// 
    /// The execution process involves several important steps:
    /// 1. **Plugin Ordering**: Sort plugins by priority to ensure proper execution sequence
    /// 2. **Assembly Filtering**: Allow each plugin to determine which assemblies it should process
    /// 3. **Isolated Execution**: Run each plugin in isolation to prevent cross-plugin interference
    /// 4. **Result Validation**: Validate plugin results and handle conflicts or errors
    /// 5. **Result Aggregation**: Combine all plugin results into a unified collection
    /// 6. **Conflict Resolution**: Handle cases where multiple plugins register the same service
    /// 
    /// This coordinated approach ensures that the plugin system behaves predictably and reliably,
    /// even when plugins have complex interactions or dependencies.
    /// </summary>
    /// <param name="assemblies">
    /// The assemblies to be processed by the plugins. Each plugin will have the opportunity to
    /// examine these assemblies and extract service registration information according to its
    /// specific discovery logic.
    /// </param>
    /// <param name="options">
    /// Configuration options that apply to all plugins, ensuring consistent behavior across
    /// the entire discovery process. This includes settings like profiles, feature flags,
    /// and performance options.
    /// </param>
    /// <returns>
    /// A comprehensive result object containing all discovered services from all plugins,
    /// along with validation results, performance metrics, and any issues encountered
    /// during the discovery process.
    /// </returns>
    PluginExecutionResult ExecutePlugins(IEnumerable<Assembly> assemblies, AutoServiceOptions options);
    
    /// <summary>
    /// Registers a plugin with the coordinator for inclusion in future discovery operations.
    /// 
    /// This method allows dynamic plugin registration, which is useful for scenarios where:
    /// - Plugins are loaded at runtime from external assemblies
    /// - Different plugins need to be active in different environments
    /// - Plugin configuration needs to be determined dynamically
    /// 
    /// The registration process validates that the plugin is properly implemented and
    /// integrates it into the coordination system's execution plan.
    /// </summary>
    /// <param name="plugin">
    /// The plugin instance to register. The plugin will be validated to ensure it
    /// implements the required interface correctly and doesn't conflict with existing plugins.
    /// </param>
    void RegisterPlugin(IServiceDiscoveryPlugin plugin);
    
    /// <summary>
    /// Removes a previously registered plugin from the coordinator.
    /// 
    /// This method enables dynamic plugin management, allowing plugins to be added and removed
    /// at runtime based on changing requirements or configuration.
    /// </summary>
    /// <param name="pluginName">
    /// The name of the plugin to remove. This should match the Name property of the plugin
    /// that was previously registered.
    /// </param>
    /// <returns>
    /// True if the plugin was found and removed successfully, false if no plugin with the
    /// specified name was registered.
    /// </returns>
    bool UnregisterPlugin(string pluginName);
    
    /// <summary>
    /// Gets information about all currently registered plugins, including their configuration and status.
    /// 
    /// This method provides visibility into the plugin system's current state, which is valuable for:
    /// - Debugging discovery issues
    /// - Understanding which plugins are active in different environments
    /// - Monitoring plugin performance and effectiveness
    /// - Validating plugin registration and configuration
    /// </summary>
    /// <returns>
    /// A collection of plugin information objects describing each registered plugin's
    /// configuration, status, and recent performance metrics.
    /// </returns>
    IEnumerable<PluginInfo> GetRegisteredPlugins();
    
    /// <summary>
    /// Gets comprehensive performance statistics for the plugin coordination system.
    /// 
    /// These statistics provide insights into:
    /// - Overall plugin system performance
    /// - Individual plugin execution times and success rates
    /// - Resource usage patterns
    /// - Potential optimization opportunities
    /// 
    /// This information is crucial for maintaining optimal performance in production systems
    /// and identifying plugins that might need optimization or replacement.
    /// </summary>
    /// <returns>
    /// Detailed performance metrics for the entire plugin coordination system.
    /// </returns>
    PluginCoordinatorStatistics GetStatistics();
}