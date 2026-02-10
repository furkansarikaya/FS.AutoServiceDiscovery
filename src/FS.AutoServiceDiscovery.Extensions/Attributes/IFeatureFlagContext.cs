namespace FS.AutoServiceDiscovery.Extensions.Attributes;

/// <summary>
/// Provides feature flag evaluation context for conditional expressions.
/// 
/// Feature flags (also known as feature toggles) are a powerful deployment pattern
/// that allows functionality to be turned on or off without code changes. This
/// interface provides clean access to feature flag systems in conditional expressions.
/// </summary>
/// <example>
/// <code>
/// // Use in conditional service registration:
/// [ConditionalService(ctx => ctx.FeatureFlags.IsEnabled("NewPaymentGateway"))]
/// public class NewPaymentService : IPaymentService { }
/// </code>
/// </example>
/// <remarks>
/// The default implementation <see cref="ConfigurationFeatureFlagContext"/> reads feature flags
/// from IConfiguration using the "FeatureFlags:{flagName}" key pattern. Custom implementations
/// can integrate with dedicated feature flag services like LaunchDarkly or Azure App Configuration.
/// </remarks>
/// <seealso cref="ConfigurationFeatureFlagContext"/>
/// <seealso cref="IConditionalContext"/>
public interface IFeatureFlagContext
{
    /// <summary>
    /// Checks if a feature flag is enabled.
    /// This is the core feature flag operation that most expressions will use.
    /// </summary>
    /// <param name="flagName">The name of the feature flag to check</param>
    /// <returns>True if the feature flag is enabled, false otherwise</returns>
    bool IsEnabled(string flagName);
    
    /// <summary>
    /// Checks if a feature flag is enabled for a specific user or context.
    /// Advanced feature flag systems often support targeted rollouts based on
    /// user attributes, percentage rollouts, or other criteria.
    /// </summary>
    /// <param name="flagName">The name of the feature flag to check</param>
    /// <param name="context">Additional context for feature flag evaluation</param>
    /// <returns>True if the feature flag is enabled for the given context</returns>
    bool IsEnabledFor(string flagName, Dictionary<string, object> context);
    
    /// <summary>
    /// Gets the value of a feature flag that returns more than just boolean values.
    /// Some feature flag systems support string, number, or JSON values for more
    /// sophisticated configuration scenarios.
    /// </summary>
    /// <typeparam name="T">The type of value expected from the feature flag</typeparam>
    /// <param name="flagName">The name of the feature flag</param>
    /// <param name="defaultValue">The default value if the flag is not found</param>
    /// <returns>The feature flag value converted to the specified type</returns>
    T GetFlagValue<T>(string flagName, T defaultValue = default!);
}