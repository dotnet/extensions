// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

// This class is a copy from the Razor repo.

using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Microsoft.AspNetCore.Razor.LanguageServer.Serialization
{
    internal static class JsonConverterCollectionExtensions
    {
        public static readonly IReadOnlyList<JsonConverter> RazorConverters = new List<JsonConverter>()
        {
            TagHelperDescriptorJsonConverter.Instance,
            RazorDiagnosticJsonConverter.Instance,
            RazorExtensionJsonConverter.Instance,
            RazorConfigurationJsonConverter.Instance,
        };

        public static void RegisterRazorConverters(this JsonConverterCollection collection)
        {
            if (collection == null)
            {
                throw new ArgumentNullException(nameof(collection));
            }

            for (var i = 0; i < RazorConverters.Count; i++)
            {
                collection.Add(RazorConverters[i]);
            }
        }
    }
}
