# Troubleshooting Guide

When things don't work as expected with auto service discovery, it can feel like trying to debug a magic trick - you know something should happen, but you can't see why it isn't working. This guide will help you systematically diagnose and resolve the most common issues, transforming mysterious problems into understandable solutions.

Think of this guide as your diagnostic toolkit. Just like a mechanic uses different tools to diagnose car problems, we'll use different techniques to understand what's happening (or not happening) in your service discovery process.

## 🔧 Quick Diagnostic Checklist

Before diving into specific problems, run through this quick checklist to catch the most common issues:

### Basic Setup Verification

```csharp
// ✅ Verify your basic setup looks like this
builder.Services.AddAutoServices(options =>
{
    options.EnableLogging = true; // Always enable for troubleshooting
    options.Configuration = builder.Configuration; // Required for conditional services
});
```

**Common Setup Issues:**
- Missing `[ServiceRegistration]` attribute on your service class
- Service class is not `public`
- Service class is `abstract`
- Assembly containing services is not being scanned
- Interface naming doesn't follow `I{ClassName}` convention

### Assembly Scanning Verification

```csharp
// ✅ Ensure your assemblies are being included
builder.Services.AddAutoServices(options =>
{
    options.EnableLogging = true;
}, Assembly.GetExecutingAssembly(), Assembly.GetAssembly(typeof(SomeClassInTargetAssembly)));
```

### Service Attribute Verification

```csharp
// ✅ Verify your service has the correct attribute
[ServiceRegistration(ServiceLifetime.Scoped)] // ← Must be present
public class UserService : IUserService        // ← Must be public, implement interface
{
    // Implementation
}
```

## 🚨 Services Not Being Discovered

This is the most common issue - you've marked a service for registration, but it's not appearing in your dependency injection container.

### Symptom: Service Not Found Exception

```
System.InvalidOperationException: Unable to resolve service for type 'IUserService'
```

### Diagnostic Steps

**Step 1: Enable Detailed Logging**

```csharp
builder.Services.AddAutoServices(options =>
{
    options.EnableLogging = true; // This will show you exactly what's happening
});
```

Look for output like this during startup:
```
Starting optimized service discovery for 1 assemblies...
Scanned MyApp in 15.2ms, found 3 services
Registered: IUserService -> UserService (Scoped, Order: 0)
```

If you don't see your service in the output, it's not being discovered.

**Step 2: Verify Assembly is Being Scanned**

```csharp
// Add explicit assembly specification
builder.Services.AddAutoServices(options =>
{
    options.EnableLogging = true;
}, Assembly.GetExecutingAssembly()); // ← Explicitly specify assemblies
```

**Step 3: Check Service Class Requirements**

Your service class must meet these requirements:

```csharp
[ServiceRegistration(ServiceLifetime.Scoped)] // ✅ Has attribute
public class UserService : IUserService       // ✅ Is public
{                                             // ✅ Is not abstract
    // ✅ Has public constructor
    public UserService() { }
}
```

Common violations:
```csharp
// ❌ Missing attribute
public class UserService : IUserService { }

// ❌ Not public
[ServiceRegistration(ServiceLifetime.Scoped)]
internal class UserService : IUserService { }

// ❌ Abstract class
[ServiceRegistration(ServiceLifetime.Scoped)]
public abstract class UserService : IUserService { }

// ❌ Generic type definition
[ServiceRegistration(ServiceLifetime.Scoped)]
public class UserService<T> : IUserService<T> { }
```

**Step 4: Verify Interface Resolution**

The system looks for interfaces following the `I{ClassName}` convention:

```csharp
// ✅ Good - follows convention
public interface IUserService { }
public class UserService : IUserService { }

// ❌ Bad - doesn't follow convention
public interface IUserOperations { }
public class UserService : IUserOperations { } // Won't auto-resolve

// ✅ Solution - specify explicitly
[ServiceRegistration(ServiceLifetime.Scoped, ServiceType = typeof(IUserOperations))]
public class UserService : IUserOperations { }
```

### Advanced Diagnostics

**Use Reflection to Debug Discovery**

```csharp
// Add this temporary code to see what types are found
var assembly = Assembly.GetExecutingAssembly();
var typesWithAttribute = assembly.GetTypes()
    .Where(t => t.IsDefined(typeof(ServiceRegistrationAttribute), false))
    .ToList();

Console.WriteLine($"Found {typesWithAttribute.Count} types with ServiceRegistration attribute:");
foreach (var type in typesWithAttribute)
{
    Console.WriteLine($"  - {type.FullName}");
    var attr = type.GetCustomAttribute<ServiceRegistrationAttribute>();
    Console.WriteLine($"    Lifetime: {attr?.Lifetime}");
    Console.WriteLine($"    Explicit ServiceType: {attr?.ServiceType?.Name ?? "None"}");
}
```

