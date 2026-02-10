namespace FS.AutoServiceDiscovery.Extensions.Attributes;

/// <summary>
/// Default implementation of environment context using string-based environment names.
/// 
/// This implementation provides the standard environment detection logic that works
/// with ASP.NET Core hosting environments and custom environment configurations.
/// </summary>
/// <remarks>
/// The environment name is typically sourced from ASPNETCORE_ENVIRONMENT or DOTNET_ENVIRONMENT
/// environment variables. If no environment is specified, defaults to "Production" for safety.
/// </remarks>
/// <seealso cref="IEnvironmentContext"/>
/// <seealso cref="IConditionalContext"/>
public class EnvironmentContext : IEnvironmentContext
{
    /// <summary>
    /// Initializes a new environment context with the specified environment name.
    /// </summary>
    /// <param name="environmentName">The current environment name</param>
    public EnvironmentContext(string environmentName)
    {
        Name = environmentName ?? "Production";
    }

    /// <summary>
    /// Gets the current environment name.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Checks if the current environment is Development using case-insensitive comparison.
    /// </summary>
    public bool IsDevelopment() => Is("Development");

    /// <summary>
    /// Checks if the current environment is Production using case-insensitive comparison.
    /// </summary>
    public bool IsProduction() => Is("Production");

    /// <summary>
    /// Checks if the current environment is Staging using case-insensitive comparison.
    /// </summary>
    public bool IsStaging() => Is("Staging");

    /// <summary>
    /// Checks if the current environment matches the specified name using case-insensitive comparison.
    /// </summary>
    public bool Is(string environmentName) => 
        string.Equals(Name, environmentName, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Checks if the current environment matches any of the specified names.
    /// This method enables grouping related environments in conditional expressions.
    /// </summary>
    public bool IsAnyOf(params string[] environmentNames) => 
        environmentNames.Any(env => Is(env));
}