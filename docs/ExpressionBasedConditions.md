# Expression-Based Conditions Guide

Imagine you're setting up a smart home system where different devices should activate based on complex combinations of conditions: "Turn on the outdoor lights when it's dark AND someone is home AND it's after 6 PM AND the security system isn't in vacation mode." You could hard-code these rules, but that would make them inflexible and hard to modify. Instead, you'd want a system where you can express these complex logical conditions in a natural, readable way that can be easily understood and modified.

Expression-based conditions work exactly the same way for service registration - they allow you to express sophisticated conditional logic using familiar C# syntax while providing compile-time safety, IntelliSense support, and powerful access to runtime context information.

## 🎯 Understanding Expression-Based Conditions

Traditional string-based conditional registration works well for simple scenarios, but it has significant limitations when you need more sophisticated logic. Consider the evolution from simple to complex conditional requirements:

```csharp
// Simple: String-based condition
[ConditionalService("Features:EnableCaching", "true")]
public class SimpleCacheService : ICacheService { }

// Complex: Multiple string conditions (becomes unwieldy)
[ConditionalService("Features:EnableCaching", "true")]
[ConditionalService("Cache:Provider", "Redis")]
[ConditionalService("Environment", "Production")]
[ConditionalService("HighAvailability:Enabled", "true")]
public class ComplexCacheService : ICacheService { }

// Sophisticated: Expression-based condition (natural and powerful)
[ConditionalService(ctx => 
    ctx.FeatureEnabled("Caching") && 
    ctx.Environment.IsProduction() &&
    ctx.GetConfigValue<string>("Cache:Provider") == "Redis" &&
    ctx.GetConfigValue<bool>("HighAvailability:Enabled") &&
    ctx.GetConfigValue<int>("ExpectedUsers") > 1000)]
public class EnterpriseCacheService : ICacheService { }
```

The expression-based approach provides several key advantages:

- **Type Safety**: Compile-time checking ensures your conditions are syntactically correct
- **IntelliSense Support**: Full IDE support makes writing conditions faster and less error-prone
- **Complex Logic**: Natural support for AND, OR, NOT, and complex boolean expressions
- **Rich Context**: Access to environment, configuration, and custom business logic
- **Readability**: Conditions read like natural language and self-document their intent

## 🧠 The Conditional Context: Your Information Dashboard

The power of expression-based conditions comes from the rich context object that's available to your expressions. Think of this context as a comprehensive dashboard that provides access to all the information your conditional logic might need to make intelligent decisions.

### Understanding IConditionalContext

The `IConditionalContext` interface provides structured access to runtime information:

```csharp
public interface IConditionalContext
{
    IConfiguration? Configuration { get; }           // Application configuration
    IEnvironmentContext Environment { get; }         // Environment information  
    IFeatureFlagContext FeatureFlags { get; }       // Feature flag system
    Dictionary<string, object> CustomProperties { get; } // Application-specific data
    
    // Convenience methods for common operations
    bool FeatureEnabled(string featureName);
    T GetConfigValue<T>(string key, T defaultValue = default!);
    bool EvaluateCustomCondition(string conditionName);
}
```

Let's explore each component and understand how to use them effectively in your conditional expressions.

### Environment Context: Smart Environment Detection

The environment context provides intelligent, type-safe access to environment information:

```csharp
// Basic environment checks
[ConditionalService(ctx => ctx.Environment.IsDevelopment())]
public class DevLoggingService : ILoggingService { }

[ConditionalService(ctx => ctx.Environment.IsProduction())]
public class ProductionLoggingService : ILoggingService { }

// Multiple environment conditions
[ConditionalService(ctx => ctx.Environment.IsAnyOf("Staging", "PreProduction"))]
public class TestingService : ITestingService { }

// Custom environment logic
[ConditionalService(ctx => 
    ctx.Environment.IsProduction() && 
    ctx.Environment.Name.Contains("HighAvailability"))]
public class HAProductionService : IHighAvailabilityService { }
```

The environment context goes beyond simple string comparisons to provide semantic understanding of your deployment environments.

### Configuration Access: Type-Safe Configuration Reading

The configuration access provides type-safe, convenient access to your application settings:

```csharp
// Simple configuration checks with type safety
[ConditionalService(ctx => ctx.GetConfigValue<bool>("Features:EnableAdvancedLogging"))]
public class AdvancedLoggingService : ILoggingService { }

// Numeric configuration checks
[ConditionalService(ctx => ctx.GetConfigValue<int>("Performance:MaxConcurrentUsers") > 100)]
public class HighVolumeService : IHighVolumeService { }

// String configuration with fallback logic
[ConditionalService(ctx => 
    ctx.GetConfigValue<string>("Database:Provider", "SqlServer") == "PostgreSQL")]
public class PostgreSQLService : IDatabaseService { }

// Complex configuration logic
[ConditionalService(ctx => 
    ctx.GetConfigValue<TimeSpan>("Cache:TTL", TimeSpan.FromMinutes(5)).TotalMinutes > 60 &&
    ctx.GetConfigValue<string>("Cache:Provider") != "Memory")]
public class LongTermCacheService : ICacheService { }
```

Notice how the type-safe approach prevents common configuration errors and provides clear intent about what each condition is checking.

### Feature Flag Integration: Dynamic Feature Control

Feature flags enable dynamic control over service registration without requiring application restarts:

```csharp
// Simple feature flag checks
[ConditionalService(ctx => ctx.FeatureEnabled("NewPaymentSystem"))]
public class NewPaymentService : IPaymentService { }

// Complex feature flag combinations
[ConditionalService(ctx => 
    ctx.FeatureEnabled("BetaFeatures") && 
    !ctx.FeatureEnabled("MaintenanceMode") &&
    ctx.FeatureFlags.GetFlagValue<int>("MaxBetaUsers", 0) > 0)]
public class BetaService : IBetaService { }

// Feature flag with user context (advanced scenarios)
[ConditionalService(ctx => 
    ctx.FeatureFlags.IsEnabledFor("PremiumFeatures", new Dictionary<string, object>
    {
        ["Region"] = ctx.GetConfigValue<string>("Deployment:Region"),
        ["TenantType"] = "Enterprise"
    }))]
public class PremiumService : IPremiumService { }
```

Feature flags provide the foundation for sophisticated deployment strategies like canary releases, A/B testing, and gradual rollouts.

## 🔧 Building Complex Conditional Logic

Real-world applications often require sophisticated conditional logic that combines multiple types of checks. Expression-based conditions excel at expressing this complexity clearly and maintainably.

### Combining Multiple Condition Types

```csharp
// Comprehensive service registration condition
[ConditionalService(ctx => 
    // Environment requirements
    ctx.Environment.IsProduction() &&
    
    // Feature flag requirements  
    ctx.FeatureEnabled("AdvancedReporting") &&
    !ctx.FeatureEnabled("MaintenanceMode") &&
    
    // Configuration requirements
    ctx.GetConfigValue<string>("Database:Provider") == "PostgreSQL" &&
    ctx.GetConfigValue<int>("Reporting:MaxConcurrentReports", 0) > 10 &&
    
    // Custom business logic
    ctx.EvaluateCustomCondition("DatabaseHealthy") &&
    ctx.EvaluateCustomCondition("ExternalApiAvailable"))]
public class AdvancedReportingService : IReportingService
{
    // This service is only registered when ALL conditions are met:
    // 1. Running in production environment
    // 2. Advanced reporting feature is enabled
    // 3. System is not in maintenance mode
    // 4. PostgreSQL database is configured
    // 5. System is configured for high-volume reporting
    // 6. Database health check passes
    // 7. External API dependencies are available
}
```

This comprehensive example demonstrates how expression-based conditions can encode complex business rules that would be difficult or impossible to express with simple string-based checks.

### Conditional Logic Patterns

**Pattern 1: Progressive Feature Rollout**
```csharp
[ConditionalService(ctx => 
    ctx.FeatureEnabled("NewSearchEngine") &&
    (ctx.Environment.IsDevelopment() || 
     ctx.Environment.IsStaging() ||
     (ctx.Environment.IsProduction() && 
      ctx.GetConfigValue<double>("NewSearch:RolloutPercentage", 0.0) > 0.5)))]
public class NewSearchService : ISearchService
{
    // Enables progressive rollout: dev/staging always enabled,
    // production only when rollout percentage > 50%
}
```

