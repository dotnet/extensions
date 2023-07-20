// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Gen.AutoClient.Model;

internal sealed class RestApiMethodParameter
{
    public string Name = string.Empty;
    public string Type = string.Empty;
    public string? HeaderName;
    public string? QueryKey;
    public BodyContentTypeParam? BodyType;
    public bool IsCancellationToken;
    public bool Nullable;

    public bool IsHeader => HeaderName != null;
    public bool IsQuery => QueryKey != null;
    public bool IsBody => BodyType != null;
}
