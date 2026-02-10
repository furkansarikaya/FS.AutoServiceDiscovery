using Microsoft.Extensions.DependencyInjection;

namespace FS.AutoServiceDiscovery.Extensions.Validation;

/// <summary>
/// Represents the result of a scope validation analysis.
/// Contains information about any violations and warnings found during validation.
/// </summary>
/// <remarks>
/// A scope validation result is produced by <see cref="ScopeValidator.ValidateScopes"/>
/// after analyzing all registered services for captive dependency issues.
/// </remarks>
/// <seealso cref="ScopeValidator"/>
/// <seealso cref="ScopeViolation"/>
/// <seealso cref="ScopeWarning"/>
public class ScopeValidationResult
{
    /// <summary>
    /// Gets or sets whether the validation passed without any errors.
    /// A result is valid when there are no <see cref="ScopeViolation"/> entries.
    /// Warnings alone do not make the result invalid.
    /// </summary>
    public bool IsValid { get; set; } = true;

    /// <summary>
    /// Gets or sets the list of scope violations (errors) detected during validation.
    /// Violations represent definite scope mismatches such as a Singleton depending on a Scoped service.
    /// </summary>
    public List<ScopeViolation> Violations { get; set; } = [];

    /// <summary>
    /// Gets or sets the list of scope warnings detected during validation.
    /// Warnings represent potential issues that may or may not cause problems at runtime.
    /// </summary>
    public List<ScopeWarning> Warnings { get; set; } = [];
}

/// <summary>
/// Represents a scope violation where a longer-lived service depends on a shorter-lived service.
/// This is a definite error that will likely cause runtime issues.
/// </summary>
/// <example>
/// <code>
/// // This would produce a ScopeViolation:
/// // Singleton MySingletonService depends on Scoped MyScopedDependency
/// [ServiceRegistration(ServiceLifetime.Singleton)]
/// public class MySingletonService(IMyScopedDependency dep) : IMySingletonService { }
/// </code>
/// </example>
/// <remarks>
/// A captive dependency occurs when a service with a longer lifetime captures (holds a reference to)
/// a service with a shorter lifetime. The captured service instance will live as long as the capturing
/// service, effectively becoming a singleton even though it was registered as scoped or transient.
/// </remarks>
public class ScopeViolation
{
    /// <summary>
    /// Gets or sets the type that has the invalid dependency (the consumer).
    /// </summary>
    public Type ConsumerType { get; set; } = null!;

    /// <summary>
    /// Gets or sets the dependency type that is being incorrectly captured.
    /// </summary>
    public Type DependencyType { get; set; } = null!;

    /// <summary>
    /// Gets or sets the lifetime of the consumer (the longer-lived service).
    /// </summary>
    public ServiceLifetime ConsumerLifetime { get; set; }

    /// <summary>
    /// Gets or sets the lifetime of the dependency (the shorter-lived service being captured).
    /// </summary>
    public ServiceLifetime DependencyLifetime { get; set; }

    /// <summary>
    /// Gets or sets a human-readable message describing the violation.
    /// </summary>
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// Represents a scope warning that indicates a potential but not definite issue.
/// For example, a Singleton depending on a Transient service.
/// </summary>
/// <remarks>
/// While a Singleton depending on a Transient service is not always wrong, it means the
/// Transient service instance will live for the lifetime of the Singleton, which may lead
/// to unexpected behavior or memory leaks if the Transient service is expected to be short-lived.
/// </remarks>
public class ScopeWarning
{
    /// <summary>
    /// Gets or sets the type that has the potentially problematic dependency.
    /// </summary>
    public Type ConsumerType { get; set; } = null!;

    /// <summary>
    /// Gets or sets the dependency type that may be incorrectly captured.
    /// </summary>
    public Type DependencyType { get; set; } = null!;

    /// <summary>
    /// Gets or sets the lifetime of the consumer.
    /// </summary>
    public ServiceLifetime ConsumerLifetime { get; set; }

    /// <summary>
    /// Gets or sets the lifetime of the dependency.
    /// </summary>
    public ServiceLifetime DependencyLifetime { get; set; }

    /// <summary>
    /// Gets or sets a human-readable message describing the warning.
    /// </summary>
    public string Message { get; set; } = string.Empty;
}
