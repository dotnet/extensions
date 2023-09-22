// Assembly 'Microsoft.Extensions.AmbientMetadata.Application'

using System.ComponentModel.DataAnnotations;
using System.Runtime.CompilerServices;

namespace Microsoft.Extensions.AmbientMetadata;

public class ApplicationMetadata
{
    public string? DeploymentRing { get; set; }
    public string? BuildVersion { get; set; }
    [Required]
    public string ApplicationName { get; set; }
    [Required]
    public string EnvironmentName { get; set; }
    public ApplicationMetadata();
}
