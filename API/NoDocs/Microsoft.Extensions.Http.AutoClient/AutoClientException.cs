// Assembly 'Microsoft.Extensions.Http.AutoClient'

using System;
using System.Runtime.CompilerServices;

namespace Microsoft.Extensions.Http.AutoClient;

public class AutoClientException : Exception
{
    public AutoClientHttpError? HttpError { get; }
    public string Path { get; }
    public int? StatusCode { get; }
    public AutoClientException(string message, string path, AutoClientHttpError? error = null);
    public AutoClientException(string message, Exception innerException, string path, AutoClientHttpError? error = null);
}
