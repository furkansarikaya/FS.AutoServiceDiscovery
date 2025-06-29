# Fluent Configuration Guide

Imagine building a complex sentence in English - you start with a subject, add verbs, objects, and modifiers in a natural flow that creates meaning. Fluent configuration works the same way: you chain method calls together to build sophisticated service discovery configurations that read almost like natural language. Instead of cramming all your configuration into a single method call with numerous parameters, you build it up piece by piece in an intuitive, discoverable way.

The fluent configuration API transforms service discovery from a simple on/off switch into a powerful configuration language that can express complex requirements, conditions, and customizations while remaining readable and maintainable.

## 🎯 Understanding Fluent Configuration

At its core, fluent configuration is about building complexity through composition. Rather than having one massive configuration object with dozens of properties, you chain together small, focused configuration methods that each add one piece of functionality to your overall configuration.

Think of it like assembling a custom car: instead of choosing from three pre-built models, you select each component (engine, transmission, wheels, interior) individually to create exactly the combination you need. The fluent API provides the same level of control for service discovery configuration.

### The Mental Model: Builder Pattern with Method Chaining

The fluent configuration system implements a sophisticated builder pattern where each method returns a builder object that enables the next method in the chain. This creates a natural flow from general requirements to specific details:

```csharp
// This reads almost like a specification document
builder.Services.ConfigureAutoServices()
    .FromCurrentDomain()                           // Start with: what assemblies?
    .WithProfile("Production")                     // Then specify: which environment?
    .When(ctx => ctx.FeatureEnabled("AutoDI"))     // Add condition: when should this run?
    .ExcludeNamespaces("MyApp.Tests.*")            // Refine scope: what to exclude?
    .WithDefaultLifetime(ServiceLifetime.Scoped)   // Set defaults: how should services live?
    .WithPerformanceOptimizations()                // Enable features: what optimizations?
    .Apply();                                      // Finally: make it happen
```

Each method in this chain adds one specific aspect to the configuration, building up complexity in a controlled, readable way. The beauty of this approach is that you can stop at any point and have a valid configuration, then add more methods as your requirements become more sophisticated.

## 🏗️ Building Your First Fluent Configuration

Let's start with simple examples and gradually build complexity, so you can see how each piece contributes to the overall configuration.

### Starting Simple: Basic Assembly Configuration

The most fundamental aspect of service discovery is determining which assemblies to scan. The fluent API provides several intuitive ways to specify this:

```csharp
// Option 1: Scan the current assembly (most common for single-project apps)
builder.Services.ConfigureAutoServicesFromCurrentAssembly()
    .Apply();

// Option 2: Scan specific assemblies (good for multi-project solutions)
builder.Services.ConfigureAutoServices()
    .FromAssemblies(
        Assembly.GetExecutingAssembly(),
        Assembly.GetAssembly(typeof(UserService)),
        Assembly.GetAssembly(typeof(OrderService)))
    .Apply();

// Option 3: Auto-discover assemblies from current domain (powerful but requires filtering)
builder.Services.ConfigureAutoServices()
    .FromCurrentDomain(assembly => 
        !assembly.FullName?.StartsWith("System.") == true &&
        !assembly.FullName?.StartsWith("Microsoft.") == true)
    .Apply();
```

Notice how each approach becomes progressively more powerful but also more explicit about what it's doing. The fluent API lets you choose the level of control you need without forcing you to deal with complexity you don't want.

### Adding Environment Awareness

Once you've specified which assemblies to scan, the next logical step is often to make your configuration environment-aware. The fluent API makes this natural and readable:

