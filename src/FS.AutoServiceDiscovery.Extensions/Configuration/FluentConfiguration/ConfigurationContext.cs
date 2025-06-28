using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FS.AutoServiceDiscovery.Extensions.Configuration.FluentConfiguration;

/// <summary>
/// Provides contextual information for configuration decisions during the fluent configuration process.
/// 
/// This class serves as the "environment snapshot" for configuration operations. Just like a photographer
/// needs to understand the lighting conditions, camera settings, and subject matter before taking a shot,
/// the configuration system needs context about the runtime environment, available services, and
/// configuration state to make intelligent decisions.
/// 
/// The context pattern is particularly valuable in fluent configurations because it allows each
/// configuration method to access the complete state of the system when making decisions. This enables
/// sophisticated conditional logic and validation that can adapt to different deployment scenarios.
/// </summary>
public class ConfigurationContext
{
    /// <summary>
    /// Gets or sets the application configuration instance containing settings and feature flags.
    /// 
    /// This configuration instance provides access to application settings, connection strings,
    /// feature flags, and other configuration values that may influence service discovery decisions.
    /// Many conditional registration scenarios depend on configuration values to determine
    /// which services should be active in different environments.
    /// </summary>
    public IConfiguration? Configuration { get; set; }

    /// <summary>
    /// Gets or sets the service collection being configured.
    /// 
    /// Access to the service collection enables configuration logic to inspect what services
    /// are already registered, check for dependencies, and make intelligent decisions about
    /// additional registrations. This is particularly useful for avoiding duplicate registrations
    /// and ensuring that prerequisite services are available.
    /// </summary>
    public IServiceCollection? ServiceCollection { get; set; }

