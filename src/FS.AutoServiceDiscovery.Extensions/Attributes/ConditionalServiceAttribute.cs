namespace FS.AutoServiceDiscovery.Extensions.Attributes;

/// <summary>
/// Specifies that a service should only be registered if specific configuration conditions are met.
/// Multiple attributes can be applied to require multiple conditions (AND logic).
/// </summary>
/// <param name="configurationKey">The configuration key to check (e.g., "FeatureFlags:EnableEmailService")</param>
/// <param name="expectedValue">The expected value for the configuration key (e.g., "true", "enabled")</param>
/// <example>
/// <code>
/// [ConditionalService("FeatureFlags:EnableEmailService", "true")]
/// [ServiceRegistration(ServiceLifetime.Scoped)]
/// public class EmailService : IEmailService
/// {
///     // Service implementation
/// }
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class ConditionalServiceAttribute(string configurationKey, string expectedValue) : Attribute
{
    /// <summary>
    /// Gets the configuration key to check for the conditional registration.
    /// </summary>
    public string ConfigurationKey { get; } = configurationKey;
    
    /// <summary>
    /// Gets the expected value that the configuration key should have for the service to be registered.
    /// </summary>
    public string ExpectedValue { get; } = expectedValue;
}