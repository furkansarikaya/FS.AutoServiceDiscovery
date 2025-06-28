using Microsoft.Extensions.Configuration;

namespace FS.AutoServiceDiscovery.Extensions.Attributes;

/// <summary>
/// Configuration-based implementation of feature flag context.
/// 
/// This implementation demonstrates how feature flags can be implemented using the
/// standard .NET configuration system. In production applications, you might integrate
/// with dedicated feature flag services like LaunchDarkly, Azure App Configuration,
/// or other feature management platforms.
/// </summary>
public class ConfigurationFeatureFlagContext : IFeatureFlagContext
{
    private readonly IConfiguration? _configuration;
    private const string FeatureFlagPrefix = "FeatureFlags:";

    /// <summary>
    /// Initializes a new configuration-based feature flag context.
    /// </summary>
    /// <param name="configuration">The configuration instance to use for feature flag lookups</param>
    public ConfigurationFeatureFlagContext(IConfiguration? configuration)
    {
        _configuration = configuration;
    }

    /// <summary>
    /// Checks if a feature flag is enabled by looking up the flag in configuration.
    /// This method uses the convention that feature flags are stored under "FeatureFlags:FlagName".
    /// </summary>
    public bool IsEnabled(string flagName)
    {
        if (_configuration == null) return false;
        
        var flagKey = $"{FeatureFlagPrefix}{flagName}";
        return _configuration.GetValue<bool>(flagKey, false);
    }

    /// <summary>
    /// Advanced feature flag evaluation with context (placeholder implementation).
    /// Real feature flag systems would use the context for targeted rollouts,
    /// A/B testing, or user-specific feature access.
    /// </summary>
    public bool IsEnabledFor(string flagName, Dictionary<string, object> context)
    {
        // For this simple implementation, we ignore context and fall back to basic evaluation
        // A real implementation would use the context for more sophisticated evaluation
        return IsEnabled(flagName);
    }

    /// <summary>
    /// Gets a typed value from a feature flag configuration.
    /// This method supports feature flags that return values other than simple booleans.
    /// </summary>
    public T GetFlagValue<T>(string flagName, T defaultValue = default!)
    {
        if (_configuration == null) return defaultValue;
        
        var flagKey = $"{FeatureFlagPrefix}{flagName}";
        return _configuration.GetValue<T>(flagKey, defaultValue) ?? defaultValue;
    }
}