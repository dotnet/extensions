// Assembly 'System.Cloud.DocumentDb.Abstractions'

using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace System.Cloud.DocumentDb;

public readonly struct Query
{
    public string QueryText { get; }
    public IReadOnlyDictionary<string, string> Parameters { get; }
    public Query(string queryText, IReadOnlyDictionary<string, string> parameters);
    public Query(string queryText);
}
