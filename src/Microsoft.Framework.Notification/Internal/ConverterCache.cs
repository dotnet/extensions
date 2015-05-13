// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#if NET45 || DNX451 || DNXCORE50

using System;
using System.Collections.Concurrent;

namespace Microsoft.Framework.Notification.Internal
{
    public class ConverterCache : ConcurrentDictionary<Tuple<Type, Type>, CacheResult>
    {
    }
}

#endif