    /// <summary>
    /// Gets or sets the current environment name (e.g., "Development", "Production", "Testing").
    /// 
    /// The environment name is a fundamental piece of context that influences many configuration
    /// decisions. Different environments often require different service implementations,
    /// different caching strategies, or different logging levels.
    /// </summary>
    public string? Environment { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the application is running in a test context.
    /// 
    /// Test environments often have special requirements such as excluding background services,
    /// using mock implementations, or optimizing for test execution speed rather than production
    /// performance. This flag enables configuration logic to adapt appropriately.
    /// </summary>
    public bool IsTestEnvironment { get; set; }

    /// <summary>
    /// Gets or sets the active profile for profile-based service registration.
    /// 
    /// Profiles provide a way to group related configuration settings and enable different
    /// service combinations for different deployment scenarios. For example, a "HighPerformance"
    /// profile might enable additional caching services, while a "LowMemory" profile might
    /// use more lightweight implementations.
    /// </summary>
    public string? ActiveProfile { get; set; }

    /// <summary>
    /// Gets additional context properties that can be used by custom configuration logic.
    /// 
    /// This dictionary provides extensibility for custom configuration scenarios that require
    /// additional context information not covered by the standard properties. Custom naming
    /// conventions, plugins, and validation rules can store and retrieve their own context
    /// data through this collection.
    /// </summary>
    public Dictionary<string, object> AdditionalProperties { get; init; } = new();

    /// <summary>
    /// Gets or sets the timestamp when this context was created.
    /// 
    /// The creation timestamp can be useful for time-based configuration decisions,
    /// cache invalidation logic, or debugging configuration issues by understanding
    /// the temporal context of configuration operations.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets a configuration value with a specified key and optional default value.
    /// 
    /// This method provides a convenient way to access configuration values with fallback
    /// support. It handles null configuration instances gracefully and provides type-safe
    /// access to configuration data.
    /// </summary>
    /// <typeparam name="T">The type to convert the configuration value to.</typeparam>
    /// <param name="key">The configuration key to retrieve.</param>
    /// <param name="defaultValue">The default value to return if the key is not found or configuration is null.</param>
    /// <returns>The configuration value converted to the specified type, or the default value.</returns>
    public T GetConfigurationValue<T>(string key, T defaultValue = default!)
    {
        if (Configuration == null)
            return defaultValue;

        var value = Configuration[key];
        if (string.IsNullOrEmpty(value))
            return defaultValue;

        try
        {
            // Handle common type conversions
            return typeof(T) switch
            {
                Type t when t == typeof(string) => (T)(object)value,
                Type t when t == typeof(int) => (T)(object)int.Parse(value),
                Type t when t == typeof(bool) => (T)(object)bool.Parse(value),
                Type t when t == typeof(double) => (T)(object)double.Parse(value),
                Type t when t == typeof(decimal) => (T)(object)decimal.Parse(value),
                Type t when t == typeof(DateTime) => (T)(object)DateTime.Parse(value),
                _ => defaultValue
            };
        }
        catch
        {
            return defaultValue;
        }
    }

    /// <summary>
    /// Checks if a configuration key exists and has a non-empty value.
    /// 
    /// This method is useful for conditional logic that needs to determine if a configuration
    /// setting has been provided, regardless of its specific value.
    /// </summary>
    /// <param name="key">The configuration key to check.</param>
    /// <returns>True if the key exists and has a non-empty value, false otherwise.</returns>
    public bool HasConfigurationValue(string key)
    {
        return Configuration != null && !string.IsNullOrEmpty(Configuration[key]);
    }

    /// <summary>
    /// Gets a custom property value with the specified key and type.
    /// 
    /// This method provides type-safe access to additional properties stored in the context
    /// by custom configuration logic, plugins, or naming conventions.
    /// </summary>
    /// <typeparam name="T">The expected type of the property value.</typeparam>
    /// <param name="key">The property key to retrieve.</param>
    /// <param name="defaultValue">The default value to return if the key is not found or cannot be converted.</param>
    /// <returns>The property value converted to the specified type, or the default value.</returns>
    public T GetProperty<T>(string key, T defaultValue = default!)
    {
        if (!AdditionalProperties.TryGetValue(key, out var value))
            return defaultValue;

        if (value is T typedValue)
            return typedValue;

        try
        {
            return (T)Convert.ChangeType(value, typeof(T));
        }
        catch
        {
            return defaultValue;
        }
    }

    /// <summary>
    /// Sets a custom property value in the context.
    /// 
    /// This method allows custom configuration logic to store state or context information
    /// that can be accessed by other parts of the configuration process.
    /// </summary>
    /// <param name="key">The property key to set.</param>
    /// <param name="value">The value to store.</param>
    public void SetProperty(string key, object value)
    {
        AdditionalProperties[key] = value;
    }

    /// <summary>
    /// Creates a snapshot of the current context for use in asynchronous or deferred operations.
    /// 
    /// Context snapshots are useful when configuration logic needs to capture the current
    /// state for later use, such as in lazy evaluation scenarios or async operations where
    /// the original context might have changed by the time the operation executes.
    /// </summary>
    /// <returns>A new ConfigurationContext instance with the same values as the current context.</returns>
    public ConfigurationContext CreateSnapshot()
    {
        return new ConfigurationContext
        {
            Configuration = Configuration,
            ServiceCollection = ServiceCollection,
            Environment = Environment,
            IsTestEnvironment = IsTestEnvironment,
            ActiveProfile = ActiveProfile,
            CreatedAt = CreatedAt,
            AdditionalProperties = new Dictionary<string, object>(AdditionalProperties)
        };
    }

    /// <summary>
    /// Validates that the context contains the minimum required information for configuration operations.
    /// 
    /// This method ensures that the context is properly initialized and contains the essential
    /// information needed for configuration decisions. It can help catch configuration errors early.
    /// </summary>
    /// <returns>True if the context is valid for configuration operations, false otherwise.</returns>
    public bool IsValid()
    {
        // Basic validation - context should have at least service collection or configuration
        return ServiceCollection != null || Configuration != null;
    }

    /// <summary>
    /// Creates a string representation of the context for debugging and logging purposes.
    /// 
    /// This method provides a human-readable summary of the context state that can be useful
    /// for troubleshooting configuration issues or understanding the runtime environment.
    /// </summary>
    /// <returns>A formatted string describing the current context state.</returns>
    public override string ToString()
    {
        var properties = new List<string>();

        if (!string.IsNullOrEmpty(Environment))
            properties.Add($"Environment={Environment}");

        if (!string.IsNullOrEmpty(ActiveProfile))
            properties.Add($"Profile={ActiveProfile}");

        if (IsTestEnvironment)
            properties.Add("TestEnvironment=true");

        if (Configuration != null)
            properties.Add("Configuration=Available");

        if (ServiceCollection != null)
            properties.Add($"ServiceCollection={ServiceCollection.Count} services");

        if (AdditionalProperties.Count != 0)
            properties.Add($"AdditionalProperties={AdditionalProperties.Count}");

        return $"ConfigurationContext[{string.Join(", ", properties)}]";
    }
}