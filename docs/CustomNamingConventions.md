# Custom Naming Conventions Guide

Custom naming conventions allow you to extend the service discovery system to handle non-standard naming patterns in your codebase. This comprehensive guide will show you how to create, implement, and optimize custom naming conventions for your specific needs.

## 🎯 Understanding Custom Naming Conventions

Think of naming conventions as specialized translators that understand different "dialects" of service naming. While the standard convention handles the common "IUserService → UserService" pattern, custom conventions can handle legacy patterns, domain-specific naming, or unique organizational standards.

Custom naming conventions solve real-world problems like:
- Legacy codebases with established naming patterns
- Domain-driven design with descriptive service names
- Integration with third-party libraries using different patterns
- Migration scenarios where old and new patterns coexist

## 🏗️ The Anatomy of a Naming Convention

Every naming convention implements the `INamingConvention` interface, which provides a structured approach to service type resolution:

```csharp
public interface INamingConvention
{
    string Name { get; }                    // Human-readable identifier
    int Priority { get; }                   // Execution order (lower = higher priority)
    bool CanApplyTo(Type implementationType); // Quick eligibility check
    Type? ResolveServiceType(Type implementationType, IEnumerable<Type> availableInterfaces);
}
```

Let's explore each component and understand how they work together to create effective naming conventions.

## 🔧 Creating Your First Custom Convention

### Example 1: Implementation Suffix Convention

Many teams use "Impl" or "Implementation" suffixes. Here's how to handle this pattern:

```csharp
public class ImplementationSuffixConvention : INamingConvention
{
    public string Name => "Implementation Suffix Convention";
    public int Priority => 15; // Higher than standard but lower than highly specific conventions

    public bool CanApplyTo(Type implementationType)
    {
        // Quick check: does the class name end with "Impl" or "Implementation"?
        var name = implementationType.Name;
        return name.EndsWith("Impl") || name.EndsWith("Implementation");
    }

    public Type? ResolveServiceType(Type implementationType, IEnumerable<Type> availableInterfaces)
    {
        var className = implementationType.Name;
        var interfaceList = availableInterfaces.ToList();

        // Handle "Impl" suffix
        if (className.EndsWith("Impl"))
        {
            var baseName = className[..^4]; // Remove "Impl"
            var expectedInterfaceName = $"I{baseName}";
            var match = interfaceList.FirstOrDefault(i => i.Name == expectedInterfaceName);
            if (match != null) return match;
        }

        // Handle "Implementation" suffix
        if (className.EndsWith("Implementation"))
        {
            var baseName = className[..^14]; // Remove "Implementation"
            var expectedInterfaceName = $"I{baseName}";
            var match = interfaceList.FirstOrDefault(i => i.Name == expectedInterfaceName);
            if (match != null) return match;
        }

        return null; // This convention doesn't apply
    }
}
```

**Usage Example:**
```csharp
// These classes will now be automatically resolved
public interface IUserService { }
public class UserServiceImpl : IUserService { } // Resolves to IUserService

public interface IOrderProcessor { }
public class OrderProcessorImplementation : IOrderProcessor { } // Resolves to IOrderProcessor
```

### Example 2: Domain-Driven Design Convention

For DDD applications where services have descriptive names:

```csharp
public class DomainServiceConvention : INamingConvention
{
    public string Name => "Domain-Driven Design Service Convention";
    public int Priority => 12; // High priority for DDD patterns

    public bool CanApplyTo(Type implementationType)
    {
        // Look for domain service patterns
        var name = implementationType.Name;
        return name.Contains("DomainService") || 
               name.Contains("ApplicationService") || 
               name.EndsWith("Handler") ||
               name.EndsWith("Coordinator");
    }

    public Type? ResolveServiceType(Type implementationType, IEnumerable<Type> availableInterfaces)
    {
        var interfaces = availableInterfaces.ToList();
        var className = implementationType.Name;

        // Handle domain service patterns
        if (className.Contains("DomainService"))
        {
            // Look for matching domain interface
            var expectedName = className.Replace("DomainService", "Service");
            expectedName = $"I{expectedName}";
            var match = interfaces.FirstOrDefault(i => i.Name == expectedName);
            if (match != null) return match;
        }

        // Handle application service patterns
        if (className.Contains("ApplicationService"))
        {
            var baseName = className.Replace("ApplicationService", "");
            var expectedName = $"I{baseName}Service";
            var match = interfaces.FirstOrDefault(i => i.Name == expectedName);
            if (match != null) return match;
        }

        // Handle handler patterns (CQRS/MediatR style)
        if (className.EndsWith("Handler"))
        {
            // Look for handler interfaces
            var match = interfaces.FirstOrDefault(i => 
                i.Name.EndsWith("Handler") || 
                i.IsGenericType && i.Name.Contains("Handler"));
            if (match != null) return match;
        }

        return null;
    }
}
```

