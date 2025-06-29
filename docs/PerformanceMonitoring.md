# docs/PerformanceMonitoring.md

# Performance Monitoring Guide

Performance monitoring in service discovery is like having a sophisticated dashboard in a race car - it provides real-time insights into how your system is performing, identifies bottlenecks before they become critical, and enables data-driven optimization decisions. This guide will help you implement comprehensive monitoring for your service discovery system.

## 🎯 Understanding Performance Monitoring

Performance monitoring in service discovery involves tracking multiple dimensions of system behavior: timing metrics (how fast operations complete), resource utilization (how much memory and CPU is used), throughput metrics (how many services are processed), and quality metrics (success rates and error frequencies).

The key insight is that service discovery performance directly impacts application startup time, which affects user experience, container scaling speed, and development productivity. A well-monitored discovery system enables you to:

- **Detect Performance Regressions**: Identify when changes negatively impact performance
- **Optimize Resource Usage**: Understand where your system spends time and resources
- **Plan Capacity**: Predict how performance will scale with growth
- **Debug Issues**: Quickly identify the root cause of performance problems

## 📊 Core Metrics to Monitor

### Assembly Scanning Metrics

Assembly scanning is often the most expensive part of service discovery, making it crucial to monitor:

```csharp
public class AssemblyScanningMonitor
{
    private readonly IPerformanceMetricsCollector _metricsCollector;
    private readonly ILogger<AssemblyScanningMonitor> _logger;

    public AssemblyScanningMonitor(IPerformanceMetricsCollector metricsCollector, ILogger<AssemblyScanningMonitor> logger)
    {
        _metricsCollector = metricsCollector;
        _logger = logger;
    }

    public async Task<IEnumerable<ServiceRegistrationInfo>> MonitoredScanAsync(Assembly assembly)
    {
        var stopwatch = Stopwatch.StartNew();
        var assemblyName = assembly.GetName().Name ?? "Unknown";
        var servicesFound = 0;
        var wasSuccessful = false;

        try
        {
            _logger.LogDebug("Starting scan of assembly {AssemblyName}", assemblyName);

            var services = await ScanAssemblyCore(assembly);
            servicesFound = services.Count();
            wasSuccessful = true;

            stopwatch.Stop();

            // Log performance details
            _logger.LogInformation("Scanned {AssemblyName} in {ElapsedMs}ms, found {ServiceCount} services",
                assemblyName, stopwatch.ElapsedMilliseconds, servicesFound);

            // Record metrics for aggregation and trending
            _metricsCollector.RecordAssemblyScan(assemblyName, stopwatch.Elapsed, servicesFound, wasSuccessful);

            // Check for performance anomalies
            if (stopwatch.ElapsedMilliseconds > 1000) // More than 1 second
            {
                _logger.LogWarning("Assembly {AssemblyName} took {ElapsedMs}ms to scan - this may indicate performance issues",
                    assemblyName, stopwatch.ElapsedMilliseconds);
            }

            if (servicesFound == 0)
            {
                _logger.LogInformation("Assembly {AssemblyName} contained no discoverable services", assemblyName);
            }

            return services;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            wasSuccessful = false;

            _logger.LogError(ex, "Failed to scan assembly {AssemblyName} after {ElapsedMs}ms",
                assemblyName, stopwatch.ElapsedMilliseconds);

            _metricsCollector.RecordAssemblyScan(assemblyName, stopwatch.Elapsed, 0, wasSuccessful);

            // Return empty rather than propagating exception
            return Enumerable.Empty<ServiceRegistrationInfo>();
        }
    }

    private async Task<IEnumerable<ServiceRegistrationInfo>> ScanAssemblyCore(Assembly assembly)
    {
        // Core scanning logic would go here
        await Task.Delay(10); // Simulate scanning work
        return new List<ServiceRegistrationInfo>();
    }
}
```

### Cache Performance Metrics

Cache performance directly impacts overall system performance:

```csharp
public class CachePerformanceMonitor
{
    private readonly IAssemblyScanCache _cache;
    private readonly IPerformanceMetricsCollector _metricsCollector;
    private readonly ILogger<CachePerformanceMonitor> _logger;

    public CachePerformanceMonitor(IAssemblyScanCache cache, IPerformanceMetricsCollector metricsCollector, ILogger<CachePerformanceMonitor> logger)
    {
        _cache = cache;
        _metricsCollector = metricsCollector;
        _logger = logger;
    }

    public bool TryGetCachedResults(Assembly assembly, out IEnumerable<ServiceRegistrationInfo>? cachedResults)
    {
        var stopwatch = Stopwatch.StartNew();
        var assemblyName = assembly.GetName().Name ?? "Unknown";

        try
        {
            var wasHit = _cache.TryGetCachedResults(assembly, out cachedResults);
            stopwatch.Stop();

            // Record cache operation metrics
            _metricsCollector.RecordCacheOperation("Get", wasHit, stopwatch.Elapsed);

            if (wasHit)
            {
                _logger.LogDebug("Cache HIT for assembly {AssemblyName} in {ElapsedMs}ms, returned {ServiceCount} services",
                    assemblyName, stopwatch.ElapsedMilliseconds, cachedResults?.Count() ?? 0);
            }
            else
            {
                _logger.LogDebug("Cache MISS for assembly {AssemblyName} in {ElapsedMs}ms",
                    assemblyName, stopwatch.ElapsedMilliseconds);
            }

            return wasHit;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Cache operation failed for assembly {AssemblyName} after {ElapsedMs}ms",
                assemblyName, stopwatch.ElapsedMilliseconds);

            _metricsCollector.RecordCacheOperation("Get", false, stopwatch.Elapsed);
            cachedResults = null;
            return false;
        }
    }

    public void CacheResults(Assembly assembly, IEnumerable<ServiceRegistrationInfo> results)
    {
        var stopwatch = Stopwatch.StartNew();
        var assemblyName = assembly.GetName().Name ?? "Unknown";

        try
        {
            _cache.CacheResults(assembly, results);
            stopwatch.Stop();

            _metricsCollector.RecordCacheOperation("Set", true, stopwatch.Elapsed);

            _logger.LogDebug("Cached {ServiceCount} services for assembly {AssemblyName} in {ElapsedMs}ms",
                results.Count(), assemblyName, stopwatch.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Failed to cache results for assembly {AssemblyName} after {ElapsedMs}ms",
                assemblyName, stopwatch.ElapsedMilliseconds);

            _metricsCollector.RecordCacheOperation("Set", false, stopwatch.Elapsed);
        }
    }

    public async Task<CacheHealthReport> GenerateHealthReportAsync()
    {
        var statistics = _cache.GetStatistics();
        
        return new CacheHealthReport
        {
            HitRatio = statistics.HitRatio,
            TotalRequests = statistics.TotalRequests,
            CachedAssembliesCount = statistics.CachedAssembliesCount,
            AverageServicesPerAssembly = statistics.AverageServicesPerAssembly,
            HealthStatus = DetermineHealthStatus(statistics),
            Recommendations = GenerateRecommendations(statistics)
        };
    }

    private CacheHealthStatus DetermineHealthStatus(CacheStatistics statistics)
    {
        if (statistics.HitRatio > 80) return CacheHealthStatus.Excellent;
        if (statistics.HitRatio > 60) return CacheHealthStatus.Good;
        if (statistics.HitRatio > 40) return CacheHealthStatus.Fair;
        return CacheHealthStatus.Poor;
    }

    private List<string> GenerateRecommendations(CacheStatistics statistics)
    {
        var recommendations = new List<string>();

        if (statistics.HitRatio < 50)
        {
            recommendations.Add("Cache hit ratio is low. Consider increasing cache size or reviewing cache invalidation strategy.");
        }

        if (statistics.CachedAssembliesCount > 100)
        {
            recommendations.Add("Large number of cached assemblies. Monitor memory usage and consider implementing cache eviction policies.");
        }

        if (statistics.TotalRequests < 10)
        {
            recommendations.Add("Low cache usage detected. Verify that caching is properly enabled and configured.");
        }

        return recommendations;
    }

    public class CacheHealthReport
    {
        public double HitRatio { get; set; }
        public long TotalRequests { get; set; }
        public int CachedAssembliesCount { get; set; }
        public double AverageServicesPerAssembly { get; set; }
        public CacheHealthStatus HealthStatus { get; set; }
        public List<string> Recommendations { get; set; } = new();
    }

    public enum CacheHealthStatus
    {
        Poor,
        Fair,
        Good,
        Excellent
    }
}
```

### Plugin Performance Metrics

Plugin performance monitoring helps identify which plugins are performing well and which need optimization:

```csharp
public class PluginPerformanceMonitor
{
    private readonly IPerformanceMetricsCollector _metricsCollector;
    private readonly ILogger<PluginPerformanceMonitor> _logger;

    public PluginPerformanceMonitor(IPerformanceMetricsCollector metricsCollector, ILogger<PluginPerformanceMonitor> logger)
    {
        _metricsCollector = metricsCollector;
        _logger = logger;
    }

    public async Task<PluginExecutionResult> MonitorPluginExecutionAsync(
        IServiceDiscoveryPlugin plugin, 
        Assembly assembly, 
        AutoServiceOptions options)
    {
        var stopwatch = Stopwatch.StartNew();
        var pluginName = plugin.Name;
        var servicesDiscovered = 0;
        PluginValidationResult validationResult;

        try
        {
            _logger.LogDebug("Starting execution of plugin {PluginName} for assembly {AssemblyName}",
                pluginName, assembly.GetName().Name);

            // Execute plugin discovery
            var discoveredServices = plugin.DiscoverServices(assembly, options).ToList();
            servicesDiscovered = discoveredServices.Count;

            // Validate plugin results
            validationResult = plugin.ValidateDiscoveredServices(discoveredServices, discoveredServices, options);

            stopwatch.Stop();

            // Record detailed metrics
            _metricsCollector.RecordPluginExecution(pluginName, stopwatch.Elapsed, servicesDiscovered, validationResult);

            // Log execution details
            if (validationResult.IsValid)
            {
                _logger.LogInformation("Plugin {PluginName} executed successfully in {ElapsedMs}ms, discovered {ServiceCount} services",
                    pluginName, stopwatch.ElapsedMilliseconds, servicesDiscovered);
            }
            else
            {
                _logger.LogWarning("Plugin {PluginName} completed with validation errors in {ElapsedMs}ms: {Errors}",
                    pluginName, stopwatch.ElapsedMilliseconds, string.Join("; ", validationResult.Errors));
            }

            // Check for performance concerns
            if (stopwatch.ElapsedMilliseconds > 500)
            {
                _logger.LogWarning("Plugin {PluginName} took {ElapsedMs}ms - consider optimization",
                    pluginName, stopwatch.ElapsedMilliseconds);
            }

            if (servicesDiscovered == 0 && validationResult.IsValid)
            {
                _logger.LogInformation("Plugin {PluginName} discovered no services for assembly {AssemblyName}",
                    pluginName, assembly.GetName().Name);
            }

            return new PluginExecutionResult
            {
                PluginName = pluginName,
                IsSuccessful = validationResult.IsValid,
                ExecutionTime = stopwatch.Elapsed,
                ServicesDiscovered = servicesDiscovered,
                ValidationResult = validationResult,
                DiscoveredServices = discoveredServices
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            
            validationResult = PluginValidationResult.Failure($"Plugin execution failed: {ex.Message}");
            _metricsCollector.RecordPluginExecution(pluginName, stopwatch.Elapsed, 0, validationResult);

            _logger.LogError(ex, "Plugin {PluginName} failed after {ElapsedMs}ms",
                pluginName, stopwatch.ElapsedMilliseconds);

            return new PluginExecutionResult
            {
                PluginName = pluginName,
                IsSuccessful = false,
                ExecutionTime = stopwatch.Elapsed,
                ServicesDiscovered = 0,
                ValidationResult = validationResult,
                Exception = ex
            };
        }
    }

    public async Task<PluginPerformanceReport> GeneratePerformanceReportAsync()
    {
        var summary = _metricsCollector.GetMetricsSummary();
        var report = new PluginPerformanceReport();

        foreach (var pluginMetric in summary.PluginMetrics)
        {
            var pluginReport = new PluginReport
            {
                PluginName = pluginMetric.PluginName,
                ExecutionCount = pluginMetric.ExecutionCount,
                AverageExecutionTimeMs = pluginMetric.AverageExecutionTimeMs,
                TotalServicesDiscovered = pluginMetric.TotalServicesDiscovered,
                SuccessRate = pluginMetric.SuccessRate,
                AverageErrorsPerExecution = pluginMetric.AverageErrorsPerExecution,
                PerformanceRating = CalculatePerformanceRating(pluginMetric),
                Recommendations = GeneratePluginRecommendations(pluginMetric)
            };

            report.PluginReports.Add(pluginReport);
        }

        report.OverallHealthStatus = DetermineOverallHealth(report.PluginReports);
        return report;
    }

    private PerformanceRating CalculatePerformanceRating(PerformanceMetricsSummary.PluginSummary metrics)
    {
        var score = 0;

        // Success rate scoring (40% of total)
        if (metrics.SuccessRate > 95) score += 40;
        else if (metrics.SuccessRate > 85) score += 30;
        else if (metrics.SuccessRate > 70) score += 20;
        else score += 10;

        // Performance scoring (35% of total)
        if (metrics.AverageExecutionTimeMs < 50) score += 35;
        else if (metrics.AverageExecutionTimeMs < 200) score += 25;
        else if (metrics.AverageExecutionTimeMs < 500) score += 15;
        else score += 5;

        // Reliability scoring (25% of total)
        if (metrics.AverageErrorsPerExecution < 0.1) score += 25;
        else if (metrics.AverageErrorsPerExecution < 0.5) score += 15;
        else if (metrics.AverageErrorsPerExecution < 1.0) score += 10;
        else score += 0;

        return score switch
        {
            >= 85 => PerformanceRating.Excellent,
            >= 70 => PerformanceRating.Good,
            >= 50 => PerformanceRating.Fair,
            _ => PerformanceRating.Poor
        };
    }

    private List<string> GeneratePluginRecommendations(PerformanceMetricsSummary.PluginSummary metrics)
    {
        var recommendations = new List<string>();

        if (metrics.SuccessRate < 90)
        {
            recommendations.Add($"Success rate ({metrics.SuccessRate:F1}%) is below optimal. Review error handling and validation logic.");
        }

        if (metrics.AverageExecutionTimeMs > 300)
        {
            recommendations.Add($"Average execution time ({metrics.AverageExecutionTimeMs:F1}ms) is high. Consider optimization or async processing.");
        }

        if (metrics.AverageErrorsPerExecution > 0.5)
        {
            recommendations.Add($"Error rate ({metrics.AverageErrorsPerExecution:F2} per execution) is elevated. Improve error handling and input validation.");
        }

        if (metrics.TotalServicesDiscovered == 0 && metrics.ExecutionCount > 0)
        {
            recommendations.Add("Plugin is not discovering any services. Verify plugin logic and assembly compatibility.");
        }

        return recommendations;
    }

    public class PluginExecutionResult
    {
        public string PluginName { get; set; } = string.Empty;
        public bool IsSuccessful { get; set; }
        public TimeSpan ExecutionTime { get; set; }
        public int ServicesDiscovered { get; set; }
        public PluginValidationResult ValidationResult { get; set; } = PluginValidationResult.Success();
        public List<ServiceRegistrationInfo> DiscoveredServices { get; set; } = new();
        public Exception? Exception { get; set; }
    }

    public class PluginPerformanceReport
    {
        public List<PluginReport> PluginReports { get; } = new();
        public OverallHealthStatus OverallHealthStatus { get; set; }
        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    }

    public class PluginReport
    {
        public string PluginName { get; set; } = string.Empty;
        public long ExecutionCount { get; set; }
        public double AverageExecutionTimeMs { get; set; }
        public long TotalServicesDiscovered { get; set; }
        public double SuccessRate { get; set; }
        public double AverageErrorsPerExecution { get; set; }
        public PerformanceRating PerformanceRating { get; set; }
        public List<string> Recommendations { get; set; } = new();
    }

    public enum PerformanceRating
    {
        Poor,
        Fair, 
        Good,
        Excellent
    }

    public enum OverallHealthStatus
    {
        Critical,
        Warning,
        Healthy,
        Optimal
    }

    private OverallHealthStatus DetermineOverallHealth(List<PluginReport> reports)
    {
        if (!reports.Any()) return OverallHealthStatus.Warning;

        var averageRating = reports.Average(r => (int)r.PerformanceRating);
        var poorCount = reports.Count(r => r.PerformanceRating == PerformanceRating.Poor);

        if (poorCount > reports.Count / 2) return OverallHealthStatus.Critical;
        if (poorCount > 0) return OverallHealthStatus.Warning;
        if (averageRating >= 2.5) return OverallHealthStatus.Optimal;
        return OverallHealthStatus.Healthy;
    }
}
```

