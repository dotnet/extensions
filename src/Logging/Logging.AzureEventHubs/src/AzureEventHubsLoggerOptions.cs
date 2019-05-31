// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.Extensions.Logging.AzureEventHubs
{
    public class AzureEventHubsLoggerOptions
    {
        public string ConnectionString { get; set; }

        public string Namespace { get; set; }

        public string Instance { get; set; }

        public bool IncludeScopes { get; set; }
    }
}