**Usage Example:**
```csharp
// Domain service patterns
public interface IOrderService { }
public class OrderDomainService : IOrderService { } // Auto-resolved

// Application service patterns  
public interface IUserService { }
public class UserApplicationService : IUserService { } // Auto-resolved

// Handler patterns
public interface IOrderCreatedHandler { }
public class OrderCreatedHandler : IOrderCreatedHandler { } // Auto-resolved
```

## 🎨 Advanced Convention Patterns

### Pattern 1: Namespace-Aware Convention

This convention considers namespace context when resolving services:

```csharp
public class NamespaceAwareConvention : INamingConvention
{
    public string Name => "Namespace-Aware Convention";
    public int Priority => 20;

    public bool CanApplyTo(Type implementationType)
    {
        var namespaceName = implementationType.Namespace ?? "";
        return namespaceName.Contains(".Services.") || 
               namespaceName.Contains(".Repositories.") ||
               namespaceName.EndsWith(".Services") ||
               namespaceName.EndsWith(".Repositories");
    }

    public Type? ResolveServiceType(Type implementationType, IEnumerable<Type> availableInterfaces)
    {
        var namespaceName = implementationType.Namespace ?? "";
        var interfaces = availableInterfaces.ToList();

        // Services namespace pattern
        if (namespaceName.Contains(".Services"))
        {
            return ResolveForServices(implementationType, interfaces);
        }

        // Repositories namespace pattern
        if (namespaceName.Contains(".Repositories"))
        {
            return ResolveForRepositories(implementationType, interfaces);
        }

        return null;
    }

    private Type? ResolveForServices(Type implementationType, List<Type> interfaces)
    {
        var className = implementationType.Name;
        
        // Standard pattern: UserService -> IUserService
        var expectedName = $"I{className}";
        var match = interfaces.FirstOrDefault(i => i.Name == expectedName);
        if (match != null) return match;

        // Alternative pattern: UserBusinessService -> IUserService
        if (className.Contains("Business"))
        {
            var simpleName = className.Replace("Business", "");
            expectedName = $"I{simpleName}";
            match = interfaces.FirstOrDefault(i => i.Name == expectedName);
            if (match != null) return match;
        }

        return null;
    }

    private Type? ResolveForRepositories(Type implementationType, List<Type> interfaces)
    {
        var className = implementationType.Name;
        
        // Repository patterns
        if (className.EndsWith("Repository"))
        {
            var expectedName = $"I{className}";
            var match = interfaces.FirstOrDefault(i => i.Name == expectedName);
            if (match != null) return match;
        }

        // Data access patterns
        if (className.Contains("DataAccess"))
        {
            var baseName = className.Replace("DataAccess", "Repository");
            var expectedName = $"I{baseName}";
            var match = interfaces.FirstOrDefault(i => i.Name == expectedName);
            if (match != null) return match;
        }

        return null;
    }
}
```

### Pattern 2: Configuration-Driven Convention

A convention that adapts its behavior based on configuration:

```csharp
public class ConfigurableConvention : INamingConvention
{
    private readonly IConfiguration _configuration;
    private readonly ConventionSettings _settings;

    public string Name => "Configurable Convention";
    public int Priority => 25;

    public ConfigurableConvention(IConfiguration configuration)
    {
        _configuration = configuration;
        _settings = configuration.GetSection("NamingConventions:Configurable")
                                .Get<ConventionSettings>() ?? new ConventionSettings();
    }

    public bool CanApplyTo(Type implementationType)
    {
        if (!_settings.Enabled) return false;

        var className = implementationType.Name;
        return _settings.Patterns.Any(pattern => 
            className.EndsWith(pattern.Suffix) || 
            className.Contains(pattern.Contains));
    }

    public Type? ResolveServiceType(Type implementationType, IEnumerable<Type> availableInterfaces)
    {
        var className = implementationType.Name;
        var interfaces = availableInterfaces.ToList();

        foreach (var pattern in _settings.Patterns)
        {
            var match = TryResolveWithPattern(className, interfaces, pattern);
            if (match != null) return match;
        }

        return null;
    }

    private Type? TryResolveWithPattern(string className, List<Type> interfaces, NamingPattern pattern)
    {
        if (className.EndsWith(pattern.Suffix))
        {
            var baseName = className[..^pattern.Suffix.Length];
            var expectedName = pattern.InterfacePrefix + baseName + pattern.InterfaceSuffix;
            return interfaces.FirstOrDefault(i => i.Name == expectedName);
        }

        if (className.Contains(pattern.Contains))
        {
            var baseName = className.Replace(pattern.Contains, pattern.Replacement);
            var expectedName = pattern.InterfacePrefix + baseName + pattern.InterfaceSuffix;
            return interfaces.FirstOrDefault(i => i.Name == expectedName);
        }

        return null;
    }
}

public class ConventionSettings
{
    public bool Enabled { get; set; } = true;
    public List<NamingPattern> Patterns { get; set; } = new();
}

public class NamingPattern
{
    public string Suffix { get; set; } = "";
    public string Contains { get; set; } = "";
    public string Replacement { get; set; } = "";
    public string InterfacePrefix { get; set; } = "I";
    public string InterfaceSuffix { get; set; } = "";
}
```

