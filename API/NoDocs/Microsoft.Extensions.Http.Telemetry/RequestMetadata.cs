// Assembly 'Microsoft.Extensions.Telemetry.Abstractions'

using System.Runtime.CompilerServices;

namespace Microsoft.Extensions.Http.Telemetry;

public class RequestMetadata
{
    public string RequestRoute { get; set; }
    public string RequestName { get; set; }
    public string DependencyName { get; set; }
    public string MethodType { get; set; }
    public RequestMetadata();
    public RequestMetadata(string methodType, string requestRoute, string requestName = "unknown");
}