## 🔍 Diagnostic and Troubleshooting Tools

### Performance Analyzer

```csharp
public class PerformanceAnalyzer
{
    private readonly IPerformanceMetricsCollector _metricsCollector;
    private readonly ILogger<PerformanceAnalyzer> _logger;

    public PerformanceAnalyzer(IPerformanceMetricsCollector metricsCollector, ILogger<PerformanceAnalyzer> logger)
    {
        _metricsCollector = metricsCollector;
        _logger = logger;
    }

    public async Task<PerformanceAnalysisReport> AnalyzePerformanceAsync(TimeSpan analysisWindow)
    {
        var report = new PerformanceAnalysisReport
        {
            AnalysisWindow = analysisWindow,
            GeneratedAt = DateTime.UtcNow
        };

        var metrics = _metricsCollector.GetMetricsSummary();
        
        // Analyze overall system performance
        AnalyzeOverallPerformance(metrics, report);
        
        // Analyze assembly scanning performance
        AnalyzeAssemblyPerformance(metrics, report);
        
        // Analyze cache effectiveness
        AnalyzeCachePerformance(metrics, report);
        
        // Analyze plugin performance
        AnalyzePluginPerformance(metrics, report);
        
        // Generate recommendations
        GenerateOptimizationRecommendations(report);

        return report;
    }

    private void AnalyzeOverallPerformance(PerformanceMetricsSummary metrics, PerformanceAnalysisReport report)
    {
        report.OverallMetrics = new OverallPerformanceMetrics
        {
            TotalServiceRegistrations = metrics.TotalServiceRegistrations,
            AverageRegistrationTimeMs = metrics.AverageRegistrationTimeMs,
            RegistrationFailureRate = metrics.RegistrationFailureRate,
            CollectionDuration = metrics.CollectionDuration
        };

        // Identify performance trends
        if (metrics.AverageRegistrationTimeMs > 100)
        {
            report.Issues.Add(new PerformanceIssue
            {
                Severity = IssueSeverity.Warning,
                Category = "Registration Performance",
                Description = $"Average registration time ({metrics.AverageRegistrationTimeMs:F1}ms) is higher than recommended (< 100ms)",
                Impact = "Increased application startup time",
                Recommendation = "Review service registration logic and consider optimizations"
            });
        }

        if (metrics.RegistrationFailureRate > 5)
        {
            report.Issues.Add(new PerformanceIssue
            {
                Severity = IssueSeverity.Error,
                Category = "Registration Reliability",
                Description = $"Registration failure rate ({metrics.RegistrationFailureRate:F1}%) is above acceptable threshold (< 5%)",
                Impact = "Services may not be available for dependency injection",
                Recommendation = "Investigate and fix registration failures"
            });
        }
    }

    private void AnalyzeAssemblyPerformance(PerformanceMetricsSummary metrics, PerformanceAnalysisReport report)
    {
        var slowAssemblies = metrics.AssemblyMetrics
            .Where(a => a.AverageScanTimeMs > 200)
            .OrderByDescending(a => a.AverageScanTimeMs)
            .Take(5)
            .ToList();

        report.SlowAssemblies = slowAssemblies.Select(a => new SlowAssemblyReport
        {
            AssemblyName = a.AssemblyName,
            AverageScanTimeMs = a.AverageScanTimeMs,
            ScanCount = a.ScanCount,
            TotalServicesFound = a.TotalServicesFound,
            SuccessRate = a.SuccessRate
        }).ToList();

        foreach (var slowAssembly in slowAssemblies)
        {
            report.Issues.Add(new PerformanceIssue
            {
                Severity = IssueSeverity.Warning,
                Category = "Assembly Scanning",
                Description = $"Assembly {slowAssembly.AssemblyName} has slow scan time ({slowAssembly.AverageScanTimeMs:F1}ms)",
                Impact = "Increased discovery time",
                Recommendation = "Consider assembly filtering or review assembly structure"
            });
        }
    }

    private void AnalyzeCachePerformance(PerformanceMetricsSummary metrics, PerformanceAnalysisReport report)
    {
        var cacheMetrics = metrics.CacheMetrics.FirstOrDefault();
        if (cacheMetrics != null)
        {
            report.CacheAnalysis = new CacheAnalysisResult
            {
                HitRate = cacheMetrics.HitRate,
                AverageOperationTimeMs = cacheMetrics.AverageOperationTimeMs,
                OperationCount = cacheMetrics.OperationCount,
                Effectiveness = DetermineCacheEffectiveness(cacheMetrics)
            };

            if (cacheMetrics.HitRate < 60)
            {
                report.Issues.Add(new PerformanceIssue
                {
                    Severity = IssueSeverity.Warning,
                    Category = "Cache Performance",
                    Description = $"Cache hit rate ({cacheMetrics.HitRate:F1}%) is below optimal (> 60%)",
                    Impact = "Reduced performance benefits from caching",
                    Recommendation = "Review cache configuration and invalidation strategy"
                });
            }
        }
    }

    private void AnalyzePluginPerformance(PerformanceMetricsSummary metrics, PerformanceAnalysisReport report)
    {
        var problematicPlugins = metrics.PluginMetrics
            .Where(p => p.SuccessRate < 90 || p.AverageExecutionTimeMs > 300)
            .ToList();

        report.ProblematicPlugins = problematicPlugins.Select(p => new ProblematicPluginReport
        {
            PluginName = p.PluginName,
            SuccessRate = p.SuccessRate,
            AverageExecutionTimeMs = p.AverageExecutionTimeMs,
            Issues = IdentifyPluginIssues(p)
        }).ToList();
    }

    private List<string> IdentifyPluginIssues(PerformanceMetricsSummary.PluginSummary plugin)
    {
        var issues = new List<string>();

        if (plugin.SuccessRate < 90)
            issues.Add($"Low success rate: {plugin.SuccessRate:F1}%");

        if (plugin.AverageExecutionTimeMs > 300)
            issues.Add($"Slow execution: {plugin.AverageExecutionTimeMs:F1}ms average");

        if (plugin.AverageErrorsPerExecution > 0.5)
            issues.Add($"High error rate: {plugin.AverageErrorsPerExecution:F2} errors per execution");

        return issues;
    }

    private string DetermineCacheEffectiveness(PerformanceMetricsSummary.CacheSummary cache)
    {
        if (cache.HitRate > 80) return "Excellent";
        if (cache.HitRate > 60) return "Good";
        if (cache.HitRate > 40) return "Fair";
        return "Poor";
    }

    private void GenerateOptimizationRecommendations(PerformanceAnalysisReport report)
    {
        // High-impact recommendations
        if (report.SlowAssemblies.Any(a => a.AverageScanTimeMs > 500))
        {
            report.Recommendations.Add(new OptimizationRecommendation
            {
                Priority = RecommendationPriority.High,
                Category = "Assembly Optimization",
                Title = "Optimize slow assembly scanning",
                Description = "Some assemblies are taking excessive time to scan",
                ActionItems = new[]
                {
                    "Review assembly filtering configuration",
                    "Consider excluding large assemblies with few services",
                    "Implement assembly-specific optimizations"
                }
            });
        }

        // Cache optimization recommendations
        if (report.CacheAnalysis?.HitRate < 60)
        {
            report.Recommendations.Add(new OptimizationRecommendation
            {
                Priority = RecommendationPriority.Medium,
                Category = "Cache Optimization",
                Title = "Improve cache effectiveness",
                Description = "Cache hit rate is below optimal levels",
                ActionItems = new[]
                {
                    "Review cache invalidation strategy",
                    "Increase cache size if memory allows",
                    "Analyze cache usage patterns"
                }
            });
        }

        // Plugin optimization recommendations
        if (report.ProblematicPlugins.Count > 0)
        {
            report.Recommendations.Add(new OptimizationRecommendation
            {
                Priority = RecommendationPriority.Medium,
                Category = "Plugin Optimization",
                Title = "Address plugin performance issues",
                Description = $"{report.ProblematicPlugins.Count} plugins have performance issues",
                ActionItems = new[]
                {
                    "Profile slow plugins for optimization opportunities",
                    "Review plugin error handling",
                    "Consider plugin ordering and dependencies"
                }
            });
        }
    }

    public class PerformanceAnalysisReport
    {
        public TimeSpan AnalysisWindow { get; set; }
        public DateTime GeneratedAt { get; set; }
        public OverallPerformanceMetrics OverallMetrics { get; set; } = new();
        public List<SlowAssemblyReport> SlowAssemblies { get; set; } = new();
        public CacheAnalysisResult? CacheAnalysis { get; set; }
        public List<ProblematicPluginReport> ProblematicPlugins { get; set; } = new();
        public List<PerformanceIssue> Issues { get; set; } = new();
        public List<OptimizationRecommendation> Recommendations { get; set; } = new();
    }

    public class OverallPerformanceMetrics
    {
        public long TotalServiceRegistrations { get; set; }
        public double AverageRegistrationTimeMs { get; set; }
        public double RegistrationFailureRate { get; set; }
        public TimeSpan CollectionDuration { get; set; }
    }

    public class SlowAssemblyReport
    {
        public string AssemblyName { get; set; } = string.Empty;
        public double AverageScanTimeMs { get; set; }
        public long ScanCount { get; set; }
        public long TotalServicesFound { get; set; }
        public double SuccessRate { get; set; }
    }

    public class CacheAnalysisResult
    {
        public double HitRate { get; set; }
        public double AverageOperationTimeMs { get; set; }
        public long OperationCount { get; set; }
        public string Effectiveness { get; set; } = string.Empty;
    }

    public class ProblematicPluginReport
    {
        public string PluginName { get; set; } = string.Empty;
        public double SuccessRate { get; set; }
        public double AverageExecutionTimeMs { get; set; }
        public List<string> Issues { get; set; } = new();
    }

    public class PerformanceIssue
    {
        public IssueSeverity Severity { get; set; }
        public string Category { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Impact { get; set; } = string.Empty;
        public string Recommendation { get; set; } = string.Empty;
    }

    public class OptimizationRecommendation
    {
        public RecommendationPriority Priority { get; set; }
        public string Category { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string[] ActionItems { get; set; } = Array.Empty<string>();
    }

    public enum IssueSeverity
    {
        Info,
        Warning,
        Error,
        Critical
    }

    public enum RecommendationPriority
    {
        Low,
        Medium,
        High,
        Critical
    }
}
```

## 📈 Real-Time Monitoring Dashboard

### Monitoring Service

```csharp
public class ServiceDiscoveryMonitoringService : BackgroundService
{
    private readonly IPerformanceMetricsCollector _metricsCollector;
    private readonly PerformanceAnalyzer _analyzer;
    private readonly ILogger<ServiceDiscoveryMonitoringService> _logger;
    private readonly IServiceScopeFactory _scopeFactory;

    public ServiceDiscoveryMonitoringService(
        IPerformanceMetricsCollector metricsCollector,
        PerformanceAnalyzer analyzer,
        ILogger<ServiceDiscoveryMonitoringService> logger,
        IServiceScopeFactory scopeFactory)
    {
        _metricsCollector = metricsCollector;
        _analyzer = analyzer;
        _logger = logger;
        _scopeFactory = scopeFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Service Discovery Monitoring Service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CollectAndAnalyzeMetrics();
                await CheckForAlerts();
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // Expected when cancellation is requested
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in monitoring service execution");
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken); // Shorter delay on error
            }
        }

        _logger.LogInformation("Service Discovery Monitoring Service stopped");
    }

    private async Task CollectAndAnalyzeMetrics()
    {
        var metrics = _metricsCollector.GetMetricsSummary();
        
        // Export metrics to external monitoring systems
        await ExportToPrometheus(metrics);
        await ExportToApplicationInsights(metrics);
        
        // Perform analysis for alerts
        var analysisReport = await _analyzer.AnalyzePerformanceAsync(TimeSpan.FromMinutes(5));
        await ProcessAnalysisReport(analysisReport);
    }

    private async Task CheckForAlerts()
    {
        var metrics = _metricsCollector.GetMetricsSummary();
        
        // Check for critical performance degradation
        await CheckCriticalMetrics(metrics);
        
        // Check for anomalies
        await CheckForAnomalies(metrics);
    }

    private async Task CheckCriticalMetrics(PerformanceMetricsSummary metrics)
    {
        // High failure rate alert
        if (metrics.RegistrationFailureRate > 10)
        {
            await TriggerAlert(AlertLevel.Critical, 
                "High Service Registration Failure Rate",
                $"Registration failure rate is {metrics.RegistrationFailureRate:F1}% (threshold: 10%)");
        }

        // Slow performance alert
        if (metrics.AverageRegistrationTimeMs > 500)
        {
            await TriggerAlert(AlertLevel.Warning,
                "Slow Service Registration Performance", 
                $"Average registration time is {metrics.AverageRegistrationTimeMs:F1}ms (threshold: 500ms)");
        }

        // Cache performance alert
        var cacheMetrics = metrics.CacheMetrics.FirstOrDefault();
        if (cacheMetrics?.HitRate < 30)
        {
            await TriggerAlert(AlertLevel.Warning,
                "Poor Cache Performance",
                $"Cache hit rate is {cacheMetrics.HitRate:F1}% (threshold: 30%)");
        }
    }

    private async Task CheckForAnomalies(PerformanceMetricsSummary metrics)
    {
        // Check for assembly scanning anomalies
        var slowAssemblies = metrics.AssemblyMetrics.Where(a => a.AverageScanTimeMs > 1000).ToList();
        if (slowAssemblies.Count > 0)
        {
            await TriggerAlert(AlertLevel.Warning,
                "Slow Assembly Scanning Detected",
                $"{slowAssemblies.Count} assemblies taking >1000ms to scan: {string.Join(", ", slowAssemblies.Select(a => a.AssemblyName))}");
        }

        // Check for plugin issues
        var failingPlugins = metrics.PluginMetrics.Where(p => p.SuccessRate < 50).ToList();
        if (failingPlugins.Count > 0)
        {
            await TriggerAlert(AlertLevel.Error,
                "Plugin Failures Detected",
                $"{failingPlugins.Count} plugins with <50% success rate: {string.Join(", ", failingPlugins.Select(p => p.PluginName))}");
        }
    }

    private async Task ExportToPrometheus(PerformanceMetricsSummary metrics)
    {
        // Export key metrics to Prometheus
        // This would integrate with your Prometheus metrics registry
        
        // Example metrics:
        // service_discovery_registration_total
        // service_discovery_registration_duration_seconds
        // service_discovery_cache_hit_ratio
        // service_discovery_assembly_scan_duration_seconds
        // service_discovery_plugin_execution_duration_seconds
        
        await Task.CompletedTask; // Placeholder for actual implementation
    }

    private async Task ExportToApplicationInsights(PerformanceMetricsSummary metrics)
    {
        // Export to Application Insights or similar APM tool
        using var scope = _scopeFactory.CreateScope();
        
        // Log structured data for APM analysis
        _logger.LogInformation("Service Discovery Performance Metrics: {@Metrics}", new
        {
            TotalRegistrations = metrics.TotalServiceRegistrations,
            AverageRegistrationTime = metrics.AverageRegistrationTimeMs,
            FailureRate = metrics.RegistrationFailureRate,
            CacheHitRate = metrics.CacheMetrics.FirstOrDefault()?.HitRate ?? 0,
            AssemblyCount = metrics.AssemblyMetrics.Count,
            PluginCount = metrics.PluginMetrics.Count
        });

        await Task.CompletedTask;
    }

    private async Task ProcessAnalysisReport(PerformanceAnalyzer.PerformanceAnalysisReport report)
    {
        // Process critical issues
        var criticalIssues = report.Issues.Where(i => i.Severity == PerformanceAnalyzer.IssueSeverity.Critical).ToList();
        foreach (var issue in criticalIssues)
        {
            await TriggerAlert(AlertLevel.Critical, $"Critical Performance Issue: {issue.Category}", issue.Description);
        }

        // Log analysis summary
        _logger.LogInformation("Performance Analysis Report: {IssueCount} issues, {RecommendationCount} recommendations",
            report.Issues.Count, report.Recommendations.Count);
    }

    private async Task TriggerAlert(AlertLevel level, string title, string description)
    {
        _logger.LogWarning("ALERT [{Level}] {Title}: {Description}", level, title, description);
        
        // Integrate with alerting systems (email, Slack, PagerDuty, etc.)
        // Implementation would depend on your alerting infrastructure
        
        await Task.CompletedTask;
    }

    public enum AlertLevel
    {
        Info,
        Warning,
        Error,
        Critical
    }
}
```

