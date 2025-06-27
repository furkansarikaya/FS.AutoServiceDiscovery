namespace FS.AutoServiceDiscovery.Extensions.Architecture;

/// <summary>
/// Contains information about a registered service discovery plugin for monitoring and debugging purposes.
/// 
/// This class serves as a "business card" for plugins - it provides essential information about
/// each plugin without exposing the plugin's internal implementation details. This information
/// is particularly valuable for system administrators and developers who need to understand
/// what plugins are active and how they're configured.
/// </summary>
public class PluginInfo
{
    /// <summary>
    /// Gets or sets the unique name of the plugin.
    /// This name is used for identification, logging, and configuration purposes.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the execution priority of the plugin.
    /// Lower values indicate higher priority (executed first).
    /// </summary>
    public int Priority { get; set; }

    /// <summary>
    /// Gets or sets the full type name of the plugin implementation.
    /// This information is useful for debugging and understanding which specific
    /// implementation is being used.
    /// </summary>
    public string TypeName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether the plugin is currently active and available for execution.
    /// Inactive plugins are registered but temporarily disabled.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Gets or sets the time when this plugin was registered with the coordinator.
    /// </summary>
    public DateTime RegistrationTime { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets additional configuration or metadata associated with the plugin.
    /// This can include custom settings, feature flags, or other plugin-specific information.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();

    /// <summary>
    /// Gets or sets the last time this plugin was successfully executed.
    /// This information helps track plugin usage patterns.
    /// </summary>
    public DateTime? LastExecutionTime { get; set; }

    /// <summary>
    /// Gets or sets the version information for the plugin, if available.
    /// This helps with compatibility tracking and debugging.
    /// </summary>
    public string? Version { get; set; }
}