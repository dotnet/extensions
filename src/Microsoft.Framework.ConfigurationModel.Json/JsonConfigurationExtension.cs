// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.Framework.ConfigurationModel
{
    public static class JsonConfigurationExtension
    {
        public static IConfigurationSourceRoot AddJsonFile(this IConfigurationSourceRoot configuration, string path)
        {
            configuration.Add(new JsonConfigurationSource(path));
            return configuration;
        }
    }
}
