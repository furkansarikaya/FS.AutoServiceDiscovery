using System.Collections.Concurrent;
using System.Diagnostics;

namespace FS.AutoServiceDiscovery.Extensions.Architecture.Conventions;

/// <summary>
/// Coordinates multiple naming conventions to resolve service types in priority order with performance tracking.
/// 
/// This implementation represents a sophisticated approach to the "chain of responsibility" pattern,
/// where multiple handlers (naming conventions) are tried in sequence until one succeeds. However,
/// unlike a simple chain, this implementation adds several important enhancements:
/// 
/// Performance monitoring to understand which conventions are most effective, caching to avoid 
/// repeated expensive operations, statistical analysis to help optimize convention ordering, and 
/// thread-safe operation for concurrent service discovery scenarios.
/// 
/// The resolver acts as a "smart dispatcher" that learns from experience and can provide
/// insights about the naming patterns actually used in your application.
/// </summary>
public class NamingConventionResolver : INamingConventionResolver
{
    private readonly IEnumerable<INamingConvention> _conventions;
    private readonly ConcurrentDictionary<string, ConventionPerformanceMetrics> _conventionMetrics = new();

    // Direct field access for thread-safe operations with Interlocked
    private long _totalResolutionAttempts = 0;
    private long _successfulResolutions = 0;
    private long _failedResolutions = 0;

    // Simple cache to avoid re-resolving the same types repeatedly
    private readonly ConcurrentDictionary<string, Type?> _resolutionCache = new();

    /// <summary>
    /// Initializes a new instance of the naming convention resolver with the specified conventions.
    /// 
    /// The constructor automatically sorts conventions by priority and initializes performance
    /// tracking structures. This up-front organization ensures that resolution attempts are
    /// always processed in the correct order without needing to sort on every resolution.
    /// </summary>
    /// <param name="conventions">
    /// The collection of naming conventions to use for resolution. These will be automatically
    /// sorted by priority, with lower priority values being consulted first.
    /// </param>
    public NamingConventionResolver(IEnumerable<INamingConvention> conventions)
    {
        _conventions = conventions.OrderBy(c => c.Priority).ToList();

        // Initialize performance tracking for each convention
        foreach (var convention in _conventions)
        {
            _conventionMetrics.TryAdd(convention.Name, new ConventionPerformanceMetrics());
        }
    }

    /// <summary>
    /// Resolves the service type for an implementation using registered naming conventions with caching and performance tracking.
    /// 
    /// This method implements several important patterns: caching where results are cached to avoid 
    /// expensive re-computation for types that are processed multiple times, chain of responsibility 
    /// where each convention gets a chance to resolve the type in priority order, performance monitoring 
    /// where detailed metrics are collected to understand system behavior, and graceful degradation 
    /// where if all conventions fail, the method returns null rather than throwing.
    /// </summary>
    /// <param name="implementationType">
    /// The concrete implementation class for which we need to find the corresponding interface.
    /// </param>
    /// <param name="availableInterfaces">
    /// The interfaces actually implemented by the class. This method will only return
    /// interfaces from this collection.
    /// </param>
    /// <returns>
    /// The interface that matches one of the naming conventions, or null if no convention
    /// could determine an appropriate match.
    /// </returns>
    public Type? ResolveServiceType(Type implementationType, IEnumerable<Type> availableInterfaces)
    {
        // Increment total attempts counter for statistics
        Interlocked.Increment(ref _totalResolutionAttempts);

        // Check cache first to avoid expensive re-computation
        var cacheKey = implementationType.FullName ?? implementationType.Name;
        if (_resolutionCache.TryGetValue(cacheKey, out var cachedResult))
        {
            if (cachedResult != null)
            {
                Interlocked.Increment(ref _successfulResolutions);
            }
            else
            {
                Interlocked.Increment(ref _failedResolutions);
            }

            return cachedResult;
        }

        // Convert to list once to avoid multiple enumeration
        var interfaceList = availableInterfaces.ToList();

        // Try each convention in priority order
        foreach (var convention in _conventions)
        {
            var metrics = _conventionMetrics.GetOrAdd(convention.Name, _ => new ConventionPerformanceMetrics());

            // Now this will work because ConsultationCount is a field, not a property
            Interlocked.Increment(ref metrics.ConsultationCount);

            // Quick pre-check to see if convention can apply
            if (!convention.CanApplyTo(implementationType))
            {
                continue;
            }

            // Measure convention execution time for performance analysis
            var stopwatch = Stopwatch.StartNew();
            Type? resolvedType = null;

            try
            {
                resolvedType = convention.ResolveServiceType(implementationType, interfaceList);
            }
            catch (Exception ex)
            {
                // Log the exception but don't let it break the entire resolution process
                System.Diagnostics.Debug.WriteLine($"Convention {convention.Name} threw exception: {ex.Message}");
                continue;
            }
            finally
            {
                stopwatch.Stop();
                // Thread-safe update of execution time
                UpdateExecutionTime(metrics, stopwatch.Elapsed);
            }

            // If this convention succeeded, cache and return the result
            if (resolvedType != null)
            {
                // Now this will work because SuccessfulResolutions is a field, not a property
                Interlocked.Increment(ref metrics.SuccessfulResolutions);
                Interlocked.Increment(ref _successfulResolutions);

                _resolutionCache.TryAdd(cacheKey, resolvedType);
                return resolvedType;
            }
        }

        // No convention could resolve this type - cache the failure and update statistics
        Interlocked.Increment(ref _failedResolutions);
        _resolutionCache.TryAdd(cacheKey, null);
        return null;
    }