```csharp
builder.Services.ConfigureAutoServices()
    .FromCurrentDomain(assembly => !assembly.FullName.StartsWith("System"))
    .WithProfile(builder.Environment.EnvironmentName)  // Use ASP.NET Core environment
    .Apply();

// Or with dynamic profile resolution based on complex logic
builder.Services.ConfigureAutoServices()
    .FromCurrentDomain()
    .WithProfile(ctx => 
    {
        // Complex logic to determine profile based on multiple factors
        if (ctx.Environment.IsProduction() && ctx.HasConfigurationValue("HighAvailability:Enabled"))
            return "ProductionHA";
        else if (ctx.Environment.IsProduction())
            return "Production";
        else if (ctx.Environment.IsStaging())
            return "Staging";
        else
            return "Development";
    })
    .Apply();
```

The dynamic profile resolution example shows how the fluent API enables sophisticated decision-making logic while keeping the overall configuration structure clear and readable.

### Implementing Conditional Logic

Conditional logic is where the fluent API really shines. Instead of scattering conditional statements throughout your startup code, you can express complex conditions directly in your configuration:

```csharp
builder.Services.ConfigureAutoServices()
    .FromCurrentDomain()
    .WithProfile("Production")
    
    // Simple configuration-based condition
    .When("Features:EnableAutoDiscovery", "true")
    
    // Complex expression-based condition  
    .When(ctx => 
        ctx.Environment.IsProduction() &&
        ctx.FeatureEnabled("AdvancedServices") &&
        !ctx.Configuration.GetValue<bool>("MaintenanceMode"))
    
    // Multiple conditions can be chained (AND logic)
    .When(ctx => ctx.GetConfigValue<int>("MaxUsers") > 100)
    
    .Apply();
```

Each `When()` call adds another condition that must be satisfied for the discovery to proceed. This creates a clear audit trail of what conditions your service discovery depends on.

## 🎨 Advanced Configuration Patterns

As your application grows more sophisticated, you'll need more powerful configuration patterns. The fluent API is designed to scale with your complexity while maintaining readability.

### Type Filtering and Namespace Management

Large applications often need fine-grained control over which types are included in service discovery. The fluent API provides several complementary approaches:

```csharp
builder.Services.ConfigureAutoServices()
    .FromCurrentDomain()
    
    // Exclude entire namespaces (broad brush approach)
    .ExcludeNamespaces(
        "MyApp.Tests.*",           // All test classes
        "MyApp.Internal.*",        // Internal utilities
        "MyApp.Legacy",            // Legacy code we're not ready to migrate
        "*.Migrations")            // Database migration classes
    
    // Exclude specific types (surgical approach)
    .ExcludeTypes(type => 
        type.Name.EndsWith("Test") ||           // Unit test classes
        type.Name.EndsWith("Migration") ||      // Database migrations
        type.IsAbstract ||                      // Abstract base classes
        type.GetCustomAttribute<ObsoleteAttribute>() != null) // Obsolete classes
    
    // Or take an inclusive approach (whitelist)
    .IncludeOnlyTypes(type =>
        type.Namespace?.StartsWith("MyApp.Services") == true ||
        type.Namespace?.StartsWith("MyApp.Repositories") == true ||
        type.Namespace?.StartsWith("MyApp.Controllers") == true)
    
    .Apply();
```

The key insight here is that you can use these filtering methods in combination. For example, you might start with a broad namespace inclusion, then exclude specific problematic types within those namespaces.

### Custom Naming Conventions Integration

When your codebase doesn't follow standard naming patterns, you can integrate custom naming conventions directly into your fluent configuration:

```csharp
builder.Services.ConfigureAutoServices()
    .FromAssemblies(Assembly.GetExecutingAssembly())
    
    // Add custom naming conventions in priority order
    .WithNamingConvention<LegacyNamingConvention>()      // Handles old patterns
    .WithNamingConvention<DomainSpecificConvention>()    // Handles DDD patterns
    .WithNamingConvention(new CustomConvention())        // Instance-based convention
    
    // Set a default lifetime for services that don't specify one
    .WithDefaultLifetime(ServiceLifetime.Scoped)
    
    .Apply();
```

