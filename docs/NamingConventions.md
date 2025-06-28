# Naming Conventions Guide

Understanding naming conventions is like learning the grammar rules of a language - once you grasp how the system thinks about matching implementations to interfaces, you can write services that the discovery system understands naturally. This guide will take you from basic naming patterns to creating your own custom resolution rules.

## 🎯 What Are Naming Conventions?

Think of naming conventions as translation rules between two languages. In one language, you have concrete implementation classes like `UserService`, and in another language, you have abstract interface contracts like `IUserService`. Naming conventions are the rules that help the discovery system automatically translate between these two languages.

Just as human languages have grammar rules that tell us how words relate to each other, naming conventions tell the discovery system how class names relate to interface names. This automatic translation eliminates the need for manual mapping in most cases.

### The Default Convention: I{ClassName}

The most common and intuitive naming convention follows Microsoft's own guidelines. When you see a class named `UserService`, the system automatically looks for an interface named `IUserService`. This pattern works because it mirrors how most .NET developers naturally name their interfaces.

```csharp
// The discovery system sees this class...
[ServiceRegistration(ServiceLifetime.Scoped)]
public class UserService : IUserService, IDisposable
{
    // Implementation details
}

// ...and automatically knows to register it as IUserService
// because "UserService" + "I" prefix = "IUserService"
```

This automatic matching happens during the discovery process, eliminating the need for you to explicitly specify the service type in most cases.

## 🔄 The Convention Resolution Process

Understanding how the system resolves naming conventions helps you predict how your services will be registered and troubleshoot any unexpected behavior. The process follows a logical sequence, much like how you might manually figure out which interface a class should implement.

```mermaid
flowchart TD
    A[Start with Implementation Type] --> B{Explicit ServiceType specified?}
    B -->|Yes| C[Use Explicit Type]
    B -->|No| D[Apply Naming Conventions]
    
    D --> E[Try Standard I{ClassName} Pattern]
    E --> F{Found matching interface?}
    F -->|Yes| G[Use Matched Interface]
    F -->|No| H[Try Single Interface Pattern]
    
    H --> I{Only one non-system interface?}
    I -->|Yes| J[Use Single Interface]
    I -->|No| K[Use Concrete Type as Fallback]
    
    C --> L[Register Service]
    G --> L
    J --> L
    K --> L
    
    style C fill:#e8f5e9
    style G fill:#e8f5e9
    style J fill:#e8f5e9
    style K fill:#ffebee
```

Let me walk you through each step of this process with concrete examples, so you can see exactly how the system makes these decisions.

### Step 1: Check for Explicit Service Type

The system first checks if you've explicitly told it which interface to use. This takes highest priority because you're being completely clear about your intentions:

```csharp
// Explicit specification - no guessing needed
[ServiceRegistration(ServiceLifetime.Scoped, ServiceType = typeof(IUserService))]
public class UserBusinessLogic : IUserService, IValidator, IDisposable
{
    // Even though this class implements multiple interfaces,
    // we've explicitly said to register it as IUserService
}
```

When you provide an explicit service type, the convention system doesn't need to do any detective work. It simply uses what you've specified.

### Step 2: Apply the Standard I{ClassName} Convention

If no explicit service type is provided, the system applies the most common .NET naming pattern. It takes your class name and looks for an interface with the same name but prefixed with "I":

```csharp
// Class name: "UserService"
// Expected interface: "IUserService"
[ServiceRegistration(ServiceLifetime.Scoped)]
public class UserService : IUserService, ILogger
{
    // System finds IUserService and uses it
}

// Class name: "EmailSender" 
// Expected interface: "IEmailSender"
[ServiceRegistration(ServiceLifetime.Transient)]
public class EmailSender : IEmailSender, IDisposable
{
    // System finds IEmailSender and uses it
}
```

This convention works so well because it aligns with how most developers naturally name their interfaces and implementations.

### Step 3: Try the Single Interface Pattern

If the standard naming convention doesn't find a match, the system checks whether your class implements exactly one non-system interface. This handles cases where developers use different naming patterns but still follow the one-interface-per-implementation principle:

```csharp
// Class implements only one business interface
[ServiceRegistration(ServiceLifetime.Scoped)]
public class EmailNotificationHandler : INotificationHandler, IDisposable
{
    // IDisposable is a system interface, so it's ignored
    // INotificationHandler is the only business interface
    // System automatically uses INotificationHandler
}
```

The system is smart enough to ignore common system interfaces like `IDisposable`, `IAsyncDisposable`, and others that start with "System." This means it focuses only on your business interfaces.

### Step 4: Fallback to Concrete Type

If none of the above strategies work, the system falls back to registering your class as its concrete type. This ensures that even classes without interfaces can still be automatically registered:

```csharp
// No interfaces implemented - registers as concrete type
[ServiceRegistration(ServiceLifetime.Singleton)]
public class ApplicationSettings
{
    public string DatabaseConnectionString { get; set; } = "";
    public int MaxRetryAttempts { get; set; } = 3;
    public TimeSpan RequestTimeout { get; set; } = TimeSpan.FromSeconds(30);
}

// This can be injected directly as ApplicationSettings
public class UserService
{
    public UserService(ApplicationSettings settings)
    {
        // settings is injected as the concrete type
    }
}
```

## 🛠️ Working with Different Naming Patterns

Real-world codebases often have different naming patterns due to team preferences, legacy code, or integration with third-party libraries. Understanding how to work with various patterns helps you adapt the discovery system to your existing codebase.

### Pattern 1: Standard Microsoft Convention

This is the gold standard and works automatically without any additional configuration:

```csharp
public interface IUserService { }
[ServiceRegistration(ServiceLifetime.Scoped)]
public class UserService : IUserService { }

public interface IEmailService { }
[ServiceRegistration(ServiceLifetime.Transient)]
public class EmailService : IEmailService { }

public interface IDataRepository { }
[ServiceRegistration(ServiceLifetime.Scoped)]
public class DataRepository : IDataRepository { }
```

### Pattern 2: Implementation Suffix Pattern

Some teams prefer to add "Impl" or "Implementation" to their class names. While this doesn't follow the standard convention, you can handle it with explicit service types:

```csharp
public interface IUserService { }

// Explicit mapping needed due to non-standard naming
[ServiceRegistration(ServiceLifetime.Scoped, ServiceType = typeof(IUserService))]
public class UserServiceImpl : IUserService
{
    // Implementation details
}

public interface IEmailService { }

[ServiceRegistration(ServiceLifetime.Transient, ServiceType = typeof(IEmailService))]
public class EmailServiceImplementation : IEmailService
{
    // Implementation details
}
```

### Pattern 3: Domain-Driven Design Naming

In DDD applications, you might have more descriptive names that don't follow the simple I{ClassName} pattern:

```csharp
public interface IUserRepository { }

// The class name doesn't match the interface pattern
[ServiceRegistration(ServiceLifetime.Scoped, ServiceType = typeof(IUserRepository))]
public class SqlServerUserDataStore : IUserRepository
{
    // Implements user storage using SQL Server
}

public interface INotificationService { }

[ServiceRegistration(ServiceLifetime.Transient, ServiceType = typeof(INotificationService))]
public class SmtpEmailNotificationProvider : INotificationService
{
    // Implements notifications using SMTP email
}
```

## 🎨 Custom Naming Conventions

When explicit service type specification becomes repetitive, or when you need to support legacy naming patterns throughout your application, creating custom naming conventions provides an elegant solution. Think of custom conventions as teaching the discovery system to understand your team's specific naming language.

### Creating a Simple Custom Convention

Let's create a convention that handles the "Implementation" suffix pattern automatically. This example will help you understand the basic structure of naming conventions:

```csharp
using FS.AutoServiceDiscovery.Extensions.Architecture.Conventions;

public class ImplementationSuffixConvention : INamingConvention
{
    public string Name => "Implementation Suffix Convention";
    public int Priority => 20; // Higher priority number = lower precedence

    public bool CanApplyTo(Type implementationType)
    {
        // Quick check: does the class name end with "Implementation"?
        return implementationType.Name.EndsWith("Implementation");
    }

    public Type? ResolveServiceType(Type implementationType, IEnumerable<Type> availableInterfaces)
    {
        // Remove "Implementation" suffix and add "I" prefix
        var className = implementationType.Name;
        if (!className.EndsWith("Implementation"))
            return null;

        var baseName = className[..^"Implementation".Length]; // Remove "Implementation"
        var expectedInterfaceName = $"I{baseName}";

        // Look for matching interface
        return availableInterfaces.FirstOrDefault(i => i.Name == expectedInterfaceName);
    }
}
```

Register your custom convention in the dependency injection container:

```csharp
// Program.cs
builder.Services.AddNamingConvention<ImplementationSuffixConvention>();
builder.Services.AddAutoServices();
```

Now the system automatically handles this pattern:

```csharp
public interface IUserService { }

// No explicit ServiceType needed anymore!
[ServiceRegistration(ServiceLifetime.Scoped)]
public class UserServiceImplementation : IUserService
{
    // The custom convention automatically maps this to IUserService
}
```

### Advanced Custom Convention Example

Here's a more sophisticated convention that handles multiple naming patterns and provides detailed logging:

```csharp
public class FlexibleNamingConvention : INamingConvention
{
    public string Name => "Flexible Multi-Pattern Convention";
    public int Priority => 15; // Higher precedence than default patterns

    private readonly ILogger<FlexibleNamingConvention> _logger;

    public FlexibleNamingConvention(ILogger<FlexibleNamingConvention> logger)
    {
        _logger = logger;
    }

    public bool CanApplyTo(Type implementationType)
    {
        var name = implementationType.Name;
        
        // Can handle multiple patterns
        return name.EndsWith("Impl") || 
               name.EndsWith("Service") || 
               name.StartsWith("Concrete") ||
               name.Contains("Provider");
    }

    public Type? ResolveServiceType(Type implementationType, IEnumerable<Type> availableInterfaces)
    {
        var className = implementationType.Name;
        var interfaceList = availableInterfaces.ToList();
        
        _logger.LogDebug("Resolving service type for {ClassName} with {InterfaceCount} available interfaces", 
            className, interfaceList.Count);

        // Pattern 1: Remove "Impl" suffix and add "I" prefix
        if (className.EndsWith("Impl"))
        {
            var baseName = className[..^4]; // Remove "Impl"
            var expectedName = $"I{baseName}";
            var match = interfaceList.FirstOrDefault(i => i.Name == expectedName);
            
            if (match != null)
            {
                _logger.LogDebug("Matched {ClassName} to {InterfaceName} using Impl pattern", className, expectedName);
                return match;
            }
        }

        // Pattern 2: Replace "Concrete" prefix with "I"
        if (className.StartsWith("Concrete"))
        {
            var baseName = className[8..]; // Remove "Concrete"
            var expectedName = $"I{baseName}";
            var match = interfaceList.FirstOrDefault(i => i.Name == expectedName);
            
            if (match != null)
            {
                _logger.LogDebug("Matched {ClassName} to {InterfaceName} using Concrete pattern", className, expectedName);
                return match;
            }
        }

        // Pattern 3: For classes containing "Provider", look for interfaces ending with "Service"
        if (className.Contains("Provider"))
        {
            var match = interfaceList.FirstOrDefault(i => i.Name.EndsWith("Service"));
            
            if (match != null)
            {
                _logger.LogDebug("Matched {ClassName} to {InterfaceName} using Provider pattern", className, match.Name);
                return match;
            }
        }

        _logger.LogDebug("No naming convention match found for {ClassName}", className);
        return null;
    }
}
```

This advanced convention can handle multiple naming patterns in a single codebase, providing flexibility while maintaining clear rules.

### Convention Priority and Ordering

When multiple conventions are registered, they're evaluated in priority order. Lower priority numbers are evaluated first, giving them higher precedence:

```csharp
public class HighPriorityConvention : INamingConvention
{
    public int Priority => 5; // Evaluated first
    // ... implementation
}

public class MediumPriorityConvention : INamingConvention
{
    public int Priority => 15; // Evaluated second
    // ... implementation
}

public class LowPriorityConvention : INamingConvention
{
    public int Priority => 25; // Evaluated last
    // ... implementation
}
```

The evaluation stops as soon as one convention successfully resolves a service type, so higher-priority conventions can override lower-priority ones.

## 🔍 Debugging Naming Convention Issues

When services aren't being registered as expected, understanding how to debug naming convention issues saves significant troubleshooting time. Enable detailed logging to see exactly how the system is making its decisions:

```csharp
builder.Services.AddAutoServices(options =>
{
    options.EnableLogging = true; // Shows detailed resolution process
});
```

### Common Issues and Solutions

**Issue 1: Service not being registered at all**

```
Expected: IUserService registered
Actual: Nothing registered
```

**Debug approach:**
- Verify the class has `[ServiceRegistration]` attribute
- Check that the class is public and not abstract
- Ensure the assembly is being scanned

**Issue 2: Service registered as wrong interface**

```
Expected: IUserService
Actual: IDisposable
```

**Debug approach:**
- Check if multiple interfaces are implemented
- Look at the order of interface implementation
- Consider using explicit ServiceType specification

**Issue 3: Service registered as concrete type instead of interface**

```
Expected: IUserService
Actual: UserService (concrete type)
```

**Debug approach:**
- Verify interface naming follows I{ClassName} pattern
- Check if the interface is in the same assembly
- Consider creating a custom naming convention

### Enabling Convention Statistics

You can get detailed statistics about how naming conventions are performing:

```csharp
// In a controller or service
public class DiagnosticsController : ControllerBase
{
    private readonly INamingConventionResolver _resolver;

    public DiagnosticsController(INamingConventionResolver resolver)
    {
        _resolver = resolver;
    }

    [HttpGet("naming-stats")]
    public IActionResult GetNamingStatistics()
    {
        var stats = _resolver.GetStatistics();
        return Ok(new
        {
            TotalResolutions = stats.TotalResolutionAttempts,
            SuccessfulResolutions = stats.SuccessfulResolutions,
            SuccessRate = stats.SuccessRate,
            MostSuccessfulConvention = stats.MostSuccessfulConvention,
            ConventionDetails = stats.ConventionMetrics.Select(kvp => new
            {
                Convention = kvp.Key,
                Consultations = kvp.Value.ConsultationCount,
                Successes = kvp.Value.SuccessfulResolutions,
                SuccessRate = kvp.Value.SuccessRate,
                AverageExecutionTime = kvp.Value.AverageExecutionTimeMs
            })
        });
    }
}
```

This endpoint provides valuable insights into how well your naming conventions are working in practice.

## 📊 Performance Considerations

Naming conventions are evaluated frequently during application startup, so their performance characteristics matter. Here are some guidelines for writing efficient conventions:

### Efficient Convention Design

```csharp
public class EfficientConvention : INamingConvention
{
    public bool CanApplyTo(Type implementationType)
    {
        // ✅ Fast string operations first
        var name = implementationType.Name;
        if (name.Length < 5) return false; // Quick length check
        
        // ✅ Simple string operations
        return name.EndsWith("Impl") || name.EndsWith("Service");
    }

    public Type? ResolveServiceType(Type implementationType, IEnumerable<Type> availableInterfaces)
    {
        // ✅ Convert to list once to avoid multiple enumeration
        var interfaceList = availableInterfaces.ToList();
        
        // ✅ Use simple string operations
        var className = implementationType.Name;
        var expectedName = $"I{className[..^4]}"; // Remove "Impl"
        
        // ✅ Use LINQ efficiently
        return interfaceList.FirstOrDefault(i => i.Name == expectedName);
    }
}
```

### Performance Anti-Patterns