### Performance Dashboard Controller

```csharp
[ApiController]
[Route("api/[controller]")]
public class PerformanceMonitoringController : ControllerBase
{
    private readonly IPerformanceMetricsCollector _metricsCollector;
    private readonly PerformanceAnalyzer _analyzer;
    private readonly CachePerformanceMonitor _cacheMonitor;

    public PerformanceMonitoringController(
        IPerformanceMetricsCollector metricsCollector,
        PerformanceAnalyzer analyzer,
        CachePerformanceMonitor cacheMonitor)
    {
        _metricsCollector = metricsCollector;
        _analyzer = analyzer;
        _cacheMonitor = cacheMonitor;
    }

    [HttpGet("metrics")]
    public ActionResult<PerformanceMetricsSummary> GetMetrics()
    {
        var metrics = _metricsCollector.GetMetricsSummary();
        return Ok(metrics);
    }

    [HttpGet("analysis")]
    public async Task<ActionResult<PerformanceAnalyzer.PerformanceAnalysisReport>> GetAnalysis(
        [FromQuery] int windowMinutes = 60)
    {
        var analysisWindow = TimeSpan.FromMinutes(windowMinutes);
        var report = await _analyzer.AnalyzePerformanceAsync(analysisWindow);
        return Ok(report);
    }

    [HttpGet("cache/health")]
    public async Task<ActionResult<CachePerformanceMonitor.CacheHealthReport>> GetCacheHealth()
    {
        var healthReport = await _cacheMonitor.GenerateHealthReportAsync();
        return Ok(healthReport);
    }

    [HttpGet("dashboard")]
    public async Task<ActionResult<PerformanceDashboard>> GetDashboard()
    {
        var metrics = _metricsCollector.GetMetricsSummary();
        var analysis = await _analyzer.AnalyzePerformanceAsync(TimeSpan.FromHours(1));
        var cacheHealth = await _cacheMonitor.GenerateHealthReportAsync();

        var dashboard = new PerformanceDashboard
        {
            GeneratedAt = DateTime.UtcNow,
            OverallHealth = DetermineOverallHealth(analysis),
            KeyMetrics = ExtractKeyMetrics(metrics),
            RecentIssues = analysis.Issues.Take(5).ToList(),
            TopRecommendations = analysis.Recommendations.Take(3).ToList(),
            CacheStatus = cacheHealth
        };

        return Ok(dashboard);
    }

    [HttpPost("reset")]
    public ActionResult ResetMetrics()
    {
        _metricsCollector.ResetMetrics();
        return Ok(new { Message = "Metrics reset successfully" });
    }

    private string DetermineOverallHealth(PerformanceAnalyzer.PerformanceAnalysisReport analysis)
    {
        var criticalIssues = analysis.Issues.Count(i => i.Severity == PerformanceAnalyzer.IssueSeverity.Critical);
        var errorIssues = analysis.Issues.Count(i => i.Severity == PerformanceAnalyzer.IssueSeverity.Error);

        if (criticalIssues > 0) return "Critical";
        if (errorIssues > 0) return "Warning";
        if (analysis.Issues.Any()) return "Attention";
        return "Healthy";
    }

    private object ExtractKeyMetrics(PerformanceMetricsSummary metrics)
    {
        return new
        {
            TotalServiceRegistrations = metrics.TotalServiceRegistrations,
            AverageRegistrationTime = $"{metrics.AverageRegistrationTimeMs:F1}ms",
            FailureRate = $"{metrics.RegistrationFailureRate:F1}%",
            CacheHitRate = $"{metrics.CacheMetrics.FirstOrDefault()?.HitRate ?? 0:F1}%",
            AssembliesScanned = metrics.AssemblyMetrics.Count,
            ActivePlugins = metrics.PluginMetrics.Count
        };
    }

    public class PerformanceDashboard
    {
        public DateTime GeneratedAt { get; set; }
        public string OverallHealth { get; set; } = string.Empty;
        public object KeyMetrics { get; set; } = new();
        public List<PerformanceAnalyzer.PerformanceIssue> RecentIssues { get; set; } = new();
        public List<PerformanceAnalyzer.OptimizationRecommendation> TopRecommendations { get; set; } = new();
        public CachePerformanceMonitor.CacheHealthReport CacheStatus { get; set; } = new();
    }
}
```

## 🔄 Advanced Performance Analysis

For production environments, basic performance monitoring is just the beginning. Advanced analysis helps you understand trends, predict issues, and optimize for specific deployment scenarios.

### Real-Time Performance Dashboard

Create a comprehensive dashboard that provides live insights into discovery performance:

```csharp
[ApiController]
[Route("api/[controller]")]
public class PerformanceDashboardController : ControllerBase
{
    private readonly IOptimizedDiscoveryService _discoveryService;
    private readonly PerformanceAnalyzer _performanceAnalyzer;
    private readonly CachePerformanceMonitor _cacheMonitor;
    private readonly PluginPerformanceMonitor _pluginMonitor;

    public PerformanceDashboardController(
        IOptimizedDiscoveryService discoveryService,
        PerformanceAnalyzer performanceAnalyzer,
        CachePerformanceMonitor cacheMonitor,
        PluginPerformanceMonitor pluginMonitor)
    {
        _discoveryService = discoveryService;
        _performanceAnalyzer = performanceAnalyzer;
        _cacheMonitor = cacheMonitor;
        _pluginMonitor = pluginMonitor;
    }

    [HttpGet("realtime")]
    public async Task<ActionResult<RealTimeDashboard>> GetRealTimeDashboard()
    {
        var currentMetrics = _discoveryService.GetPerformanceStatistics();
        var analysisReport = await _performanceAnalyzer.AnalyzePerformanceAsync(TimeSpan.FromMinutes(30));
        var cacheHealth = await _cacheMonitor.GenerateHealthReportAsync();
        var pluginReport = await _pluginMonitor.GeneratePerformanceReportAsync();

        var dashboard = new RealTimeDashboard
        {
            LastUpdated = DateTime.UtcNow,
            SystemStatus = DetermineSystemStatus(analysisReport),
            
            // Key Performance Indicators
            KeyMetrics = new DashboardMetrics
            {
                TotalDiscoveries = currentMetrics.TotalServiceRegistrations,
                AverageDiscoveryTime = CalculateAverageDiscoveryTime(currentMetrics),
                CacheHitRate = cacheHealth.HitRatio,
                SystemThroughput = CalculateThroughput(currentMetrics),
                ErrorRate = CalculateErrorRate(currentMetrics)
            },
            
            // Recent performance trends
            PerformanceTrends = await GetPerformanceTrends(),
            
            // Current issues and recommendations
            ActiveAlerts = analysisReport.Issues.Where(i => i.Severity >= IssueSeverity.Warning).ToList(),
            Recommendations = analysisReport.Recommendations.Take(5).ToList(),
            
            // Component health status
            ComponentHealth = new ComponentHealthStatus
            {
                CacheHealth = cacheHealth.HealthStatus.ToString(),
                PluginHealth = DeterminePluginHealth(pluginReport),
                AssemblyProcessingHealth = DetermineAssemblyHealth(currentMetrics)
            }
        };

        return Ok(dashboard);
    }

    [HttpGet("trends")]
    public async Task<ActionResult<PerformanceTrends>> GetPerformanceTrends([FromQuery] int hours = 24)
    {
        var endTime = DateTime.UtcNow;
        var startTime = endTime.AddHours(-hours);
        
        // Collect historical data points
        var dataPoints = await CollectHistoricalData(startTime, endTime);
        
        var trends = new PerformanceTrends
        {
            TimeRange = new TimeRange { Start = startTime, End = endTime },
            DiscoveryTimesTrend = CalculateDiscoveryTimeTrend(dataPoints),
            CacheHitRateTrend = CalculateCacheHitRateTrend(dataPoints),
            ThroughputTrend = CalculateThroughputTrend(dataPoints),
            ErrorRateTrend = CalculateErrorRateTrend(dataPoints),
            Anomalies = DetectAnomalies(dataPoints)
        };

        return Ok(trends);
    }

    [HttpGet("hotspots")]
    public async Task<ActionResult<PerformanceHotspots>> GetPerformanceHotspots()
    {
        var metrics = _discoveryService.GetPerformanceStatistics();
        var analysisReport = await _performanceAnalyzer.AnalyzePerformanceAsync(TimeSpan.FromHours(1));

        var hotspots = new PerformanceHotspots
        {
            // Assemblies that consume the most time
            SlowestAssemblies = metrics.AssemblyMetrics
                .OrderByDescending(a => a.AverageScanTimeMs)
                .Take(10)
                .Select(a => new AssemblyHotspot
                {
                    AssemblyName = a.AssemblyName,
                    AverageTimeMs = a.AverageScanTimeMs,
                    TotalExecutions = a.ScanCount,
                    ImpactScore = CalculateAssemblyImpactScore(a),
                    OptimizationPotential = AssessOptimizationPotential(a)
                }).ToList(),

            // Plugins with performance issues
            ProblematicPlugins = metrics.PluginMetrics
                .Where(p => p.AverageExecutionTimeMs > 200 || p.SuccessRate < 90)
                .OrderByDescending(p => p.AverageExecutionTimeMs)
                .Select(p => new PluginHotspot
                {
                    PluginName = p.PluginName,
                    AverageTimeMs = p.AverageExecutionTimeMs,
                    SuccessRate = p.SuccessRate,
                    ImpactScore = CalculatePluginImpactScore(p),
                    RecommendedActions = GeneratePluginRecommendations(p)
                }).ToList(),

            // Cache efficiency opportunities
            CacheOptimizationOpportunities = IdentifyCacheOptimizations(metrics.CacheMetrics)
        };

        return Ok(hotspots);
    }

    private SystemStatus DetermineSystemStatus(PerformanceAnalysisReport report)
    {
        var criticalIssues = report.Issues.Count(i => i.Severity == IssueSeverity.Critical);
        var errorIssues = report.Issues.Count(i => i.Severity == IssueSeverity.Error);

        if (criticalIssues > 0) return SystemStatus.Critical;
        if (errorIssues > 0) return SystemStatus.Warning;
        if (report.Issues.Any()) return SystemStatus.Attention;
        return SystemStatus.Healthy;
    }

    private double CalculateAverageDiscoveryTime(PerformanceMetricsSummary metrics)
    {
        return metrics.AssemblyMetrics.Any() 
            ? metrics.AssemblyMetrics.Average(a => a.AverageScanTimeMs)
            : 0;
    }

    private double CalculateThroughput(PerformanceMetricsSummary metrics)
    {
        var collectionDuration = metrics.CollectionDuration.TotalHours;
        return collectionDuration > 0 
            ? metrics.TotalServiceRegistrations / collectionDuration
            : 0;
    }

    private double CalculateErrorRate(PerformanceMetricsSummary metrics)
    {
        var totalOperations = metrics.AssemblyMetrics.Sum(a => a.ScanCount);
        var failedOperations = metrics.AssemblyMetrics.Sum(a => a.ScanCount - (a.ScanCount * a.SuccessRate / 100));
        return totalOperations > 0 ? (failedOperations / totalOperations) * 100 : 0;
    }

    public class RealTimeDashboard
    {
        public DateTime LastUpdated { get; set; }
        public SystemStatus SystemStatus { get; set; }
        public DashboardMetrics KeyMetrics { get; set; } = new();
        public PerformanceTrends PerformanceTrends { get; set; } = new();
        public List<PerformanceAnalyzer.PerformanceIssue> ActiveAlerts { get; set; } = new();
        public List<PerformanceAnalyzer.OptimizationRecommendation> Recommendations { get; set; } = new();
        public ComponentHealthStatus ComponentHealth { get; set; } = new();
    }

    public class DashboardMetrics
    {
        public long TotalDiscoveries { get; set; }
        public double AverageDiscoveryTime { get; set; }
        public double CacheHitRate { get; set; }
        public double SystemThroughput { get; set; }
        public double ErrorRate { get; set; }
    }

    public enum SystemStatus
    {
        Healthy,
        Attention,
        Warning,
        Critical
    }
}
```

### Predictive Performance Analysis

Implement predictive analysis to anticipate performance issues before they impact users:

