// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.Framework.ConfigurationModel
{
    public static class IConfigurationSourceExtensions
    {
        public static string Get(this IConfigurationSource configSource,string key)
        {
            string value;

            if (!configSource.TryGet(key, out value))
            {
                throw new InvalidOperationException("Key not found");
            }

            return value;
        }
    }
}