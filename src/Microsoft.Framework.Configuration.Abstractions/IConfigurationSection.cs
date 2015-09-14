// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.


namespace Microsoft.Framework.Configuration
{
    public interface IConfigurationSection : IConfiguration
    {
        string Key { get; }
        string Path { get; }
        string Value { get; set; }
    }
}
