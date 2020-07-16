// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using MediatR;

namespace Microsoft.AspNetCore.Razor.LanguageServer
{
    internal class MonitorProjectConfigurationFilePathParams : IRequest
    {
        public string ProjectFilePath { get; set; }

        public string ConfigurationFilePath { get; set; }
    }
}
