using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace FS.AutoServiceDiscovery.Extensions.Diagnostics;

/// <summary>
/// Provides standardized metrics and tracing instrumentation for the auto service discovery pipeline.
/// Uses the modern System.Diagnostics.Metrics API for seamless integration with OpenTelemetry,
/// Prometheus, Application Insights, and other observability backends.
/// </summary>
/// <remarks>
/// All instruments are created lazily and have zero overhead when no listener is attached.
/// Metric names follow the OpenTelemetry semantic conventions with the "autoservice." prefix.
/// </remarks>
public static class AutoServiceDiscoveryMetrics
{
    /// <summary>
    /// The meter instance used to create all discovery-related instruments.
    /// </summary>
    public static readonly Meter Meter = new("FS.AutoServiceDiscovery", "10.0.2");

    /// <summary>
    /// The activity source for distributed tracing of discovery operations.
    /// </summary>
    public static readonly ActivitySource ActivitySource = new("FS.AutoServiceDiscovery", "10.0.2");

    // ── Startup / Discovery metrics ──

    /// <summary>
    /// Total time spent performing service discovery (milliseconds).
    /// </summary>
    public static readonly Histogram<double> DiscoveryDuration =
        Meter.CreateHistogram<double>("autoservice.discovery.duration", "ms",
            "Total time spent performing service discovery");

    /// <summary>
    /// Time spent scanning a single assembly (milliseconds).
    /// </summary>
    public static readonly Histogram<double> AssemblyScanDuration =
        Meter.CreateHistogram<double>("autoservice.assembly.scan.duration", "ms",
            "Time spent scanning a single assembly");

    /// <summary>
    /// Number of assemblies scanned during discovery.
    /// </summary>
    public static readonly Counter<int> AssembliesScanned =
        Meter.CreateCounter<int>("autoservice.assemblies.scanned",
            description: "Number of assemblies scanned during discovery");

    /// <summary>
    /// Number of services successfully registered.
    /// </summary>
    public static readonly Counter<int> ServicesRegistered =
        Meter.CreateCounter<int>("autoservice.services.registered",
            description: "Number of services successfully registered");

    /// <summary>
    /// Number of services skipped during discovery. Use the "reason" tag for details.
    /// </summary>
    public static readonly Counter<int> ServicesSkipped =
        Meter.CreateCounter<int>("autoservice.services.skipped",
            description: "Number of services skipped during discovery");

    // ── Cache metrics ──

    /// <summary>
    /// Number of cache hits during assembly scan lookups.
    /// </summary>
    public static readonly Counter<int> CacheHits =
        Meter.CreateCounter<int>("autoservice.cache.hits",
            description: "Number of cache hits during assembly scan lookups");

    /// <summary>
    /// Number of cache misses during assembly scan lookups.
    /// </summary>
    public static readonly Counter<int> CacheMisses =
        Meter.CreateCounter<int>("autoservice.cache.misses",
            description: "Number of cache misses during assembly scan lookups");

    // ── Feature-specific metrics ──

    /// <summary>
    /// Number of decorators applied during discovery.
    /// </summary>
    public static readonly Counter<int> DecoratorsApplied =
        Meter.CreateCounter<int>("autoservice.decorators.applied",
            description: "Number of decorators applied during discovery");

    /// <summary>
    /// Number of scope violations detected.
    /// </summary>
    public static readonly Counter<int> ScopeViolations =
        Meter.CreateCounter<int>("autoservice.scope.violations",
            description: "Number of scope violations detected");

    /// <summary>
    /// Number of scope warnings detected.
    /// </summary>
    public static readonly Counter<int> ScopeWarnings =
        Meter.CreateCounter<int>("autoservice.scope.warnings",
            description: "Number of scope warnings detected");

    /// <summary>
    /// Number of duplicate registrations prevented by TryAdd.
    /// </summary>
    public static readonly Counter<int> DuplicatesPrevented =
        Meter.CreateCounter<int>("autoservice.tryadd.prevented",
            description: "Number of duplicate registrations prevented by TryAdd");

    /// <summary>
    /// Number of keyed service registrations.
    /// </summary>
    public static readonly Counter<int> KeyedRegistrations =
        Meter.CreateCounter<int>("autoservice.keyed.registrations",
            description: "Number of keyed service registrations");

    /// <summary>
    /// Number of open generic registrations.
    /// </summary>
    public static readonly Counter<int> OpenGenericRegistrations =
        Meter.CreateCounter<int>("autoservice.opengeneric.registrations",
            description: "Number of open generic registrations");

    /// <summary>
    /// Scope validation duration (milliseconds).
    /// </summary>
    public static readonly Histogram<double> ScopeValidationDuration =
        Meter.CreateHistogram<double>("autoservice.scope.validation.duration", "ms",
            "Time spent on scope validation");
}
