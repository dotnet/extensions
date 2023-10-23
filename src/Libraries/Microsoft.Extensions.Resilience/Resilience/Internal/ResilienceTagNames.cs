// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.Resilience.Internal;

internal static class ResilienceTagNames
{
    public const string ExceptionSource = "dotnet.exception.source";

    public const string ErrorType = "error.type";

    public const string DependencyName = "dotnet.dependency.name";

    public const string RequestName = "dotnet.request.name";
}