**Pattern 2: Resource-Based Conditional Registration**
```csharp
[ConditionalService(ctx => 
    ctx.GetConfigValue<long>("System:AvailableMemoryMB", 0) > 2048 &&
    ctx.GetConfigValue<int>("System:CPUCores", 1) >= 4 &&
    ctx.GetConfigValue<bool>("Features:EnableHighPerformanceMode", false))]
public class HighPerformanceService : IHighPerformanceService
{
    // Only register when system has sufficient resources
    // and high-performance mode is explicitly enabled
}
```

**Pattern 3: Time-Based Conditional Registration**
```csharp
[ConditionalService(ctx => 
    ctx.Environment.IsProduction() &&
    ctx.GetConfigValue<DateTime>("Features:NewServiceActivationDate", DateTime.MaxValue) <= DateTime.UtcNow &&
    ctx.GetConfigValue<DateTime>("Features:NewServiceDeactivationDate", DateTime.MaxValue) > DateTime.UtcNow)]
public class ScheduledService : IScheduledService
{
    // Service is only active during a specific time window
    // Useful for limited-time features or scheduled migrations
}
```

## 🎨 Custom Conditional Extensions

For frequently used conditional patterns, you can create extension methods that make your expressions more readable and reusable.

### Creating Domain-Specific Extensions

```csharp
public static class ConditionalExtensions
{
    // Environment extensions for common patterns
    public static bool IsProductionOrStaging(this IEnvironmentContext environment)
    {
        return environment.IsProduction() || environment.IsStaging();
    }
    
    public static bool IsNonDevelopment(this IEnvironmentContext environment)
    {
        return !environment.IsDevelopment();
    }
    
    // Configuration extensions for business logic
    public static bool HasValidDatabaseConfiguration(this IConditionalContext ctx)
    {
        var connectionString = ctx.Configuration?.GetConnectionString("DefaultConnection");
        return !string.IsNullOrEmpty(connectionString) && 
               connectionString.Contains("Server=") &&
               !connectionString.Contains("localhost");
    }
    
    public static bool IsHighAvailabilityDeployment(this IConditionalContext ctx)
    {
        return ctx.Environment.IsProduction() &&
               ctx.GetConfigValue<bool>("HighAvailability:Enabled") &&
               ctx.GetConfigValue<int>("HighAvailability:MinInstances", 1) >= 2;
    }
    
    // Business rule extensions
    public static bool IsEnterpriseTenant(this IConditionalContext ctx)
    {
        var tenantType = ctx.GetConfigValue<string>("Tenant:Type", "Standard");
        var userCount = ctx.GetConfigValue<int>("Tenant:UserCount", 0);
        
        return tenantType == "Enterprise" || userCount > 1000;
    }
    
    public static bool SupportsAdvancedFeatures(this IConditionalContext ctx)
    {
        return ctx.IsEnterpriseTenant() &&
               ctx.FeatureEnabled("AdvancedFeatures") &&
               ctx.HasValidDatabaseConfiguration();
    }
}
```

Now your conditional expressions become much more readable and self-documenting:

```csharp
[ConditionalService(ctx => 
    ctx.Environment.IsProductionOrStaging() &&
    ctx.IsHighAvailabilityDeployment() &&
    ctx.SupportsAdvancedFeatures())]
public class EnterpriseHighAvailabilityService : IEnterpriseService
{
    // The condition now reads like a business requirement document
}
```

### Advanced Extension Patterns

**Pattern 1: Validation Extensions**
```csharp
public static class ValidationExtensions
{
    public static bool HasValidConfiguration<T>(this IConditionalContext ctx, string key, Func<T, bool> validator)
    {
        try
        {
            var value = ctx.GetConfigValue<T>(key);
            return value != null && validator(value);
        }
        catch
        {
            return false;
        }
    }
    
    public static bool HasAllRequiredConfiguration(this IConditionalContext ctx, params string[] requiredKeys)
    {
        return requiredKeys.All(key => !string.IsNullOrEmpty(ctx.Configuration?[key]));
    }
}

// Usage in conditions
[ConditionalService(ctx => 
    ctx.HasValidConfiguration<int>("MaxConnections", x => x > 0 && x <= 1000) &&
    ctx.HasAllRequiredConfiguration("Database:Host", "Database:Port", "Database:Name"))]
public class ValidatedDatabaseService : IDatabaseService { }
```

