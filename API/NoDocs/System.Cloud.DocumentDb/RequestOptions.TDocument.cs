// Assembly 'System.Cloud.DocumentDb.Abstractions'

using System.Runtime.CompilerServices;

namespace System.Cloud.DocumentDb;

public class RequestOptions<TDocument> : RequestOptions where TDocument : notnull
{
    public TDocument? Document { get; set; }
    public RequestOptions();
}