```csharp
public class PredictivePerformanceAnalyzer
{
    private readonly IPerformanceMetricsCollector _metricsCollector;
    private readonly ILogger<PredictivePerformanceAnalyzer> _logger;
    private readonly List<PerformanceDataPoint> _historicalData = new();

    public PredictivePerformanceAnalyzer(
        IPerformanceMetricsCollector metricsCollector,
        ILogger<PredictivePerformanceAnalyzer> logger)
    {
        _metricsCollector = metricsCollector;
        _logger = logger;
    }

    public async Task<PredictiveAnalysisResult> AnalyzeTrendsAsync(TimeSpan lookbackPeriod, TimeSpan forecastPeriod)
    {
        var historicalData = await CollectHistoricalData(lookbackPeriod);
        var trends = AnalyzeTrends(historicalData);
        var predictions = GeneratePredictions(trends, forecastPeriod);
        var alerts = GeneratePredictiveAlerts(predictions);

        return new PredictiveAnalysisResult
        {
            AnalysisPeriod = lookbackPeriod,
            ForecastPeriod = forecastPeriod,
            HistoricalTrends = trends,
            Predictions = predictions,
            PredictiveAlerts = alerts,
            ConfidenceLevel = CalculateConfidenceLevel(historicalData),
            RecommendedActions = GeneratePreventiveActions(predictions, alerts)
        };
    }

    private TrendAnalysis AnalyzeTrends(List<PerformanceDataPoint> data)
    {
        return new TrendAnalysis
        {
            PerformanceTrend = CalculatePerformanceTrend(data),
            CacheEfficiencyTrend = CalculateCacheEfficiencyTrend(data),
            ErrorRateTrend = CalculateErrorRateTrend(data),
            ResourceUtilizationTrend = CalculateResourceUtilizationTrend(data),
            TrendConfidence = CalculateTrendConfidence(data)
        };
    }

    private List<PerformancePrediction> GeneratePredictions(TrendAnalysis trends, TimeSpan forecastPeriod)
    {
        var predictions = new List<PerformancePrediction>();

        // Predict performance degradation
        if (trends.PerformanceTrend.Slope > 0.1) // Performance getting worse
        {
            var degradationPrediction = new PerformancePrediction
            {
                Type = PredictionType.PerformanceDegradation,
                Probability = CalculateDegradationProbability(trends.PerformanceTrend),
                EstimatedTimeToImpact = EstimateTimeToImpact(trends.PerformanceTrend),
                ExpectedImpact = "Discovery times may increase by 20-50%",
                Severity = trends.PerformanceTrend.Slope > 0.3 ? PredictionSeverity.High : PredictionSeverity.Medium
            };
            predictions.Add(degradationPrediction);
        }

        // Predict cache efficiency decline
        if (trends.CacheEfficiencyTrend.Slope < -0.05) // Cache getting less efficient
        {
            var cacheDeclinePrediction = new PerformancePrediction
            {
                Type = PredictionType.CacheEfficiencyDecline,
                Probability = CalculateCacheDeclineProbability(trends.CacheEfficiencyTrend),
                EstimatedTimeToImpact = EstimateTimeToImpact(trends.CacheEfficiencyTrend),
                ExpectedImpact = "Cache hit rate may decrease, leading to longer discovery times",
                Severity = PredictionSeverity.Medium
            };
            predictions.Add(cacheDeclinePrediction);
        }

        // Predict resource capacity issues
        if (trends.ResourceUtilizationTrend.Slope > 0.2)
        {
            var capacityPrediction = new PerformancePrediction
            {
                Type = PredictionType.CapacityLimit,
                Probability = CalculateCapacityLimitProbability(trends.ResourceUtilizationTrend),
                EstimatedTimeToImpact = EstimateCapacityLimitTime(trends.ResourceUtilizationTrend),
                ExpectedImpact = "System may approach resource limits, causing performance bottlenecks",
                Severity = PredictionSeverity.High
            };
            predictions.Add(capacityPrediction);
        }

        return predictions;
    }

    private List<PredictiveAlert> GeneratePredictiveAlerts(List<PerformancePrediction> predictions)
    {
        var alerts = new List<PredictiveAlert>();

        foreach (var prediction in predictions.Where(p => p.Probability > 0.7 && p.Severity >= PredictionSeverity.Medium))
        {
            alerts.Add(new PredictiveAlert
            {
                AlertId = Guid.NewGuid().ToString(),
                Type = prediction.Type,
                Severity = prediction.Severity,
                Title = GetAlertTitle(prediction.Type),
                Description = prediction.ExpectedImpact,
                EstimatedOccurrence = DateTime.UtcNow.Add(prediction.EstimatedTimeToImpact),
                Probability = prediction.Probability,
                RecommendedActions = GetRecommendedActions(prediction.Type),
                CreatedAt = DateTime.UtcNow
            });
        }

        return alerts;
    }

    private List<PreventiveAction> GeneratePreventiveActions(
        List<PerformancePrediction> predictions,
        List<PredictiveAlert> alerts)
    {
        var actions = new List<PreventiveAction>();

        // High-priority preventive actions based on predictions
        if (predictions.Any(p => p.Type == PredictionType.PerformanceDegradation && p.Severity == PredictionSeverity.High))
        {
            actions.Add(new PreventiveAction
            {
                Priority = ActionPriority.High,
                Category = "Performance Optimization",
                Title = "Immediate Performance Review Required",
                Description = "System trending toward significant performance degradation",
                EstimatedEffort = "2-4 hours",
                ExpectedBenefit = "Prevent 20-50% performance degradation",
                ActionItems = new[]
                {
                    "Review recent changes to assembly loading patterns",
                    "Analyze memory usage trends for potential leaks",
                    "Evaluate cache configuration for optimization opportunities",
                    "Consider implementing additional performance optimizations"
                }
            });
        }

        if (predictions.Any(p => p.Type == PredictionType.CacheEfficiencyDecline))
        {
            actions.Add(new PreventiveAction
            {
                Priority = ActionPriority.Medium,
                Category = "Cache Optimization",
                Title = "Cache Strategy Review",
                Description = "Cache efficiency declining, proactive optimization recommended",
                EstimatedEffort = "1-2 hours",
                ExpectedBenefit = "Maintain or improve cache hit rates",
                ActionItems = new[]
                {
                    "Review cache size and eviction policies",
                    "Analyze cache usage patterns for optimization",
                    "Consider implementing cache warming strategies",
                    "Evaluate cache invalidation logic"
                }
            });
        }

        return actions;
    }

    public class PredictiveAnalysisResult
    {
        public TimeSpan AnalysisPeriod { get; set; }
        public TimeSpan ForecastPeriod { get; set; }
        public TrendAnalysis HistoricalTrends { get; set; } = new();
        public List<PerformancePrediction> Predictions { get; set; } = new();
        public List<PredictiveAlert> PredictiveAlerts { get; set; } = new();
        public double ConfidenceLevel { get; set; }
        public List<PreventiveAction> RecommendedActions { get; set; } = new();
    }

    public class PerformancePrediction
    {
        public PredictionType Type { get; set; }
        public double Probability { get; set; }
        public TimeSpan EstimatedTimeToImpact { get; set; }
        public string ExpectedImpact { get; set; } = string.Empty;
        public PredictionSeverity Severity { get; set; }
    }

    public enum PredictionType
    {
        PerformanceDegradation,
        CacheEfficiencyDecline,
        CapacityLimit,
        ErrorRateIncrease
    }

    public enum PredictionSeverity
    {
        Low,
        Medium,
        High,
        Critical
    }
}
```

## 📊 Performance Optimization Strategies

Based on monitoring data, implement targeted optimization strategies that address specific performance bottlenecks.

### Assembly-Level Optimizations

```csharp
public class AssemblyOptimizationEngine
{
    private readonly IPerformanceMetricsCollector _metricsCollector;
    private readonly PerformanceAnalyzer _analyzer;
    private readonly ILogger<AssemblyOptimizationEngine> _logger;

    public AssemblyOptimizationEngine(
        IPerformanceMetricsCollector metricsCollector,
        PerformanceAnalyzer analyzer,
        ILogger<AssemblyOptimizationEngine> logger)
    {
        _metricsCollector = metricsCollector;
        _analyzer = analyzer;
        _logger = logger;
    }

    public async Task<AssemblyOptimizationPlan> GenerateOptimizationPlanAsync()
    {
        var metrics = _metricsCollector.GetMetricsSummary();
        var analysisReport = await _analyzer.AnalyzePerformanceAsync(TimeSpan.FromDays(7));

        var plan = new AssemblyOptimizationPlan
        {
            GeneratedAt = DateTime.UtcNow,
            AnalysisPeriod = TimeSpan.FromDays(7),
            OptimizationOpportunities = IdentifyOptimizationOpportunities(metrics, analysisReport),
            PrioritizedActions = PrioritizeOptimizationActions(metrics, analysisReport),
            ExpectedBenefits = CalculateExpectedBenefits(metrics, analysisReport)
        };

        return plan;
    }

    private List<OptimizationOpportunity> IdentifyOptimizationOpportunities(
        PerformanceMetricsSummary metrics,
        PerformanceAnalyzer.PerformanceAnalysisReport analysisReport)
    {
        var opportunities = new List<OptimizationOpportunity>();

        // Identify slow assemblies that could benefit from filtering
        var slowAssemblies = metrics.AssemblyMetrics
            .Where(a => a.AverageScanTimeMs > 200)
            .OrderByDescending(a => a.AverageScanTimeMs * a.ScanCount) // Impact = time * frequency
            .Take(5);

        foreach (var assembly in slowAssemblies)
        {
            opportunities.Add(new OptimizationOpportunity
            {
                Type = OptimizationType.AssemblyFiltering,
                TargetComponent = assembly.AssemblyName,
                CurrentPerformance = assembly.AverageScanTimeMs,
                EstimatedImprovement = EstimateFilteringImprovement(assembly),
                ImplementationEffort = EstimateFilteringEffort(assembly),
                Description = $"Assembly {assembly.AssemblyName} takes {assembly.AverageScanTimeMs:F1}ms on average. Consider namespace filtering or type exclusions.",
                RecommendedActions = GenerateFilteringRecommendations(assembly)
            });
        }

        // Identify assemblies with low service density (many types, few services)
        var lowDensityAssemblies = metrics.AssemblyMetrics
            .Where(a => a.TotalServicesFound > 0)
            .Where(a => CalculateServiceDensity(a) < 0.05) // Less than 5% of types are services
            .OrderBy(a => CalculateServiceDensity(a));

        foreach (var assembly in lowDensityAssemblies)
        {
            opportunities.Add(new OptimizationOpportunity
            {
                Type = OptimizationType.ServiceDensityOptimization,
                TargetComponent = assembly.AssemblyName,
                CurrentPerformance = CalculateServiceDensity(assembly),
                EstimatedImprovement = 0.3, // 30% improvement potential
                ImplementationEffort = ImplementationEffort.Medium,
                Description = $"Assembly {assembly.AssemblyName} has low service density. Consider targeted type filtering.",
                RecommendedActions = GenerateDensityOptimizationRecommendations(assembly)
            });
        }

        return opportunities;
    }

    private List<PrioritizedAction> PrioritizeOptimizationActions(
        PerformanceMetricsSummary metrics,
        PerformanceAnalyzer.PerformanceAnalysisReport analysisReport)
    {
        var actions = new List<PrioritizedAction>();

        // High-impact, low-effort optimizations first
        if (analysisReport.CacheAnalysis?.HitRate < 60)
        {
            actions.Add(new PrioritizedAction
            {
                Priority = 1,
                Title = "Optimize Cache Configuration",
                Impact = ActionImpact.High,
                Effort = ImplementationEffort.Low,
                Description = "Current cache hit rate is low. Adjusting cache size and retention policies could significantly improve performance.",
                Steps = new[]
                {
                    "Increase cache size by 50%",
                    "Extend cache retention time to 2 hours",
                    "Implement cache prewarming for frequently accessed assemblies",
                    "Monitor cache performance for 1 week"
                },
                EstimatedBenefit = "20-40% improvement in discovery times"
            });
        }

        // Medium-impact optimizations
        var highFrequencySlowAssemblies = metrics.AssemblyMetrics
            .Where(a => a.AverageScanTimeMs > 150 && a.ScanCount > 10)
            .OrderByDescending(a => a.AverageScanTimeMs * a.ScanCount);

        foreach (var assembly in highFrequencySlowAssemblies.Take(3))
        {
            actions.Add(new PrioritizedAction
            {
                Priority = actions.Count + 2,
                Title = $"Optimize {assembly.AssemblyName} Scanning",
                Impact = ActionImpact.Medium,
                Effort = ImplementationEffort.Medium,
                Description = $"Assembly {assembly.AssemblyName} is frequently scanned and slow.",
                Steps = GenerateAssemblyOptimizationSteps(assembly),
                EstimatedBenefit = $"Reduce {assembly.AssemblyName} scan time by 30-50%"
            });
        }

        return actions.OrderBy(a => a.Priority).ToList();
    }

    private double CalculateServiceDensity(PerformanceMetricsSummary.AssemblySummary assembly)
    {
        // This would need additional metrics about total types in assembly
        // For now, we'll estimate based on services found and scan time
        var estimatedTotalTypes = assembly.AverageScanTimeMs * 10; // Rough estimate
        return assembly.TotalServicesFound / Math.Max(estimatedTotalTypes, 1);
    }

    public class AssemblyOptimizationPlan
    {
        public DateTime GeneratedAt { get; set; }
        public TimeSpan AnalysisPeriod { get; set; }
        public List<OptimizationOpportunity> OptimizationOpportunities { get; set; } = new();
        public List<PrioritizedAction> PrioritizedActions { get; set; } = new();
        public ExpectedBenefits ExpectedBenefits { get; set; } = new();
    }

    public class OptimizationOpportunity
    {
        public OptimizationType Type { get; set; }
        public string TargetComponent { get; set; } = string.Empty;
        public double CurrentPerformance { get; set; }
        public double EstimatedImprovement { get; set; }
        public ImplementationEffort ImplementationEffort { get; set; }
        public string Description { get; set; } = string.Empty;
        public string[] RecommendedActions { get; set; } = Array.Empty<string>();
    }

    public enum OptimizationType
    {
        AssemblyFiltering,
        ServiceDensityOptimization,
        CacheOptimization,
        PluginOptimization,
        ParallelProcessingTuning
    }

    public enum ImplementationEffort
    {
        Low,
        Medium,
        High
    }

    public enum ActionImpact
    {
        Low,
        Medium,
        High,
        Critical
    }
}
```

## 🚨 Automated Alerting and Notifications

Implement comprehensive alerting to proactively identify and respond to performance issues.

### Intelligent Alert System

```csharp
public class PerformanceAlertingService : BackgroundService
{
    private readonly IPerformanceMetricsCollector _metricsCollector;
    private readonly PerformanceAnalyzer _analyzer;
    private readonly INotificationService _notificationService;
    private readonly ILogger<PerformanceAlertingService> _logger;
    private readonly List<AlertRule> _alertRules;

    public PerformanceAlertingService(
        IPerformanceMetricsCollector metricsCollector,
        PerformanceAnalyzer analyzer,
        INotificationService notificationService,
        ILogger<PerformanceAlertingService> logger)
    {
        _metricsCollector = metricsCollector;
        _analyzer = analyzer;
        _notificationService = notificationService;
        _logger = logger;
        _alertRules = InitializeAlertRules();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await EvaluateAlertRules();
                await Task.Delay(TimeSpan.FromMinutes(2), stoppingToken); // Check every 2 minutes
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in performance alerting service");
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken); // Shorter delay on error
            }
        }
    }

    private async Task EvaluateAlertRules()
    {
        var metrics = _metricsCollector.GetMetricsSummary();
        var analysisReport = await _analyzer.AnalyzePerformanceAsync(TimeSpan.FromMinutes(15));

        foreach (var rule in _alertRules)
        {
            try
            {
                var alertCondition = await EvaluateAlertRule(rule, metrics, analysisReport);
                if (alertCondition.ShouldTrigger)
                {
                    await TriggerAlert(rule, alertCondition);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error evaluating alert rule {RuleName}", rule.Name);
            }
        }
    }

    private async Task<AlertCondition> EvaluateAlertRule(
        AlertRule rule,
        PerformanceMetricsSummary metrics,
        PerformanceAnalyzer.PerformanceAnalysisReport analysisReport)
    {
        return rule.Type switch
        {
            AlertType.PerformanceDegradation => await EvaluatePerformanceDegradation(rule, metrics),
            AlertType.CacheEfficiency => await EvaluateCacheEfficiency(rule, metrics),
            AlertType.ErrorRate => await EvaluateErrorRate(rule, metrics),
            AlertType.ResourceUtilization => await EvaluateResourceUtilization(rule, metrics),
            AlertType.TrendAlert => await EvaluateTrendAlert(rule, metrics),
            _ => new AlertCondition { ShouldTrigger = false }
        };
    }

    private async Task<AlertCondition> EvaluatePerformanceDegradation(AlertRule rule, PerformanceMetricsSummary metrics)
    {
        var currentAvgTime = metrics.AssemblyMetrics.Any() 
            ? metrics.AssemblyMetrics.Average(a => a.AverageScanTimeMs)
            : 0;

        var baselineAvgTime = await GetBaselinePerformance();
        var degradationPercentage = baselineAvgTime > 0 
            ? ((currentAvgTime - baselineAvgTime) / baselineAvgTime) * 100
            : 0;

        return new AlertCondition
        {
            ShouldTrigger = degradationPercentage > rule.Threshold,
            CurrentValue = degradationPercentage,
            Threshold = rule.Threshold,
            Severity = degradationPercentage > rule.CriticalThreshold ? AlertSeverity.Critical : AlertSeverity.Warning,
            Context = new Dictionary<string, object>
            {
                ["CurrentAvgTime"] = currentAvgTime,
                ["BaselineAvgTime"] = baselineAvgTime,
                ["DegradationPercentage"] = degradationPercentage
            }
        };
    }

    private async Task<AlertCondition> EvaluateCacheEfficiency(AlertRule rule, PerformanceMetricsSummary metrics)
    {
        var averageHitRate = metarics.CacheMetrics.Any() 
            ? metrics.CacheMetrics.Average(c => c.HitRate)
            : 0;

        return new AlertCondition
        {
            ShouldTrigger = averageHitRate < rule.Threshold,
            CurrentValue = averageHitRate,
            Threshold = rule.Threshold,
            Severity = averageHitRate < rule.CriticalThreshold ? AlertSeverity.Critical : AlertSeverity.Warning,
            Context = new Dictionary<string, object>
            {
                ["CacheHitRate"] = averageHitRate,
                ["CacheOperationCount"] = metrics.CacheMetrics.Sum(c => c.OperationCount)
            }
        };
    }

    private async Task ExportToPrometheus(PerformanceMetricsSummary metrics)
    {
        // Export key metrics to Prometheus
        // This would integrate with your Prometheus metrics registry
        
        // Example metrics:
        // service_discovery_registration_total
        // service_discovery_registration_duration_seconds
        // service_discovery_cache_hit_ratio
        // service_discovery_assembly_scan_duration_seconds
        // service_discovery_plugin_execution_duration_seconds
        
        await Task.CompletedTask; // Placeholder for actual implementation
    }

    private async Task ExportToApplicationInsights(PerformanceMetricsSummary metrics)
    {
        // Export to Application Insights or similar APM tool
        using var scope = _scopeFactory.CreateScope();
        
        // Log structured data for APM analysis
        _logger.LogInformation("Service Discovery Performance Metrics: {@Metrics}", new
        {
            TotalRegistrations = metrics.TotalServiceRegistrations,
            AverageRegistrationTime = metrics.AverageRegistrationTimeMs,
            FailureRate = metrics.RegistrationFailureRate,
            CacheHitRate = metrics.CacheMetrics.FirstOrDefault()?.HitRate ?? 0,
            AssemblyCount = metrics.AssemblyMetrics.Count,
            PluginCount = metrics.PluginMetrics.Count
        });

        await Task.CompletedTask;
    }

    private async Task ProcessAnalysisReport(PerformanceAnalyzer.PerformanceAnalysisReport report)
    {
        // Process critical issues
        var criticalIssues = report.Issues.Where(i => i.Severity == PerformanceAnalyzer.IssueSeverity.Critical).ToList();
        foreach (var issue in criticalIssues)
        {
            await TriggerAlert(AlertLevel.Critical, $"Critical Performance Issue: {issue.Category}", issue.Description);
        }

        // Log analysis summary
        _logger.LogInformation("Performance Analysis Report: {IssueCount} issues, {RecommendationCount} recommendations",
            report.Issues.Count, report.Recommendations.Count);
    }

    private async Task TriggerAlert(AlertLevel level, string title, string description)
    {
        _logger.LogWarning("ALERT [{Level}] {title}: {Description}", level, title, description);
        
        // Integrate with alerting systems (email, Slack, PagerDuty, etc.)
        // Implementation would depend on your alerting infrastructure
        
        await Task.CompletedTask;
    }

    public enum AlertLevel
    {
        Info,
        Warning,
        Error,
        Critical
    }
}
```