**Pattern 2: Performance-Aware Extensions**
```csharp
public static class PerformanceExtensions
{
    public static bool MeetsPerformanceRequirements(this IConditionalContext ctx)
    {
        var minMemory = ctx.GetConfigValue<long>("Performance:MinMemoryMB", 512);
        var minCpuCores = ctx.GetConfigValue<int>("Performance:MinCPUCores", 1);
        var actualMemory = GetAvailableMemory(); // Custom implementation
        var actualCores = Environment.ProcessorCount;
        
        return actualMemory >= minMemory && actualCores >= minCpuCores;
    }
    
    private static long GetAvailableMemory()
    {
        // Implementation would check actual system resources
        // This is a simplified version
        return GC.GetTotalMemory(false) / (1024 * 1024);
    }
}
```

## 🔍 Custom Conditional Context Creation

For advanced scenarios, you might need to extend the conditional context with application-specific information or business logic.

### Extending the Default Context

```csharp
public class CustomConditionalContext : ConditionalContext
{
    private readonly ITenantService _tenantService;
    private readonly ISystemHealthService _healthService;

    public CustomConditionalContext(
        IConfiguration? configuration, 
        string environmentName,
        ITenantService tenantService,
        ISystemHealthService healthService) 
        : base(configuration, environmentName)
    {
        _tenantService = tenantService;
        _healthService = healthService;
        
        RegisterCustomConditions();
    }

    private void RegisterCustomConditions()
    {
        // Register tenant-specific conditions
        RegisterCustomCondition("IsMultiTenant", () => _tenantService.IsMultiTenantDeployment());
        RegisterCustomCondition("HasActiveTenants", () => _tenantService.GetActiveTenantCount() > 0);
        
        // Register health-based conditions
        RegisterCustomCondition("DatabaseHealthy", () => _healthService.IsDatabaseHealthy());
        RegisterCustomCondition("ExternalServicesHealthy", () => _healthService.AreExternalServicesHealthy());
        RegisterCustomCondition("SystemUnderLoad", () => _healthService.IsSystemUnderHighLoad());
        
        // Register time-based conditions
        RegisterCustomCondition("BusinessHours", () => IsBusinessHours());
        RegisterCustomCondition("MaintenanceWindow", () => IsMaintenanceWindow());
    }

    private bool IsBusinessHours()
    {
        var now = DateTime.UtcNow;
        var businessStart = GetConfigValue<TimeSpan>("BusinessHours:Start", TimeSpan.FromHours(9));
        var businessEnd = GetConfigValue<TimeSpan>("BusinessHours:End", TimeSpan.FromHours(17));
        
        return now.TimeOfDay >= businessStart && now.TimeOfDay <= businessEnd;
    }

    private bool IsMaintenanceWindow()
    {
        var maintenanceStart = GetConfigValue<DateTime>("Maintenance:WindowStart", DateTime.MaxValue);
        var maintenanceEnd = GetConfigValue<DateTime>("Maintenance:WindowEnd", DateTime.MaxValue);
        var now = DateTime.UtcNow;
        
        return now >= maintenanceStart && now <= maintenanceEnd;
    }
}
```

### Using Custom Context in Discovery

```csharp
// Register the custom context
builder.Services.AddScoped<ITenantService, TenantService>();
builder.Services.AddScoped<ISystemHealthService, SystemHealthService>();

// Configure discovery to use custom context
builder.Services.AddAutoServices(options =>
{
    options.Configuration = builder.Configuration;
    options.Profile = builder.Environment.EnvironmentName;
    options.CustomContextFactory = (config, env) => new CustomConditionalContext(
        config, 
        env, 
        serviceProvider.GetRequiredService<ITenantService>(),
        serviceProvider.GetRequiredService<ISystemHealthService>());
});
```

Now you can use the custom conditions in your expressions:

```csharp
[ConditionalService(ctx => 
    ctx.Environment.IsProduction() &&
    ctx.EvaluateCustomCondition("IsMultiTenant") &&
    ctx.EvaluateCustomCondition("DatabaseHealthy") &&
    !ctx.EvaluateCustomCondition("MaintenanceWindow"))]
public class ProductionMultiTenantService : IMultiTenantService { }
```

