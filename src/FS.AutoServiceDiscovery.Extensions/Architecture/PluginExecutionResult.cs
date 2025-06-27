using FS.AutoServiceDiscovery.Extensions.Configuration;

namespace FS.AutoServiceDiscovery.Extensions.Architecture;

/// <summary>
/// Represents the comprehensive result of executing all service discovery plugins, including
/// performance metrics, validation results, and aggregated service discoveries.
/// 
/// This class serves as the "final report" of the plugin coordination process. Just like a
/// project manager would provide a comprehensive status report after coordinating multiple
/// teams, this class provides a complete picture of what happened during plugin execution.
/// 
/// The result includes both the "what" (discovered services) and the "how" (performance metrics,
/// validation results, execution details) to enable thorough analysis and debugging.
/// </summary>
public class PluginExecutionResult
{
    /// <summary>
    /// Gets or sets whether the overall plugin execution completed successfully.
    /// 
    /// This represents the overall health of the plugin execution process. True indicates
    /// that all plugins executed without critical failures, though individual plugins
    /// might have had warnings or minor issues.
    /// </summary>
    public bool IsSuccessful { get; set; } = true;

    /// <summary>
    /// Gets or sets whether any errors occurred during plugin execution.
    /// 
    /// This flag provides a quick way to determine if detailed error analysis is needed.
    /// Even if HasErrors is true, some plugins might have succeeded.
    /// </summary>
    public bool HasErrors { get; set; } = false;

    /// <summary>
    /// Gets or sets the total time spent executing all plugins.
    /// 
    /// This metric is crucial for understanding the performance impact of the plugin system
    /// and identifying opportunities for optimization.
    /// </summary>
    public TimeSpan TotalExecutionTime { get; set; }

    /// <summary>
    /// Gets the collection of all services discovered by all plugins.
    /// 
    /// This is the primary output of the plugin system - the aggregated collection of
    /// services that should be registered with the dependency injection container.
    /// </summary>
    public List<ServiceRegistrationInfo> AllDiscoveredServices { get; } = new();

    /// <summary>
    /// Gets the detailed results from each individual plugin execution.
    /// 
    /// This collection provides granular visibility into how each plugin performed,
    /// which is essential for debugging issues and optimizing plugin configurations.
    /// </summary>
    public List<PluginResult> PluginResults { get; } = new();

    /// <summary>
    /// Gets aggregate statistics about the plugin execution process.
    /// 
    /// These statistics provide insights into the overall effectiveness and performance
    /// of the plugin coordination system.
    /// </summary>
    public PluginExecutionStatistics Statistics => new()
    {
        TotalPluginsExecuted = PluginResults.Count,
        SuccessfulPlugins = PluginResults.Count(r => r.IsSuccessful),
        FailedPlugins = PluginResults.Count(r => !r.IsSuccessful),
        TotalServicesDiscovered = AllDiscoveredServices.Count,
        TotalExecutionTimeMs = (long)TotalExecutionTime.TotalMilliseconds,
        AveragePluginExecutionTimeMs = PluginResults.Count > 0
            ? PluginResults.Average(r => r.ExecutionTime?.TotalMilliseconds ?? 0)
            : 0
    };

    /// <summary>
    /// Represents the execution result of a single plugin within the overall coordination process.
    /// </summary>
    public class PluginResult
    {
        /// <summary>
        /// Gets or sets the name of the plugin that was executed.
        /// </summary>
        public string PluginName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the order in which this plugin was executed.
        /// This helps understand the execution sequence and dependencies.
        /// </summary>
        public int ExecutionOrder { get; set; }

        /// <summary>
        /// Gets or sets whether this specific plugin executed successfully.
        /// </summary>
        public bool IsSuccessful { get; set; } = false;

        /// <summary>
        /// Gets or sets the time spent executing this specific plugin.
        /// </summary>
        public TimeSpan? ExecutionTime { get; set; }

        /// <summary>
        /// Gets or sets the services discovered by this plugin.
        /// </summary>
        public List<ServiceRegistrationInfo> DiscoveredServices { get; set; } = new();

        /// <summary>
        /// Gets or sets the validation result for this plugin's discoveries.
        /// </summary>
        public PluginValidationResult ValidationResult { get; set; } = PluginValidationResult.Success();
    }

    /// <summary>
    /// Aggregated statistics about the plugin execution process.
    /// </summary>
    public class PluginExecutionStatistics
    {
        /// <summary>
        /// Gets the total number of plugins that were executed.
        /// </summary>
        public int TotalPluginsExecuted { get; set; }

        /// <summary>
        /// Gets the number of plugins that executed successfully.
        /// </summary>
        public int SuccessfulPlugins { get; set; }

        /// <summary>
        /// Gets the number of plugins that failed to execute.
        /// </summary>
        public int FailedPlugins { get; set; }

        /// <summary>
        /// Gets the total number of services discovered across all plugins.
        /// </summary>
        public int TotalServicesDiscovered { get; set; }

        /// <summary>
        /// Gets the total time spent executing all plugins.
        /// </summary>
        public long TotalExecutionTimeMs { get; set; }

        /// <summary>
        /// Gets the average time spent executing a single plugin.
        /// </summary>
        public double AveragePluginExecutionTimeMs { get; set; }

        /// <summary>
        /// Calculates the success rate as a percentage of successful plugin executions.
        /// </summary>
        public double SuccessRate => TotalPluginsExecuted > 0
            ? (double)SuccessfulPlugins / TotalPluginsExecuted * 100
            : 0;
    }
}