This pattern allows you to layer multiple naming strategies, with earlier conventions taking precedence over later ones. This is particularly valuable when migrating legacy codebases or integrating with third-party libraries that use different naming patterns.

### Plugin Integration

The fluent API seamlessly integrates with the plugin architecture, allowing you to configure custom discovery strategies:

```csharp
builder.Services.ConfigureAutoServices()
    .FromCurrentDomain()
    .WithProfile("Production")
    
    // Add custom discovery plugins
    .WithPlugin<MediatRDiscoveryPlugin>()          // Discover MediatR handlers
    .WithPlugin<FluentValidationPlugin>()          // Discover validators
    .WithPlugin(new CustomBusinessRulePlugin())    // Custom business logic discovery
    
    // Configure performance optimizations
    .WithPerformanceOptimizations(true)
    .WithLogging(false) // Disable logging in production for performance
    
    .Apply();
```

Each plugin extends the discovery capabilities in specialized ways, and the fluent API makes it easy to compose multiple plugins together.

## 🔧 Configuration Validation and Error Handling

One of the most powerful aspects of the fluent configuration system is its ability to validate your configuration before attempting to use it. This catches configuration errors early and provides clear feedback about what needs to be fixed.

### Built-in Validation

The fluent API includes built-in validation that catches common configuration mistakes:

```csharp
try
{
    builder.Services.ConfigureAutoServices()
        .FromAssemblies() // ❌ No assemblies specified - this will be caught
        .WithProfile("")  // ❌ Empty profile - this will be caught
        .Apply();
}
catch (InvalidOperationException ex)
{
    // The error message will be clear and actionable:
    // "Configuration validation failed: No assemblies specified for discovery. 
    //  Use FromAssemblies(), FromAssemblyPaths(), or FromCurrentDomain()."
}
```

### Custom Validation Rules

You can add your own validation rules to catch application-specific configuration issues:

```csharp
builder.Services.ConfigureAutoServices()
    .FromCurrentDomain()
    .WithProfile("Production")
    
    // Add custom validation logic
    .WithValidation(ctx =>
    {
        var result = new ConfigurationValidationResult();
        
        // Check that required configuration values are present
        if (!ctx.HasConfigurationValue("ConnectionStrings:DefaultConnection"))
        {
            result.AddError("Database connection string is required in production");
        }
        
        // Check that conflicting features aren't both enabled
        var useCache = ctx.GetConfigurationValue<bool>("Features:EnableCaching");
        var useMemory = ctx.GetConfigurationValue<bool>("Features:LowMemoryMode");
        if (useCache && useMemory)
        {
            result.AddWarning("Caching and low memory mode are both enabled - this may cause performance issues");
        }
        
        // Provide helpful information
        result.AddInformation($"Configuration validated for {ctx.Environment} environment with {ctx.Assemblies.Count} assemblies");
        
        return result;
    })
    
    .Apply();
```

Custom validation rules are particularly valuable in large teams where configuration mistakes can cause production issues. By encoding your configuration requirements as validation rules, you make them explicit and automatically enforced.

## 🚀 Performance Optimization Integration

The fluent API integrates seamlessly with the performance optimization features, allowing you to enable sophisticated optimizations while maintaining configuration clarity:

### Basic Performance Configuration

```csharp
builder.Services.ConfigureAutoServices()
    .FromCurrentDomain()
    .WithProfile("Production")
    
    // Enable all performance optimizations with sensible defaults
    .WithPerformanceOptimizations()
    
    .Apply();
```

### Detailed Performance Configuration

For more control over performance characteristics, you can configure specific optimization features:

```csharp
builder.Services.ConfigureAutoServices()
    .FromCurrentDomain()
    .WithProfile("Production")
    
    // Enable performance optimizations with custom settings
    .WithPerformanceOptimizations(true)
    
    // Configure logging appropriately for the environment
    .WithLogging(builder.Environment.IsDevelopment())
    
    // Optimize for the specific deployment environment
    .When(ctx => ctx.Environment.IsProduction())
    
    .Apply();
```

