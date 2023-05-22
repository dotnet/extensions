// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Gen.AutoClient.Model;

internal static class BodyContentTypeParamExtensions
{
    public static string ConvertToString(this BodyContentTypeParam? param)
    {
        return param switch
        {
            BodyContentTypeParam.ApplicationJson => "application/json",
            BodyContentTypeParam.TextPlain => "text/plain",
            _ => string.Empty,
        };
    }
}
