# Scope Validation

Scope validation detects captive dependency issues -- cases where a longer-lived service holds a reference to a shorter-lived service, causing the shorter-lived instance to outlive its intended scope. FS.AutoServiceDiscovery v10.0.2 includes a built-in `ScopeValidator` and options to run validation automatically after service discovery.

## What Captive Dependencies Are

A captive dependency occurs when a Singleton service depends on a Scoped service through constructor injection. The Scoped service instance is created once and captured by the Singleton for the lifetime of the application, even though it was designed to live only for the duration of a single request scope. This can cause subtle bugs such as stale data, disposed database contexts being reused, or unexpected state sharing across requests.

For example:

```csharp
[ServiceRegistration(ServiceLifetime.Scoped)]
public class OrderRepository : IOrderRepository
{
    private readonly DbContext _context; // Scoped, tied to a request
    public OrderRepository(DbContext context) => _context = context;
}

[ServiceRegistration(ServiceLifetime.Singleton)]
public class OrderReportService : IOrderReportService
{
    private readonly IOrderRepository _repository; // Captive dependency
    public OrderReportService(IOrderRepository repository) => _repository = repository;
}
```

Here, `OrderReportService` (Singleton) captures `OrderRepository` (Scoped). The repository's `DbContext` will never be refreshed for new requests, leading to stale data and potential connection issues.

## Violations vs Warnings

The scope validator distinguishes between two categories:

**Violations** (errors): A Singleton depends on a Scoped service. This is almost always a bug and sets `IsValid = false` on the validation result.

**Warnings**: A Singleton depends on a Transient service. While not always wrong, this means the Transient instance lives as long as the Singleton, which may cause unexpected behavior or memory leaks if the Transient was intended to be short-lived. Warnings do not set `IsValid = false`.

## Enabling Scope Validation via Options

Enable validation by setting `EnableScopeValidation = true` on `AutoServiceOptions`. Validation runs automatically after all services are registered:

```csharp
builder.Services.AddAutoServices(options =>
{
    options.EnableScopeValidation = true;
    options.ThrowOnScopeViolation = false; // Log only, do not throw
    options.EnableLogging = true;
}, Assembly.GetExecutingAssembly());
```

When violations are found and `EnableLogging` is `true`, messages are printed to the console. Set `ThrowOnScopeViolation = true` to throw an `InvalidOperationException` listing all violations:

```csharp
builder.Services.AddAutoServices(options =>
{
    options.EnableScopeValidation = true;
    options.ThrowOnScopeViolation = true;
}, Assembly.GetExecutingAssembly());
```

## Direct Usage with ScopeValidator

You can also call `ScopeValidator.ValidateScopes()` directly at any point after building your service collection, independent of the auto-discovery pipeline:

```csharp
var services = new ServiceCollection();
// ... register services manually or via AddAutoServices ...

var result = ScopeValidator.ValidateScopes(services);

if (!result.IsValid)
{
    foreach (var violation in result.Violations)
    {
        Console.WriteLine($"ERROR: {violation.Message}");
        Console.WriteLine($"  {violation.ConsumerType.Name} ({violation.ConsumerLifetime}) " +
                          $"-> {violation.DependencyType.Name} ({violation.DependencyLifetime})");
    }
}

foreach (var warning in result.Warnings)
{
    Console.WriteLine($"WARN: {warning.Message}");
}
```

The `ScopeValidationResult` contains `IsValid` (false only when violations exist), `Violations` (a list of `ScopeViolation`), and `Warnings` (a list of `ScopeWarning`). Each entry includes `ConsumerType`, `DependencyType`, `ConsumerLifetime`, `DependencyLifetime`, and `Message`.

## Fluent API: WithScopeValidation()

The fluent configuration API provides `WithScopeValidation()`:

```csharp
builder.Services.ConfigureAutoServices()
    .FromAssemblies(Assembly.GetExecutingAssembly())
    .WithScopeValidation(throwOnViolation: true)
    .WithLogging()
    .Apply();
```

## Recommended Usage in CI/CD

Enable scope validation with `ThrowOnScopeViolation = true` in your CI/CD build or integration test suite to catch captive dependency issues before deployment:

```csharp
[Fact]
public void ServiceRegistration_ShouldHaveNoScopeViolations()
{
    var services = new ServiceCollection();
    services.AddDbContext<AppDbContext>(o => o.UseInMemoryDatabase("test"));

    services.AddAutoServices(options =>
    {
        options.EnableScopeValidation = true;
        options.ThrowOnScopeViolation = true;
        options.EnableLogging = false;
    }, typeof(Program).Assembly);

    // If there are scope violations, AddAutoServices throws
    // If we reach here, all scopes are valid
    var provider = services.BuildServiceProvider();
    Assert.NotNull(provider);
}
```

This test fails the build if any Singleton-to-Scoped dependency is introduced, providing fast feedback during development.

## How Validation Works

The validator builds a lookup of service type to lifetime from the service collection, then inspects the public constructors of each registered implementation type. For each constructor parameter that matches a registered service type, it compares lifetimes:

- Singleton consumer + Scoped dependency = Violation
- Singleton consumer + Transient dependency = Warning

The validator uses the constructor with the most parameters (the DI convention for primary constructor selection).

## Best Practices

- Enable scope validation during development and in CI/CD pipelines. Disable it in production to avoid the startup cost of constructor reflection.
- Treat violations as build-breaking errors in CI. Captive dependencies are almost always bugs.
- Review warnings periodically. A Singleton depending on a Transient may be intentional (e.g., a factory pattern), but it is worth verifying.
- Combine scope validation with [logging](GettingStarted.md) to get a complete picture of your service registrations and any lifetime issues.