### Environment-Specific Performance Tuning

Different environments often have different performance requirements and constraints:

```csharp
// Development environment - prioritize debugging over performance
if (builder.Environment.IsDevelopment())
{
    builder.Services.ConfigureAutoServices()
        .FromCurrentDomain()
        .WithProfile("Development")
        .WithLogging(true)                    // Full logging for debugging
        .WithPerformanceOptimizations(false)  // Disable optimizations for better error messages
        .Apply();
}
// Production environment - prioritize performance
else
{
    builder.Services.ConfigureAutoServices()
        .FromCurrentDomain()
        .WithProfile("Production")
        .WithLogging(false)                   // Minimal logging for performance
        .WithPerformanceOptimizations(true)   // Full optimizations
        .Apply();
}
```

## 🧪 Testing Fluent Configurations

Testing applications that use fluent configuration requires strategies that can validate the configuration logic itself, not just the services that result from it.

### Configuration Testing Strategies

```csharp
[TestClass]
public class FluentConfigurationTests
{
    [TestMethod]
    public void FluentConfig_ShouldRegisterExpectedServices_InDevelopmentEnvironment()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new[]
            {
                new KeyValuePair<string, string>("Features:EnableAutoDiscovery", "true"),
                new KeyValuePair<string, string>("Environment", "Development")
            })
            .Build();

        // Act - Use fluent configuration
        services.ConfigureAutoServices()
            .FromAssemblies(Assembly.GetExecutingAssembly())
            .WithProfile("Development")
            .WithConfiguration(configuration)
            .When("Features:EnableAutoDiscovery", "true")
            .Apply();

        var serviceProvider = services.BuildServiceProvider();

        // Assert
        Assert.IsNotNull(serviceProvider.GetService<IUserService>());
        Assert.IsType<MockUserService>(serviceProvider.GetService<IUserService>());
    }

    [TestMethod]
    public void FluentConfig_ShouldValidateConfiguration_WhenRequiredValuesAreMissing()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act & Assert
        Assert.ThrowsException<InvalidOperationException>(() =>
        {
            services.ConfigureAutoServices()
                .FromAssemblies() // ❌ No assemblies specified
                .Apply();
        });
    }

    [TestMethod]
    public void FluentConfig_ShouldApplyFiltering_WhenNamespaceExclusionsAreSpecified()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.ConfigureAutoServices()
            .FromAssemblies(Assembly.GetExecutingAssembly())
            .ExcludeNamespaces("MyApp.Tests.*")
            .Apply();

        var serviceProvider = services.BuildServiceProvider();

        // Assert - Test services should not be registered
        Assert.IsNull(serviceProvider.GetService<ITestUtilityService>());
        
        // But regular services should be registered
        Assert.IsNotNull(serviceProvider.GetService<IUserService>());
    }
}
```

### Integration Testing with Fluent Configuration

```csharp
[TestClass]
public class FluentConfigurationIntegrationTests
{
    [TestMethod]
    public async Task FluentConfig_ShouldWork_InFullApplicationContext()
    {
        // Arrange - Create a test web application
        var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    // Override the normal configuration with test-specific setup
                    services.ConfigureAutoServices()
                        .FromCurrentDomain()
                        .WithProfile("Testing")
                        .ExcludeTypes(type => type.Name.Contains("Production"))
                        .WithLogging(true)
                        .Apply();
                });
            });

        // Act - Use the configured application
        var client = factory.CreateClient();
        var response = await client.GetAsync("/api/users");

        // Assert
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        Assert.IsTrue(content.Contains("test")); // Should use test implementations
    }
}
```

## 🎯 Real-World Configuration Examples

Let's look at some complete, real-world examples that demonstrate how to combine all these features effectively:

### Enterprise Application Configuration

