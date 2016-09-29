// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.Extensions.Logging.Azure.AppServicesDiagnostics.Test
{
    // The format of this file is defined by the Azure Portal. Do not change
    internal class SettingsFileContent
    {
        public bool AzureDriveEnabled { get; set; }
        public string AzureDriveTraceLevel { get; set; }

        public bool AzureTableEnabled { get; set; }
        public string AzureTableTraceLevel { get; set; }

        public bool AzureBlobEnabled { get; set; }
        public string AzureBlobTraceLevel { get; set; }
    }
}
