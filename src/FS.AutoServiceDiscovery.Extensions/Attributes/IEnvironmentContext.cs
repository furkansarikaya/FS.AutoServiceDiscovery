namespace FS.AutoServiceDiscovery.Extensions.Attributes;

/// <summary>
/// Provides environment-specific context information for conditional expressions.
/// 
/// Environment detection is one of the most common needs in conditional registration.
/// This interface provides convenient, readable methods for all common environment
/// checks, making expressions more self-documenting and less error-prone.
/// </summary>
public interface IEnvironmentContext
{
    /// <summary>
    /// Gets the current environment name (e.g., "Development", "Production", "Staging").
    /// </summary>
    string Name { get; }
    
    /// <summary>
    /// Checks if the current environment is Development.
    /// This method provides a more readable alternative to string comparisons.
    /// </summary>
    bool IsDevelopment();
    
    /// <summary>
    /// Checks if the current environment is Production.
    /// Production environments often have different service requirements than other environments.
    /// </summary>
    bool IsProduction();
    
    /// <summary>
    /// Checks if the current environment is Staging.
    /// Staging environments often need special configurations for testing purposes.
    /// </summary>
    bool IsStaging();
    
    /// <summary>
    /// Checks if the current environment matches the specified name.
    /// This method provides case-insensitive environment name comparison.
    /// </summary>
    /// <param name="environmentName">The environment name to check against</param>
    /// <returns>True if the current environment matches the specified name</returns>
    bool Is(string environmentName);
    
    /// <summary>
    /// Checks if the current environment is one of the specified environments.
    /// This method enables expressions like "IsAnyOf('Development', 'Testing')" for
    /// grouping related environments.
    /// </summary>
    /// <param name="environmentNames">The environment names to check against</param>
    /// <returns>True if the current environment matches any of the specified names</returns>
    bool IsAnyOf(params string[] environmentNames);
}