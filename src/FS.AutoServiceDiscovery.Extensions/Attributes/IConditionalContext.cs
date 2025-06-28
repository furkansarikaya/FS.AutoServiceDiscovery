using Microsoft.Extensions.Configuration;

namespace FS.AutoServiceDiscovery.Extensions.Attributes;

/// <summary>
/// Defines a rich context interface for expression-based conditional service registration.
/// 
/// Think of this interface as a "control panel" that your conditional expressions can use
/// to make intelligent decisions about service registration. Just like a pilot has access
/// to various instruments and controls in a cockpit, your conditional expressions have
/// access to environment information, configuration values, and utility methods through
/// this context.
/// 
/// The context pattern solves a fundamental challenge in DSL design: how do you provide
/// rich, discoverable functionality while maintaining clean, readable expressions? By
/// encapsulating all available information and operations in a well-designed interface,
/// we enable powerful expressions while keeping them intuitive and maintainable.
/// </summary>
public interface IConditionalContext
{
    /// <summary>
    /// Gets access to the application's configuration system.
    /// This provides direct access to appsettings.json, environment variables,
    /// command line arguments, and other configuration sources.
    /// </summary>
    IConfiguration? Configuration { get; }
    
    /// <summary>
    /// Gets environment-specific information and utilities.
    /// This object provides convenient methods for checking the current environment
    /// and making environment-based decisions.
    /// </summary>
    IEnvironmentContext Environment { get; }
    
    /// <summary>
    /// Gets access to feature flag evaluation utilities.
    /// Feature flags are a powerful pattern for controlling functionality deployment,
    /// and this interface makes them easily accessible in conditional expressions.
    /// </summary>
    IFeatureFlagContext FeatureFlags { get; }
    
    /// <summary>
    /// Gets custom properties that can be set by applications for specialized conditional logic.
    /// This dictionary allows applications to inject their own context data for use in
    /// conditional expressions, enabling highly customized conditional behavior.
    /// </summary>
    Dictionary<string, object> CustomProperties { get; }

    /// <summary>
    /// Convenience method for checking if a feature flag is enabled.
    /// This method provides a shorthand for the most common feature flag operation,
    /// making expressions more readable and concise.
    /// </summary>
    /// <param name="featureName">The name of the feature to check</param>
    /// <returns>True if the feature is enabled, false otherwise</returns>
    bool FeatureEnabled(string featureName);
    
    /// <summary>
    /// Convenience method for checking configuration values with type conversion.
    /// This method simplifies the common pattern of retrieving configuration values
    /// with default values and type safety.
    /// </summary>
    /// <typeparam name="T">The type to convert the configuration value to</typeparam>
    /// <param name="key">The configuration key to retrieve</param>
    /// <param name="defaultValue">The default value if the key is not found</param>
    /// <returns>The configuration value converted to the specified type</returns>
    T GetConfigValue<T>(string key, T defaultValue = default!);
    
    /// <summary>
    /// Evaluates a custom condition using application-provided logic.
    /// This method enables applications to register their own named conditions
    /// that can be used in expressions, providing unlimited extensibility.
    /// </summary>
    /// <param name="conditionName">The name of the custom condition to evaluate</param>
    /// <returns>True if the custom condition is satisfied, false otherwise</returns>
    bool EvaluateCustomCondition(string conditionName);
}