**Configuration Example:**
```json
{
  "NamingConventions": {
    "Configurable": {
      "Enabled": true,
      "Patterns": [
        {
          "Suffix": "Component",
          "InterfacePrefix": "I",
          "InterfaceSuffix": ""
        },
        {
          "Contains": "Manager",
          "Replacement": "Service",
          "InterfacePrefix": "I",
          "InterfaceSuffix": ""
        }
      ]
    }
  }
}
```

## 📊 Performance Optimization for Conventions

### Efficient CanApplyTo Implementation

The `CanApplyTo` method is called frequently, so it must be highly optimized:

```csharp
public class OptimizedConvention : INamingConvention
{
    // Pre-compile patterns for better performance
    private static readonly string[] KnownSuffixes = { "Impl", "Implementation", "Service" };
    private static readonly HashSet<string> KnownPatterns = new(KnownSuffixes);

    public bool CanApplyTo(Type implementationType)
    {
        var name = implementationType.Name;
        
        // Fast length check first
        if (name.Length < 4) return false;
        
        // Use HashSet for O(1) lookups when possible
        return KnownPatterns.Any(suffix => name.EndsWith(suffix));
    }

    // Alternative: Compiled regex for complex patterns
    private static readonly Regex PatternRegex = new Regex(
        @"^(.+)(Impl|Implementation|Service)$", 
        RegexOptions.Compiled);

    public bool CanApplyToWithRegex(Type implementationType)
    {
        return PatternRegex.IsMatch(implementationType.Name);
    }
}
```

### Caching Strategy

Implement caching for expensive operations:

```csharp
public class CachedConvention : INamingConvention
{
    private readonly ConcurrentDictionary<string, Type?> _resolutionCache = new();
    private readonly ConcurrentDictionary<string, bool> _applicabilityCache = new();

    public bool CanApplyTo(Type implementationType)
    {
        var key = implementationType.FullName ?? implementationType.Name;
        return _applicabilityCache.GetOrAdd(key, _ => DetermineApplicability(implementationType));
    }

    public Type? ResolveServiceType(Type implementationType, IEnumerable<Type> availableInterfaces)
    {
        var key = GenerateCacheKey(implementationType, availableInterfaces);
        return _resolutionCache.GetOrAdd(key, _ => 
            PerformResolution(implementationType, availableInterfaces));
    }

    private bool DetermineApplicability(Type implementationType)
    {
        // Expensive applicability logic here
        return true;
    }

    private Type? PerformResolution(Type implementationType, IEnumerable<Type> availableInterfaces)
    {
        // Expensive resolution logic here
        return null;
    }

    private string GenerateCacheKey(Type implementationType, IEnumerable<Type> availableInterfaces)
    {
        var interfaceNames = string.Join(",", availableInterfaces.Select(i => i.Name).OrderBy(n => n));
        return $"{implementationType.FullName}|{interfaceNames}";
    }
}
```

## 🧪 Testing Custom Conventions

### Unit Testing Framework

