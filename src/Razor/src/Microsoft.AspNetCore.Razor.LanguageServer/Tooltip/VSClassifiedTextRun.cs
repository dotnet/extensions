// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Newtonsoft.Json;

namespace Microsoft.AspNetCore.Razor.LanguageServer.Tooltip
{
    /// <summary>
    /// Equivalent to VS' ClassifiedTextRun. The class has been adapted here so we
    /// can use it for LSP serialization since we don't have access to the VS version.
    /// Refer to original class for additional details.
    /// </summary>
    internal sealed class VSClassifiedTextRun
    {
        public VSClassifiedTextRun(string classificationTypeName, string text)
            : this(classificationTypeName, text, VSClassifiedTextRunStyle.Plain)
        {
        }

        public VSClassifiedTextRun(string classificationTypeName, string text, VSClassifiedTextRunStyle style)
        {
            ClassificationTypeName = classificationTypeName
                ?? throw new ArgumentNullException(nameof(classificationTypeName));
            Text = text ?? throw new ArgumentNullException(nameof(text));
            Style = style;
        }

        public VSClassifiedTextRun(string classificationTypeName, string text, VSClassifiedTextRunStyle style, string markerTagType)
        {
            ClassificationTypeName = classificationTypeName
                ?? throw new ArgumentNullException(nameof(classificationTypeName));
            Text = text ?? throw new ArgumentNullException(nameof(text));
            MarkerTagType = markerTagType;
            Style = style;
        }

        public VSClassifiedTextRun(
            string classificationTypeName,
            string text,
            Action navigationAction,
            string tooltip = null,
            VSClassifiedTextRunStyle style = VSClassifiedTextRunStyle.Plain)
        {
            ClassificationTypeName = classificationTypeName
                ?? throw new ArgumentNullException(nameof(classificationTypeName));
            Text = text ?? throw new ArgumentNullException(nameof(text));
            Style = style;

            NavigationAction = navigationAction ?? throw new ArgumentNullException(nameof(navigationAction));
            Tooltip = tooltip;
        }

        [JsonProperty("ClassificationTypeName")]
        public string ClassificationTypeName { get; }

        [JsonProperty("Text")]
        public string Text { get; }

        [JsonProperty("MarkerTagType")]
        public string MarkerTagType { get; }

        [JsonProperty("Style")]
        public VSClassifiedTextRunStyle Style { get; }

        [JsonProperty("Tooltip")]
        public string Tooltip { get; }

        [JsonProperty("NavigationAction")]
        public Action NavigationAction { get; }
    }
}
