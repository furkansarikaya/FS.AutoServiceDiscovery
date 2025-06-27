using Microsoft.Extensions.Configuration;

namespace FS.AutoServiceDiscovery.Extensions.Configuration;

/// <summary>
/// Configuration options for automatic service discovery and registration.
/// </summary>
public class AutoServiceOptions
{
    /// <summary>
    /// Gets or sets the active profile for profile-based service registration.
    /// Services marked with specific profiles will only be registered when this profile matches.
    /// </summary>
    /// <example>
    /// <code>
    /// options.Profile = "Production"; // Only services with Profile = "Production" will be registered
    /// </code>
    /// </example>
    public string? Profile { get; set; }
    
    /// <summary>
    /// Gets or sets whether the application is running in a test environment.
    /// Services marked with IgnoreInTests = true will be skipped during registration.
    /// Default is false.
    /// </summary>
    public bool IsTestEnvironment { get; set; } = false;
    
    /// <summary>
    /// Gets or sets whether to enable logging of service registration operations.
    /// When enabled, each registered service will be logged to the console.
    /// Default is true.
    /// </summary>
    public bool EnableLogging { get; set; } = true;
    
    /// <summary>
    /// Gets or sets the configuration instance used for conditional service registration.
    /// Required for services that use <see cref="Attributes.ConditionalServiceAttribute"/>.
    /// </summary>
    public IConfiguration? Configuration { get; set; }
}
