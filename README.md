# FS.AutoServiceDiscovery.Extensions

[![NuGet Version](https://img.shields.io/nuget/v/FS.AutoServiceDiscovery.Extensions.svg)](https://www.nuget.org/packages/FS.AutoServiceDiscovery.Extensions)
[![NuGet Downloads](https://img.shields.io/nuget/dt/FS.AutoServiceDiscovery.Extensions.svg)](https://www.nuget.org/packages/FS.AutoServiceDiscovery.Extensions)
[![GitHub License](https://img.shields.io/github/license/furkansarikaya/FS.AutoServiceDiscovery)](https://github.com/furkansarikaya/FS.AutoServiceDiscovery/blob/main/LICENSE)
[![GitHub Stars](https://img.shields.io/github/stars/furkansarikaya/FS.AutoServiceDiscovery.svg)](https://github.com/furkansarikaya/FS.AutoServiceDiscovery/stargazers)

A powerful .NET 9.0 library that transforms how you handle dependency injection in your applications. Think of it as having an intelligent assistant that automatically discovers and registers your services, eliminating the tedious boilerplate code that traditionally clutters your startup configuration.

Imagine you're organizing a large conference where hundreds of speakers need to be registered. Instead of manually registering each speaker one by one, you could have a smart system that automatically discovers speakers based on certain criteria and registers them appropriately. That's exactly what this library does for your .NET services.

## Why This Library Exists

In traditional .NET applications, setting up dependency injection often looks like this repetitive pattern:

```csharp
services.AddScoped<IUserService, UserService>();
services.AddScoped<IProductService, ProductService>();
services.AddScoped<IOrderService, OrderService>();
services.AddSingleton<ICacheService, CacheService>();
// ... and this continues for dozens or hundreds of services
```

This approach has several problems. First, it's incredibly repetitive and error-prone. Second, you often forget to register new services when you create them. Third, managing different lifetimes and configurations becomes increasingly complex as your application grows. Finally, in large enterprise applications, this manual registration process can significantly impact startup performance.

Our library solves these challenges by implementing a convention-based approach where your services tell the system how they want to be registered, rather than requiring you to configure everything manually in a central location.

## Getting Started: Your First Steps

Let's begin your journey with the most basic usage and gradually build up to advanced scenarios. Think of this as learning to drive - we'll start with understanding the basics before moving to highway driving.

### Installation

```bash
dotnet add package FS.AutoServiceDiscovery.Extensions
```

### Understanding the Core Concept

The fundamental idea behind this library is simple: instead of manually registering services, you mark them with attributes that describe how they should be registered. It's like putting labels on boxes that tell a warehouse worker where each box should be stored.

### Your First Service Registration

Let's start with the simplest possible example. Imagine you have a service that handles user operations:

```csharp
// First, define your service interface
public interface IUserService
{
    Task<User> GetUserAsync(int id);
    Task<User> CreateUserAsync(User user);
}

// Then, implement your service with the registration attribute
[ServiceRegistration(ServiceLifetime.Scoped)]
public class UserService : IUserService
{
    public async Task<User> GetUserAsync(int id)
    {
        // Your implementation here
        return new User { Id = id, Name = "Sample User" };
    }
    
    public async Task<User> CreateUserAsync(User user)
    {
        // Your implementation here
        return user;
    }
}
```

Notice how we've added the `[ServiceRegistration(ServiceLifetime.Scoped)]` attribute to our class. This single line tells the system "I want to be registered as a scoped service in the dependency injection container."

Now, instead of manually registering this service in your Program.cs or Startup.cs, you simply add one line:

```csharp
// In Program.cs (for .NET 6+ applications)
var builder = WebApplication.CreateBuilder(args);

// This single line automatically discovers and registers all attributed services
builder.Services.AddAutoServices();

var app = builder.Build();
```

The magic happens behind the scenes. The library scans your assemblies, finds classes marked with the `ServiceRegistration` attribute, and automatically registers them with the appropriate service interfaces and lifetimes.

## Understanding Service Lifetimes

Before we dive deeper, let's make sure you understand the three service lifetimes available in .NET dependency injection, as choosing the right lifetime is crucial for both performance and correctness.

**Transient Services** (`ServiceLifetime.Transient`) are created every time they're requested. Think of these like disposable cups at a coffee shop - you get a new one each time you order. Use this for lightweight, stateless services.

```csharp
[ServiceRegistration(ServiceLifetime.Transient)]
public class EmailService : IEmailService
{
    // This service will be created fresh every time it's injected
    public async Task SendEmailAsync(string to, string subject, string body)
    {
        // Implementation
    }
}
```

**Scoped Services** (`ServiceLifetime.Scoped`) are created once per request (in web applications) or once per scope in other application types. Think of these like a rental car - you get one for the duration of your trip, but the next customer gets a different car. This is perfect for services that maintain state during a single operation but shouldn't be shared across different operations.

```csharp
[ServiceRegistration(ServiceLifetime.Scoped)]
public class DatabaseContext : DbContext, IDatabaseContext
{
    // This service will be the same instance throughout a single web request
    // but different requests will get different instances
}
```

**Singleton Services** (`ServiceLifetime.Singleton`) are created once for the entire application lifetime. Think of these like the building's elevator - there's only one, and everyone shares it. Use this for expensive-to-create services or services that need to maintain state across the entire application.

```csharp
[ServiceRegistration(ServiceLifetime.Singleton)]
public class CacheService : ICacheService
{
    private readonly ConcurrentDictionary<string, object> _cache = new();
    
    // This instance will be shared across the entire application
    public T Get<T>(string key) => (T)_cache.GetValueOrDefault(key);
    public void Set<T>(string key, T value) => _cache[key] = value;
}
```

## How Service Type Resolution Works

Understanding how the library determines which interface to register your service with is important for avoiding confusion. The system uses a smart convention-based approach with several fallback strategies.

**Primary Convention**: The library first looks for an interface that follows the pattern `I{ClassName}`. For example, if you have a class named `UserService`, it will look for an interface named `IUserService`.

```csharp
[ServiceRegistration(ServiceLifetime.Scoped)]
public class UserService : IUserService, IDisposable
{
    // Will be registered as IUserService -> UserService
    // IDisposable is ignored because it starts with "System."
}
```

**Single Interface Fallback**: If the naming convention doesn't match but your class implements exactly one non-system interface, that interface will be used.

```csharp
[ServiceRegistration(ServiceLifetime.Scoped)]
public class CustomerManager : ICustomerOperations
{
    // Will be registered as ICustomerOperations -> CustomerManager
    // because it's the only non-system interface
}
```

**Explicit Type Specification**: When you need complete control, you can explicitly specify which type to register.

```csharp
[ServiceRegistration(ServiceLifetime.Scoped, ServiceType = typeof(ISpecificInterface))]
public class MultiInterfaceService : ISpecificInterface, IDisposable, IAnotherInterface
{
    // Will be registered as ISpecificInterface -> MultiInterfaceService
    // regardless of other interfaces or naming conventions
}
```

**Concrete Type Registration**: If no suitable interface is found, the service will be registered as its concrete type.

```csharp
[ServiceRegistration(ServiceLifetime.Scoped)]
public class StandaloneService
{
    // Will be registered as StandaloneService -> StandaloneService
    // Useful for services that don't need interface abstraction
}
```

## Controlling Registration Order

In complex applications, the order in which services are registered can matter. Some services might depend on others being registered first, or you might want to ensure that decorators are registered after the services they decorate.

```csharp
[ServiceRegistration(ServiceLifetime.Singleton, Order = 1)]
public class DatabaseConnectionService : IDatabaseConnectionService
{
    // This will be registered first
}

[ServiceRegistration(ServiceLifetime.Scoped, Order = 2)]
public class UserRepository : IUserRepository
{
    public UserRepository(IDatabaseConnectionService dbConnection)
    {
        // This depends on DatabaseConnectionService, so it's registered after
    }
}

[ServiceRegistration(ServiceLifetime.Scoped, Order = 3)]
public class CachedUserRepository : IUserRepository
{
    private readonly IUserRepository _inner;
    
    public CachedUserRepository(IUserRepository inner)
    {
        _inner = inner; // This decorates UserRepository
    }
}
```

The `Order` property ensures that services are registered in the correct sequence, preventing dependency resolution issues.

## Environment-Specific Registration with Profiles

Real-world applications often need different implementations of the same service in different environments. For example, you might want to use a real email service in production but a mock email service during development.

```csharp
// Development implementation
[ServiceRegistration(ServiceLifetime.Scoped, Profile = "Development")]
public class MockEmailService : IEmailService
{
    public async Task SendEmailAsync(string to, string subject, string body)
    {
        Console.WriteLine($"Mock: Sending email to {to} with subject: {subject}");
        // No actual email is sent in development
    }
}

// Production implementation  
[ServiceRegistration(ServiceLifetime.Scoped, Profile = "Production")]
public class SmtpEmailService : IEmailService
{
    public async Task SendEmailAsync(string to, string subject, string body)
    {
        // Real SMTP implementation
        // This will only be registered in production
    }
}
```

To activate profile-based registration, configure the profile in your startup code:

```csharp
builder.Services.AddAutoServices(options =>
{
    options.Profile = builder.Environment.EnvironmentName; // "Development", "Production", etc.
});
```

This approach allows you to maintain different service implementations for different environments without cluttering your main registration code with conditional logic.

## Feature Flag Based Registration

Modern applications often use feature flags to enable or disable functionality based on configuration. This library supports conditional service registration based on configuration values.

```csharp
// This service will only be registered if the feature flag is enabled
[ConditionalService("FeatureFlags:EnableAdvancedAnalytics", "true")]
[ServiceRegistration(ServiceLifetime.Scoped)]
public class AdvancedAnalyticsService : IAnalyticsService
{
    // Advanced analytics implementation
}

// Fallback service that's always available
[ServiceRegistration(ServiceLifetime.Scoped)]
public class BasicAnalyticsService : IAnalyticsService
{
    // Basic analytics implementation
}
```

You can even have multiple conditions that must all be met:

```csharp
[ConditionalService("FeatureFlags:EnableEmailNotifications", "true")]
[ConditionalService("Email:Provider", "SendGrid")]
[ServiceRegistration(ServiceLifetime.Scoped)]
public class SendGridEmailService : IEmailService
{
    // This will only be registered if both conditions are met
}
```

To use conditional registration, pass your configuration to the discovery process:

```csharp
builder.Services.AddAutoServices(options =>
{
    options.Configuration = builder.Configuration;
});
```

## Test Environment Considerations

Testing is a crucial part of software development, and this library provides specific support for test scenarios. Some services might be appropriate for production but should be excluded during testing.

```csharp
[ServiceRegistration(ServiceLifetime.Singleton, IgnoreInTests = true)]
public class BackgroundTaskService : IBackgroundTaskService
{
    // This service performs background work that might interfere with tests
    // It will be automatically excluded when IsTestEnvironment = true
}

[ServiceRegistration(ServiceLifetime.Scoped)]
public class TestFriendlyService : ITestFriendlyService
{
    // This service works well in both production and test environments
}
```

In your test setup, configure the system to recognize the test environment:

```csharp
// In test setup
services.AddAutoServices(options =>
{
    options.IsTestEnvironment = true;
    options.EnableLogging = false; // Reduce noise in test output
});
```

## Performance Optimizations: Taking It to the Next Level

Now that you understand the basic concepts, let's explore the performance optimizations that make this library suitable for large-scale applications. Think of this as upgrading from a bicycle to a sports car - the basic transportation function is the same, but the performance characteristics are dramatically different.

### Understanding the Performance Challenge

In large applications, assembly scanning can become a performance bottleneck. Imagine having to read through thousands of books every time you start your application just to create a catalog of what's available. The performance optimizations in this library address this challenge through several strategies:

**Assembly Caching** remembers what was found in each assembly so subsequent scans can skip the expensive reflection operations. It's like keeping a card catalog that gets updated only when books are added or changed.

**Parallel Processing** scans multiple assemblies simultaneously on multi-core systems, dramatically reducing the time needed for discovery in applications with many assemblies.

**Type Metadata Caching** remembers the characteristics of types that have been examined, avoiding repeated reflection operations on the same types.

### Enabling Performance Optimizations

The simplest way to enable performance optimizations is to use the dedicated method:

```csharp
builder.Services.AddAutoServicesWithPerformanceOptimizations(options =>
{
    options.Profile = builder.Environment.EnvironmentName;
    options.Configuration = builder.Configuration;
    options.EnableLogging = true; // See performance statistics
}, Assembly.GetExecutingAssembly());
```

This automatically enables caching, parallel processing, and other optimizations that can reduce startup time by 50-70% in large applications.

### Advanced Performance Configuration

For applications with specific performance requirements, you can fine-tune the optimization behavior:

```csharp
builder.Services.AddAutoServices(options =>
{
    // Enable performance optimizations
    options.EnablePerformanceOptimizations = true;
    
    // Control parallel processing
    options.EnableParallelProcessing = true;
    options.MaxDegreeOfParallelism = Environment.ProcessorCount;
    
    // Enable detailed performance metrics
    options.EnablePerformanceMetrics = true;
    
    // Other configuration
    options.Profile = builder.Environment.EnvironmentName;
    options.Configuration = builder.Configuration;
});
```

### Monitoring Cache Performance

Understanding how well your cache is performing is crucial for optimization. You can access cache statistics to monitor effectiveness:

```csharp
// After application startup, you can check cache performance
var stats = PerformanceServiceCollectionExtensions.GetCacheStatistics();

Console.WriteLine($"Cache Hit Ratio: {stats.HitRatio:F1}%");
Console.WriteLine($"Total Requests: {stats.TotalRequests}");
Console.WriteLine($"Cached Assemblies: {stats.CachedAssembliesCount}");
Console.WriteLine($"Total Cached Services: {stats.TotalCachedServices}");
```

In production applications, you might want to export these metrics to monitoring systems like Application Insights or Prometheus for ongoing performance tracking.

### When Performance Optimizations Matter Most

Performance optimizations provide the most benefit in these scenarios:

**Large Enterprise Applications** with dozens of assemblies and hundreds of services see dramatic startup time improvements.

**Microservice Architectures** where fast startup time is crucial for scaling and deployment strategies benefit significantly from caching.

**Development Environments** where applications are frequently restarted during development cycles experience much faster iteration times.

**Automated Testing Scenarios** where test applications are created and destroyed frequently see substantial time savings in test suite execution.

## Real-World Usage Patterns and Examples

Let's explore how this library solves common real-world scenarios that you're likely to encounter in production applications.

### Repository Pattern Implementation

The repository pattern is commonly used in enterprise applications for data access abstraction:

```csharp
// Base repository interface
public interface IRepository<T> where T : class
{
    Task<T?> GetByIdAsync(int id);
    Task<IEnumerable<T>> GetAllAsync();
    Task<T> AddAsync(T entity);
    Task UpdateAsync(T entity);
    Task DeleteAsync(int id);
}

// Generic repository implementation
[ServiceRegistration(ServiceLifetime.Scoped)]
public class Repository<T> : IRepository<T> where T : class
{
    private readonly DbContext _context;
    
    public Repository(DbContext context)
    {
        _context = context;
    }
    
    // Implementation details...
}

// Specific repository with additional methods
[ServiceRegistration(ServiceLifetime.Scoped)]
public class UserRepository : Repository<User>, IUserRepository
{
    public UserRepository(DbContext context) : base(context) { }
    
    public async Task<User?> GetByEmailAsync(string email)
    {
        // User-specific query implementation
    }
}
```

### Service Layer with Business Logic

Business services often coordinate between multiple repositories and implement domain logic:

```csharp
[ServiceRegistration(ServiceLifetime.Scoped, Order = 2)]
public class OrderService : IOrderService
{
    private readonly IUserRepository _userRepository;
    private readonly IProductRepository _productRepository;
    private readonly IEmailService _emailService;
    
    public OrderService(
        IUserRepository userRepository,
        IProductRepository productRepository,
        IEmailService emailService)
    {
        _userRepository = userRepository;
        _productRepository = productRepository;
        _emailService = emailService;
    }
    
    public async Task<Order> CreateOrderAsync(CreateOrderRequest request)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId);
        var product = await _productRepository.GetByIdAsync(request.ProductId);
        
        // Business logic here
        var order = new Order { /* initialization */ };
        
        await _emailService.SendEmailAsync(user.Email, "Order Confirmation", "...");
        
        return order;
    }
}
```

### Decorator Pattern for Cross-Cutting Concerns

Decorators are useful for adding cross-cutting concerns like logging, caching, or validation:

```csharp
// Base service
[ServiceRegistration(ServiceLifetime.Scoped, Order = 1)]
public class ProductService : IProductService
{
    public async Task<Product> GetProductAsync(int id)
    {
        // Core product retrieval logic
    }
}

// Caching decorator
[ServiceRegistration(ServiceLifetime.Scoped, Order = 2, ServiceType = typeof(IProductService))]
public class CachedProductService : IProductService
{
    private readonly IProductService _inner;
    private readonly ICacheService _cache;
    
    public CachedProductService(IProductService inner, ICacheService cache)
    {
        _inner = inner;
        _cache = cache;
    }
    
    public async Task<Product> GetProductAsync(int id)
    {
        var cacheKey = $"product_{id}";
        var cached = _cache.Get<Product>(cacheKey);
        if (cached != null) return cached;
        
        var product = await _inner.GetProductAsync(id);
        _cache.Set(cacheKey, product, TimeSpan.FromMinutes(15));
        return product;
    }
}
```

### Background Services and Hosted Services

For applications that need background processing:

```csharp
[ServiceRegistration(ServiceLifetime.Singleton)]
public class EmailQueueProcessor : BackgroundService, IHostedService
{
    private readonly IServiceScopeFactory _scopeFactory;
    
    public EmailQueueProcessor(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            using var scope = _scopeFactory.CreateScope();
            var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();
            
            // Process email queue
            await Task.Delay(1000, stoppingToken);
        }
    }
}
```

### Multi-Tenant Applications

For applications serving multiple tenants with different configurations:

```csharp
[ServiceRegistration(ServiceLifetime.Scoped, Profile = "MultiTenant")]
public class TenantAwareUserService : IUserService
{
    private readonly ITenantProvider _tenantProvider;
    private readonly IUserRepository _userRepository;
    
    public TenantAwareUserService(ITenantProvider tenantProvider, IUserRepository userRepository)
    {
        _tenantProvider = tenantProvider;
        _userRepository = userRepository;
    }
    
    public async Task<User> GetUserAsync(int id)
    {
        var tenantId = _tenantProvider.GetCurrentTenantId();
        return await _userRepository.GetUserByIdAndTenantAsync(id, tenantId);
    }
}

[ServiceRegistration(ServiceLifetime.Scoped, Profile = "SingleTenant")]
public class StandardUserService : IUserService
{
    private readonly IUserRepository _userRepository;
    
    public StandardUserService(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }
    
    public async Task<User> GetUserAsync(int id)
    {
        return await _userRepository.GetByIdAsync(id);
    }
}
```

## Troubleshooting Common Issues

Even with the best documentation, you might encounter issues. Let's address the most common problems and their solutions.

### Services Not Being Discovered

If your services aren't being registered, the most common cause is that the assembly containing them isn't being scanned. By default, only the calling assembly is scanned:

```csharp
// Problem: Services in other assemblies won't be found
services.AddAutoServices();

// Solution: Explicitly specify all assemblies to scan
services.AddAutoServices(
    Assembly.GetExecutingAssembly(),          // Current assembly
    Assembly.GetAssembly(typeof(UserService)), // Assembly containing UserService
    typeof(ProductService).Assembly            // Assembly containing ProductService
);
```

### Multiple Services Registered for Same Interface

When you have multiple implementations of the same interface, the last one registered typically wins:

```csharp
// Both of these implement IEmailService
[ServiceRegistration(ServiceLifetime.Scoped)]
public class SmtpEmailService : IEmailService { }

[ServiceRegistration(ServiceLifetime.Scoped)]
public class SendGridEmailService : IEmailService { }

// Solution: Use profiles or conditional registration
[ServiceRegistration(ServiceLifetime.Scoped, Profile = "Smtp")]
public class SmtpEmailService : IEmailService { }

[ServiceRegistration(ServiceLifetime.Scoped, Profile = "SendGrid")]
public class SendGridEmailService : IEmailService { }
```

### Conditional Services Not Working

If conditional services aren't behaving as expected, ensure you're passing the configuration:

```csharp
// Problem: Configuration not provided
services.AddAutoServices(); // Conditional attributes are ignored

// Solution: Provide configuration
services.AddAutoServices(options =>
{
    options.Configuration = builder.Configuration; // Required for conditional services
});
```

### Performance Issues During Startup

If you're experiencing slow startup times, enable performance optimizations:

```csharp
// Problem: Using basic scanning in large applications
services.AddAutoServices();

// Solution: Enable performance optimizations
services.AddAutoServicesWithPerformanceOptimizations();

// Or configure specific optimizations
services.AddAutoServices(options =>
{
    options.EnablePerformanceOptimizations = true;
    options.EnableParallelProcessing = true;
});
```

### Cache Not Working in Development

During development, if you're not seeing cache benefits, it might be because assemblies are being rebuilt frequently:

```csharp
// Enable logging to see cache statistics
services.AddAutoServicesWithPerformanceOptimizations(options =>
{
    options.EnableLogging = true; // See cache hit/miss information
});
```

## Testing Your Auto-Discovered Services

Testing applications that use auto-discovery requires some special considerations to ensure test isolation and reliability.

### Unit Testing Individual Services

Unit testing your services works exactly as before since the auto-discovery doesn't change how your services function:

```csharp
[Test]
public async Task UserService_GetUserAsync_ReturnsUser()
{
    // Arrange
    var mockRepository = new Mock<IUserRepository>();
    var userService = new UserService(mockRepository.Object);
    
    // Act & Assert
    var result = await userService.GetUserAsync(123);
    Assert.IsNotNull(result);
}
```

### Integration Testing with Auto-Discovery

For integration tests, you might want to test that your services are being discovered and registered correctly:

```csharp
[Test]
public void AutoDiscovery_Should_Register_All_Expected_Services()
{
    // Arrange
    var services = new ServiceCollection();
    var configuration = new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string>
        {
            ["FeatureFlags:EnableEmailService"] = "true"
        })
        .Build();

    // Act
    services.AddAutoServices(options =>
    {
        options.Configuration = configuration;
        options.IsTestEnvironment = true;
    }, Assembly.GetExecutingAssembly());

    // Assert
    var serviceProvider = services.BuildServiceProvider();
    
    // Verify specific services are registered
    Assert.IsNotNull(serviceProvider.GetService<IUserService>());
    Assert.IsNotNull(serviceProvider.GetService<IEmailService>());
    
    // Verify test-ignored services are not registered
    Assert.IsNull(serviceProvider.GetService<IBackgroundTaskService>());
}
```

### Creating Test Base Classes

For complex applications, consider creating base test classes that set up auto-discovery consistently:

```csharp
public abstract class IntegrationTestBase
{
    protected IServiceProvider ServiceProvider { get; private set; }

    [SetUp]
    public void SetUp()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.test.json", optional: true)
            .Build();

        services.AddAutoServices(options =>
        {
            options.Configuration = configuration;
            options.IsTestEnvironment = true;
            options.Profile = "Testing";
            options.EnableLogging = false; // Reduce test noise
        });

        // Add test-specific services
        ConfigureTestServices(services);

        ServiceProvider = services.BuildServiceProvider();
    }

    protected virtual void ConfigureTestServices(IServiceCollection services)
    {
        // Override in derived classes to add test-specific services
    }

    [TearDown]
    public void TearDown()
    {
        ServiceProvider?.Dispose();
        PerformanceServiceCollectionExtensions.ClearAllCaches(); // Ensure test isolation
    }
}
```

## Performance Monitoring and Optimization

Understanding how your auto-discovery system performs in production is crucial for maintaining optimal application performance.

### Collecting Performance Metrics

Enable detailed performance metrics in your production configuration:

```csharp
services.AddAutoServices(options =>
{
    options.EnablePerformanceOptimizations = true;
    options.EnablePerformanceMetrics = true; // Enable detailed tracking
    options.EnableLogging = false; // Disable console logging in production
});
```

### Integrating with Application Performance Monitoring

For production applications, integrate cache statistics with your APM system:

```csharp
// In a background service or startup completion handler
public class CacheMetricsReporter : IHostedService
{
    private readonly ILogger<CacheMetricsReporter> _logger;
    private readonly IMetrics _metrics; // Your metrics system (e.g., Application Insights)

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var stats = PerformanceServiceCollectionExtensions.GetCacheStatistics();
        
        _metrics.TrackMetric("ServiceDiscovery.CacheHitRatio", stats.HitRatio);
        _metrics.TrackMetric("ServiceDiscovery.TotalRequests", stats.TotalRequests);
        _metrics.TrackMetric("ServiceDiscovery.CachedAssemblies", stats.CachedAssembliesCount);
        
        _logger.LogInformation("Service discovery cache hit ratio: {HitRatio:F1}%", stats.HitRatio);
    }
}
```

### Optimizing for Your Specific Use Case

Different applications benefit from different optimization strategies:

**Microservices** with fast startup requirements should prioritize cache performance and parallel processing.

**Monolithic applications** might benefit more from type metadata caching since they tend to have many services in fewer assemblies.

**Development environments** should balance optimization with cache invalidation to ensure changes are picked up quickly.

## Advanced Configuration Scenarios

For complex enterprise applications, you might need more sophisticated configuration approaches.

### External Configuration Sources

You can load service registration rules from external sources like databases or configuration services:

```csharp
public class DatabaseServiceRegistrationProvider
{
    public async Task<Dictionary<string, string>> GetFeatureFlagsAsync()
    {
        // Load feature flags from database
        return new Dictionary<string, string>
        {
            ["FeatureFlags:EnableAdvancedReporting"] = "true",
            ["FeatureFlags:UseNewPaymentGateway"] = "false"
        };
    }
}

// In startup
var featureFlags = await dbProvider.GetFeatureFlagsAsync();
var configuration = new ConfigurationBuilder()
    .AddInMemoryCollection(featureFlags)
    .Build();

services.AddAutoServices(options =>
{
    options.Configuration = configuration;
});
```

### Dynamic Service Registration

For applications that need to register services based on runtime conditions:

```csharp
services.AddAutoServices(options =>
{
    options.Profile = DetermineProfileAtRuntime();
    options.Configuration = BuildDynamicConfiguration();
});

private string DetermineProfileAtRuntime()
{
    // Complex logic to determine which profile to use
    // Could be based on environment variables, database settings, etc.
    return Environment.GetEnvironmentVariable("DEPLOYMENT_SLOT") switch
    {
        "production" => "Production",
        "staging" => "Staging", 
        _ => "Development"
    };
}
```

## Contributing and Extending the Library

This library is designed to be extensible. If you need custom behavior, you can implement your own caching strategies or type scanners.

### Custom Cache Implementation

You can implement your own caching strategy by implementing the `IAssemblyScanCache` interface:

```csharp
public class RedisAssemblyScanCache : IAssemblyScanCache
{
    private readonly IDatabase _redis;
    
    public RedisAssemblyScanCache(IConnectionMultiplexer redis)
    {
        _redis = redis.GetDatabase();
    }
    
    public bool TryGetCachedResults(Assembly assembly, out IEnumerable<ServiceRegistrationInfo>? cachedResults)
    {
        var cacheKey = GenerateCacheKey(assembly);
        var serializedData = _redis.StringGet(cacheKey);
        
        if (serializedData.HasValue)
        {
            cachedResults = JsonSerializer.Deserialize<ServiceRegistrationInfo[]>(serializedData);
            return true;
        }
        
        cachedResults = null;
        return false;
    }
    
    // Implement other interface methods...
}
```

### Contributing to the Project

We welcome contributions to improve this library. Whether you've found a bug, have a feature request, or want to contribute code, here's how you can help:

**Reporting Issues**: Use the GitHub issue tracker to report bugs or request features. Please provide detailed information about your use case and environment.

**Contributing Code**: Fork the repository, create a feature branch, implement your changes, and submit a pull request. Make sure to include tests for any new functionality.

**Documentation**: Help improve the documentation by suggesting clarifications, adding examples, or fixing typos.

## Conclusion

This library transforms the tedious process of manual service registration into an elegant, convention-based system that grows with your application. By using attributes to declare how services should be registered, you eliminate boilerplate code, reduce errors, and gain powerful features like conditional registration, performance optimization, and environment-specific configuration.

The performance optimizations ensure that this convenience doesn't come at the cost of startup performance, making it suitable for everything from small applications to large enterprise systems.

Remember that the goal isn't just to reduce the amount of code you write, but to make your dependency injection configuration more maintainable, more reliable, and more expressive of your intentions. Each service declares its own registration requirements, making the system more self-documenting and less prone to configuration errors.

Start with the basic features, experiment with the examples provided, and gradually adopt the more advanced features as your application's complexity grows. The library is designed to scale with your needs, providing simple solutions for simple scenarios and powerful tools for complex enterprise requirements.

## Support and Resources

If you encounter any issues or have questions:

- 🐛 [Report Issues](https://github.com/furkansarikaya/FS.AutoServiceDiscovery/issues)
- 💬 [Discussions](https://github.com/furkansarikaya/FS.AutoServiceDiscovery/discussions)
- ⭐ [Star the Project](https://github.com/furkansarikaya/FS.AutoServiceDiscovery) if you find it useful!

**Made with ❤️ by [Furkan Sarıkaya](https://github.com/furkansarikaya)**