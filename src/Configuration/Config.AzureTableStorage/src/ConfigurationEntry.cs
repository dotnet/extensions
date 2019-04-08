// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.WindowsAzure.Storage.Table;

namespace Microsoft.Extensions.Configuration.AzureTableStorage
{
    internal class ConfigurationEntry : TableEntity
    {
        public string Value { get; set; }
        public bool IsActive { get; set; }
    }
}
