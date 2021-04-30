// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Razor.LanguageServer.Serialization;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;

namespace Microsoft.AspNetCore.Razor.LanguageServer
{
    /// <summary>
    /// These client capabilities represent the superset of client capabilities from VS and VSCode.
    /// </summary>
    internal class PlatformAgnosticClientCapabilities : ClientCapabilities
    {
        public static readonly PlatformExtensionConverter<ClientCapabilities, PlatformAgnosticClientCapabilities> JsonConverter = new PlatformExtensionConverter<ClientCapabilities, PlatformAgnosticClientCapabilities>();

        public bool SupportsCodeActionResolve { get; set; } = false;

        public bool SupportsVisualStudioExtensions { get; set; } = false;
    }
}
