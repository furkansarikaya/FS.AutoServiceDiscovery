# Multiple Interface Registration

A single implementation class can serve multiple roles in an application. FS.AutoServiceDiscovery v10.0.2 supports registering one implementation under multiple service types through the `ServiceTypes` property on `ServiceRegistrationAttribute`. This avoids duplicating implementation logic and ensures that resolving any of the registered interfaces returns the same underlying type.

## Why Register Under Multiple Interfaces

Applications following Interface Segregation Principle (ISP) often split a broad interface into several focused ones. A single class may implement all of them. Registering it once under each interface ensures that consumers only depend on the interface they need while sharing the same implementation.

Without `ServiceTypes`, you would need multiple manual registrations or duplicate attribute declarations. The `ServiceTypes` property handles this in a single declaration.

## Using the ServiceTypes Property

Specify an array of interface types in the `ServiceTypes` property. When `ServiceTypes` is set, the `ServiceType` property is ignored:

```csharp
public interface IUserService
{
    Task<User> GetByIdAsync(int id);
    Task<User> CreateAsync(CreateUserRequest request);
}

public interface IProfileService
{
    Task<UserProfile> GetProfileAsync(int userId);
    Task UpdateProfileAsync(int userId, UpdateProfileRequest request);
}

[ServiceRegistration(ServiceLifetime.Scoped,
    ServiceTypes = new[] { typeof(IUserService), typeof(IProfileService) })]
public class UserService : IUserService, IProfileService
{
    public Task<User> GetByIdAsync(int id) { /* ... */ }
    public Task<User> CreateAsync(CreateUserRequest request) { /* ... */ }
    public Task<UserProfile> GetProfileAsync(int userId) { /* ... */ }
    public Task UpdateProfileAsync(int userId, UpdateProfileRequest request) { /* ... */ }
}
```

After discovery, the container has two registrations that both resolve to `UserService`:

```
IUserService    -> UserService (Scoped)
IProfileService -> UserService (Scoped)
```

## Service Lifetime Considerations

The specified `Lifetime` applies to every registration in the `ServiceTypes` array. All entries share the same lifetime. If you need different lifetimes for different interfaces, use separate classes or manual registrations instead.

Note that each registration is independent in the DI container. With `Scoped` lifetime, resolving `IUserService` and `IProfileService` in the same scope produces two separate instances of `UserService`, not a shared one. If you need a shared instance, register a concrete singleton and forward the interfaces to it manually.

## Interaction with TryAdd

When `UseTryAdd = true` is set alongside `ServiceTypes`, the TryAdd check runs independently for each service type. This means some types may be registered while others are skipped:

```csharp
[ServiceRegistration(ServiceLifetime.Scoped, UseTryAdd = true,
    ServiceTypes = new[] { typeof(IUserService), typeof(IProfileService) })]
public class UserService : IUserService, IProfileService { /* ... */ }
```

If `IUserService` already has a registration but `IProfileService` does not, only the `IProfileService` registration proceeds. See the [TryAdd Pattern](TryAddPattern.md) guide for more on this behavior.

## Interaction with Keyed Services

`ServiceTypes` and `ServiceKey` can be combined. When both are specified, the implementation is registered as a keyed service under each interface in the array:

```csharp
[ServiceRegistration(ServiceLifetime.Scoped,
    ServiceKey = "v2",
    ServiceTypes = new[] { typeof(IOrderService), typeof(IInvoiceService) })]
public class OrderServiceV2 : IOrderService, IInvoiceService { /* ... */ }
```

This produces keyed registrations for both `IOrderService` and `IInvoiceService` under the key `"v2"`. See the [Keyed Services](KeyedServices.md) guide for more on keyed resolution.

## Example: Read and Write Segregation

A repository that supports both read and write operations through separate interfaces:

```csharp
public interface IReadRepository<T>
{
    Task<T?> GetByIdAsync(int id);
    Task<IReadOnlyList<T>> GetAllAsync();
}

public interface IWriteRepository<T>
{
    Task AddAsync(T entity);
    Task UpdateAsync(T entity);
    Task DeleteAsync(int id);
}

[ServiceRegistration(ServiceLifetime.Scoped,
    ServiceTypes = new[] { typeof(IReadRepository<Product>), typeof(IWriteRepository<Product>) })]
public class ProductRepository : IReadRepository<Product>, IWriteRepository<Product>
{
    // Full implementation
}
```

Controllers or services that only read data depend on `IReadRepository<Product>`, while command handlers depend on `IWriteRepository<Product>`. Both resolve to the same `ProductRepository` type.

## Best Practices

- Only include interfaces that the class actually implements. The library does not verify at registration time that the implementation type is assignable to every type in the array; a runtime error will occur if it is not.
- Keep the `ServiceTypes` array focused. If a class implements many interfaces, only register the ones that external consumers need.
- Use `ServiceTypes` when the interfaces represent different views of the same capability. If the interfaces serve fundamentally different concerns, consider splitting the implementation into separate classes.
- Combine with [profiles](ConditionalRegistration.md) to swap multi-interface implementations across environments.
- When the same implementation needs different lifetimes for different interfaces, use separate `ServiceRegistrationAttribute` declarations on wrapper classes rather than `ServiceTypes`.
