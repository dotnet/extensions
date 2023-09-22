// Assembly 'Microsoft.Extensions.Http.AutoClient'

using System.ComponentModel.DataAnnotations;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace Microsoft.Extensions.Http.AutoClient;

public class AutoClientOptions
{
    [Required]
    public JsonSerializerOptions JsonSerializerOptions { get; set; }
    public AutoClientOptions();
}