## 🧪 Testing Expression-Based Conditions

Testing complex conditional logic requires strategies that can validate the conditions without requiring full application startup.

### Unit Testing Conditional Logic

```csharp
[TestClass]
public class ConditionalExpressionTests
{
    [TestMethod]
    public void EnterpriseService_ShouldRegister_WhenAllConditionsMet()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new[]
            {
                new KeyValuePair<string, string>("Features:Enterprise", "true"),
                new KeyValuePair<string, string>("Tenant:Type", "Enterprise"),
                new KeyValuePair<string, string>("Tenant:UserCount", "2000"),
                new KeyValuePair<string, string>("HighAvailability:Enabled", "true"),
                new KeyValuePair<string, string>("HighAvailability:MinInstances", "3")
            })
            .Build();

        var context = new ConditionalContext(configuration, "Production");

        // Act - Extract the condition logic for testing
        var condition = (IConditionalContext ctx) => 
            ctx.Environment.IsProduction() &&
            ctx.FeatureEnabled("Enterprise") &&
            ctx.GetConfigValue<string>("Tenant:Type") == "Enterprise" &&
            ctx.GetConfigValue<int>("Tenant:UserCount", 0) > 1000 &&
            ctx.GetConfigValue<bool>("HighAvailability:Enabled") &&
            ctx.GetConfigValue<int>("HighAvailability:MinInstances", 1) >= 2;

        var result = condition(context);

        // Assert
        Assert.IsTrue(result, "Enterprise service should be registered when all conditions are met");
    }

    [TestMethod]
    public void EnterpriseService_ShouldNotRegister_WhenUserCountTooLow()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new[]
            {
                new KeyValuePair<string, string>("Features:Enterprise", "true"),
                new KeyValuePair<string, string>("Tenant:Type", "Enterprise"),
                new KeyValuePair<string, string>("Tenant:UserCount", "500"), // Too low
                new KeyValuePair<string, string>("HighAvailability:Enabled", "true"),
                new KeyValuePair<string, string>("HighAvailability:MinInstances", "3")
            })
            .Build();

        var context = new ConditionalContext(configuration, "Production");

        // Act
        var condition = (IConditionalContext ctx) => 
            ctx.Environment.IsProduction() &&
            ctx.FeatureEnabled("Enterprise") &&
            ctx.GetConfigValue<string>("Tenant:Type") == "Enterprise" &&
            ctx.GetConfigValue<int>("Tenant:UserCount", 0) > 1000 && // This should fail
            ctx.GetConfigValue<bool>("HighAvailability:Enabled") &&
            ctx.GetConfigValue<int>("HighAvailability:MinInstances", 1) >= 2;

        var result = condition(context);

        // Assert
        Assert.IsFalse(result, "Enterprise service should not be registered when user count is too low");
    }
}
```

### Integration Testing with Conditional Services

```csharp
[TestClass]
public class ConditionalServiceIntegrationTests
{
    [TestMethod]
    public void ServiceDiscovery_ShouldRegisterCorrectServices_BasedOnConfiguration()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new[]
            {
                new KeyValuePair<string, string>("Environment", "Production"),
                new KeyValuePair<string, string>("Features:AdvancedLogging", "true"),
                new KeyValuePair<string, string>("Features:BasicLogging", "false"),
                new KeyValuePair<string, string>("Performance:MaxUsers", "5000")
            })
            .Build();

        var services = new ServiceCollection();

        // Act
        services.AddAutoServices(options =>
        {
            options.Configuration = configuration;
            options.Profile = "Production";
        });

        var serviceProvider = services.BuildServiceProvider();

        // Assert
        var loggingService = serviceProvider.GetService<ILoggingService>();
        Assert.IsNotNull(loggingService, "Logging service should be registered");
        Assert.IsInstanceOfType(loggingService, typeof(AdvancedLoggingService), 
            "Advanced logging service should be registered based on configuration");

        var highPerformanceService = serviceProvider.GetService<IHighPerformanceService>();
        Assert.IsNotNull(highPerformanceService, 
            "High performance service should be registered when max users > 1000");
    }
}
```

### Testing Custom Conditional Extensions

