namespace FS.AutoServiceDiscovery.Extensions.Attributes;

/// <summary>
/// Marks a class as a decorator for an existing service registration.
/// Decorators wrap existing service implementations to add cross-cutting
/// concerns such as caching, logging, validation, or retry logic.
/// </summary>
/// <example>
/// <code>
/// [DecoratorService(typeof(IUserService))]
/// public class CachingUserService : IUserService
/// {
///     private readonly IUserService _inner;
///     public CachingUserService(IUserService inner) => _inner = inner;
/// }
///
/// [DecoratorService(typeof(IOrderService), Order = 1)]
/// public class LoggingOrderService : IOrderService
/// {
///     private readonly IOrderService _inner;
///     public LoggingOrderService(IOrderService inner) => _inner = inner;
/// }
/// </code>
/// </example>
/// <remarks>
/// Decorators are applied in order of their <see cref="Order"/> property.
/// Lower order values are applied first (closer to the original service).
/// The decorator must implement the same interface as the decorated service
/// and accept the decorated service via constructor injection.
/// <para>
/// Multiple decorators can be applied to the same service. They are chained
/// in order, with each decorator wrapping the previous one.
/// </para>
/// </remarks>
/// <param name="decoratedServiceType">The service type being decorated</param>
/// <seealso cref="ServiceRegistrationAttribute"/>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public class DecoratorServiceAttribute(Type decoratedServiceType) : Attribute
{
    /// <summary>
    /// Gets the service type that this decorator wraps.
    /// The decorator class must implement this type.
    /// </summary>
    public Type DecoratedServiceType { get; } = decoratedServiceType;

    /// <summary>
    /// Gets or sets the decorator order. Lower values are applied first (closer to the original service).
    /// Default is 0.
    /// </summary>
    /// <remarks>
    /// When multiple decorators target the same service, the order determines the wrapping sequence.
    /// For example, with Order 0 (Logging) and Order 1 (Caching), the call chain would be:
    /// Caching -> Logging -> Original Service.
    /// </remarks>
    public int Order { get; set; } = 0;

    /// <summary>
    /// Gets or sets the profile for conditional decorator registration.
    /// If specified, the decorator will only be applied when the matching profile is active.
    /// </summary>
    public string? Profile { get; set; }
}
