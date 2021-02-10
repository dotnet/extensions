// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#nullable enable

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.ExternalAccess.Razor;
using Microsoft.CodeAnalysis.Razor;
using Newtonsoft.Json;

namespace Microsoft.CodeAnalysis.Remote.Razor
{
    internal static class RazorServiceDescriptors
    {
        private const string ComponentName = "Razor";

        private static readonly ImmutableArray<JsonConverter> JsonConverters = new JsonConverterCollection()
            .RegisterRazorConverters()
            .ToImmutableArray();

        public static readonly RazorServiceDescriptorsWrapper TagHelperProviderServiceDescriptors = new RazorServiceDescriptorsWrapper(ComponentName, _ => "Razor TagHelper Provider", JsonConverters, new (Type, Type?)[] { (typeof(IRemoteTagHelperProviderService), null) });
    }
}
