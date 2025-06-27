using Microsoft.Extensions.Configuration;

namespace FS.AutoServiceDiscovery.Extensions.Configuration;

public class AutoServiceOptions
{
    public string? Profile { get; set; }
    public bool IsTestEnvironment { get; set; } = false;
    public bool EnableLogging { get; set; } = true;
    public IConfiguration? Configuration { get; set; }
}
