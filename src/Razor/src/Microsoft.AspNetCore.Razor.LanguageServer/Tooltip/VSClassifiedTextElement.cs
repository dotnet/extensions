// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Newtonsoft.Json;

namespace Microsoft.AspNetCore.Razor.LanguageServer.Tooltip
{
    /// <summary>
    /// Equivalent to VS' ClassifiedTextElement. The class has been adapted here so we
    /// can use it for LSP serialization since we don't have access to the VS version.
    /// Refer to original class for additional details.
    /// </summary>
    internal sealed class VSClassifiedTextElement
    {
        public const string TextClassificationTypeName = "text";

        public VSClassifiedTextElement(params VSClassifiedTextRun[] runs)
        {
            Runs = runs?.ToImmutableList() ?? throw new ArgumentNullException(nameof(runs));
        }

        public VSClassifiedTextElement(IEnumerable<VSClassifiedTextRun> runs)
        {
            Runs = runs?.ToImmutableList() ?? throw new ArgumentNullException(nameof(runs));
        }

        [JsonProperty("Runs")]
        public IEnumerable<VSClassifiedTextRun> Runs { get; }

        public static VSClassifiedTextElement CreateHyperlink(string text, string tooltip, Action navigationAction)
        {
            Requires.NotNull(text, nameof(text));
            Requires.NotNull(navigationAction, nameof(navigationAction));
            return new VSClassifiedTextElement(new VSClassifiedTextRun(TextClassificationTypeName, text, navigationAction, tooltip));
        }

        public static VSClassifiedTextElement CreatePlainText(string text)
        {
            Requires.NotNull(text, nameof(text));
            return new VSClassifiedTextElement(new VSClassifiedTextRun(TextClassificationTypeName, text, VSClassifiedTextRunStyle.Plain));
        }
    }
}
