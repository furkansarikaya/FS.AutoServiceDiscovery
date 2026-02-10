# Decorator Pattern

The decorator pattern wraps an existing service implementation with additional behavior without modifying the original class. FS.AutoServiceDiscovery v10.0.2 provides `DecoratorServiceAttribute` to declare decorators that are automatically discovered and applied to the services they wrap.

## What the Decorator Pattern Is

A decorator implements the same interface as the service it wraps and delegates calls to the inner instance while adding behavior before, after, or around each call. Common uses include caching, logging, validation, retry logic, and metrics collection. The consumer of the service is unaware that decoration has occurred because the interface contract is unchanged.

## Using DecoratorServiceAttribute

Apply `DecoratorServiceAttribute` to a class, specifying the service type it decorates. The decorator must implement the same interface and accept the inner service via constructor injection:

```csharp
[DecoratorService(typeof(IUserService))]
public class CachingUserService : IUserService
{
    private readonly IUserService _inner;
    private readonly IMemoryCache _cache;

    public CachingUserService(IUserService inner, IMemoryCache cache)
    {
        _inner = inner;
        _cache = cache;
    }

    public async Task<User> GetByIdAsync(int id)
    {
        var cacheKey = $"user:{id}";
        if (_cache.TryGetValue(cacheKey, out User? cached))
            return cached!;

        var user = await _inner.GetByIdAsync(id);
        _cache.Set(cacheKey, user, TimeSpan.FromMinutes(5));
        return user;
    }

    public Task<IEnumerable<User>> GetAllUsersAsync()
        => _inner.GetAllUsersAsync();

    public Task<User> CreateUserAsync(CreateUserRequest request)
        => _inner.CreateUserAsync(request);
}
```

The original `IUserService` implementation is registered normally with `ServiceRegistrationAttribute`:

```csharp
[ServiceRegistration(ServiceLifetime.Scoped)]
public class UserService : IUserService
{
    // Original implementation
}
```

After discovery, resolving `IUserService` returns `CachingUserService`, which internally delegates to `UserService`.

## Logging Decorator Example

```csharp
[DecoratorService(typeof(IOrderService))]
public class LoggingOrderService : IOrderService
{
    private readonly IOrderService _inner;
    private readonly ILogger<LoggingOrderService> _logger;

    public LoggingOrderService(IOrderService inner, ILogger<LoggingOrderService> logger)
    {
        _inner = inner;
        _logger = logger;
    }

    public async Task<Order> PlaceOrderAsync(PlaceOrderRequest request)
    {
        _logger.LogInformation("Placing order for {CustomerId}", request.CustomerId);
        var order = await _inner.PlaceOrderAsync(request);
        _logger.LogInformation("Order {OrderId} placed", order.Id);
        return order;
    }
}
```

## Multiple Decorators with Order

When multiple decorators target the same service, the `Order` property controls the wrapping sequence. Lower order values are applied first, meaning they sit closer to the original service. Higher order values wrap the outer layers.

```csharp
[DecoratorService(typeof(IOrderService), Order = 0)]
public class LoggingOrderService(IOrderService inner) : IOrderService
{
    // Delegates to inner with logging
}

[DecoratorService(typeof(IOrderService), Order = 1)]
public class CachingOrderService(IOrderService inner, IMemoryCache cache) : IOrderService
{
    // Checks cache, then delegates to inner
}

[DecoratorService(typeof(IOrderService), Order = 2)]
public class RetryOrderService(IOrderService inner) : IOrderService
{
    // Retries on transient failure, then delegates to inner
}
```

The resulting call chain when resolving `IOrderService`:

```
RetryOrderService (Order 2, outermost)
  -> CachingOrderService (Order 1)
    -> LoggingOrderService (Order 0)
      -> OrderService (original implementation)
```

## How Decorators Are Chained Internally

The library processes decorators after all standard services are registered. For each decorator (ordered by `Order`), it locates the existing `ServiceDescriptor` for the decorated service type, removes it, and adds a new factory-based descriptor. The factory creates the original service instance from the removed descriptor, then passes it to the decorator's constructor via `ActivatorUtilities.CreateInstance`.

Each decorator wraps whatever was previously registered for that service type, including any earlier decorators. The decorator can accept additional constructor parameters (such as `ILogger` or `IMemoryCache`) which are resolved from the container normally.

## Combining with Profiles

Decorators support profile-based conditional registration through the `Profile` property. Set `Profile = "Development"` to apply a debug decorator only in development:

```csharp
[DecoratorService(typeof(IUserService), Profile = "Development")]
public class DebugUserService : IUserService
{
    private readonly IUserService _inner;
    public DebugUserService(IUserService inner) => _inner = inner;
    // Adds debug output only in development
}
```

When `EnableLogging = true`, each decorator application is logged during startup:

```
Decorated: IUserService with CachingUserService
```

## Best Practices

- Keep decorators focused on a single concern. A caching decorator should only cache; a logging decorator should only log.
- Use the `Order` property deliberately. Document the intended call chain order in comments or project documentation.
- Decorators inherit the lifetime of the original service descriptor. If the original is Scoped, the decorated version is also Scoped.
- Do not apply `ServiceRegistrationAttribute` to decorator classes. Use only `DecoratorServiceAttribute`. The library treats these as distinct registration paths.
- Test decorators by injecting a mock of the inner service to verify that delegation and additional behavior work correctly.
- For open generic services registered with `OpenGenericRegistrationAttribute`, decorator support is limited to closed types. See the [Open Generics](OpenGenerics.md) guide for related considerations.