## 📊 Performance Dashboard and Visualization

Building effective performance dashboards transforms raw metrics into actionable insights. Let's create comprehensive dashboard components that provide real-time visibility into discovery performance.

### Real-Time Performance Dashboard

```csharp
[ApiController]
[Route("api/[controller]")]
public class PerformanceMonitoringController : ControllerBase
{
    private readonly IPerformanceMetricsCollector _metricsCollector;
    private readonly PerformanceAnalyzer _analyzer;
    private readonly CachePerformanceMonitor _cacheMonitor;
    private readonly PluginPerformanceMonitor _pluginMonitor;

    public PerformanceMonitoringController(
        IPerformanceMetricsCollector metricsCollector,
        PerformanceAnalyzer analyzer,
        CachePerformanceMonitor cacheMonitor,
        PluginPerformanceMonitor pluginMonitor)
    {
        _metricsCollector = metricsCollector;
        _analyzer = analyzer;
        _cacheMonitor = cacheMonitor;
        _pluginMonitor = pluginMonitor;
    }

    [HttpGet("metrics")]
    public ActionResult<PerformanceMetricsSummary> GetMetrics()
    {
        var metrics = _metricsCollector.GetMetricsSummary();
        return Ok(metrics);
    }

    [HttpGet("analysis")]
    public async Task<ActionResult<PerformanceAnalyzer.PerformanceAnalysisReport>> GetAnalysis(
        [FromQuery] int windowMinutes = 60)
    {
        var analysisWindow = TimeSpan.FromMinutes(windowMinutes);
        var report = await _analyzer.AnalyzePerformanceAsync(analysisWindow);
        return Ok(report);
    }

    [HttpGet("cache/health")]
    public async Task<ActionResult<CachePerformanceMonitor.CacheHealthReport>> GetCacheHealth()
    {
        var healthReport = await _cacheMonitor.GenerateHealthReportAsync();
        return Ok(healthReport);
    }

    [HttpGet("plugins/performance")]
    public async Task<ActionResult<PluginPerformanceMonitor.PluginPerformanceReport>> GetPluginPerformance()
    {
        var pluginReport = await _pluginMonitor.GeneratePerformanceReportAsync();
        return Ok(pluginReport);
    }

    [HttpGet("dashboard")]
    public async Task<ActionResult<PerformanceDashboard>> GetDashboard()
    {
        var metrics = _metricsCollector.GetMetricsSummary();
        var analysis = await _analyzer.AnalyzePerformanceAsync(TimeSpan.FromHours(1));
        var cacheHealth = await _cacheMonitor.GenerateHealthReportAsync();
        var pluginReport = await _pluginMonitor.GeneratePerformanceReportAsync();

        var dashboard = new PerformanceDashboard
        {
            GeneratedAt = DateTime.UtcNow,
            OverallHealth = DetermineOverallHealth(analysis),
            KeyMetrics = ExtractKeyMetrics(metrics),
            RecentIssues = analysis.Issues.Take(5).ToList(),
            TopRecommendations = analysis.Recommendations.Take(3).ToList(),
            CacheStatus = cacheHealth,
            PluginStatus = pluginReport,
            TrendData = await GetTrendData()
        };

        return Ok(dashboard);
    }

    [HttpPost("reset")]
    public ActionResult ResetMetrics()
    {
        _metricsCollector.ResetMetrics();
        return Ok(new { Message = "Metrics reset successfully" });
    }

    [HttpGet("health")]
    public async Task<ActionResult<SystemHealthStatus>> GetSystemHealth()
    {
        var metrics = _metricsCollector.GetMetricsSummary();
        var analysis = await _analyzer.AnalyzePerformanceAsync(TimeSpan.FromMinutes(30));
        
        var health = new SystemHealthStatus
        {
            OverallStatus = DetermineSystemHealth(metrics, analysis),
            LastChecked = DateTime.UtcNow,
            Components = new Dictionary<string, ComponentHealth>
            {
                ["Assembly Scanning"] = AnalyzeAssemblyScanningHealth(metrics),
                ["Cache Performance"] = await AnalyzeCacheHealth(),
                ["Plugin Execution"] = AnalyzePluginHealth(metrics),
                ["Service Registration"] = AnalyzeRegistrationHealth(metrics)
            }
        };

        return Ok(health);
    }

    private async Task<TrendData> GetTrendData()
    {
        // This would typically query a time-series database
        // For this example, we'll return sample trend data
        return new TrendData
        {
            TimeWindow = TimeSpan.FromHours(24),
            DiscoveryTimesTrend = GenerateSampleTrend("DiscoveryTimes", 24),
            CacheHitRateTrend = GenerateSampleTrend("CacheHitRate", 24),
            ServiceCountTrend = GenerateSampleTrend("ServiceCount", 24),
            ErrorRateTrend = GenerateSampleTrend("ErrorRate", 24)
        };
    }

    private List<DataPoint> GenerateSampleTrend(string metricName, int hours)
    {
        var points = new List<DataPoint>();
        var baseTime = DateTime.UtcNow.AddHours(-hours);
        
        for (int i = 0; i < hours; i++)
        {
            points.Add(new DataPoint
            {
                Timestamp = baseTime.AddHours(i),
                Value = metricName switch
                {
                    "DiscoveryTimes" => Random.Shared.NextDouble() * 1000 + 200,
                    "CacheHitRate" => Random.Shared.NextDouble() * 40 + 60,
                    "ServiceCount" => Random.Shared.Next(50, 200),
                    "ErrorRate" => Random.Shared.NextDouble() * 5,
                    _ => 0
                }
            });
        }
        
        return points;
    }

    private string DetermineOverallHealth(PerformanceAnalyzer.PerformanceAnalysisReport analysis)
    {
        var criticalIssues = analysis.Issues.Count(i => i.Severity == PerformanceAnalyzer.IssueSeverity.Critical);
        var errorIssues = analysis.Issues.Count(i => i.Severity == PerformanceAnalyzer.IssueSeverity.Error);

        if (criticalIssues > 0) return "Critical";
        if (errorIssues > 0) return "Warning";
        if (analysis.Issues.Any()) return "Attention";
        return "Healthy";
    }

    private string DetermineSystemHealth(PerformanceMetricsSummary metrics, PerformanceAnalyzer.PerformanceAnalysisReport analysis)
    {
        var score = 100;

        // Deduct points for performance issues
        if (metrics.AverageRegistrationTimeMs > 500) score -= 20;
        if (metrics.RegistrationFailureRate > 5) score -= 30;
        
        var cacheHitRate = metrics.CacheMetrics.FirstOrDefault()?.HitRate ?? 0;
        if (cacheHitRate < 60) score -= 15;

        // Deduct points for issues
        score -= analysis.Issues.Count(i => i.Severity == PerformanceAnalyzer.IssueSeverity.Critical) * 20;
        score -= analysis.Issues.Count(i => i.Severity == PerformanceAnalyzer.IssueSeverity.Error) * 10;

        return score switch
        {
            >= 90 => "Excellent",
            >= 70 => "Good",
            >= 50 => "Fair",
            >= 30 => "Poor",
            _ => "Critical"
        };
    }

    private ComponentHealth AnalyzeAssemblyScanningHealth(PerformanceMetricsSummary metrics)
    {
        var avgScanTime = metrics.AssemblyMetrics.Any() 
            ? metrics.AssemblyMetrics.Average(a => a.AverageScanTimeMs)
            : 0;
        
        var avgSuccessRate = metrics.AssemblyMetrics.Any()
            ? metrics.AssemblyMetrics.Average(a => a.SuccessRate)
            : 100;

        var status = (avgScanTime, avgSuccessRate) switch
        {
            (< 100, > 95) => "Excellent",
            (< 200, > 90) => "Good",
            (< 500, > 80) => "Fair",
            _ => "Poor"
        };

        return new ComponentHealth
        {
            Status = status,
            LastChecked = DateTime.UtcNow,
            Metrics = new Dictionary<string, object>
            {
                ["Average Scan Time (ms)"] = avgScanTime,
                ["Average Success Rate (%)"] = avgSuccessRate,
                ["Assembly Count"] = metrics.AssemblyMetrics.Count
            }
        };
    }

    private async Task<ComponentHealth> AnalyzeCacheHealth()
    {
        var cacheHealth = await _cacheMonitor.GenerateHealthReportAsync();
        
        return new ComponentHealth
        {
            Status = cacheHealth.HealthStatus.ToString(),
            LastChecked = DateTime.UtcNow,
            Metrics = new Dictionary<string, object>
            {
                ["Hit Ratio (%)"] = cacheHealth.HitRatio,
                ["Total Requests"] = cacheHealth.TotalRequests,
                ["Cached Assemblies"] = cacheHealth.CachedAssembliesCount
            }
        };
    }

    private ComponentHealth AnalyzePluginHealth(PerformanceMetricsSummary metrics)
    {
        if (!metrics.PluginMetrics.Any())
        {
            return new ComponentHealth
            {
                Status = "N/A",
                LastChecked = DateTime.UtcNow,
                Metrics = new Dictionary<string, object> { ["Plugin Count"] = 0 }
            };
        }

        var avgSuccessRate = metrics.PluginMetrics.Average(p => p.SuccessRate);
        var avgExecutionTime = metrics.PluginMetrics.Average(p => p.AverageExecutionTimeMs);

        var status = (avgSuccessRate, avgExecutionTime) switch
        {
            (> 95, < 100) => "Excellent",
            (> 90, < 200) => "Good",
            (> 80, < 500) => "Fair",
            _ => "Poor"
        };

        return new ComponentHealth
        {
            Status = status,
            LastChecked = DateTime.UtcNow,
            Metrics = new Dictionary<string, object>
            {
                ["Average Success Rate (%)"] = avgSuccessRate,
                ["Average Execution Time (ms)"] = avgExecutionTime,
                ["Plugin Count"] = metrics.PluginMetrics.Count
            }
        };
    }

    private ComponentHealth AnalyzeRegistrationHealth(PerformanceMetricsSummary metrics)
    {
        var status = (metrics.AverageRegistrationTimeMs, metrics.RegistrationFailureRate) switch
        {
            (< 50, < 1) => "Excellent",
            (< 100, < 3) => "Good",
            (< 200, < 5) => "Fair",
            _ => "Poor"
        };

        return new ComponentHealth
        {
            Status = status,
            LastChecked = DateTime.UtcNow,
            Metrics = new Dictionary<string, object>
            {
                ["Average Registration Time (ms)"] = metrics.AverageRegistrationTimeMs,
                ["Failure Rate (%)"] = metrics.RegistrationFailureRate,
                ["Total Registrations"] = metrics.TotalServiceRegistrations
            }
        };
    }

    private object ExtractKeyMetrics(PerformanceMetricsSummary metrics)
    {
        return new
        {
            TotalServiceRegistrations = metrics.TotalServiceRegistrations,
            AverageRegistrationTime = $"{metrics.AverageRegistrationTimeMs:F1}ms",
            FailureRate = $"{metrics.RegistrationFailureRate:F1}%",
            CacheHitRate = $"{metrics.CacheMetrics.FirstOrDefault()?.HitRate ?? 0:F1}%",
            AssembliesScanned = metrics.AssemblyMetrics.Count,
            ActivePlugins = metrics.PluginMetrics.Count,
            CollectionDuration = metrics.CollectionDuration.ToString(@"hh\:mm\:ss")
        };
    }

    public class PerformanceDashboard
    {
        public DateTime GeneratedAt { get; set; }
        public string OverallHealth { get; set; } = string.Empty;
        public object KeyMetrics { get; set; } = new();
        public List<PerformanceAnalyzer.PerformanceIssue> RecentIssues { get; set; } = new();
        public List<PerformanceAnalyzer.OptimizationRecommendation> TopRecommendations { get; set; } = new();
        public CachePerformanceMonitor.CacheHealthReport CacheStatus { get; set; } = new();
        public PluginPerformanceMonitor.PluginPerformanceReport PluginStatus { get; set; } = new();
        public TrendData TrendData { get; set; } = new();
    }

    public class SystemHealthStatus
    {
        public string OverallStatus { get; set; } = string.Empty;
        public DateTime LastChecked { get; set; }
        public Dictionary<string, ComponentHealth> Components { get; set; } = new();
    }

    public class ComponentHealth
    {
        public string Status { get; set; } = string.Empty;
        public DateTime LastChecked { get; set; }
        public Dictionary<string, object> Metrics { get; set; } = new();
    }

    public class TrendData
    {
        public TimeSpan TimeWindow { get; set; }
        public List<DataPoint> DiscoveryTimesTrend { get; set; } = new();
        public List<DataPoint> CacheHitRateTrend { get; set; } = new();
        public List<DataPoint> ServiceCountTrend { get; set; } = new();
        public List<DataPoint> ErrorRateTrend { get; set; } = new();
    }

    public class DataPoint
    {
        public DateTime Timestamp { get; set; }
        public double Value { get; set; }
    }
}
```

## 🔍 Advanced Performance Analysis

For production environments, basic performance monitoring is just the beginning. Advanced analysis helps you understand trends, predict issues, and optimize for specific deployment scenarios.

### Predictive Performance Analysis