```csharp
// Enterprise application with complex requirements
builder.Services.ConfigureAutoServices()
    
    // Scan business assemblies but exclude infrastructure concerns
    .FromCurrentDomain(assembly => 
        assembly.FullName?.StartsWith("MyCompany.") == true &&
        !assembly.FullName.Contains("Tests") &&
        !assembly.FullName.Contains("Migrations"))
    
    // Environment-aware profile selection
    .WithProfile(ctx => 
        ctx.Environment.IsProduction() && ctx.HasConfigurationValue("HighAvailability:Enabled") 
            ? "ProductionHA" 
            : ctx.Environment.EnvironmentName)
    
    // Only enable auto-discovery when explicitly configured
    .When("Features:EnableAutoDiscovery", "true")
    
    // Additional safety checks for production
    .When(ctx => 
        !ctx.Environment.IsProduction() || 
        ctx.HasConfigurationValue("Database:ConnectionString"))
    
    // Exclude problematic legacy code
    .ExcludeNamespaces(
        "MyCompany.Legacy.*",
        "MyCompany.ThirdParty.*")
    
    // Include specialized discovery for domain patterns
    .WithNamingConvention<DomainServiceConvention>()
    .WithNamingConvention<RepositoryConvention>()
    
    // Add framework-specific plugins
    .WithPlugin<MediatRHandlerPlugin>()
    .WithPlugin<FluentValidationPlugin>()
    
    // Configure for production performance
    .WithPerformanceOptimizations(true)
    .WithLogging(builder.Environment.IsDevelopment())
    
    // Ensure configuration is valid before proceeding
    .WithValidation(ctx =>
    {
        var result = new ConfigurationValidationResult();
        
        if (ctx.Environment.IsProduction())
        {
            // Production-specific validations
            if (!ctx.HasConfigurationValue("ConnectionStrings:DefaultConnection"))
                result.AddError("Production database connection string is required");
            
            if (!ctx.HasConfigurationValue("Redis:ConnectionString"))
                result.AddError("Production Redis connection string is required");
        }
        
        return result;
    })
    
    .Apply();
```

### Microservice Configuration

```csharp
// Microservice with service mesh integration
builder.Services.ConfigureAutoServices()
    
    // Single assembly for microservice simplicity
    .FromAssemblies(Assembly.GetExecutingAssembly())
    
    // Use Kubernetes environment variables for profile
    .WithProfile(Environment.GetEnvironmentVariable("DEPLOYMENT_ENV") ?? "Development")
    
    // Conditional registration based on service mesh availability
    .When(ctx => 
        ctx.Environment.IsProduction() && 
        !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("SERVICE_MESH_ENABLED")))
    
    // Microservice-specific type filtering
    .IncludeOnlyTypes(type =>
        type.Namespace?.Contains(".Services") == true ||
        type.Namespace?.Contains(".Controllers") == true ||
        type.Namespace?.Contains(".Repositories") == true)
    
    // Optimize for container startup speed
    .WithPerformanceOptimizations(true)
    .WithLogging(false) // Use structured logging instead
    
    .Apply();
```

### Development and Testing Configuration

```csharp
// Development environment with extensive debugging
if (builder.Environment.IsDevelopment())
{
    builder.Services.ConfigureAutoServices()
        .FromCurrentDomain()
        .WithProfile("Development")
        
        // Include test utilities in development
        .IncludeOnlyTypes(type => 
            !type.Name.EndsWith("Test") || 
            type.Name.Contains("TestUtility"))
        
        // Enable all debugging features
        .WithLogging(true)
        .WithPerformanceOptimizations(false) // Clearer error messages
        
        // Validate development-specific requirements
        .WithValidation(ctx =>
        {
            var result = new ConfigurationValidationResult();
            
            // Ensure test database is configured
            var connectionString = ctx.Configuration?.GetConnectionString("TestDatabase");
            if (string.IsNullOrEmpty(connectionString))
                result.AddWarning("Test database connection string not configured - using in-memory database");
            
            return result;
        })
        
        .Apply();
}
```