## 🔄 Wrong Service Implementation Being Used

Sometimes services are discovered, but the wrong implementation is being used. This typically happens when multiple implementations exist for the same interface.

### Symptom: Unexpected Behavior

Your code runs, but the wrong service implementation is being used, leading to unexpected behavior.

### Diagnostic Steps

**Step 1: Check for Multiple Registrations**

```csharp
// Add this after AddAutoServices to see all registrations
var serviceProvider = builder.Services.BuildServiceProvider();
var userServiceDescriptor = builder.Services
    .Where(s => s.ServiceType == typeof(IUserService))
    .ToList();

Console.WriteLine($"Found {userServiceDescriptor.Count} registrations for IUserService:");
foreach (var descriptor in userServiceDescriptor)
{
    Console.WriteLine($"  - {descriptor.ImplementationType?.Name} ({descriptor.Lifetime})");
}
```

**Step 2: Use Registration Order**

If you have multiple implementations, use the `Order` property to control which one is used:

```csharp
[ServiceRegistration(ServiceLifetime.Scoped, Order = 1)] // Lower order = higher priority
public class PrimaryUserService : IUserService { }

[ServiceRegistration(ServiceLifetime.Scoped, Order = 2)]
public class BackupUserService : IUserService { }
```

**Step 3: Use Profiles for Environment-Specific Services**

```csharp
[ServiceRegistration(ServiceLifetime.Scoped, Profile = "Development")]
public class MockUserService : IUserService { }

[ServiceRegistration(ServiceLifetime.Scoped, Profile = "Production")]
public class DatabaseUserService : IUserService { }

// In Program.cs
builder.Services.AddAutoServices(options =>
{
    options.Profile = builder.Environment.EnvironmentName;
});
```

## ⚙️ Conditional Services Not Working

Conditional services should register only when certain conditions are met, but they're either always registering or never registering.

### Symptom: Conditional Logic Ignored

Services marked with `[ConditionalService]` are registering regardless of configuration values, or they're never registering when they should.

### Diagnostic Steps

**Step 1: Verify Configuration is Available**

```csharp
builder.Services.AddAutoServices(options =>
{
    options.Configuration = builder.Configuration; // ← Must provide configuration
    options.EnableLogging = true;
});
```

**Step 2: Check Configuration Values**

```csharp
// Add this to verify your configuration values
var config = builder.Configuration;
var featureValue = config["Features:EnableAdvancedLogging"];
Console.WriteLine($"Configuration 'Features:EnableAdvancedLogging' = '{featureValue}'");

if (string.IsNullOrEmpty(featureValue))
{
    Console.WriteLine("⚠️  Configuration value is null or empty!");
}
```

**Step 3: Verify Conditional Attribute Syntax**

```csharp
// ✅ Correct conditional service
[ConditionalService("Features:EnableAdvancedLogging", "true")]
[ServiceRegistration(ServiceLifetime.Singleton)]
public class AdvancedLoggingService : ILoggingService { }
```

Common mistakes:
```csharp
// ❌ Wrong key format
[ConditionalService("EnableAdvancedLogging", "true")] // Missing section

// ❌ Case sensitivity issues
[ConditionalService("Features:EnableAdvancedLogging", "True")] // Should be lowercase "true"

// ❌ Missing configuration
// Configuration not provided to AddAutoServices
```

**Step 4: Test Expression-Based Conditions**

```csharp
// For expression-based conditions, test the logic separately
[ConditionalService(ctx => 
    ctx.Environment.IsProduction() && 
    ctx.FeatureEnabled("AdvancedReporting"))]
[ServiceRegistration(ServiceLifetime.Scoped)]
public class ProductionReportingService : IReportingService { }

// Test the condition logic:
var context = new ConditionalContext(builder.Configuration, builder.Environment.EnvironmentName);
var condition = (IConditionalContext ctx) => 
    ctx.Environment.IsProduction() && 
    ctx.FeatureEnabled("AdvancedReporting");

var result = condition(context);
Console.WriteLine($"Condition result: {result}");
```

## 🏗️ Fluent Configuration Issues

Problems with the fluent configuration API not working as expected.

### Symptom: Fluent Configuration Not Applied

You've used the fluent API, but the configuration seems to be ignored.

### Diagnostic Steps

