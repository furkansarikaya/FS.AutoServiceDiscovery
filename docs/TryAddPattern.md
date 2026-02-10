# TryAdd Pattern

The TryAdd pattern prevents duplicate service registrations in the dependency injection container. When enabled, a service is only registered if no existing registration for the same service type is already present. FS.AutoServiceDiscovery v10.0.2 supports TryAdd both at the per-service level through the attribute and globally through the options.

## Why TryAdd Matters

In modular applications, multiple modules or assemblies may attempt to register the same service interface. Without TryAdd, the last registration wins, which can silently override an intentional registration from another module. With TryAdd, the first registration is preserved, making the registration order predictable and preventing accidental overrides.

Internally, the library uses `ServiceCollectionDescriptorExtensions.TryAdd` from `Microsoft.Extensions.DependencyInjection.Extensions`, which checks the `ServiceType` on the descriptor before adding it.

## Per-Service TryAdd

Set `UseTryAdd = true` on the `ServiceRegistrationAttribute` to enable TryAdd for a specific service:

```csharp
[ServiceRegistration(ServiceLifetime.Scoped, UseTryAdd = true)]
public class DefaultUserService : IUserService
{
    // This registration is skipped if IUserService is already registered
}
```

This is useful when you provide a default implementation that should only be used if the consuming application has not registered its own.

## Global TryAdd via Options

To enable TryAdd for all auto-discovered services, set `UseTryAddByDefault = true` on `AutoServiceOptions`:

```csharp
builder.Services.AddAutoServices(options =>
{
    options.UseTryAddByDefault = true;
}, Assembly.GetExecutingAssembly());
```

When this option is enabled, every discovered service uses TryAdd unless the individual attribute explicitly opts out. The attribute-level `UseTryAdd` property takes precedence when explicitly set.

The same setting is available through the fluent API:

```csharp
builder.Services.ConfigureAutoServices()
    .FromAssemblies(Assembly.GetExecutingAssembly())
    .WithTryAdd()
    .Apply();
```

## Modular Application Scenario

Consider an application composed of a core module and optional feature modules:

```csharp
// Core module: provides default implementations
[ServiceRegistration(ServiceLifetime.Scoped, UseTryAdd = true)]
public class DefaultNotificationService : INotificationService
{
    public Task NotifyAsync(string message) => Task.CompletedTask;
}

// Premium module: provides a richer implementation
[ServiceRegistration(ServiceLifetime.Scoped)]
public class PremiumNotificationService : INotificationService
{
    public Task NotifyAsync(string message)
    {
        // Send push notification, email, and SMS
    }
}
```

If the premium module's assembly is scanned first, its `PremiumNotificationService` is registered normally. When the core module is scanned afterward, the `DefaultNotificationService` uses TryAdd and is skipped because `INotificationService` already has a registration.

Control scanning order through the `Order` property or by ordering the assembly list passed to `AddAutoServices`.

## Library and Framework Default Registrations

Library authors can use TryAdd to provide sensible defaults that host applications can override:

```csharp
// In a shared library package
[ServiceRegistration(ServiceLifetime.Singleton, UseTryAdd = true)]
public class InMemoryCache : ICacheProvider
{
    // Simple default cache; applications can register Redis, Memcached, etc.
}
```

The host application registers its own `ICacheProvider` normally (without TryAdd). Because it runs first, the library's default is skipped:

```csharp
// In the host application
[ServiceRegistration(ServiceLifetime.Singleton)]
public class RedisCacheProvider : ICacheProvider
{
    // Production-grade cache
}
```

## Interaction with Keyed Services

When TryAdd is combined with [keyed services](KeyedServices.md), the duplicate check considers both the service type and the service key. Two registrations for the same interface but different keys are treated as distinct:

```csharp
[ServiceRegistration(ServiceLifetime.Scoped, ServiceKey = "primary", UseTryAdd = true)]
public class PrimaryStore : IDataStore { }

[ServiceRegistration(ServiceLifetime.Scoped, ServiceKey = "backup", UseTryAdd = true)]
public class BackupStore : IDataStore { }
```

Both registrations succeed because they target different keys.

## Interaction with Multiple Interface Registration

When using the `ServiceTypes` property (see [Multiple Interface Registration](MultipleInterfaceRegistration.md)), TryAdd is evaluated independently for each service type in the array. If `IUserService` is already registered but `IProfileService` is not, only the `IProfileService` registration proceeds.

## Best Practices

- Use TryAdd on default or fallback implementations in shared libraries.
- Use `UseTryAddByDefault = true` globally when scanning assemblies that may overlap.
- Be deliberate about assembly scan order when relying on TryAdd; the first registration wins.
- Combine TryAdd with logging (`EnableLogging = true`) to see which registrations are skipped during startup.