```csharp
public class PredictivePerformanceAnalyzer
{
    private readonly IPerformanceMetricsCollector _metricsCollector;
    private readonly ILogger<PredictivePerformanceAnalyzer> _logger;
    private readonly List<PerformanceDataPoint> _historicalData = new();

    public PredictivePerformanceAnalyzer(
        IPerformanceMetricsCollector metricsCollector,
        ILogger<PredictivePerformanceAnalyzer> logger)
    {
        _metricsCollector = metricsCollector;
        _logger = logger;
    }

    public async Task<PredictiveAnalysisResult> AnalyzeTrendsAsync(TimeSpan lookbackPeriod, TimeSpan forecastPeriod)
    {
        var historicalData = await CollectHistoricalData(lookbackPeriod);
        var trends = AnalyzeTrends(historicalData);
        var predictions = GeneratePredictions(trends, forecastPeriod);
        var alerts = GeneratePredictiveAlerts(predictions);

        return new PredictiveAnalysisResult
        {
            AnalysisPeriod = lookbackPeriod,
            ForecastPeriod = forecastPeriod,
            HistoricalTrends = trends,
            Predictions = predictions,
            PredictiveAlerts = alerts,
            ConfidenceLevel = CalculateConfidenceLevel(historicalData),
            RecommendedActions = GeneratePreventiveActions(predictions, alerts)
        };
    }

    private async Task<List<PerformanceDataPoint>> CollectHistoricalData(TimeSpan lookbackPeriod)
    {
        // In a real implementation, this would query a time-series database
        // For this example, we'll generate sample historical data
        var dataPoints = new List<PerformanceDataPoint>();
        var startTime = DateTime.UtcNow.Subtract(lookbackPeriod);
        
        for (var time = startTime; time <= DateTime.UtcNow; time = time.AddHours(1))
        {
            dataPoints.Add(new PerformanceDataPoint
            {
                Timestamp = time,
                DiscoveryTimeMs = Random.Shared.NextDouble() * 500 + 100,
                CacheHitRate = Random.Shared.NextDouble() * 40 + 60,
                ErrorRate = Random.Shared.NextDouble() * 3,
                ServiceCount = Random.Shared.Next(50, 200),
                MemoryUsageMb = Random.Shared.NextDouble() * 100 + 50
            });
        }

        await Task.CompletedTask;
        return dataPoints;
    }

    private TrendAnalysis AnalyzeTrends(List<PerformanceDataPoint> data)
    {
        if (data.Count < 2)
        {
            return new TrendAnalysis();
        }

        return new TrendAnalysis
        {
            PerformanceTrend = CalculateLinearTrend(data.Select(d => d.DiscoveryTimeMs).ToList()),
            CacheEfficiencyTrend = CalculateLinearTrend(data.Select(d => d.CacheHitRate).ToList()),
            ErrorRateTrend = CalculateLinearTrend(data.Select(d => d.ErrorRate).ToList()),
            MemoryUsageTrend = CalculateLinearTrend(data.Select(d => d.MemoryUsageMb).ToList()),
            TrendConfidence = CalculateTrendConfidence(data)
        };
    }

    private LinearTrend CalculateLinearTrend(List<double> values)
    {
        if (values.Count < 2)
        {
            return new LinearTrend { Slope = 0, Intercept = values.FirstOrDefault(), RSquared = 0 };
        }

        var n = values.Count;
        var xValues = Enumerable.Range(0, n).Select(i => (double)i).ToList();
        
        var xMean = xValues.Average();
        var yMean = values.Average();
        
        var numerator = xValues.Zip(values, (x, y) => (x - xMean) * (y - yMean)).Sum();
        var denominator = xValues.Select(x => Math.Pow(x - xMean, 2)).Sum();
        
        var slope = denominator != 0 ? numerator / denominator : 0;
        var intercept = yMean - slope * xMean;
        
        // Calculate R-squared
        var totalSumSquares = values.Select(y => Math.Pow(y - yMean, 2)).Sum();
        var residualSumSquares = xValues.Zip(values, (x, y) => Math.Pow(y - (slope * x + intercept), 2)).Sum();
        var rSquared = totalSumSquares != 0 ? 1 - (residualSumSquares / totalSumSquares) : 0;

        return new LinearTrend
        {
            Slope = slope,
            Intercept = intercept,
            RSquared = Math.Max(0, rSquared) // Ensure non-negative
        };
    }

    private double CalculateTrendConfidence(List<PerformanceDataPoint> data)
    {
        // Simple confidence calculation based on data consistency
        if (data.Count < 10) return 0.5; // Low confidence with little data
        
        var recentData = data.TakeLast(10).ToList();
        var variance = CalculateVariance(recentData.Select(d => d.DiscoveryTimeMs).ToList());
        
        // Lower variance = higher confidence
        return Math.Max(0.1, Math.Min(0.95, 1.0 - (variance / 10000.0)));
    }

    private double CalculateVariance(List<double> values)
    {
        if (values.Count < 2) return 0;
        
        var mean = values.Average();
        return values.Select(v => Math.Pow(v - mean, 2)).Average();
    }

    private List<PerformancePrediction> GeneratePredictions(TrendAnalysis trends, TimeSpan forecastPeriod)
    {
        var predictions = new List<PerformancePrediction>();

        // Predict performance degradation
        if (trends.PerformanceTrend.Slope > 5) // Performance getting worse by 5ms per hour
        {
            var hoursToThreshold = (1000 - trends.PerformanceTrend.Intercept) / trends.PerformanceTrend.Slope;
            if (hoursToThreshold > 0 && hoursToThreshold <= forecastPeriod.TotalHours)
            {
                predictions.Add(new PerformancePrediction
                {
                    Type = PredictionType.PerformanceDegradation,
                    Probability = Math.Min(0.9, trends.TrendConfidence * 1.2),
                    EstimatedTimeToImpact = TimeSpan.FromHours(hoursToThreshold),
                    ExpectedImpact = $"Discovery times may exceed 1000ms in {hoursToThreshold:F1} hours",
                    Severity = hoursToThreshold < 24 ? PredictionSeverity.High : PredictionSeverity.Medium,
                    Confidence = trends.TrendConfidence
                });
            }
        }

        // Predict cache efficiency decline
        if (trends.CacheEfficiencyTrend.Slope < -1) // Cache efficiency declining by 1% per hour
        {
            var hoursToLowEfficiency = (40 - trends.CacheEfficiencyTrend.Intercept) / trends.CacheEfficiencyTrend.Slope;
            if (hoursToLowEfficiency > 0 && hoursToLowEfficiency <= forecastPeriod.TotalHours)
            {
                predictions.Add(new PerformancePrediction
                {
                    Type = PredictionType.CacheEfficiencyDecline,
                    Probability = Math.Min(0.8, trends.TrendConfidence),
                    EstimatedTimeToImpact = TimeSpan.FromHours(hoursToLowEfficiency),
                    ExpectedImpact = $"Cache hit rate may drop below 40% in {hoursToLowEfficiency:F1} hours",
                    Severity = PredictionSeverity.Medium,
                    Confidence = trends.TrendConfidence
                });
            }
        }

        // Predict memory usage issues
        if (trends.MemoryUsageTrend.Slope > 2) // Memory usage increasing by 2MB per hour
        {
            var hoursToMemoryLimit = (500 - trends.MemoryUsageTrend.Intercept) / trends.MemoryUsageTrend.Slope;
            if (hoursToMemoryLimit > 0 && hoursToMemoryLimit <= forecastPeriod.TotalHours)
            {
                predictions.Add(new PerformancePrediction
                {
                    Type = PredictionType.MemoryPressure,
                    Probability = Math.Min(0.7, trends.TrendConfidence),
                    EstimatedTimeToImpact = TimeSpan.FromHours(hoursToMemoryLimit),
                    ExpectedImpact = $"Memory usage may reach concerning levels (500MB) in {hoursToMemoryLimit:F1} hours",
                    Severity = hoursToMemoryLimit < 48 ? PredictionSeverity.High : PredictionSeverity.Medium,
                    Confidence = trends.TrendConfidence
                });
            }
        }

        return predictions;
    }

    private List<PredictiveAlert> GeneratePredictiveAlerts(List<PerformancePrediction> predictions)
    {
        var alerts = new List<PredictiveAlert>();

        foreach (var prediction in predictions.Where(p => p.Probability > 0.6 && p.Severity >= PredictionSeverity.Medium))
        {
            alerts.Add(new PredictiveAlert
            {
                AlertId = Guid.NewGuid().ToString(),
                Type = prediction.Type,
                Severity = prediction.Severity,
                Title = GetAlertTitle(prediction.Type),
                Description = prediction.ExpectedImpact,
                EstimatedOccurrence = DateTime.UtcNow.Add(prediction.EstimatedTimeToImpact),
                Probability = prediction.Probability,
                Confidence = prediction.Confidence,
                RecommendedActions = GetRecommendedActions(prediction.Type),
                CreatedAt = DateTime.UtcNow
            });
        }

        return alerts;
    }

    private string GetAlertTitle(PredictionType type)
    {
        return type switch
        {
            PredictionType.PerformanceDegradation => "Predicted Performance Degradation",
            PredictionType.CacheEfficiencyDecline => "Predicted Cache Efficiency Decline",
            PredictionType.MemoryPressure => "Predicted Memory Pressure",
            PredictionType.ErrorRateIncrease => "Predicted Error Rate Increase",
            _ => "Predicted Performance Issue"
        };
    }

    private List<string> GetRecommendedActions(PredictionType type)
    {
        return type switch
        {
            PredictionType.PerformanceDegradation => new List<string>
            {
                "Review recent changes to discovery configuration",
                "Analyze assembly scanning performance for bottlenecks",
                "Consider implementing additional caching strategies",
                "Monitor for memory leaks or resource contention"
            },
            PredictionType.CacheEfficiencyDecline => new List<string>
            {
                "Review cache configuration and size limits",
                "Analyze cache invalidation patterns",
                "Consider cache warming strategies",
                "Monitor assembly modification patterns"
            },
            PredictionType.MemoryPressure => new List<string>
            {
                "Monitor memory usage patterns",
                "Review cache size configurations",
                "Analyze for memory leaks in custom plugins",
                "Consider implementing memory pressure relief mechanisms"
            },
            _ => new List<string> { "Monitor system performance closely", "Review performance metrics regularly" }
        };
    }

    private double CalculateConfidenceLevel(List<PerformanceDataPoint> historicalData)
    {
        if (historicalData.Count < 24) return 0.5; // Low confidence with less than 24 hours of data
        if (historicalData.Count < 168) return 0.7; // Medium confidence with less than a week
        return 0.9; // High confidence with a week or more of data
    }

    private List<PreventiveAction> GeneratePreventiveActions(
        List<PerformancePrediction> predictions,
        List<PredictiveAlert> alerts)
    {
        var actions = new List<PreventiveAction>();

        // High-priority preventive actions based on predictions
        if (predictions.Any(p => p.Type == PredictionType.PerformanceDegradation && p.Severity == PredictionSeverity.High))
        {
            actions.Add(new PreventiveAction
            {
                Priority = ActionPriority.High,
                Category = "Performance Optimization",
                Title = "Immediate Performance Review Required",
                Description = "System trending toward significant performance degradation",
                EstimatedEffort = "2-4 hours",
                ExpectedBenefit = "Prevent performance degradation and maintain optimal response times",
                ActionItems = new[]
                {
                    "Review recent changes to assembly loading patterns",
                    "Analyze memory usage trends for potential leaks",
                    "Evaluate cache configuration for optimization opportunities",
                    "Consider implementing additional performance optimizations"
                }
            });
        }

        if (predictions.Any(p => p.Type == PredictionType.CacheEfficiencyDecline))
        {
            actions.Add(new PreventiveAction
            {
                Priority = ActionPriority.Medium,
                Category = "Cache Optimization",
                Title = "Cache Strategy Review",
                Description = "Cache efficiency declining, proactive optimization recommended",
                EstimatedEffort = "1-2 hours",
                ExpectedBenefit = "Maintain or improve cache hit rates and system performance",
                ActionItems = new[]
                {
                    "Review cache size and eviction policies",
                    "Analyze cache usage patterns for optimization",
                    "Consider implementing cache warming strategies",
                    "Evaluate cache invalidation logic"
                }
            });
        }

        if (predictions.Any(p => p.Type == PredictionType.MemoryPressure))
        {
            actions.Add(new PreventiveAction
            {
                Priority = ActionPriority.High,
                Category = "Memory Management",
                Title = "Memory Usage Investigation",
                Description = "Memory usage trending upward, investigate potential issues",
                EstimatedEffort = "1-3 hours",
                ExpectedBenefit = "Prevent memory-related performance issues and potential outages",
                ActionItems = new[]
                {
                    "Monitor memory allocation patterns",
                    "Review cache size configurations",
                    "Analyze plugin memory usage",
                    "Consider implementing memory pressure relief mechanisms"
                }
            });
        }

        return actions;
    }

    public class PredictiveAnalysisResult
    {
        public TimeSpan AnalysisPeriod { get; set; }
        public TimeSpan ForecastPeriod { get; set; }
        public TrendAnalysis HistoricalTrends { get; set; } = new();
        public List<PerformancePrediction> Predictions { get; set; } = new();
        public List<PredictiveAlert> PredictiveAlerts { get; set; } = new();
        public double ConfidenceLevel { get; set; }
        public List<PreventiveAction> RecommendedActions { get; set; } = new();
    }

    public class PerformanceDataPoint
    {
        public DateTime Timestamp { get; set; }
        public double DiscoveryTimeMs { get; set; }
        public double CacheHitRate { get; set; }
        public double ErrorRate { get; set; }
        public int ServiceCount { get; set; }
        public double MemoryUsageMb { get; set; }
    }

    public class TrendAnalysis
    {
        public LinearTrend PerformanceTrend { get; set; } = new();
        public LinearTrend CacheEfficiencyTrend { get; set; } = new();
        public LinearTrend ErrorRateTrend { get; set; } = new();
        public LinearTrend MemoryUsageTrend { get; set; } = new();
        public double TrendConfidence { get; set; }
    }

    public class LinearTrend
    {
        public double Slope { get; set; }
        public double Intercept { get; set; }
        public double RSquared { get; set; }
    }

    public class PerformancePrediction
    {
        public PredictionType Type { get; set; }
        public double Probability { get; set; }
        public TimeSpan EstimatedTimeToImpact { get; set; }
        public string ExpectedImpact { get; set; } = string.Empty;
        public PredictionSeverity Severity { get; set; }
        public double Confidence { get; set; }
    }

    public class PredictiveAlert
    {
        public string AlertId { get; set; } = string.Empty;
        public PredictionType Type { get; set; }
        public PredictionSeverity Severity { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime EstimatedOccurrence { get; set; }
        public double Probability { get; set; }
        public double Confidence { get; set; }
        public List<string> RecommendedActions { get; set; } = new();
        public DateTime CreatedAt { get; set; }
    }

    public class PreventiveAction
    {
        public ActionPriority Priority { get; set; }
        public string Category { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string EstimatedEffort { get; set; } = string.Empty;
        public string ExpectedBenefit { get; set; } = string.Empty;
        public string[] ActionItems { get; set; } = Array.Empty<string>();
    }

    public enum PredictionType
    {
        PerformanceDegradation,
        CacheEfficiencyDecline,
        MemoryPressure,
        ErrorRateIncrease
    }

    public enum PredictionSeverity
    {
        Low,
        Medium,
        High,
        Critical
    }

    public enum ActionPriority
    {
        Low,
        Medium,
        High,
        Critical
    }
}
```

## 🔧 Performance Optimization Engine

Building on the monitoring and analysis capabilities, let's create an intelligent optimization engine that can automatically apply performance improvements based on observed patterns.

### Automated Performance Optimizer