**Step 1: Ensure Apply() is Called**

```csharp
// ❌ Missing Apply() call
services.ConfigureAutoServices()
    .FromCurrentDomain()
    .WithProfile("Production")
    .WithLogging(true); // ← Nothing happens without Apply()

// ✅ Correct usage
services.ConfigureAutoServices()
    .FromCurrentDomain()
    .WithProfile("Production")
    .WithLogging(true)
    .Apply(); // ← Essential!
```

**Step 2: Check Assembly Selection**

```csharp
// Debug which assemblies are being processed
var builder = services.ConfigureAutoServices()
    .FromCurrentDomain();

Console.WriteLine($"Fluent config will process {builder.Assemblies.Count} assemblies:");
foreach (var assembly in builder.Assemblies)
{
    Console.WriteLine($"  - {assembly.GetName().Name}");
}

builder.Apply();
```

**Step 3: Verify Filters and Conditions**

```csharp
// Test your filters separately
services.ConfigureAutoServices()
    .FromCurrentDomain()
    .ExcludeTypes(type => type.Name.EndsWith("Test"))
    .When(ctx => ctx.Environment.IsProduction())
    .WithLogging(true) // Enable to see what's filtered
    .Apply();
```

## 🚀 Performance Issues

Services are being discovered, but the process is too slow for your needs.

### Symptom: Slow Startup Times

Application startup is slow due to service discovery taking too long.

### Diagnostic Steps

**Step 1: Enable Performance Monitoring**

```csharp
builder.Services.AddAutoServicesWithPerformanceOptimizations(options =>
{
    options.EnableLogging = true;
    options.EnablePerformanceMetrics = true;
});
```

**Step 2: Analyze Assembly Performance**

```csharp
// Add this after service registration to see timing
var services = builder.Services.BuildServiceProvider();
var cacheStats = PerformanceServiceCollectionExtensions.GetCacheStatistics();

Console.WriteLine($"Cache Performance:");
Console.WriteLine($"  Total Requests: {cacheStats.TotalRequests}");
Console.WriteLine($"  Cache Hits: {cacheStats.CacheHits}");
Console.WriteLine($"  Hit Ratio: {cacheStats.HitRatio:F1}%");
```

**Step 3: Optimize Assembly Selection**

```csharp
// Instead of scanning all assemblies
services.ConfigureAutoServices()
    .FromCurrentDomain() // ← Scans everything
    .Apply();

// Be specific about which assemblies to scan
services.ConfigureAutoServices()
    .FromAssemblies(
        Assembly.GetExecutingAssembly(),
        Assembly.GetAssembly(typeof(UserService))
    )
    .Apply();
```

**Step 4: Use Caching for Repeated Operations**

```csharp
// Enable caching for applications that restart frequently
builder.Services.AddAutoServicesWithPerformanceOptimizations(options =>
{
    options.EnablePerformanceOptimizations = true;
    options.EnableParallelProcessing = true;
});
```

## 🧪 Testing Environment Issues

Services behave differently in tests than in the application.

### Symptom: Tests Fail Due to Service Registration

Services that work in the application fail to register properly in tests.

### Diagnostic Steps

**Step 1: Configure Test Environment**

```csharp
// In your test setup
var services = new ServiceCollection();
services.AddAutoServices(options =>
{
    options.IsTestEnvironment = true; // Excludes services marked with IgnoreInTests
    options.EnableLogging = true;
    options.Configuration = configuration; // Provide test configuration
});

var serviceProvider = services.BuildServiceProvider();
```

**Step 2: Handle Assembly Loading in Tests**

```csharp
// Tests might not load assemblies the same way
[Test]
public void TestServiceRegistration()
{
    var services = new ServiceCollection();
    
    // Explicitly specify the assembly containing your services
    services.AddAutoServices(options =>
    {
        options.IsTestEnvironment = true;
    }, Assembly.GetAssembly(typeof(UserService))); // ← Be explicit
    
    var serviceProvider = services.BuildServiceProvider();
    var userService = serviceProvider.GetService<IUserService>();
    
    Assert.IsNotNull(userService);
}
```

**Step 3: Mock Configuration for Conditional Services**

```csharp
[Test]
public void ConditionalService_RegistersWhenConditionMet()
{
    // Create test configuration
    var configuration = new ConfigurationBuilder()
        .AddInMemoryCollection(new[]
        {
            new KeyValuePair<string, string>("Features:EnableTestFeature", "true")
        })
        .Build();

    var services = new ServiceCollection();
    services.AddAutoServices(options =>
    {
        options.Configuration = configuration;
        options.IsTestEnvironment = true;
    });

    var serviceProvider = services.BuildServiceProvider();
    var testService = serviceProvider.GetService<ITestService>();
    
    Assert.IsNotNull(testService);
}
```

