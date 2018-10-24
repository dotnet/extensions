// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Reflection;

namespace Microsoft.Extensions.DiagnosticAdapter.Internal
{
    public class ProxyTypeCacheResult
    {
        public static ProxyTypeCacheResult FromError(Tuple<Type, Type> key, string error)
        {
            return new ProxyTypeCacheResult()
            {
                Key = key,
                Error = error,
            };
        }

        public static ProxyTypeCacheResult FromType(
            Tuple<Type, Type> key,
            Type type,
            ConstructorInfo constructor)
        {
            return new ProxyTypeCacheResult()
            {
                Key = key,
                Type = type,
                Constructor = constructor,
            };
        }

        public ConstructorInfo Constructor { get; private set; }

        public string Error { get; private set; }

        public bool IsError => Error != null;

        public Tuple<Type, Type> Key { get; private set; }

        public Type Type { get; private set; }
    }
}
