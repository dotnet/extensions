// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Newtonsoft.Json;

namespace Microsoft.AspNetCore.Razor.LanguageServer.Tooltip
{
    /// <summary>
    /// Equivalent to VS' ContainerElement. The class has been adapted here so we can
    /// use it for LSP serialization since we don't have access to the VS version.
    /// Refer to original class for additional details.
    /// </summary>
    internal sealed class VSContainerElement
    {
        [JsonProperty("type")]
        public static readonly string Type = "ContainerElement";

        public VSContainerElement(VSContainerElementStyle style, IEnumerable<object> elements)
        {
            Style = style;
            Elements = elements?.ToImmutableList() ?? throw new ArgumentNullException(nameof(elements));
        }

        public VSContainerElement(VSContainerElementStyle style, params object[] elements)
        {
            Style = style;
            Elements = elements?.ToImmutableList() ?? throw new ArgumentNullException(nameof(elements));
        }

        [JsonProperty("Elements")]
        public IEnumerable<object> Elements { get; }

        [JsonProperty("Style")]
        public VSContainerElementStyle Style { get; }
    }
}
