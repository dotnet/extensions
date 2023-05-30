// Assembly 'Microsoft.Extensions.Http.AutoClient'

using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Primitives;

namespace Microsoft.Extensions.Http.AutoClient;

public class AutoClientHttpError
{
    public int StatusCode { get; }
    public IReadOnlyDictionary<string, StringValues> ResponseHeaders { get; }
    public string RawContent { get; }
    public string? ReasonPhrase { get; }
    public AutoClientHttpError(int statusCode, IReadOnlyDictionary<string, StringValues> responseHeaders, string rawContent, string? reasonPhrase);
    public static Task<AutoClientHttpError> CreateAsync(HttpResponseMessage response, CancellationToken cancellationToken);
}
