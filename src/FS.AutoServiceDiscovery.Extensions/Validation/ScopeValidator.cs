using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace FS.AutoServiceDiscovery.Extensions.Validation;

/// <summary>
/// Validates service registration scopes to detect potential captive dependency issues.
/// A captive dependency occurs when a longer-lived service depends on a shorter-lived service
/// (e.g., a Singleton depending on a Scoped service).
/// </summary>
/// <example>
/// <code>
/// var result = ScopeValidator.ValidateScopes(services);
/// if (!result.IsValid)
/// {
///     foreach (var violation in result.Violations)
///     {
///         Console.WriteLine(violation.Message);
///     }
/// }
/// </code>
/// </example>
/// <remarks>
/// Scope validation runs after all services are registered and before the service provider is built.
/// It analyzes constructor dependencies to detect invalid scope combinations:
/// <list type="bullet">
/// <item><description>Singleton -> Scoped (ERROR: captive dependency)</description></item>
/// <item><description>Singleton -> Transient (WARNING: potential memory leak)</description></item>
/// </list>
/// </remarks>
/// <seealso cref="ScopeValidationResult"/>
/// <seealso cref="ScopeViolation"/>
/// <seealso cref="ScopeWarning"/>
public static class ScopeValidator
{
    /// <summary>
    /// Validates the scopes of all registered services in the service collection.
    /// Detects captive dependency issues and potential lifetime mismatches.
    /// </summary>
    /// <param name="services">The service collection to validate.</param>
    /// <returns>A <see cref="ScopeValidationResult"/> containing any violations and warnings found.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> is null.</exception>
    public static ScopeValidationResult ValidateScopes(IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        var result = new ScopeValidationResult();

        // Build a lookup of service type -> lifetime for registered services
        var lifetimeLookup = new Dictionary<Type, ServiceLifetime>();
        foreach (var descriptor in services)
        {
            // Use the last registration for each service type (matching DI container behavior)
            lifetimeLookup[descriptor.ServiceType] = descriptor.Lifetime;
        }

        // Check each service's constructor dependencies
        foreach (var descriptor in services)
        {
            var implementationType = descriptor.ImplementationType;
            if (implementationType == null)
                continue;

            var constructors = implementationType.GetConstructors(BindingFlags.Public | BindingFlags.Instance);
            if (constructors.Length == 0)
                continue;

            // Use the constructor with the most parameters (DI convention)
            var primaryConstructor = constructors.OrderByDescending(c => c.GetParameters().Length).First();

            foreach (var parameter in primaryConstructor.GetParameters())
            {
                var parameterType = parameter.ParameterType;

                if (!lifetimeLookup.TryGetValue(parameterType, out var dependencyLifetime))
                    continue;

                // Check for captive dependency: Singleton -> Scoped
                if (descriptor.Lifetime == ServiceLifetime.Singleton &&
                    dependencyLifetime == ServiceLifetime.Scoped)
                {
                    result.IsValid = false;
                    result.Violations.Add(new ScopeViolation
                    {
                        ConsumerType = implementationType,
                        DependencyType = parameterType,
                        ConsumerLifetime = descriptor.Lifetime,
                        DependencyLifetime = dependencyLifetime,
                        Message = $"Captive dependency: Singleton '{implementationType.Name}' depends on Scoped '{parameterType.Name}'. " +
                                  $"The Scoped service will be captured and live as long as the Singleton."
                    });
                }

                // Check for potential issue: Singleton -> Transient
                if (descriptor.Lifetime == ServiceLifetime.Singleton &&
                    dependencyLifetime == ServiceLifetime.Transient)
                {
                    result.Warnings.Add(new ScopeWarning
                    {
                        ConsumerType = implementationType,
                        DependencyType = parameterType,
                        ConsumerLifetime = descriptor.Lifetime,
                        DependencyLifetime = dependencyLifetime,
                        Message = $"Potential memory leak: Singleton '{implementationType.Name}' depends on Transient '{parameterType.Name}'. " +
                                  $"The Transient service will live as long as the Singleton, which may cause unexpected behavior."
                    });
                }
            }
        }

        return result;
    }
}