```csharp
[TestClass]
public class ImplementationSuffixConventionTests
{
    private readonly ImplementationSuffixConvention _convention = new();

    [TestMethod]
    public void CanApplyTo_WithImplSuffix_ReturnsTrue()
    {
        // Arrange
        var type = typeof(UserServiceImpl);

        // Act
        var result = _convention.CanApplyTo(type);

        // Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public void ResolveServiceType_WithImplSuffix_ReturnsCorrectInterface()
    {
        // Arrange
        var implementationType = typeof(UserServiceImpl);
        var availableInterfaces = new[] { typeof(IUserService), typeof(IDisposable) };

        // Act
        var result = _convention.ResolveServiceType(implementationType, availableInterfaces);

        // Assert
        Assert.AreEqual(typeof(IUserService), result);
    }

    [TestMethod]
    public void ResolveServiceType_WithNoMatchingInterface_ReturnsNull()
    {
        // Arrange
        var implementationType = typeof(UserServiceImpl);
        var availableInterfaces = new[] { typeof(IDisposable) }; // No matching interface

        // Act
        var result = _convention.ResolveServiceType(implementationType, availableInterfaces);

        // Assert
        Assert.IsNull(result);
    }
}

// Test types
public interface IUserService { }
public class UserServiceImpl : IUserService, IDisposable
{
    public void Dispose() { }
}
```

### Integration Testing

```csharp
[TestClass]
public class ConventionIntegrationTests
{
    [TestMethod]
    public void ServiceDiscovery_WithCustomConvention_RegistersServicesCorrectly()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddNamingConvention<ImplementationSuffixConvention>();
        services.AddAutoServices();

        // Act
        var serviceProvider = services.BuildServiceProvider();

        // Assert
        var userService = serviceProvider.GetService<IUserService>();
        Assert.IsNotNull(userService);
        Assert.IsInstanceOfType(userService, typeof(UserServiceImpl));
    }
}
```

## 🔧 Registration and Configuration

### Basic Registration

```csharp
// Simple registration
builder.Services.AddNamingConvention<ImplementationSuffixConvention>();
builder.Services.AddAutoServices();
```

### Fluent Configuration

```csharp
// Using fluent configuration
builder.Services.ConfigureAutoServices()
    .FromCurrentDomain()
    .WithNamingConvention<ImplementationSuffixConvention>()
    .WithNamingConvention<DomainServiceConvention>()
    .WithProfile("Production")
    .Apply();
```

### Instance-Based Registration

```csharp
// Register pre-configured instance
var customConvention = new ConfigurableConvention(configuration);
builder.Services.ConfigureAutoServices()
    .FromCurrentDomain()
    .WithNamingConvention(customConvention)
    .Apply();
```

### Priority Management

```csharp
public class HighPriorityConvention : INamingConvention
{
    public int Priority => 5; // High priority - evaluated first
    // ...
}

public class LowPriorityConvention : INamingConvention
{
    public int Priority => 50; // Low priority - evaluated last
    // ...
}
```

## 🎯 Best Practices

### 1. Keep Conventions Focused

```csharp
// ✅ Good: Focused on one specific pattern
public class RepositoryConvention : INamingConvention
{
    // Handles only repository naming patterns
}

// ❌ Bad: Tries to handle multiple unrelated patterns
public class EverythingConvention : INamingConvention
{
    // Handles repositories, services, controllers, etc.
}
```

### 2. Optimize for Common Cases

```csharp
public bool CanApplyTo(Type implementationType)
{
    // ✅ Quick checks first
    var name = implementationType.Name;
    if (name.Length < 5) return false; // Too short to match patterns
    
    // ✅ Most common patterns first
    return name.EndsWith("Impl") || name.EndsWith("Service");
}
```

### 3. Provide Clear Error Messages

```csharp
public Type? ResolveServiceType(Type implementationType, IEnumerable<Type> availableInterfaces)
{
    try
    {
        return PerformResolution(implementationType, availableInterfaces);
    }
    catch (Exception ex)
    {
        throw new InvalidOperationException(
            $"Convention '{Name}' failed to resolve service type for {implementationType.Name}. " +
            $"Available interfaces: {string.Join(", ", availableInterfaces.Select(i => i.Name))}",
            ex);
    }
}
```

### 4. Document Convention Logic

```csharp
/// <summary>
/// Resolves service types for the legacy naming pattern used in version 1.x of our system.
/// 
/// Patterns handled:
/// - UserManagerImpl -> IUserManager
/// - OrderProcessorImpl -> IOrderProcessor  
/// - *ServiceImpl -> I*Service (where * is any prefix)
/// 
/// Priority: 15 (executes after standard conventions but before fallbacks)
/// </summary>
public class LegacyNamingConvention : INamingConvention
{
    // Implementation...
}
```

## 🔍 Debugging Conventions

### Enable Detailed Logging