```csharp
[TestClass]
public class CustomConditionalExtensionsTests
{
    [TestMethod]
    public void HasValidDatabaseConfiguration_ShouldReturnTrue_WhenConnectionStringIsValid()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new[]
            {
                new KeyValuePair<string, string>("ConnectionStrings:DefaultConnection", 
                    "Server=prod-db.company.com;Database=MyApp;Trusted_Connection=true;")
            })
            .Build();

        var context = new ConditionalContext(configuration, "Production");

        // Act
        var result = context.HasValidDatabaseConfiguration();

        // Assert
        Assert.IsTrue(result, "Should return true for valid production database connection string");
    }

    [TestMethod]
    public void HasValidDatabaseConfiguration_ShouldReturnFalse_WhenConnectionStringIsLocalhost()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new[]
            {
                new KeyValuePair<string, string>("ConnectionStrings:DefaultConnection", 
                    "Server=localhost;Database=MyApp;Trusted_Connection=true;")
            })
            .Build();

        var context = new ConditionalContext(configuration, "Production");

        // Act
        var result = context.HasValidDatabaseConfiguration();

        // Assert
        Assert.IsFalse(result, "Should return false for localhost connection string in production");
    }
}
```

## 🎯 Best Practices for Expression-Based Conditions

Based on extensive experience with expression-based conditions in production applications, these practices lead to maintainable, reliable conditional logic:

### 1. Keep Expressions Readable

Complex logic should be broken down into understandable pieces:

```csharp
// ❌ Hard to understand - too complex in one expression
[ConditionalService(ctx => 
    (ctx.Environment.IsProduction() && ctx.GetConfigValue<bool>("HA:Enabled") && ctx.GetConfigValue<int>("HA:MinInstances") >= 2) ||
    (ctx.Environment.IsStaging() && ctx.GetConfigValue<bool>("Staging:SimulateHA")) ||
    (ctx.Environment.IsDevelopment() && ctx.GetConfigValue<bool>("Dev:TestHA") && DateTime.UtcNow.Hour >= 9))]

// ✅ Clear and readable - uses extension methods
[ConditionalService(ctx => 
    ctx.IsHighAvailabilityEnvironment() ||
    ctx.IsStagingWithHASimulation() ||
    ctx.IsDevelopmentDuringBusinessHours())]
public class HighAvailabilityService : IHighAvailabilityService { }
```

### 2. Use Type-Safe Configuration Access

Always prefer type-safe configuration access with appropriate defaults:

```csharp
// ❌ String-based access without defaults
[ConditionalService(ctx => 
    int.Parse(ctx.Configuration["MaxUsers"] ?? "0") > 1000)]

// ✅ Type-safe access with sensible defaults
[ConditionalService(ctx => 
    ctx.GetConfigValue<int>("Performance:MaxUsers", 100) > 1000)]
public class HighVolumeService : IHighVolumeService { }
```

### 3. Handle Edge Cases Gracefully

Design your conditions to handle missing configuration or unexpected values:

```csharp
// ✅ Robust condition handling
[ConditionalService(ctx => 
{
    try
    {
        var connectionString = ctx.Configuration?.GetConnectionString("Database");
        var isValidConnection = !string.IsNullOrEmpty(connectionString) && 
                               connectionString.Contains("Server=") &&
                               !connectionString.Contains("localhost");
        
        var isProductionEnvironment = ctx.Environment.IsProduction();
        var hasRequiredFeature = ctx.FeatureEnabled("DatabaseIntegration");
        
        return isValidConnection && isProductionEnvironment && hasRequiredFeature;
    }
    catch
    {
        // If condition evaluation fails, don't register the service
        return false;
    }
})]
public class DatabaseIntegratedService : IDatabaseService { }
```

### 4. Document Complex Business Logic

When conditions encode important business rules, document them clearly:

```csharp
/// <summary>
/// Payment processing service that is only enabled when:
/// 1. Running in production environment (security requirement)
/// 2. Payment gateway feature is enabled (business requirement)
/// 3. PCI compliance is configured (regulatory requirement)
/// 4. Expected transaction volume justifies overhead (performance requirement)
/// </summary>
[ConditionalService(ctx => 
    ctx.Environment.IsProduction() &&                           // Security: only in production
    ctx.FeatureEnabled("PaymentGateway") &&                    // Business: feature must be enabled
    ctx.GetConfigValue<bool>("PCI:ComplianceConfigured") &&    // Regulatory: PCI compliance required
    ctx.GetConfigValue<int>("Expected:DailyTransactions") > 100)] // Performance: minimum volume
public class PaymentProcessingService : IPaymentService { }
```

