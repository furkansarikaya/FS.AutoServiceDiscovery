# Open Generic Registration

Open generic registration allows you to register a single generic type definition (such as `Repository<T>`) that the DI container automatically closes for any requested type argument (such as `IRepository<User>`, `IRepository<Order>`, etc.). FS.AutoServiceDiscovery v10.0.2 provides the `OpenGenericRegistrationAttribute` for this purpose, separate from the standard `ServiceRegistrationAttribute`.

## What Open Generics Are

In .NET, an open generic type is one whose type parameters have not been specified: `Repository<T>` or `IRepository<T>`. A closed generic type has concrete type arguments: `Repository<User>`. When you register an open generic pair like `IRepository<> -> Repository<>` with the DI container, it can resolve any closed form without explicit registration for each type argument.

## Using OpenGenericRegistrationAttribute

Apply `OpenGenericRegistrationAttribute` to a generic type definition:

```csharp
public interface IRepository<T> where T : class
{
    Task<T?> GetByIdAsync(int id);
    Task<IReadOnlyList<T>> GetAllAsync();
    Task AddAsync(T entity);
    Task UpdateAsync(T entity);
    Task DeleteAsync(int id);
}

[OpenGenericRegistration(ServiceLifetime.Scoped)]
public class Repository<T> : IRepository<T> where T : class
{
    private readonly DbContext _context;

    public Repository(DbContext context)
    {
        _context = context;
    }

    public async Task<T?> GetByIdAsync(int id)
        => await _context.Set<T>().FindAsync(id);

    public async Task<IReadOnlyList<T>> GetAllAsync()
        => await _context.Set<T>().ToListAsync();

    public async Task AddAsync(T entity)
        => await _context.Set<T>().AddAsync(entity);

    public async Task UpdateAsync(T entity)
        => _context.Set<T>().Update(entity);

    public async Task DeleteAsync(int id)
    {
        var entity = await GetByIdAsync(id);
        if (entity != null) _context.Set<T>().Remove(entity);
    }
}
```

With this registration, any constructor requesting `IRepository<Product>`, `IRepository<Customer>`, or any other entity type will be resolved automatically.

## Convention-Based Service Type Detection

The attribute uses the same `I{ClassName}` convention as the standard registration. For a class named `Repository<T>`, the library looks for an interface named `IRepository<T>` among the implemented interfaces. The generic arity suffix (the backtick and number, e.g., `` `1 ``) is stripped during comparison.

The convention search order is:

1. Look for an open generic interface whose name (without arity suffix) matches `I{ClassName}`.
2. If no match, fall back to the first implemented open generic interface (only if there is exactly one).
3. If no interface is found, the type is skipped with a warning.

## Explicit ServiceType Specification

When the convention does not apply, specify the service type directly:

```csharp
[OpenGenericRegistration(ServiceLifetime.Singleton, ServiceType = typeof(ICache<>))]
public class RedisCache<T> : ICache<T>
{
    public Task<T?> GetAsync(string key) { /* ... */ }
    public Task SetAsync(string key, T value, TimeSpan expiry) { /* ... */ }
}
```

Note that the `ServiceType` must be an open generic type definition (using `<>` syntax in `typeof`).

## Combining with TryAdd

The `UseTryAdd` property works the same way as on standard registrations. Set it to `true` to only register if no existing open generic registration for the same service type exists:

```csharp
// Default in-memory implementation; skipped if a real one is already registered
[OpenGenericRegistration(ServiceLifetime.Scoped, UseTryAdd = true)]
public class InMemoryRepository<T> : IRepository<T> where T : class
{
    private readonly List<T> _store = new();
    // In-memory implementation
}
```

This is particularly useful in libraries that provide default generic implementations. See the [TryAdd Pattern](TryAddPattern.md) guide.

## Combining with Profiles

Use the `Profile` property for environment-specific open generic registrations:

```csharp
[OpenGenericRegistration(ServiceLifetime.Scoped, Profile = "Production")]
public class SqlRepository<T> : IRepository<T> where T : class
{
    // SQL Server backed implementation
}

[OpenGenericRegistration(ServiceLifetime.Scoped, Profile = "Development")]
public class InMemoryRepository<T> : IRepository<T> where T : class
{
    // Fast in-memory implementation for development
}
```

Only the implementation matching the active profile is registered.

## Registration Order

The `Order` property controls the sequence in which open generic registrations are processed relative to all other discovered services:

```csharp
[OpenGenericRegistration(ServiceLifetime.Scoped, Order = 10)]
public class AuditingRepository<T> : IRepository<T> where T : class
{
    // Registered after lower-order services
}
```

## Limitations

- `OpenGenericRegistrationAttribute` does not support `ServiceTypes` (multiple interface registration) or `ServiceKey` (keyed services). These features are only available on `ServiceRegistrationAttribute` for closed types.
- The implementation type must be an open generic type definition (e.g., `Repository<T>`), not a closed or partially closed generic.
- Constrained generics work correctly; the DI container will enforce the type constraints at resolution time.

## Best Practices

- Prefer convention-based detection when your naming follows the `I{Name}` pattern. It keeps the attribute declaration minimal.
- Use `UseTryAdd = true` in shared libraries so that applications can override generic registrations with specialized implementations.
- Consider registering specialized closed-type implementations alongside open generics for types that need custom behavior. The DI container prefers exact closed-type registrations over open generic ones.
- Combine open generic registration with the [Decorator Pattern](DecoratorPattern.md) for cross-cutting concerns like caching or logging on all repository operations.
