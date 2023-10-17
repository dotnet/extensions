// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace Microsoft.Gen.AutoClient.Model;

internal sealed class RestApiMethod
{
    public readonly List<RestApiMethodParameter> AllParameters = [];
    public readonly List<string> FormatParameters = [];
    public string MethodName = string.Empty;
    public string? HttpMethod = string.Empty;
    public string? Path = string.Empty;
    public string? ReturnType = string.Empty;
    public string RequestName = string.Empty;
    public Dictionary<string, string> StaticHeaders = [];
}