    /// <summary>
    /// Thread-safe method to update execution time metrics.
    /// 
    /// Since TimeSpan cannot be used with Interlocked operations directly, we use a lock-based approach
    /// to maintain thread safety while updating timing information. This is the safest approach for
    /// TimeSpan updates in concurrent scenarios.
    /// </summary>
    /// <param name="metrics">The metrics object to update</param>
    /// <param name="elapsed">The elapsed time to add</param>
    private static void UpdateExecutionTime(ConventionPerformanceMetrics metrics, TimeSpan elapsed)
    {
        // Use the lock object specifically designed for this purpose
        lock (metrics.TimeLock)
        {
            metrics.TotalExecutionTime = metrics.TotalExecutionTime.Add(elapsed);
        }
    }

    /// <summary>
    /// Returns a snapshot of current performance statistics for analysis and optimization.
    /// 
    /// The statistics provide valuable insights for understanding which naming patterns are most 
    /// common in your codebase, optimizing convention priority ordering for better performance, 
    /// identifying opportunities to add new conventions for unhandled patterns, and monitoring 
    /// system performance over time.
    /// </summary>
    /// <returns>
    /// A complete snapshot of naming convention performance statistics that can be safely
    /// shared and analyzed without affecting the ongoing operation of the resolver.
    /// </returns>
    public NamingConventionStatistics GetStatistics()
    {
        var snapshotMetrics = new Dictionary<string, ConventionPerformanceMetrics>();

        foreach (var kvp in _conventionMetrics)
        {
            // Create a snapshot of each metric to avoid exposing mutable state
            var original = kvp.Value;
            var snapshot = new ConventionPerformanceMetrics();

            // Copy field values - these are atomic reads for long fields
            snapshot.ConsultationCount = original.ConsultationCount;
            snapshot.SuccessfulResolutions = original.SuccessfulResolutions;

            // Copy TimeSpan with lock protection
            lock (original.TimeLock)
            {
                snapshot.TotalExecutionTime = original.TotalExecutionTime;
            }

            snapshotMetrics[kvp.Key] = snapshot;
        }

        return new NamingConventionStatistics
        {
            TotalResolutionAttempts = Interlocked.Read(ref _totalResolutionAttempts),
            SuccessfulResolutions = Interlocked.Read(ref _successfulResolutions),
            FailedResolutions = Interlocked.Read(ref _failedResolutions),
            ConventionMetrics = snapshotMetrics
        };
    }

    /// <summary>
    /// Returns the ordered collection of registered naming conventions for inspection and debugging.
    /// </summary>
    /// <returns>
    /// A copy of the naming conventions in the order they will be evaluated, with highest 
    /// priority conventions first.
    /// </returns>
    public IEnumerable<INamingConvention> GetRegisteredConventions()
    {
        return _conventions.ToList();
    }

    /// <summary>
    /// Clears the resolution cache, forcing fresh resolution attempts for all types.
    /// </summary>
    public void ClearCache()
    {
        _resolutionCache.Clear();
    }

    /// <summary>
    /// Gets the current size of the resolution cache for memory monitoring purposes.
    /// </summary>
    public int CacheSize => _resolutionCache.Count;
}