```csharp
public class InefficientConvention : INamingConvention
{
    public bool CanApplyTo(Type implementationType)
    {
        // ❌ Expensive reflection operations in CanApplyTo
        var attributes = implementationType.GetCustomAttributes();
        var methods = implementationType.GetMethods();
        return methods.Length > 5; // This is expensive and unnecessary
    }

    public Type? ResolveServiceType(Type implementationType, IEnumerable<Type> availableInterfaces)
    {
        // ❌ Multiple enumeration of interfaces
        foreach (var pattern in new[] { "Impl", "Implementation", "Service" })
        {
            // Each iteration goes through all interfaces again
            var match = availableInterfaces.FirstOrDefault(i => /* complex logic */);
        }
        
        // ❌ Complex regex patterns
        var regex = new Regex(@"^I(.+)(Service|Repository|Manager)$");
        return availableInterfaces.FirstOrDefault(i => regex.IsMatch(i.Name));
    }
}
```

## 🎯 Best Practices

Based on extensive experience with naming conventions, here are the key best practices that will save you time and make your code more maintainable:

### 1. Start with Standard Conventions

Begin with the Microsoft standard I{ClassName} pattern. This works well for most scenarios and is familiar to all .NET developers:

```csharp
// ✅ Standard pattern - works automatically
public interface IUserService { }
public class UserService : IUserService { }

// ✅ Clear and predictable
public interface IEmailNotificationService { }
public class EmailNotificationService : IEmailNotificationService { }
```

### 2. Use Explicit Service Types for Complex Cases

When your naming doesn't follow standard patterns, be explicit rather than creating complex conventions:

```csharp
// ✅ Clear and explicit
[ServiceRegistration(ServiceLifetime.Scoped, ServiceType = typeof(IUserService))]
public class AdvancedUserBusinessLogicHandler : IUserService, IValidator, IDisposable
{
    // Complex class with multiple interfaces - explicit mapping is clearer
}
```

### 3. Keep Custom Conventions Simple

If you need custom conventions, make them simple and focused:

```csharp
// ✅ Simple, focused convention
public class MockSuffixConvention : INamingConvention
{
    public string Name => "Mock Suffix (Test Only)";
    public int Priority => 5; // High priority for test environments

    public bool CanApplyTo(Type implementationType)
    {
        return implementationType.Name.EndsWith("Mock");
    }

    public Type? ResolveServiceType(Type implementationType, IEnumerable<Type> availableInterfaces)
    {
        // Remove "Mock" and add "I" prefix
        var baseName = implementationType.Name[..^4];
        var expectedName = $"I{baseName}";
        return availableInterfaces.FirstOrDefault(i => i.Name == expectedName);
    }
}
```

### 4. Document Your Naming Patterns

Create a team document that clearly explains your naming conventions:

```csharp
/*
Team Naming Conventions:
1. Standard services: IUserService -> UserService
2. Repositories: IUserRepository -> SqlUserRepository
3. Test doubles: IUserService -> UserServiceMock
4. External adapters: IPaymentService -> StripePaymentAdapter

Custom conventions handle patterns 2, 3, and 4 automatically.
*/
```

## 🎓 Understanding the Theory

To truly master naming conventions, it helps to understand the underlying design principles. The naming convention system implements the Strategy pattern, where different strategies (conventions) can be applied to solve the same problem (interface resolution).

This design provides several key benefits:

**Extensibility**: You can add new naming patterns without modifying existing code
**Flexibility**: Different parts of your application can use different naming styles
**Performance**: The system can optimize based on which conventions are most commonly successful
**Maintainability**: Each convention is independent and can be tested in isolation

## 🔗 Next Steps

Now that you understand how naming conventions work, you're ready to explore more advanced topics:

1. **[Conditional Registration](ConditionalRegistration.md)** - Learn how to register services based on environment and configuration
2. **[Custom Naming Conventions](CustomNamingConventions.md)** - Deep dive into creating sophisticated naming rules
3. **[Plugin Architecture](PluginArchitecture.md)** - Extend the discovery system with custom plugins

Understanding naming conventions gives you the foundation to predict and control how your services are discovered and registered. This knowledge becomes particularly valuable as your application grows and you need more sophisticated service organization strategies.