```csharp
public class AutomatedPerformanceOptimizer
{
    private readonly IPerformanceMetricsCollector _metricsCollector;
    private readonly IAssemblyScanCache _cache;
    private readonly ILogger<AutomatedPerformanceOptimizer> _logger;
    private readonly List<IOptimizationStrategy> _strategies;

    public AutomatedPerformanceOptimizer(
        IPerformanceMetricsCollector metricsCollector,
        IAssemblyScanCache cache,
        ILogger<AutomatedPerformanceOptimizer> logger,
        IEnumerable<IOptimizationStrategy> strategies)
    {
        _metricsCollector = metricsCollector;
        _cache = cache;
        _logger = logger;
        _strategies = strategies.OrderBy(s => s.Priority).ToList();
    }

    public async Task<OptimizationResult> OptimizeSystemAsync(OptimizationConfiguration config)
    {
        var result = new OptimizationResult
        {
            StartTime = DateTime.UtcNow,
            Configuration = config
        };

        _logger.LogInformation("Starting automated performance optimization with {StrategyCount} strategies", _strategies.Count);

        var metrics = _metricsCollector.GetMetricsSummary();
        var context = new OptimizationContext
        {
            CurrentMetrics = metrics,
            CacheStatistics = _cache.GetStatistics(),
            Configuration = config,
            OptimizationHistory = await GetOptimizationHistory()
        };

        foreach (var strategy in _strategies)
        {
            if (!config.EnabledStrategies.Contains(strategy.Name) && config.EnabledStrategies.Any())
                continue;

            var strategyResult = await ExecuteOptimizationStrategy(strategy, context);
            result.StrategyResults.Add(strategyResult);

            if (strategyResult.WasApplied)
            {
                result.OptimizationsApplied++;
                _logger.LogInformation("Applied optimization: {StrategyName} - {Benefit}",
                    strategy.Name, strategyResult.ExpectedBenefit);
            }
        }

        result.EndTime = DateTime.UtcNow;
        result.TotalExecutionTime = result.EndTime - result.StartTime;
        result.IsSuccessful = true;

        await RecordOptimizationResult(result);

        _logger.LogInformation("Optimization completed in {Duration}ms. Applied {Count} optimizations",
            result.TotalExecutionTime.TotalMilliseconds, result.OptimizationsApplied);

        return result;
    }

    private async Task<OptimizationStrategyResult> ExecuteOptimizationStrategy(
        IOptimizationStrategy strategy, OptimizationContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        var result = new OptimizationStrategyResult
        {
            StrategyName = strategy.Name,
            StartTime = DateTime.UtcNow
        };

        try
        {
            // Analyze if this strategy should be applied
            var analysis = await strategy.AnalyzeAsync(context);
            result.AnalysisResult = analysis;

            if (!analysis.ShouldApply)
            {
                result.WasApplied = false;
                result.SkipReason = analysis.SkipReason;
                _logger.LogDebug("Skipping optimization strategy {StrategyName}: {Reason}",
                    strategy.Name, analysis.SkipReason);
                return result;
            }

            // Apply the optimization
            var applicationResult = await strategy.ApplyAsync(context);
            result.ApplicationResult = applicationResult;
            result.WasApplied = applicationResult.Success;
            result.ExpectedBenefit = applicationResult.ExpectedBenefit;

            if (applicationResult.Success)
            {
                _logger.LogInformation("Successfully applied optimization {StrategyName}: {Benefit}",
                    strategy.Name, applicationResult.ExpectedBenefit);
            }
            else
            {
                _logger.LogWarning("Failed to apply optimization {StrategyName}: {Error}",
                    strategy.Name, applicationResult.ErrorMessage);
            }
        }
        catch (Exception ex)
        {
            result.WasApplied = false;
            result.ErrorMessage = ex.Message;
            _logger.LogError(ex, "Error executing optimization strategy {StrategyName}", strategy.Name);
        }
        finally
        {
            stopwatch.Stop();
            result.ExecutionTime = stopwatch.Elapsed;
        }

        return result;
    }

    private async Task<List<OptimizationHistoryEntry>> GetOptimizationHistory()
    {
        // In a real implementation, this would query a database or file system
        // For this example, we'll return an empty list
        await Task.CompletedTask;
        return new List<OptimizationHistoryEntry>();
    }

    private async Task RecordOptimizationResult(OptimizationResult result)
    {
        // In a real implementation, this would persist the result for future analysis
        await Task.CompletedTask;
        _logger.LogInformation("Recorded optimization result with {Count} strategy results", result.StrategyResults.Count);
    }
}

public interface IOptimizationStrategy
{
    string Name { get; }
    int Priority { get; }
    Task<OptimizationAnalysis> AnalyzeAsync(OptimizationContext context);
    Task<OptimizationApplication> ApplyAsync(OptimizationContext context);
}

public class CacheOptimizationStrategy : IOptimizationStrategy
{
    public string Name => "Cache Size Optimization";
    public int Priority => 10;

    public async Task<OptimizationAnalysis> AnalyzeAsync(OptimizationContext context)
    {
        await Task.CompletedTask;
        
        var cacheStats = context.CacheStatistics;
        var hitRate = cacheStats.HitRatio;

        if (hitRate < 60)
        {
            return new OptimizationAnalysis
            {
                ShouldApply = true,
                Confidence = 0.8,
                ExpectedImpact = "Medium",
                Reasoning = $"Cache hit rate is {hitRate:F1}%, which is below optimal. Increasing cache size may improve performance."
            };
        }

        return new OptimizationAnalysis
        {
            ShouldApply = false,
            SkipReason = $"Cache hit rate is acceptable at {hitRate:F1}%"
        };
    }

    public async Task<OptimizationApplication> ApplyAsync(OptimizationContext context)
    {
        await Task.CompletedTask;
        
        // In a real implementation, this would adjust cache configuration
        return new OptimizationApplication
        {
            Success = true,
            ExpectedBenefit = "Improved cache hit rate should reduce discovery times by 15-25%",
            ChangesApplied = new[] { "Increased cache size by 50%" }
        };
    }
}

public class ParallelProcessingOptimizationStrategy : IOptimizationStrategy
{
    public string Name => "Parallel Processing Tuning";
    public int Priority => 20;

    public async Task<OptimizationAnalysis> AnalyzeAsync(OptimizationContext context)
    {
        await Task.CompletedTask;
        
        var metrics = context.CurrentMetrics;
        var avgScanTime = metrics.AssemblyMetrics.Any() 
            ? metrics.AssemblyMetrics.Average(a => a.AverageScanTimeMs)
            : 0;

        // Analyze if parallel processing could help
        var assemblyCount = metrics.AssemblyMetrics.Count;
        var cpuCores = Environment.ProcessorCount;

        if (assemblyCount > cpuCores && avgScanTime > 200)
        {
            return new OptimizationAnalysis
            {
                ShouldApply = true,
                Confidence = 0.9,
                ExpectedImpact = "High",
                Reasoning = $"With {assemblyCount} assemblies and {cpuCores} CPU cores, parallel processing optimization could significantly improve performance."
            };
        }

        return new OptimizationAnalysis
        {
            ShouldApply = false,
            SkipReason = "Current workload doesn't justify parallel processing optimization"
        };
    }

    public async Task<OptimizationApplication> ApplyAsync(OptimizationContext context)
    {
        await Task.CompletedTask;
        
        return new OptimizationApplication
        {
            Success = true,
            ExpectedBenefit = "Parallel processing should reduce overall discovery time by 30-50%",
            ChangesApplied = new[] 
            { 
                "Enabled parallel assembly processing",
                $"Set degree of parallelism to {Environment.ProcessorCount}"
            }
        };
    }
}

public class AssemblyFilterOptimizationStrategy : IOptimizationStrategy
{
    public string Name => "Assembly Filter Optimization";
    public int Priority => 30;

    public async Task<OptimizationAnalysis> AnalyzeAsync(OptimizationContext context)
    {
        await Task.CompletedTask;
        
        var metrics = context.CurrentMetrics;
        
        // Find assemblies with very low service density
        var lowDensityAssemblies = metrics.AssemblyMetrics
            .Where(a => a.TotalServicesFound == 0 || (a.TotalServicesFound < 3 && a.AverageScanTimeMs > 100))
            .ToList();

        if (lowDensityAssemblies.Count > 2)
        {
            return new OptimizationAnalysis
            {
                ShouldApply = true,
                Confidence = 0.7,
                ExpectedImpact = "Medium",
                Reasoning = $"Found {lowDensityAssemblies.Count} assemblies with poor service density. Adding filters could improve performance."
            };
        }

        return new OptimizationAnalysis
        {
            ShouldApply = false,
            SkipReason = "No assemblies found with poor service density"
        };
    }

    public async Task<OptimizationApplication> ApplyAsync(OptimizationContext context)
    {
        await Task.CompletedTask;
        
        return new OptimizationApplication
        {
            Success = true,
            ExpectedBenefit = "Assembly filtering should reduce discovery time by 10-20%",
            ChangesApplied = new[] { "Added filters for low-density assemblies" }
        };
    }
}

public class OptimizationContext
{
    public PerformanceMetricsSummary CurrentMetrics { get; set; } = new();
    public CacheStatistics CacheStatistics { get; set; } = new();
    public OptimizationConfiguration Configuration { get; set; } = new();
    public List<OptimizationHistoryEntry> OptimizationHistory { get; set; } = new();
}

public class OptimizationConfiguration
{
    public List<string> EnabledStrategies { get; set; } = new();
    public bool AllowAutomaticChanges { get; set; } = false;
    public double MinimumConfidenceThreshold { get; set; } = 0.7;
    public TimeSpan CooldownPeriod { get; set; } = TimeSpan.FromHours(1);
}

public class OptimizationAnalysis
{
    public bool ShouldApply { get; set; }
    public double Confidence { get; set; }
    public string ExpectedImpact { get; set; } = string.Empty;
    public string Reasoning { get; set; } = string.Empty;
    public string SkipReason { get; set; } = string.Empty;
}

public class OptimizationApplication
{
    public bool Success { get; set; }
    public string ExpectedBenefit { get; set; } = string.Empty;
    public string[] ChangesApplied { get; set; } = Array.Empty<string>();
    public string ErrorMessage { get; set; } = string.Empty;
}

public class OptimizationResult
{
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public TimeSpan TotalExecutionTime { get; set; }
    public bool IsSuccessful { get; set; }
    public int OptimizationsApplied { get; set; }
    public OptimizationConfiguration Configuration { get; set; } = new();
    public List<OptimizationStrategyResult> StrategyResults { get; set; } = new();
}

public class OptimizationStrategyResult
{
    public string StrategyName { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public TimeSpan ExecutionTime { get; set; }
    public bool WasApplied { get; set; }
    public string ExpectedBenefit { get; set; } = string.Empty;
    public string SkipReason { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty;
    public OptimizationAnalysis AnalysisResult { get; set; } = new();
    public OptimizationApplication ApplicationResult { get; set; } = new();
}

public class OptimizationHistoryEntry
{
    public DateTime Timestamp { get; set; }
    public string StrategyName { get; set; } = string.Empty;
    public bool WasSuccessful { get; set; }
    public string BenefitAchieved { get; set; } = string.Empty;
}
```

## 📈 Integration with Monitoring Systems

To make performance monitoring truly effective in production environments, integration with external monitoring systems is essential.

### Prometheus Metrics Exporter

```csharp
public class PrometheusMetricsExporter
{
    private readonly IPerformanceMetricsCollector _metricsCollector;
    private readonly Timer _exportTimer;

    // Prometheus metric definitions
    private readonly Counter _discoveryOperations = Metrics
        .CreateCounter("service_discovery_operations_total", "Total number of service discovery operations");
    
    private readonly Histogram _discoveryDuration = Metrics
        .CreateHistogram("service_discovery_duration_seconds", "Time spent on service discovery operations");
    
    private readonly Gauge _cacheHitRatio = Metrics
        .CreateGauge("service_discovery_cache_hit_ratio", "Cache hit ratio percentage");
    
    private readonly Counter _assemblyScanOperations = Metrics
        .CreateCounter("service_discovery_assembly_scans_total", "Total number of assembly scan operations",
            new[] { "assembly_name", "result" });
    
    private readonly Histogram _assemblyScanDuration = Metrics
        .CreateHistogram("service_discovery_assembly_scan_duration_seconds", "Time spent scanning assemblies",
            new[] { "assembly_name" });
    
    private readonly Counter _pluginExecutions = Metrics
        .CreateCounter("service_discovery_plugin_executions_total", "Total number of plugin executions",
            new[] { "plugin_name", "result" });
    
    private readonly Histogram _pluginExecutionDuration = Metrics
        .CreateHistogram("service_discovery_plugin_execution_duration_seconds", "Time spent executing plugins",
            new[] { "plugin_name" });

    public PrometheusMetricsExporter(IPerformanceMetricsCollector metricsCollector)
    {
        _metricsCollector = metricsCollector;
        
        // Export metrics every 30 seconds
        _exportTimer = new Timer(ExportMetrics, null, TimeSpan.Zero, TimeSpan.FromSeconds(30));
    }

    private void ExportMetrics(object? state)
    {
        try
        {
            var summary = _metricsCollector.GetMetricsSummary();
            
            // Update discovery operation metrics
            _discoveryOperations.IncTo(summary.TotalServiceRegistrations);
            _cacheHitRatio.Set(summary.CacheMetrics.FirstOrDefault()?.HitRate ?? 0);

            // Update assembly scanning metrics
            foreach (var assembly in summary.AssemblyMetrics)
            {
                _assemblyScanOperations
                    .WithLabels(assembly.AssemblyName, "success")
                    .IncTo((ulong)(assembly.ScanCount * assembly.SuccessRate / 100));
                
                _assemblyScanOperations
                    .WithLabels(assembly.AssemblyName, "failure")
                    .IncTo((ulong)(assembly.ScanCount * (100 - assembly.SuccessRate) / 100));

                _assemblyScanDuration
                    .WithLabels(assembly.AssemblyName)
                    .Observe(assembly.AverageScanTimeMs / 1000.0);
            }

            // Update plugin execution metrics
            foreach (var plugin in summary.PluginMetrics)
            {
                _pluginExecutions
                    .WithLabels(plugin.PluginName, "success")
                    .IncTo((ulong)(plugin.ExecutionCount * plugin.SuccessRate / 100));
                
                _pluginExecutions
                    .WithLabels(plugin.PluginName, "failure")
                    .IncTo((ulong)(plugin.ExecutionCount * (100 - plugin.SuccessRate) / 100));

                _pluginExecutionDuration
                    .WithLabels(plugin.PluginName)
                    .Observe(plugin.AverageExecutionTimeMs / 1000.0);
            }
        }
        catch (Exception ex)
        {
            // Log error but don't let it crash the exporter
            Console.WriteLine($"Error exporting Prometheus metrics: {ex.Message}");
        }
    }

    public void Dispose()
    {
        _exportTimer?.Dispose();
    }
}
```

### Application Insights Integration

```csharp
public class ApplicationInsightsMetricsExporter
{
    private readonly TelemetryClient _telemetryClient;
    private readonly IPerformanceMetricsCollector _metricsCollector;
    private readonly Timer _exportTimer;

    public ApplicationInsightsMetricsExporter(
        TelemetryClient telemetryClient, 
        IPerformanceMetricsCollector metricsCollector)
    {
        _telemetryClient = telemetryClient;
        _metricsCollector = metricsCollector;
        
        // Export metrics every 60 seconds
        _exportTimer = new Timer(ExportMetrics, null, TimeSpan.Zero, TimeSpan.FromMinutes(1));
    }

    private void ExportMetrics(object? state)
    {
        try
        {
            var summary = _metricsCollector.GetMetricsSummary();
            
            // Track custom metrics
            _telemetryClient.TrackMetric("ServiceDiscovery.TotalRegistrations", summary.TotalServiceRegistrations);
            _telemetryClient.TrackMetric("ServiceDiscovery.AverageRegistrationTime", summary.AverageRegistrationTimeMs);
            _telemetryClient.TrackMetric("ServiceDiscovery.FailureRate", summary.RegistrationFailureRate);

            // Track cache performance
            if (summary.CacheMetrics.Any())
            {
                var cacheMetric = summary.CacheMetrics.First();
                _telemetryClient.TrackMetric("ServiceDiscovery.Cache.HitRate", cacheMetric.HitRate);
                _telemetryClient.TrackMetric("ServiceDiscovery.Cache.OperationTime", cacheMetric.AverageOperationTimeMs);
            }

            // Track assembly performance
            foreach (var assembly in summary.AssemblyMetrics.Take(10)) // Limit to top 10 to avoid too much data
            {
                var properties = new Dictionary<string, string>
                {
                    ["AssemblyName"] = assembly.AssemblyName
                };

                _telemetryClient.TrackMetric("ServiceDiscovery.Assembly.ScanTime", 
                    assembly.AverageScanTimeMs, properties);
                _telemetryClient.TrackMetric("ServiceDiscovery.Assembly.SuccessRate", 
                    assembly.SuccessRate, properties);
                _telemetryClient.TrackMetric("ServiceDiscovery.Assembly.ServiceCount", 
                    assembly.TotalServicesFound, properties);
            }

            // Track plugin performance
            foreach (var plugin in summary.PluginMetrics)
            {
                var properties = new Dictionary<string, string>
                {
                    ["PluginName"] = plugin.PluginName
                };

                _telemetryClient.TrackMetric("ServiceDiscovery.Plugin.ExecutionTime", 
                    plugin.AverageExecutionTimeMs, properties);
                _telemetryClient.TrackMetric("ServiceDiscovery.Plugin.SuccessRate", 
                    plugin.SuccessRate, properties);
                _telemetryClient.TrackMetric("ServiceDiscovery.Plugin.ServicesDiscovered", 
                    plugin.TotalServicesDiscovered, properties);
            }

            // Track custom events for significant performance changes
            TrackPerformanceEvents(summary);
        }
        catch (Exception ex)
        {
            _telemetryClient.TrackException(ex);
        }
    }

    private void TrackPerformanceEvents(PerformanceMetricsSummary summary)
    {
        // Track events for performance anomalies
        if (summary.AverageRegistrationTimeMs > 1000)
        {
            _telemetryClient.TrackEvent("ServiceDiscovery.PerformanceAlert.SlowRegistration", new Dictionary<string, string>
            {
                ["AverageTime"] = summary.AverageRegistrationTimeMs.ToString("F2"),
                ["Threshold"] = "1000"
            });
        }

        if (summary.RegistrationFailureRate > 10)
        {
            _telemetryClient.TrackEvent("ServiceDiscovery.PerformanceAlert.HighFailureRate", new Dictionary<string, string>
            {
                ["FailureRate"] = summary.RegistrationFailureRate.ToString("F2"),
                ["Threshold"] = "10"
            });
        }

        var cacheHitRate = summary.CacheMetrics.FirstOrDefault()?.HitRate ?? 0;
        if (cacheHitRate < 50)
        {
            _telemetryClient.TrackEvent("ServiceDiscovery.PerformanceAlert.LowCacheHitRate", new Dictionary<string, string>
            {
                ["HitRate"] = cacheHitRate.ToString("F2"),
                ["Threshold"] = "50"
            });
        }
    }

    public void Dispose()
    {
        _exportTimer?.Dispose();
    }
}
```

