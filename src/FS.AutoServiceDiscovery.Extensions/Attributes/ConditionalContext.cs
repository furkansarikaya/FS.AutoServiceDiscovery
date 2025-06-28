using Microsoft.Extensions.Configuration;

namespace FS.AutoServiceDiscovery.Extensions.Attributes;

/// <summary>
/// Default implementation of the conditional context that integrates with standard .NET configuration.
/// 
/// This implementation demonstrates how the context interfaces can be implemented using
/// standard .NET patterns and libraries. Real applications might extend this implementation
/// or provide their own implementations that integrate with specific feature flag systems
/// or environment management tools.
/// </summary>
public class ConditionalContext : IConditionalContext
{
    private readonly string _environmentName;
    private readonly Dictionary<string, Func<bool>> _customConditions = new();

    /// <summary>
    /// Initializes a new conditional context with configuration and environment information.
    /// 
    /// The constructor demonstrates the dependency injection pattern - the context receives
    /// the dependencies it needs rather than creating them internally. This approach makes
    /// the context more testable and flexible.
    /// </summary>
    /// <param name="configuration">The application configuration instance</param>
    /// <param name="environmentName">The current environment name</param>
    public ConditionalContext(IConfiguration? configuration, string environmentName)
    {
        Configuration = configuration;
        _environmentName = environmentName ?? "Production"; // Safe default
        
        // Create environment context with the provided environment name
        Environment = new EnvironmentContext(_environmentName);
        
        // Create feature flag context that integrates with the configuration system
        FeatureFlags = new ConfigurationFeatureFlagContext(configuration);
        
        // Initialize custom properties collection for application-specific data
        CustomProperties = new Dictionary<string, object>();
    }

    /// <summary>
    /// Gets the application configuration instance for direct access in expressions.
    /// </summary>
    public IConfiguration? Configuration { get; }
    
    /// <summary>
    /// Gets the environment context for environment-specific conditional logic.
    /// </summary>
    public IEnvironmentContext Environment { get; }
    
    /// <summary>
    /// Gets the feature flag context for feature flag-based conditional logic.
    /// </summary>
    public IFeatureFlagContext FeatureFlags { get; }
    
    /// <summary>
    /// Gets the custom properties dictionary for application-specific conditional data.
    /// </summary>
    public Dictionary<string, object> CustomProperties { get; }

    /// <summary>
    /// Provides convenient access to feature flag evaluation.
    /// This method delegates to the feature flag context while providing a more concise API.
    /// </summary>
    public bool FeatureEnabled(string featureName) => FeatureFlags.IsEnabled(featureName);

    /// <summary>
    /// Provides convenient access to strongly-typed configuration values.
    /// This method handles the common pattern of retrieving configuration values with
    /// type conversion and default value support.
    /// </summary>
    public T GetConfigValue<T>(string key, T defaultValue = default!)
    {
        if (Configuration == null) return defaultValue;
        
        try
        {
            return Configuration.GetValue<T>(key) ?? defaultValue;
        }
        catch
        {
            // Return default value if conversion fails
            return defaultValue;
        }
    }

    /// <summary>
    /// Evaluates custom conditions that applications can register for specialized logic.
    /// This method enables applications to extend the conditional system with their own
    /// named conditions that can be used in expressions.
    /// </summary>
    public bool EvaluateCustomCondition(string conditionName)
    {
        return _customConditions.TryGetValue(conditionName, out var condition) && condition();
    }

    /// <summary>
    /// Registers a custom condition that can be used in expressions.
    /// This method allows applications to extend the conditional system with domain-specific logic.
    /// </summary>
    /// <param name="name">The name of the custom condition</param>
    /// <param name="condition">The function that evaluates the condition</param>
    public void RegisterCustomCondition(string name, Func<bool> condition)
    {
        _customConditions[name] = condition;
    }
}