## 🔌 Plugin and Extension Issues

Problems with custom plugins or naming conventions not working correctly.

### Symptom: Custom Plugins Not Executing

Your custom plugins are registered but don't seem to be executing during discovery.

### Diagnostic Steps

**Step 1: Verify Plugin Registration**

```csharp
// Ensure plugins are registered correctly
builder.Services.AddServiceDiscoveryPlugin<MyCustomPlugin>();

// Or register the infrastructure first
builder.Services.AddAutoDiscoveryInfrastructure(options =>
{
    options.AddPlugin<MyCustomPlugin>();
});
```

**Step 2: Check Plugin Implementation**

```csharp
public class MyCustomPlugin : IServiceDiscoveryPlugin
{
    public string Name => "My Custom Plugin";
    public int Priority => 10; // Lower = higher priority

    public bool CanProcessAssembly(Assembly assembly)
    {
        // Make sure this returns true for assemblies you want to process
        var canProcess = assembly.GetName().Name?.StartsWith("MyApp") == true;
        Console.WriteLine($"Plugin {Name}: Can process {assembly.GetName().Name}? {canProcess}");
        return canProcess;
    }

    public IEnumerable<ServiceRegistrationInfo> DiscoverServices(Assembly assembly, AutoServiceOptions options)
    {
        Console.WriteLine($"Plugin {Name}: Discovering services in {assembly.GetName().Name}");
        // Your discovery logic here
        return Enumerable.Empty<ServiceRegistrationInfo>();
    }

    public PluginValidationResult ValidateDiscoveredServices(
        IEnumerable<ServiceRegistrationInfo> discoveredServices,
        IEnumerable<ServiceRegistrationInfo> allServices,
        AutoServiceOptions options)
    {
        return PluginValidationResult.Success();
    }
}
```

**Step 3: Enable Plugin Logging**

```csharp
// Use the plugin-enabled discovery method
builder.Services.AddAutoServicesWithPlugins(
    new[] { new MyCustomPlugin() },
    options =>
    {
        options.EnableLogging = true; // See plugin execution
    }
);
```

## 🐛 Common Error Messages and Solutions

### "Multiple implementations found for service type"

**Problem:** More than one class implements the same interface without proper ordering.

**Solution:**
```csharp
[ServiceRegistration(ServiceLifetime.Scoped, Order = 1)]
public class PrimaryImplementation : IMyService { }

[ServiceRegistration(ServiceLifetime.Scoped, Order = 2)]
public class SecondaryImplementation : IMyService { }
```

### "Unable to resolve service for type"

**Problem:** Service not registered or wrong interface type specified.

**Solution:**
1. Verify the service has `[ServiceRegistration]` attribute
2. Check the interface naming convention
3. Ensure the assembly is being scanned

### "Conditional service attribute evaluation failed"

**Problem:** Error in conditional service expression or missing configuration.

**Solution:**
```csharp
// Ensure configuration is provided
builder.Services.AddAutoServices(options =>
{
    options.Configuration = builder.Configuration; // Required!
});

// Test conditional expressions separately
var context = new ConditionalContext(configuration, environmentName);
var result = yourCondition(context); // Test your condition
```

### "Assembly load exception during discovery"

**Problem:** Referenced assemblies can't be loaded during discovery.

**Solution:**
```csharp
// Handle assembly load issues gracefully
builder.Services.AddAutoServices(options =>
{
    options.EnableLogging = true; // See which assemblies fail
});

// Or exclude problematic assemblies
services.ConfigureAutoServices()
    .FromCurrentDomain(assembly => 
        !assembly.FullName?.StartsWith("ProblematicAssembly") == true)
    .Apply();
```

## 📋 Debugging Checklist

When troubleshooting, work through this systematic checklist:

### ✅ Basic Setup
- [ ] `[ServiceRegistration]` attribute present
- [ ] Service class is `public`
- [ ] Service class is not `abstract`
- [ ] Interface follows `I{ClassName}` convention or `ServiceType` is explicitly specified
- [ ] Assembly containing services is being scanned

### ✅ Configuration
- [ ] `AddAutoServices()` is called in `Program.cs`
- [ ] `options.Configuration` is provided for conditional services
- [ ] Environment is set correctly for profile-based registration
- [ ] Logging is enabled for debugging