```csharp
public class DebuggableConvention : INamingConvention
{
    private readonly ILogger<DebuggableConvention> _logger;

    public DebuggableConvention(ILogger<DebuggableConvention> logger)
    {
        _logger = logger;
    }

    public Type? ResolveServiceType(Type implementationType, IEnumerable<Type> availableInterfaces)
    {
        _logger.LogDebug("Attempting to resolve service type for {TypeName}", implementationType.Name);
        
        var interfaces = availableInterfaces.ToList();
        _logger.LogDebug("Available interfaces: {Interfaces}", 
            string.Join(", ", interfaces.Select(i => i.Name)));

        var result = PerformResolution(implementationType, interfaces);
        
        if (result != null)
        {
            _logger.LogDebug("Successfully resolved {TypeName} to {InterfaceName}", 
                implementationType.Name, result.Name);
        }
        else
        {
            _logger.LogDebug("Failed to resolve service type for {TypeName}", implementationType.Name);
        }

        return result;
    }
}
```

### Convention Statistics

```csharp
public class StatisticsTrackingConvention : INamingConvention
{
    private long _totalCalls = 0;
    private long _successfulResolutions = 0;
    private readonly ConcurrentDictionary<string, int> _resolutionPatterns = new();

    public Type? ResolveServiceType(Type implementationType, IEnumerable<Type> availableInterfaces)
    {
        Interlocked.Increment(ref _totalCalls);
        
        var result = PerformResolution(implementationType, availableInterfaces);
        
        if (result != null)
        {
            Interlocked.Increment(ref _successfulResolutions);
            var pattern = $"{implementationType.Name} -> {result.Name}";
            _resolutionPatterns.AddOrUpdate(pattern, 1, (key, count) => count + 1);
        }

        return result;
    }

    public ConventionStatistics GetStatistics()
    {
        return new ConventionStatistics
        {
            TotalCalls = _totalCalls,
            SuccessfulResolutions = _successfulResolutions,
            SuccessRate = _totalCalls > 0 ? (double)_successfulResolutions / _totalCalls * 100 : 0,
            CommonPatterns = _resolutionPatterns.OrderByDescending(kvp => kvp.Value)
                                               .Take(10)
                                               .ToDictionary(kvp => kvp.Key, kvp => kvp.Value)
        };
    }
}

public class ConventionStatistics
{
    public long TotalCalls { get; set; }
    public long SuccessfulResolutions { get; set; }
    public double SuccessRate { get; set; }
    public Dictionary<string, int> CommonPatterns { get; set; } = new();
}
```

## 🌟 Real-World Examples

### Example 1: Microservice Convention

For microservices with service-specific naming:

```csharp
public class MicroserviceConvention : INamingConvention
{
    public string Name => "Microservice Convention";
    public int Priority => 8; // High priority for microservice environments

    public bool CanApplyTo(Type implementationType)
    {
        var namespaceName = implementationType.Namespace ?? "";
        return namespaceName.Contains(".Services.") && 
               (implementationType.Name.Contains("Client") || 
                implementationType.Name.Contains("Gateway") ||
                implementationType.Name.Contains("Proxy"));
    }

    public Type? ResolveServiceType(Type implementationType, IEnumerable<Type> availableInterfaces)
    {
        var className = implementationType.Name;
        var interfaces = availableInterfaces.ToList();

        // Handle API clients: UserApiClient -> IUserApiClient
        if (className.Contains("Client"))
        {
            var expectedName = $"I{className}";
            var match = interfaces.FirstOrDefault(i => i.Name == expectedName);
            if (match != null) return match;
        }

        // Handle service gateways: PaymentGateway -> IPaymentGateway  
        if (className.Contains("Gateway"))
        {
            var expectedName = $"I{className}";
            var match = interfaces.FirstOrDefault(i => i.Name == expectedName);
            if (match != null) return match;
        }

        // Handle service proxies: OrderServiceProxy -> IOrderService
        if (className.Contains("Proxy"))
        {
            var baseName = className.Replace("Proxy", "");
            var expectedName = $"I{baseName}";
            var match = interfaces.FirstOrDefault(i => i.Name == expectedName);
            if (match != null) return match;
        }

        return null;
    }
}
```

### Example 2: Plugin Convention

For plugin-based architectures:

```csharp
public class PluginConvention : INamingConvention
{
    public string Name => "Plugin Convention";
    public int Priority => 12;

    public bool CanApplyTo(Type implementationType)
    {
        var attributes = implementationType.GetCustomAttributes(typeof(PluginAttribute), false);
        return attributes.Length > 0 || implementationType.Name.EndsWith("Plugin");
    }

    public Type? ResolveServiceType(Type implementationType, IEnumerable<Type> availableInterfaces)
    {
        var interfaces = availableInterfaces.ToList();
        
        // Look for plugin-specific interfaces first
        var pluginInterface = interfaces.FirstOrDefault(i => i.Name.Contains("Plugin"));
        if (pluginInterface != null) return pluginInterface;

        // Look for feature-specific interfaces
        var className = implementationType.Name.Replace("Plugin", "");
        var expectedName = $"I{className}";
        var match = interfaces.FirstOrDefault(i => i.Name == expectedName);
        if (match != null) return match;

        // Fallback to any non-system interface
        return interfaces.FirstOrDefault(i => !i.Name.StartsWith("System."));
    }
}

[AttributeUsage(AttributeTargets.Class)]
public class PluginAttribute : Attribute
{
    public string PluginName { get; }
    
    public PluginAttribute(string pluginName)
    {
        PluginName = pluginName;
    }
}
```

## 🚀 Advanced Scenarios

### Convention Composition

Combine multiple conventions for complex scenarios:

```csharp
public class CompositeConvention : INamingConvention
{
    private readonly List<INamingConvention> _childConventions;

    public string Name => "Composite Convention";
    public int Priority => 30;

    public CompositeConvention(params INamingConvention[] childConventions)
    {
        _childConventions = childConventions.OrderBy(c => c.Priority).ToList();
    }

    public bool CanApplyTo(Type implementationType)
    {
        return _childConventions.Any(c => c.CanApplyTo(implementationType));
    }

    public Type? ResolveServiceType(Type implementationType, IEnumerable<Type> availableInterfaces)
    {
        foreach (var convention in _childConventions)
        {
            if (!convention.CanApplyTo(implementationType)) continue;
            
            var result = convention.ResolveServiceType(implementationType, availableInterfaces);
            if (result != null) return result;
        }

        return null;
    }
}
```

### Dynamic Convention Loading

Load conventions from configuration or assemblies:

```csharp
public class DynamicConventionLoader
{
    public static IEnumerable<INamingConvention> LoadFromConfiguration(IConfiguration configuration)
    {
        var conventionConfigs = configuration.GetSection("NamingConventions")
                                           .Get<List<ConventionConfig>>();
        
        return conventionConfigs?.Where(c => c.Enabled)
                                .Select(CreateConvention)
                                .Where(c => c != null)
                                .Cast<INamingConvention>() ?? Enumerable.Empty<INamingConvention>();
    }

    public static IEnumerable<INamingConvention> LoadFromAssembly(Assembly assembly)
    {
        return assembly.GetTypes()
                      .Where(t => typeof(INamingConvention).IsAssignableFrom(t) && 
                                 !t.IsInterface && !t.IsAbstract)
                      .Select(t => Activator.CreateInstance(t) as INamingConvention)
                      .Where(c => c != null)
                      .Cast<INamingConvention>();
    }

    private static INamingConvention? CreateConvention(ConventionConfig config)
    {
        var type = Type.GetType(config.TypeName);
        if (type == null) return null;

        return Activator.CreateInstance(type) as INamingConvention;
    }
}

public class ConventionConfig
{
    public string TypeName { get; set; } = "";
    public bool Enabled { get; set; } = true;
    public int Priority { get; set; } = 100;
}
```

## 🎯 Key Takeaways

1. **Start Simple**: Begin with basic patterns and evolve to complex scenarios
2. **Performance Matters**: Optimize `CanApplyTo` for frequent calls
3. **Test Thoroughly**: Unit test each pattern your convention handles
4. **Document Clearly**: Explain what patterns your convention supports
5. **Monitor Usage**: Track statistics to understand effectiveness
6. **Handle Errors**: Provide clear error messages for debugging
7. **Consider Caching**: Cache expensive operations for better performance

## 🔗 Related Documentation

- **[Naming Conventions](NamingConventions.md)** - Understanding the built-in conventions
- **[Service Registration](ServiceRegistration.md)** - Basic service registration concepts
- **[Plugin Architecture](PluginArchitecture.md)** - Creating custom discovery plugins
- **[System Architecture](SystemArchitecture.md)** - How conventions fit into the overall system

Custom naming conventions provide the flexibility to handle any naming pattern in your codebase while maintaining the benefits of automatic service discovery. With proper implementation and testing, they become powerful tools for maintaining clean, discoverable service architectures.