## 🔄 Performance Testing and Benchmarking

To validate the effectiveness of performance monitoring and optimization efforts, comprehensive testing and benchmarking are essential.

### Performance Benchmark Suite

```csharp
[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net80)]
public class ServiceDiscoveryBenchmarks
{
    private IServiceCollection _services = null!;
    private Assembly[] _testAssemblies = null!;
    private IPerformanceMetricsCollector _metricsCollector = null!;

    [GlobalSetup]
    public void Setup()
    {
        _services = new ServiceCollection();
        _testAssemblies = new[]
        {
            Assembly.GetExecutingAssembly(),
            typeof(ServiceRegistrationAttribute).Assembly,
            typeof(IServiceCollection).Assembly
        };
        _metricsCollector = new PerformanceMetricsCollector();
    }

    [Benchmark]
    [Arguments(1)]
    [Arguments(5)]
    [Arguments(10)]
    public void StandardDiscovery(int assemblyCount)
    {
        var services = new ServiceCollection();
        var assemblies = _testAssemblies.Take(assemblyCount).ToArray();
        
        services.AddAutoServices(options =>
        {
            options.EnableLogging = false;
            options.EnablePerformanceOptimizations = false;
        }, assemblies);
    }

    [Benchmark]
    [Arguments(1)]
    [Arguments(5)]
    [Arguments(10)]
    public void OptimizedDiscovery(int assemblyCount)
    {
        var services = new ServiceCollection();
        var assemblies = _testAssemblies.Take(assemblyCount).ToArray();
        
        services.AddAutoServicesWithPerformanceOptimizations(options =>
        {
            options.EnableLogging = false;
            options.EnablePerformanceOptimizations = true;
            options.EnableParallelProcessing = true;
        }, assemblies);
    }

    [Benchmark]
    public void CachedDiscovery()
    {
        var cache = new MemoryAssemblyScanCache();
        var scanner = new OptimizedTypeScanner();
        
        // First run to populate cache
        var services1 = new ServiceCollection();
        services1.AddAutoServicesWithCustomOptimizations(cache, scanner, options =>
        {
            options.EnableLogging = false;
        }, _testAssemblies);

        // Second run should benefit from cache
        var services2 = new ServiceCollection();
        services2.AddAutoServicesWithCustomOptimizations(cache, scanner, options =>
        {
            options.EnableLogging = false;
        }, _testAssemblies);
    }

    [Benchmark]
    public void MetricsCollection()
    {
        for (int i = 0; i < 100; i++)
        {
            _metricsCollector.RecordAssemblyScan($"TestAssembly{i}", TimeSpan.FromMilliseconds(50), 10, true);
            _metricsCollector.RecordCacheOperation("Get", i % 3 == 0, TimeSpan.FromMilliseconds(5));
        }
        
        var summary = _metricsCollector.GetMetricsSummary();
    }
}
```

## 🎯 Key Takeaways and Best Practices

As we conclude this comprehensive guide to performance monitoring, let's consolidate the key concepts and best practices that will serve you well in production environments:

### Essential Monitoring Principles

**Comprehensive Coverage**: Monitor every aspect of the discovery process - from assembly scanning and cache performance to plugin execution and service registration. Each component contributes to overall system performance, and blind spots in monitoring can hide critical issues.

**Actionable Metrics**: Focus on metrics that directly inform optimization decisions. Raw numbers are less valuable than trends, ratios, and comparative measurements that guide specific actions. For example, cache hit rates are more actionable than raw cache operation counts.

**Performance Context**: Always interpret metrics within the context of your specific application and deployment environment. A 500ms discovery time might be excellent for a complex enterprise application but problematic for a lightweight microservice.

**Proactive Alerting**: Implement predictive alerting that warns about potential issues before they impact users. Trend analysis and predictive modeling can identify problems hours or days before they become critical.

### Production Deployment Strategies

When deploying performance monitoring in production environments, follow these proven strategies:

#### Gradual Rollout Approach

```csharp
public class ProductionMonitoringConfiguration
{
    public static AutoServiceOptions CreateProductionConfig(IConfiguration configuration)
    {
        var environment = configuration["Environment"];
        var enableMetrics = configuration.GetValue<bool>("Monitoring:EnableDetailedMetrics", false);
        
        return new AutoServiceOptions
        {
            EnablePerformanceOptimizations = true,
            EnableParallelProcessing = true,
            EnablePerformanceMetrics = enableMetrics,
            EnableLogging = environment == "Development",
            
            // Production-specific optimizations
            MaxDegreeOfParallelism = Environment.ProcessorCount,
            CustomCache = CreateProductionCache(configuration)
        };
    }
    
    private static IAssemblyScanCache CreateProductionCache(IConfiguration configuration)
    {
        var cacheConfig = CacheConfiguration.ForProduction();
        
        // Adjust based on production requirements
        cacheConfig.MaxCachedAssemblies = configuration.GetValue<int>("Cache:MaxAssemblies", 100);
        cacheConfig.MaxCacheAge = TimeSpan.FromHours(configuration.GetValue<int>("Cache:MaxAgeHours", 4));
        
        return new MemoryAssemblyScanCache();
    }
}
```

#### Monitoring Dashboard Configuration

```csharp
public class MonitoringDashboardStartup
{
    public static void ConfigureMonitoring(IServiceCollection services, IConfiguration configuration)
    {
        // Register core monitoring components
        services.AddAutoDiscoveryInfrastructure(options =>
        {
            options.EnablePerformanceMetrics = true;
            options.CacheConfiguration = CacheConfiguration.ForProduction();
        });
        
        // Register monitoring services
        services.AddSingleton<PerformanceAnalyzer>();
        services.AddSingleton<CachePerformanceMonitor>();
        services.AddSingleton<PluginPerformanceMonitor>();
        services.AddHostedService<ServiceDiscoveryMonitoringService>();
        
        // Register external integrations
        if (configuration.GetValue<bool>("Monitoring:EnablePrometheus", false))
        {
            services.AddSingleton<PrometheusMetricsExporter>();
        }
        
        if (configuration.GetValue<bool>("Monitoring:EnableApplicationInsights", false))
        {
            services.AddSingleton<ApplicationInsightsMetricsExporter>();
        }
        
        // Register optimization engine
        services.AddTransient<AutomatedPerformanceOptimizer>();
        services.AddTransient<IOptimizationStrategy, CacheOptimizationStrategy>();
        services.AddTransient<IOptimizationStrategy, ParallelProcessingOptimizationStrategy>();
        services.AddTransient<IOptimizationStrategy, AssemblyFilterOptimizationStrategy>();
    }
}
```

### Performance Optimization Workflow

Establish a systematic approach to performance optimization based on monitoring data:

#### 1. Baseline Establishment
- Measure current performance across all metrics
- Document typical workload patterns
- Identify performance bottlenecks and constraints
- Establish acceptable performance thresholds

#### 2. Continuous Monitoring
- Collect metrics continuously during normal operations
- Track trends and identify anomalies
- Monitor the effectiveness of applied optimizations
- Adjust monitoring sensitivity based on operational experience

#### 3. Analysis and Optimization
- Regularly analyze collected metrics for optimization opportunities
- Apply targeted optimizations based on data-driven insights
- Validate optimization effectiveness through before/after comparisons
- Document successful optimizations for future reference

#### 4. Alerting and Response
- Configure alerts for critical performance degradation
- Establish clear response procedures for different alert types
- Regularly review and tune alert thresholds
- Implement automated responses for common issues

### Integration with CI/CD Pipelines

Performance monitoring should be integrated into your development and deployment processes:

```csharp
public class ContinuousIntegrationPerformanceTests
{
    [Test]
    public async Task PerformanceRegression_ShouldNotExceedBaseline()
    {
        // Arrange
        var services = new ServiceCollection();
        var stopwatch = Stopwatch.StartNew();
        
        // Act
        services.AddAutoServicesWithPerformanceOptimizations(options =>
        {
            options.EnableLogging = false;
            options.EnablePerformanceMetrics = true;
        }, Assembly.GetExecutingAssembly());
        
        stopwatch.Stop();
        
        // Assert
        var discoveryTime = stopwatch.ElapsedMilliseconds;
        Assert.That(discoveryTime, Is.LessThan(500), 
            $"Service discovery took {discoveryTime}ms, which exceeds the 500ms baseline");
        
        // Additional performance assertions
        var serviceProvider = services.BuildServiceProvider();
        var metricsCollector = serviceProvider.GetService<IPerformanceMetricsCollector>();
        
        if (metricsCollector != null)
        {
            var metrics = metricsCollector.GetMetricsSummary();
            Assert.That(metrics.RegistrationFailureRate, Is.LessThan(1.0), 
                "Registration failure rate should be less than 1%");
        }
    }
    
    [Test]
    public async Task CacheEffectiveness_ShouldMeetExpectations()
    {
        // Test cache performance in isolation
        var cache = new MemoryAssemblyScanCache();
        var testAssembly = Assembly.GetExecutingAssembly();
        
        // First scan - should populate cache
        var scanner = new OptimizedTypeScanner();
        var services1 = scanner.ScanAssemblies(new[] { testAssembly }).ToList();
        cache.CacheResults(testAssembly, services1.Cast<ServiceRegistrationInfo>());
        
        // Second scan - should hit cache
        var hitResult = cache.TryGetCachedResults(testAssembly, out var cachedServices);
        
        Assert.That(hitResult, Is.True, "Cache should return cached results on second access");
        Assert.That(cachedServices?.Count(), Is.EqualTo(services1.Count), 
            "Cached results should match original scan results");
        
        var stats = cache.GetStatistics();
        Assert.That(stats.HitRatio, Is.GreaterThan(0), "Cache hit ratio should be greater than 0");
    }
}
```

### Troubleshooting Common Performance Issues

Based on extensive production experience, here are the most common performance issues and their solutions:

#### Slow Assembly Scanning
**Symptoms**: High average scan times for specific assemblies
**Solutions**:
- Implement assembly filtering to exclude non-relevant assemblies
- Use namespace exclusions for large assemblies with few services
- Enable parallel processing for multiple assembly scenarios
- Consider assembly-specific optimization strategies

#### Poor Cache Performance
**Symptoms**: Low cache hit rates, frequent cache invalidation
**Solutions**:
- Increase cache size if memory allows
- Review cache invalidation triggers
- Implement cache warming for frequently accessed assemblies
- Analyze assembly modification patterns

#### Plugin Performance Issues
**Symptoms**: Slow plugin execution, high plugin failure rates
**Solutions**:
- Profile plugin code for bottlenecks
- Implement plugin-specific caching
- Review plugin validation logic
- Consider plugin execution order optimization

#### Memory Pressure
**Symptoms**: Increasing memory usage, garbage collection pressure
**Solutions**:
- Review cache size configurations
- Implement memory pressure relief mechanisms
- Analyze object allocation patterns
- Consider streaming approaches for large datasets

### Advanced Monitoring Scenarios

For enterprise environments with complex requirements, consider these advanced monitoring scenarios:

#### Multi-Tenant Performance Monitoring
```csharp
public class MultiTenantPerformanceCollector : IPerformanceMetricsCollector
{
    private readonly IPerformanceMetricsCollector _baseCollector;
    private readonly ITenantContext _tenantContext;
    
    public MultiTenantPerformanceCollector(
        IPerformanceMetricsCollector baseCollector,
        ITenantContext tenantContext)
    {
        _baseCollector = baseCollector;
        _tenantContext = tenantContext;
    }
    
    public void RecordAssemblyScan(string assemblyName, TimeSpan scanDuration, int servicesFound, bool wasSuccessful)
    {
        // Add tenant context to metrics
        var tenantId = _tenantContext.GetCurrentTenantId();
        var enrichedAssemblyName = $"{tenantId}.{assemblyName}";
        
        _baseCollector.RecordAssemblyScan(enrichedAssemblyName, scanDuration, servicesFound, wasSuccessful);
    }
    
    // Implement other methods with tenant context...
}
```

#### Cross-Service Performance Correlation
```csharp
public class DistributedPerformanceTracker
{
    private readonly IDistributedCache _distributedCache;
    private readonly ILogger<DistributedPerformanceTracker> _logger;
    
    public async Task RecordCrossServiceMetric(string serviceName, string operationName, TimeSpan duration)
    {
        var correlationId = Activity.Current?.Id ?? Guid.NewGuid().ToString();
        
        var metricData = new
        {
            ServiceName = serviceName,
            OperationName = operationName,
            Duration = duration.TotalMilliseconds,
            Timestamp = DateTimeOffset.UtcNow,
            CorrelationId = correlationId
        };
        
        var key = $"perf_metric:{correlationId}:{serviceName}";
        await _distributedCache.SetStringAsync(key, JsonSerializer.Serialize(metricData), 
            new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1)
            });
        
        _logger.LogInformation("Recorded cross-service performance metric: {ServiceName}.{OperationName} = {Duration}ms",
            serviceName, operationName, duration.TotalMilliseconds);
    }
}
```

## 🔗 Next Steps and Further Reading

Performance monitoring is an ongoing discipline that evolves with your application and infrastructure. To continue improving your monitoring capabilities:

1. **[Advanced Caching Strategies](AdvancedCaching.md)** - Deep dive into sophisticated caching approaches
2. **[Plugin Performance Optimization](PluginOptimization.md)** - Optimize custom plugins for maximum performance
3. **[Production Deployment Guide](ProductionDeployment.md)** - Best practices for production deployments
4. **[Troubleshooting Guide](Troubleshooting.md)** - Common issues and their solutions

### Recommended Tools and Resources

- **APM Solutions**: Application Insights, New Relic, Datadog
- **Metrics Collection**: Prometheus, InfluxDB, CloudWatch
- **Visualization**: Grafana, Kibana, Power BI
- **Alerting**: PagerDuty, OpsGenie, Slack integrations

### Community and Support

- Join the service discovery community discussions
- Contribute performance optimization strategies
- Share monitoring dashboards and configurations
- Report performance issues and improvements

Remember: effective performance monitoring is not just about collecting data—it's about transforming that data into actionable insights that improve your application's performance and user experience. Start with basic monitoring, iterate based on your findings, and gradually build more sophisticated monitoring and optimization capabilities.

The investment in comprehensive performance monitoring pays dividends in reduced operational overhead, improved user satisfaction, and more predictable system behavior. Your future self (and your operations team) will thank you for implementing these monitoring practices early and consistently.