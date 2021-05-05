// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Newtonsoft.Json;

namespace Microsoft.VisualStudio.LanguageServerClient.Razor.HtmlCSharp
{
    internal class CompletionResolveData
    {
        public long ResultId { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public object OriginalData { get; set; }
    }
}