## 💡 Best Practices and Common Patterns

Through extensive use in production applications, several patterns have emerged as particularly effective:

### 1. Layer Your Configuration Complexity

Start with simple configuration and add complexity only as needed:

```csharp
// Start simple
services.ConfigureAutoServicesFromCurrentAssembly().Apply();

// Add environment awareness when needed
services.ConfigureAutoServicesFromCurrentAssembly()
    .WithProfile(builder.Environment.EnvironmentName)
    .Apply();

// Add conditions as requirements emerge
services.ConfigureAutoServicesFromCurrentAssembly()
    .WithProfile(builder.Environment.EnvironmentName)
    .When("Features:EnableAutoDiscovery", "true")
    .Apply();
```

### 2. Use Validation to Catch Configuration Errors Early

Always add validation for critical configuration requirements:

```csharp
services.ConfigureAutoServices()
    // ... other configuration ...
    .WithValidation(ctx =>
    {
        var result = new ConfigurationValidationResult();
        
        // Check for required configuration in production
        if (ctx.Environment.IsProduction())
        {
            foreach (var requiredKey in new[] { "Database:ConnectionString", "Cache:ConnectionString" })
            {
                if (!ctx.HasConfigurationValue(requiredKey))
                    result.AddError($"Required configuration key '{requiredKey}' is missing in production");
            }
        }
        
        return result;
    })
    .Apply();
```

### 3. Make Environment Differences Explicit

Don't hide environment differences in complex conditions - make them explicit:

```csharp
// ✅ Clear environment-specific configuration
if (builder.Environment.IsDevelopment())
{
    services.ConfigureAutoServices()
        .FromCurrentDomain()
        .WithProfile("Development")
        .WithLogging(true)
        .Apply();
}
else if (builder.Environment.IsProduction())
{
    services.ConfigureAutoServices()
        .FromCurrentDomain()
        .WithProfile("Production")
        .WithPerformanceOptimizations(true)
        .WithLogging(false)
        .Apply();
}

// ❌ Complex nested conditions that are hard to understand
services.ConfigureAutoServices()
    .FromCurrentDomain()
    .WithProfile(ctx => ctx.Environment.IsDevelopment() ? "Dev" : "Prod")
    .WithLogging(ctx => !ctx.Environment.IsProduction())
    .Apply();
```

### 4. Use Comments to Explain Complex Logic

When you do need complex conditional logic, explain why:

```csharp
services.ConfigureAutoServices()
    .FromCurrentDomain()
    
    // We need different profiles for production high-availability setups
    // because they use different service implementations with clustering support
    .WithProfile(ctx => 
        ctx.Environment.IsProduction() && ctx.GetConfigValue<bool>("HighAvailability:Enabled")
            ? "ProductionHA"  // Clustered implementations
            : ctx.Environment.EnvironmentName) // Standard implementations
    
    // Only enable auto-discovery when feature flag is set to avoid
    // accidentally discovering services during maintenance windows
    .When("Features:EnableAutoDiscovery", "true")
    
    .Apply();
```

## 🔗 Next Steps

Fluent configuration provides the foundation for sophisticated service discovery setups. Now that you understand how to build complex configurations readably, explore these related topics:

1. **[Plugin Architecture](PluginArchitecture.md)** - Learn how to extend discovery with custom plugins
2. **[Performance Optimization](PerformanceOptimization.md)** - Understand how configuration choices affect performance
3. **[Expression-Based Conditions](ExpressionBasedConditions.md)** - Deep dive into the expression system used in `.When()` methods

The fluent configuration API transforms service discovery from a simple utility into a powerful application configuration language. Master these patterns, and you'll have the tools to handle even the most complex service discovery requirements while keeping your configuration readable and maintainable.