### ✅ Advanced Features
- [ ] Conditional service configuration values exist and are correct
- [ ] Multiple implementations use `Order` property for prioritization
- [ ] Test environment settings are configured correctly
- [ ] Custom plugins implement interface correctly and are registered

### ✅ Performance
- [ ] Assembly selection is optimized (not scanning unnecessary assemblies)
- [ ] Performance optimizations are enabled if needed
- [ ] Caching is working (check cache statistics)

## 🔧 Advanced Debugging Techniques

### Reflection-Based Diagnostics

```csharp
public static class DiscoveryDiagnostics
{
    public static void AnalyzeAssembly(Assembly assembly)
    {
        Console.WriteLine($"\n=== Assembly Analysis: {assembly.GetName().Name} ===");
        
        var allTypes = assembly.GetTypes().Where(t => t.IsClass && !t.IsAbstract);
        Console.WriteLine($"Total concrete classes: {allTypes.Count()}");
        
        var servicesWithAttribute = allTypes
            .Where(t => t.IsDefined(typeof(ServiceRegistrationAttribute), false));
        Console.WriteLine($"Classes with ServiceRegistration: {servicesWithAttribute.Count()}");
        
        foreach (var serviceType in servicesWithAttribute)
        {
            var attr = serviceType.GetCustomAttribute<ServiceRegistrationAttribute>();
            var interfaces = serviceType.GetInterfaces()
                .Where(i => !i.Name.StartsWith("System."));
            
            Console.WriteLine($"\n  Service: {serviceType.Name}");
            Console.WriteLine($"    Lifetime: {attr?.Lifetime}");
            Console.WriteLine($"    Explicit ServiceType: {attr?.ServiceType?.Name ?? "None"}");
            Console.WriteLine($"    Interfaces: {string.Join(", ", interfaces.Select(i => i.Name))}");
            
            // Check naming convention
            var expectedInterface = $"I{serviceType.Name}";
            var hasExpected = interfaces.Any(i => i.Name == expectedInterface);
            Console.WriteLine($"    Follows I{{Name}} convention: {hasExpected}");
        }
    }
}

// Use in Program.cs for debugging
DiscoveryDiagnostics.AnalyzeAssembly(Assembly.GetExecutingAssembly());
```

### Service Resolution Testing

```csharp
public static class ServiceResolutionTester
{
    public static void TestServiceResolution(IServiceCollection services)
    {
        var serviceProvider = services.BuildServiceProvider();
        
        // Get all registered service descriptors
        var registeredServices = services
            .Where(s => !s.ServiceType.Name.StartsWith("System."))
            .GroupBy(s => s.ServiceType)
            .ToList();
        
        Console.WriteLine("\n=== Service Resolution Test ===");
        
        foreach (var serviceGroup in registeredServices)
        {
            var serviceType = serviceGroup.Key;
            var implementations = serviceGroup.ToList();
            
            Console.WriteLine($"\nService: {serviceType.Name}");
            Console.WriteLine($"  Registrations: {implementations.Count}");
            
            foreach (var impl in implementations)
            {
                Console.WriteLine($"    - {impl.ImplementationType?.Name ?? "Factory"} ({impl.Lifetime})");
            }
            
            try
            {
                var instance = serviceProvider.GetService(serviceType);
                Console.WriteLine($"  Resolution: ✅ {instance?.GetType().Name ?? "null"}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  Resolution: ❌ {ex.Message}");
            }
        }
    }
}
```

## 🎯 When to Seek Further Help

If you've worked through this guide and still have issues:

1. **Enable maximum logging** and capture the complete output
2. **Create a minimal reproduction** that demonstrates the problem
3. **Document your environment** (framework version, packages, etc.)
4. **Include relevant code snippets** showing your service definitions and configuration

The auto service discovery system is designed to be transparent and debuggable. With the right diagnostic approach, most issues can be identified and resolved systematically. Remember that the `EnableLogging = true` option is your best friend for understanding what the system is actually doing during discovery.

## 🔗 Related Documentation

For more advanced scenarios, consult these related guides:

1. **[Getting Started](GettingStarted.md)** - Verify your basic setup
2. **[Conditional Registration](ConditionalRegistration.md)** - Understand conditional service behavior
3. **[Naming Conventions](NamingConventions.md)** - Learn about interface resolution
4. **[Expression-Based Conditions](ExpressionBasedConditions.md)** - Debug complex conditional logic

Remember: most service discovery issues stem from simple configuration or naming problems. Start with the basics, use logging liberally, and work systematically through the diagnostic steps.