## 🔗 Integration with Other Features

Expression-based conditions integrate seamlessly with other features of the discovery system, creating powerful combinations for sophisticated applications.

### Integration with Performance Optimization

```csharp
// Conditions are cached and evaluated efficiently
builder.Services.AddAutoServicesWithPerformanceOptimizations(options =>
{
    options.EnablePerformanceOptimizations = true;
    options.Configuration = builder.Configuration;
    options.Profile = builder.Environment.EnvironmentName;
});
```

### Integration with Plugin Architecture

```csharp
public class ConditionalPlugin : IServiceDiscoveryPlugin
{
    public IEnumerable<ServiceRegistrationInfo> DiscoverServices(Assembly assembly, AutoServiceOptions options)
    {
        // Plugins can use the same conditional context for their own logic
        var context = CreateConditionalContext(options);
        
        if (!context.FeatureEnabled("CustomDiscovery"))
        {
            return Enumerable.Empty<ServiceRegistrationInfo>();
        }
        
        // Continue with plugin-specific discovery...
    }
}
```

### Integration with Fluent Configuration

```csharp
builder.Services.ConfigureAutoServices()
    .FromCurrentDomain()
    .WithProfile("Production")
    .When(ctx => 
        ctx.Environment.IsProduction() &&
        ctx.FeatureEnabled("AutoDiscovery") &&
        ctx.GetConfigValue<bool>("Systems:AllHealthy", false))
    .Apply();
```

## 🎓 Advanced Expression Patterns

For sophisticated applications, these advanced patterns provide additional power and flexibility:

### Pattern 1: Dynamic Condition Composition

```csharp
public static class DynamicConditions
{
    public static Expression<Func<IConditionalContext, bool>> BuildDynamicCondition(
        params (string feature, bool required)[] featureRequirements)
    {
        return ctx => featureRequirements.All(req => 
            ctx.FeatureEnabled(req.feature) == req.required);
    }
}

// Usage
var dynamicCondition = DynamicConditions.BuildDynamicCondition(
    ("AdvancedReporting", true),
    ("MaintenanceMode", false),
    ("BetaFeatures", true));

[ConditionalService(dynamicCondition)]
public class DynamicallyConfiguredService : IService { }
```

### Pattern 2: Conditional Service Factories

```csharp
[ConditionalService(ctx => 
    ctx.Environment.IsProduction() &&
    ctx.GetConfigValue<string>("MessageQueue:Provider") != null)]
[ServiceRegistration(ServiceLifetime.Singleton)]
public class MessageQueueFactory : IMessageQueueFactory
{
    public IMessageQueue CreateQueue(IConditionalContext context)
    {
        var provider = context.GetConfigValue<string>("MessageQueue:Provider");
        
        return provider?.ToLower() switch
        {
            "rabbitmq" when context.FeatureEnabled("RabbitMQ") => new RabbitMQQueue(),
            "azureservicebus" when context.FeatureEnabled("AzureServiceBus") => new AzureServiceBusQueue(),
            "inmemory" when context.Environment.IsDevelopment() => new InMemoryQueue(),
            _ => throw new InvalidOperationException($"Unsupported message queue provider: {provider}")
        };
    }
}
```

## 🔗 Next Steps

Expression-based conditions provide the foundation for sophisticated, maintainable conditional service registration. Now that you understand how to create and use complex conditional logic, explore these related topics:

1. **[Fluent Configuration](FluentConfiguration.md)** - Learn how expression-based conditions integrate with fluent configuration
2. **[Plugin Development](PluginDevelopment.md)** - Use conditional logic in custom discovery plugins
3. **[System Architecture](SystemArchitecture.md)** - Understand how conditional logic fits into the overall system design

Expression-based conditions transform simple service registration into an intelligent, context-aware system that can adapt to complex deployment requirements while maintaining readable, maintainable code. Master these concepts, and you'll have powerful tools for creating flexible, environment-aware service architectures.