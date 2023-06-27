// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Extensions.Http.Resilience.Internal;

internal readonly record struct HttpKey(string Name, string Key)
{
    public static readonly IEqualityComparer<HttpKey> BuilderComparer = new BuilderEqualityComparer();

    private class BuilderEqualityComparer : IEqualityComparer<HttpKey>
    {
        public bool Equals(HttpKey x, HttpKey y) => StringComparer.Ordinal.Equals(x.Name, y.Name);

        public int GetHashCode([DisallowNull] HttpKey obj) => StringComparer.Ordinal.GetHashCode(obj.Name);
    }
}
