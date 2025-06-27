namespace FS.AutoServiceDiscovery.Extensions.Attributes;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class ConditionalServiceAttribute(string configurationKey, string expectedValue) : Attribute
{
    public string ConfigurationKey { get; } = configurationKey;
    public string ExpectedValue { get; } = expectedValue;
}