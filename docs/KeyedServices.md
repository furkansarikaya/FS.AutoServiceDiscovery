# Keyed Services

Keyed services, introduced in .NET 8, allow multiple implementations of the same interface to coexist in the dependency injection container, each identified by a unique key. FS.AutoServiceDiscovery v10.0.2 integrates keyed service registration directly into the `ServiceRegistrationAttribute` via the `ServiceKey` property, making it straightforward to register and resolve keyed implementations without manual wiring.

## How Keyed Services Work

In standard .NET DI, registering two implementations of the same interface causes the second to replace (or supplement) the first. Keyed services solve this by associating each registration with a key, so the container can distinguish between them at resolution time.

With FS.AutoServiceDiscovery, you set the `ServiceKey` property on the attribute. The library then uses `AddKeyedScoped`, `AddKeyedSingleton`, or `AddKeyedTransient` internally instead of the non-keyed equivalents.

## Strategy Pattern Example: Multiple Email Providers

A common use case is the strategy pattern, where you select an implementation based on context:

```csharp
public interface IEmailService
{
    Task SendAsync(string to, string subject, string body);
}

[ServiceRegistration(ServiceLifetime.Scoped, ServiceKey = "smtp")]
public class SmtpEmailService : IEmailService
{
    public Task SendAsync(string to, string subject, string body)
    {
        // Send via SMTP relay
    }
}

[ServiceRegistration(ServiceLifetime.Scoped, ServiceKey = "sendgrid")]
public class SendGridEmailService : IEmailService
{
    public Task SendAsync(string to, string subject, string body)
    {
        // Send via SendGrid API
    }
}
```

To resolve a specific implementation, use the `[FromKeyedServices]` attribute in the consuming class:

```csharp
public class OrderNotificationService
{
    private readonly IEmailService _emailService;

    public OrderNotificationService(
        [FromKeyedServices("sendgrid")] IEmailService emailService)
    {
        _emailService = emailService;
    }
}
```

## Multi-Tenant Scenario

Keyed services work well for multi-tenant applications where each tenant may require a different service configuration:

```csharp
[ServiceRegistration(ServiceLifetime.Scoped, ServiceKey = "tenant-a")]
public class TenantADatabaseContext : ITenantDatabase
{
    // Connects to Tenant A's database
}

[ServiceRegistration(ServiceLifetime.Scoped, ServiceKey = "tenant-b")]
public class TenantBDatabaseContext : ITenantDatabase
{
    // Connects to Tenant B's database
}
```

At runtime, middleware or a factory can resolve the correct service using `IServiceProvider.GetRequiredKeyedService<ITenantDatabase>(tenantKey)`.

## Resolving Keyed Services

There are two primary ways to resolve keyed services:

**Constructor injection with `[FromKeyedServices]`:**

```csharp
public class ReportGenerator(
    [FromKeyedServices("smtp")] IEmailService smtpEmail,
    [FromKeyedServices("sendgrid")] IEmailService sendGridEmail)
{
    // Both providers available
}
```

**Direct resolution from `IServiceProvider`:**

```csharp
app.MapGet("/send/{provider}", (string provider, IServiceProvider sp) =>
{
    var emailService = sp.GetRequiredKeyedService<IEmailService>(provider);
    return emailService.SendAsync("user@example.com", "Hello", "World");
});
```

## Combining with Profiles and Conditions

Keyed services can be combined with profile-based registration. For example, you might register a mock email provider only in the development profile:

```csharp
[ServiceRegistration(ServiceLifetime.Scoped, ServiceKey = "smtp", Profile = "Production")]
public class SmtpEmailService : IEmailService { /* ... */ }

[ServiceRegistration(ServiceLifetime.Scoped, ServiceKey = "smtp", Profile = "Development")]
public class MockSmtpEmailService : IEmailService { /* ... */ }
```

When the active profile is `"Development"`, the mock implementation is registered under the `"smtp"` key instead of the real SMTP provider.

## Interaction with TryAdd

When `UseTryAdd = true` is set alongside a `ServiceKey`, the TryAdd check applies to the combination of service type and key. If a keyed registration for the same type and key already exists, the duplicate is skipped. See the [TryAdd Pattern](TryAddPattern.md) guide for details.

## Best Practices

- Use descriptive, consistent key strings. Consider defining keys as constants in a shared class.
- Prefer `[FromKeyedServices]` constructor injection over direct `IServiceProvider` resolution for better testability.
- Keep the number of keyed implementations per interface manageable. If you find yourself with many keys, consider whether a factory pattern might be more appropriate.
- Combine with [profiles](ConditionalRegistration.md) to swap keyed implementations across environments without changing consumer code.
