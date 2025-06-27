# FS.AutoServiceDiscovery.Extensions

[![NuGet Version](https://img.shields.io/nuget/v/FS.AutoServiceDiscovery.Extensions.svg)](https://www.nuget.org/packages/FS.AutoServiceDiscovery.Extensions)
[![NuGet Downloads](https://img.shields.io/nuget/dt/FS.AutoServiceDiscovery.Extensions.svg)](https://www.nuget.org/packages/FS.AutoServiceDiscovery.Extensions)
[![GitHub License](https://img.shields.io/github/license/furkansarikaya/FS.AutoServiceDiscovery.Extensions)](https://github.com/furkansarikaya/FS.AutoServiceDiscovery/blob/main/LICENSE)
[![GitHub Stars](https://img.shields.io/github/stars/furkansarikaya/FS.AutoServiceDiscovery.Extensions.svg)](https://github.com/furkansarikaya/FS.AutoServiceDiscovery/stargazers)

A powerful .NET 9.0 library that provides convention-based automatic service registration for dependency injection containers. Eliminate boilerplate code and discover services automatically using attributes and naming conventions.

## ✨ Features

- **Convention-based Discovery**: Automatically discovers services using naming conventions (e.g., `IUserService` → `UserService`)
- **Attribute-driven Registration**: Use simple attributes to mark classes for automatic registration
- **Multiple Lifetime Support**: Support for Singleton, Scoped, and Transient lifetimes
- **Conditional Registration**: Register services based on configuration values (feature flags)
- **Profile-based Registration**: Register different services for different environments (Development, Production, etc.)
- **Order Control**: Control the registration order of services
- **Test Environment Support**: Skip certain services during testing
- **Comprehensive Logging**: Optional logging of all registration operations
- **Zero Configuration**: Works out of the box with sensible defaults

## 📦 Installation

```bash
dotnet add package FS.AutoServiceDiscovery.Extensions
```

## 🚀 Quick Start

### 1. Mark Your Services

```csharp
// Simple service registration
[ServiceRegistration(ServiceLifetime.Scoped)]
public class UserService : IUserService
{
    // Implementation
}

// Advanced registration with options
[ServiceRegistration(ServiceLifetime.Singleton, Order = 1, Profile = "Production")]
public class CacheService : ICacheService
{
    // Implementation
}

// Conditional registration based on configuration
[ConditionalService("FeatureFlags:EnableEmailService", "true")]
[ServiceRegistration(ServiceLifetime.Transient)]
public class EmailService : IEmailService
{
    // Implementation
}
```

### 2. Register Services Automatically

```csharp
// Program.cs or Startup.cs
services.AddAutoServices(); // Scans calling assembly

// Or with custom configuration
services.AddAutoServices(options =>
{
    options.Profile = "Production";
    options.Configuration = configuration;
    options.EnableLogging = true;
}, Assembly.GetExecutingAssembly());
```

## 📖 Detailed Usage

### Basic Service Registration

The simplest way to register a service is using the `ServiceRegistration` attribute:

```csharp
[ServiceRegistration(ServiceLifetime.Scoped)]
public class ProductService : IProductService
{
    public async Task<Product> GetProductAsync(int id)
    {
        // Implementation
    }
}
```

### Convention-based Type Resolution

The library uses smart conventions to determine which interface to register:

1. **I{ClassName} Convention**: `ProductService` → `IProductService`
2. **Single Interface**: If class implements only one interface, uses that
3. **Explicit Type**: Specify exact type in attribute
4. **Concrete Type**: Falls back to concrete type if no interface found

```csharp
// Explicit service type
[ServiceRegistration(ServiceLifetime.Scoped, ServiceType = typeof(ISpecialService))]
public class CustomService : ISpecialService, IDisposable
{
    // Will be registered as ISpecialService, not IDisposable
}
```

### Profile-based Registration

Register different implementations for different environments:

```csharp
[ServiceRegistration(ServiceLifetime.Singleton, Profile = "Development")]
public class DevCacheService : ICacheService
{
    // Development implementation
}

[ServiceRegistration(ServiceLifetime.Singleton, Profile = "Production")]
public class RedisCacheService : ICacheService
{
    // Production implementation
}

// Usage
services.AddAutoServices(options =>
{
    options.Profile = builder.Environment.EnvironmentName;
});
```

### Conditional Registration

Register services based on configuration values:

```csharp
[ConditionalService("FeatureFlags:EnableAdvancedLogging", "true")]
[ConditionalService("Logging:Provider", "Serilog")]
[ServiceRegistration(ServiceLifetime.Singleton)]
public class AdvancedLogger : ILogger
{
    // Only registered if both conditions are met
}
```

### Registration Order

Control the order of service registration:

```csharp
[ServiceRegistration(ServiceLifetime.Singleton, Order = 1)]
public class DatabaseService : IDatabaseService { }

[ServiceRegistration(ServiceLifetime.Singleton, Order = 2)]
public class MigrationService : IMigrationService
{
    public MigrationService(IDatabaseService db) { } // Depends on DatabaseService
}
```

### Test Environment Handling

Skip services during testing:

```csharp
[ServiceRegistration(ServiceLifetime.Singleton, IgnoreInTests = true)]
public class ExternalApiService : IExternalApiService
{
    // This service won't be registered in test environment
}

// In test setup
services.AddAutoServices(options =>
{
    options.IsTestEnvironment = true;
});
```

## 🔧 Configuration Options

```csharp
services.AddAutoServices(options =>
{
    // Active profile for profile-based registration
    options.Profile = "Development";
    
    // Whether running in test environment
    options.IsTestEnvironment = false;
    
    // Enable/disable registration logging
    options.EnableLogging = true;
    
    // Configuration for conditional services
    options.Configuration = builder.Configuration;
});
```

## 🎯 Advanced Scenarios

### Repository Pattern

```csharp
[ServiceRegistration(ServiceLifetime.Scoped)]
public class UserRepository : IUserRepository
{
    // Repository implementation
}

[ServiceRegistration(ServiceLifetime.Scoped)]
public class ProductRepository : IProductRepository
{
    // Repository implementation
}
```

### Decorator Pattern

```csharp
[ServiceRegistration(ServiceLifetime.Scoped, Order = 1)]
public class UserService : IUserService { }

[ServiceRegistration(ServiceLifetime.Scoped, Order = 2, ServiceType = typeof(IUserService))]
public class CachedUserService : IUserService
{
    public CachedUserService(IUserService userService) { }
}
```

### Multiple Assembly Scanning

```csharp
services.AddAutoServices(
    Assembly.GetExecutingAssembly(),
    typeof(ExternalService).Assembly,
    Assembly.LoadFrom("Plugin.dll")
);
```

## 🤝 Contributing

Contributions are welcome! Please feel free to submit a Pull Request. For major changes, please open an issue first to discuss what you would like to change.

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🙏 Acknowledgments

- Inspired by the need for cleaner dependency injection setup in .NET applications
- Built with modern .NET 9.0 features and best practices

## 📞 Support

If you encounter any issues or have questions:

- 🐛 [Report Issues](https://github.com/FS.AutoServiceDiscovery/issues)
- 💬 [Discussions](https://github.com/FS.AutoServiceDiscovery/discussions)
- ⭐ [Star the Project](https://github.com/FS.AutoServiceDiscovery) if you find it useful!

---

**Made with ❤️ by [Furkan Sarıkaya](https://github.com/furkansarikaya)**