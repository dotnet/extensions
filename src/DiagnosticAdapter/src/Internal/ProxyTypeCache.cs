// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;

namespace Microsoft.Extensions.DiagnosticAdapter.Internal
{
    public class ProxyTypeCache : ConcurrentDictionary<Tuple<Type, Type>, ProxyTypeCacheResult>
    {
    }
}
