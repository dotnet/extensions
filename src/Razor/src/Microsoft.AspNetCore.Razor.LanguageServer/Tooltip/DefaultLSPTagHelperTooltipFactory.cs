// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis.Razor.Tooltip;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace Microsoft.AspNetCore.Razor.LanguageServer.Tooltip
{
    internal class DefaultLSPTagHelperTooltipFactory : LSPTagHelperTooltipFactory
    {
        // Need to have a lazy server here because if we try to resolve the server it creates types which create a DefaultTagHelperDescriptionFactory, and we end up StackOverflowing.
        // This lazy can be avoided in the future by using an upcoming ILanguageServerSettings interface, but it doesn't exist/work yet.
        public DefaultLSPTagHelperTooltipFactory(ClientNotifierServiceBase languageServer)
        {
            if (languageServer is null)
            {
                throw new ArgumentNullException(nameof(languageServer));
            }

            LanguageServer = languageServer;
        }

        public ClientNotifierServiceBase LanguageServer;

        public override bool TryCreateTooltip(AggregateBoundElementDescription elementDescriptionInfo, out MarkupContent tooltipContent)
        {
            if (elementDescriptionInfo is null)
            {
                throw new ArgumentNullException(nameof(elementDescriptionInfo));
            }

            var associatedTagHelperInfos = elementDescriptionInfo.AssociatedTagHelperDescriptions;
            if (associatedTagHelperInfos.Count == 0)
            {
                tooltipContent = null;
                return false;
            }

            // This generates a markdown description that looks like the following:
            // **SomeTagHelper**
            //
            // The Summary documentation text with `CrefTypeValues` in code.
            //
            // Additional description infos result in a triple `---` to separate the markdown entries.

            var descriptionBuilder = new StringBuilder();
            for (var i = 0; i < associatedTagHelperInfos.Count; i++)
            {
                var descriptionInfo = associatedTagHelperInfos[i];

                if (descriptionBuilder.Length > 0)
                {
                    descriptionBuilder.AppendLine();
                    descriptionBuilder.AppendLine("---");
                }

                var tagHelperType = descriptionInfo.TagHelperTypeName;
                var reducedTypeName = ReduceTypeName(tagHelperType);
                StartOrEndBold(descriptionBuilder);
                descriptionBuilder.Append(reducedTypeName);
                StartOrEndBold(descriptionBuilder);

                var documentation = descriptionInfo.Documentation;
                if (!TryExtractSummary(documentation, out var summaryContent))
                {
                    continue;
                }

                descriptionBuilder.AppendLine();
                descriptionBuilder.AppendLine();
                var finalSummaryContent = CleanSummaryContent(summaryContent);
                descriptionBuilder.Append(finalSummaryContent);
            }

            tooltipContent = new MarkupContent
            {
                Kind = GetMarkupKind()
            };

            tooltipContent.Value = descriptionBuilder.ToString();
            return true;
        }

        public override bool TryCreateTooltip(AggregateBoundAttributeDescription attributeDescriptionInfo, out MarkupContent tooltipContent)
        {
            if (attributeDescriptionInfo is null)
            {
                throw new ArgumentNullException(nameof(attributeDescriptionInfo));
            }

            var associatedAttributeInfos = attributeDescriptionInfo.DescriptionInfos;
            if (associatedAttributeInfos.Count == 0)
            {
                tooltipContent = null;
                return false;
            }

            // This generates a markdown description that looks like the following:
            // **ReturnTypeName** SomeTypeName.**SomeProperty**
            //
            // The Summary documentation text with `CrefTypeValues` in code.
            //
            // Additional description infos result in a triple `---` to separate the markdown entries.

            var descriptionBuilder = new StringBuilder();
            for (var i = 0; i < associatedAttributeInfos.Count; i++)
            {
                var descriptionInfo = associatedAttributeInfos[i];

                if (descriptionBuilder.Length > 0)
                {
                    descriptionBuilder.AppendLine();
                    descriptionBuilder.AppendLine("---");
                }

                StartOrEndBold(descriptionBuilder);
                if (!TypeNameStringResolver.TryGetSimpleName(descriptionInfo.ReturnTypeName, out var returnTypeName))
                {
                    returnTypeName = descriptionInfo.ReturnTypeName;
                }
                var reducedReturnTypeName = ReduceTypeName(returnTypeName);
                descriptionBuilder.Append(reducedReturnTypeName);
                StartOrEndBold(descriptionBuilder);
                descriptionBuilder.Append(" ");
                var tagHelperTypeName = descriptionInfo.TypeName;
                var reducedTagHelperTypeName = ReduceTypeName(tagHelperTypeName);
                descriptionBuilder.Append(reducedTagHelperTypeName);
                descriptionBuilder.Append(".");
                StartOrEndBold(descriptionBuilder);
                descriptionBuilder.Append(descriptionInfo.PropertyName);
                StartOrEndBold(descriptionBuilder);

                var documentation = descriptionInfo.Documentation;
                if (!TryExtractSummary(documentation, out var summaryContent))
                {
                    continue;
                }

                descriptionBuilder.AppendLine();
                descriptionBuilder.AppendLine();
                var finalSummaryContent = CleanSummaryContent(summaryContent);
                descriptionBuilder.Append(finalSummaryContent);
            }

            tooltipContent = new MarkupContent
            {
                Kind = GetMarkupKind()
            };

            tooltipContent.Value = descriptionBuilder.ToString();
            return true;
        }

        // Internal for testing
        internal static string CleanSummaryContent(string summaryContent)
        {
            // Cleans out all <see cref="..." /> and <seealso cref="..." /> elements. It's possible to
            // have additional doc comment types in the summary but none that require cleaning. For instance
            // if there's a <para> in the summary element when it's shown in the completion description window
            // it'll be serialized as html (wont show).
            summaryContent = summaryContent.Trim();
            var crefMatches = ExtractCrefMatches(summaryContent);
            var summaryBuilder = new StringBuilder(summaryContent);

            for (var i = crefMatches.Count - 1; i >= 0; i--)
            {
                var cref = crefMatches[i];
                if (cref.Success)
                {
                    var value = cref.Groups[2].Value;
                    var reducedValue = ReduceCrefValue(value);
                    reducedValue = reducedValue.Replace("{", "<").Replace("}", ">");
                    summaryBuilder.Remove(cref.Index, cref.Length);
                    summaryBuilder.Insert(cref.Index, $"`{reducedValue}`");
                }
            }
            var lines = summaryBuilder.ToString().Split(new[] { '\n' }, StringSplitOptions.None).Select(line => line.Trim());
            var finalSummaryContent = string.Join(Environment.NewLine, lines);
            return finalSummaryContent;
        }

        private void StartOrEndBold(StringBuilder stringBuilder)
        {
            if (GetMarkupKind() == MarkupKind.Markdown)
            {
                stringBuilder.Append("**");
            }
        }

        private MarkupKind GetMarkupKind()
        {
            var completionSupportedKinds = LanguageServer.ClientSettings?.Capabilities?.TextDocument?.Completion.Value?.CompletionItem?.DocumentationFormat;
            var hoverSupportedKinds = LanguageServer.ClientSettings?.Capabilities?.TextDocument?.Hover.Value?.ContentFormat;

            // For now we're assuming that if you support Markdown for either completions or hovers you support it for both.
            // If this assumption is ever untrue we'll have to start informing this class about if a request is for Hover or Completions.
            var supportedKinds = completionSupportedKinds ?? hoverSupportedKinds;

            if (supportedKinds?.Contains(MarkupKind.Markdown) ?? false)
            {
                return MarkupKind.Markdown;
            }
            else
            {
                return MarkupKind.PlainText;
            }
        }
    }
}
