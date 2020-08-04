// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis.Razor.Completion;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Server;
using RazorAttributeDescriptionInfo = Microsoft.CodeAnalysis.Razor.Completion.AttributeDescriptionInfo;

namespace Microsoft.AspNetCore.Razor.LanguageServer.Completion
{
    internal class DefaultTagHelperDescriptionFactory : TagHelperDescriptionFactory
    {
        private static readonly Lazy<Regex> ExtractCrefRegex = new Lazy<Regex>(
            () => new Regex("<(see|seealso)[\\s]+cref=\"([^\">]+)\"[^>]*>", RegexOptions.Compiled, TimeSpan.FromSeconds(1)));
        private static readonly IReadOnlyDictionary<string, string> PrimitiveDisplayTypeNameLookups = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            [typeof(byte).FullName] = "byte",
            [typeof(sbyte).FullName] = "sbyte",
            [typeof(int).FullName] = "int",
            [typeof(uint).FullName] = "uint",
            [typeof(short).FullName] = "short",
            [typeof(ushort).FullName] = "ushort",
            [typeof(long).FullName] = "long",
            [typeof(ulong).FullName] = "ulong",
            [typeof(float).FullName] = "float",
            [typeof(double).FullName] = "double",
            [typeof(char).FullName] = "char",
            [typeof(bool).FullName] = "bool",
            [typeof(object).FullName] = "object",
            [typeof(string).FullName] = "string",
            [typeof(decimal).FullName] = "decimal",
        };

        public DefaultTagHelperDescriptionFactory(ILanguageServer languageServer)
        {
            if (languageServer is null)
            {
                throw new NotImplementedException(nameof(languageServer));
            }

            LanguageServer = languageServer;
        }

        public ILanguageServer LanguageServer { get; }

        public override bool TryCreateDescription(ElementDescriptionInfo elementDescriptionInfo, out MarkupContent tagHelperDescription)
        {
            var associatedTagHelperInfos = elementDescriptionInfo.AssociatedTagHelperDescriptions;
            if (associatedTagHelperInfos.Count == 0)
            {
                tagHelperDescription = null;
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

            tagHelperDescription = new MarkupContent
            {
                Kind = GetMarkupKind()
            };

            tagHelperDescription.Value = descriptionBuilder.ToString();
            return true;
        }

        public override bool TryCreateDescription(AttributeCompletionDescription descriptionInfos, out MarkupContent tagHelperDescription)
        {
            var associatedAttributeInfos = descriptionInfos.DescriptionInfos;
            if (associatedAttributeInfos.Count == 0)
            {
                tagHelperDescription = null;
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
                var returnTypeName = GetSimpleName(descriptionInfo.ReturnTypeName);
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

            tagHelperDescription = new MarkupContent
            {
                Kind = GetMarkupKind()
            };

            tagHelperDescription.Value = descriptionBuilder.ToString();
            return true;
        }

        public override bool TryCreateDescription(AttributeDescriptionInfo attributeDescriptionInfo, out MarkupContent tagHelperDescription)
        {
            var convertedDescriptionInfos = new List<RazorAttributeDescriptionInfo>();
            foreach (var descriptionInfo in attributeDescriptionInfo.AssociatedAttributeDescriptions)
            {
                var tagHelperTypeName = ResolveTagHelperTypeName(descriptionInfo);
                var converted = new RazorAttributeDescriptionInfo(
                    descriptionInfo.ReturnTypeName,
                    tagHelperTypeName,
                    descriptionInfo.PropertyName,
                    descriptionInfo.Documentation);

                convertedDescriptionInfos.Add(converted);
            }

            var convertedDescriptionInfo = new AttributeCompletionDescription(convertedDescriptionInfos);

            return TryCreateDescription(convertedDescriptionInfo, out tagHelperDescription);
        }

        // Internal for testing
        internal static string CleanSummaryContent(string summaryContent)
        {
            // Cleans out all <see cref="..." /> and <seealso cref="..." /> elements. It's possible to
            // have additional doc comment types in the summary but none that require cleaning. For instance
            // if there's a <para> in the summary element when it's shown in the completion description window
            // it'll be serialized as html (wont show).
            summaryContent = summaryContent.Trim();
            var crefMatches = ExtractCrefRegex.Value.Matches(summaryContent);
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

        private static readonly char[] NewLineChars = new char[]{'\n', '\r'};

        // Internal for testing
        internal static bool TryExtractSummary(string documentation, out string summary)
        {
            const string summaryStartTag = "<summary>";
            const string summaryEndTag = "</summary>";

            if (string.IsNullOrEmpty(documentation))
            {
                summary = null;
                return false;
            }

            documentation = documentation.Trim(NewLineChars);

            var summaryTagStart = documentation.IndexOf(summaryStartTag, StringComparison.OrdinalIgnoreCase);
            var summaryTagEndStart = documentation.IndexOf(summaryEndTag, StringComparison.OrdinalIgnoreCase);
            if (summaryTagStart == -1 || summaryTagEndStart == -1)
            {
                // A really wrong but cheap way to check if this is XML
                if (!documentation.StartsWith("<", StringComparison.Ordinal) && !documentation.EndsWith(">", StringComparison.Ordinal))
                {
                    // This doesn't look like a doc comment, we'll return it as-is.
                    summary = documentation;
                    return true;
                }

                summary = null;
                return false;
            }

            var summaryContentStart = summaryTagStart + summaryStartTag.Length;
            var summaryContentLength = summaryTagEndStart - summaryContentStart;

            summary = documentation.Substring(summaryContentStart, summaryContentLength);
            return true;
        }

        // Internal for testing
        internal static string ReduceCrefValue(string value)
        {
            // cref values come in the following formats:
            // Type = "T:Microsoft.AspNetCore.SomeTagHelpers.SomeTypeName"
            // Property = "P:T:Microsoft.AspNetCore.SomeTagHelpers.SomeTypeName.AspAction"
            // Member = "M:T:Microsoft.AspNetCore.SomeTagHelpers.SomeTypeName.SomeMethod(System.Collections.Generic.List{System.String})"

            if (value.Length < 2)
            {
                return string.Empty;
            }

            var type = value[0];
            value = value.Substring(2);

            switch (type)
            {
                case 'T':
                    var reducedCrefType = ReduceTypeName(value);
                    return reducedCrefType;
                case 'P':
                case 'M':
                    // TypeName.MemberName
                    var reducedCrefProperty = ReduceMemberName(value);
                    return reducedCrefProperty;
            }

            return value;
        }

        // Internal for testing
        internal static string GetSimpleName(string typeName)
        {
            if (PrimitiveDisplayTypeNameLookups.TryGetValue(typeName, out var simpleName))
            {
                return simpleName;
            }

            return typeName;
        }

        // Internal for testing
        internal static string ResolveTagHelperTypeName(TagHelperAttributeDescriptionInfo info)
        {
            // A BoundAttributeDescriptor does not have a direct reference to its parent TagHelper.
            // However, when it was constructed the parent TagHelper's type name was embedded into
            // its DisplayName. In VSCode we can't use the DisplayName verbatim for descriptions
            // because the DisplayName is typically too long to display properly. Therefore we need
            // to break it apart and then reconstruct it in a reduced format.
            // i.e. this is the format the display name comes in:
            // ReturnTypeName SomeTypeName.SomePropertyName

            // We must simplify the return type name before using it to determine the type name prefix
            // because that is how the display name was originally built (a little hacky).
            var simpleReturnType = GetSimpleName(info.ReturnTypeName);

            // "ReturnTypeName "
            var typeNamePrefixLength = simpleReturnType.Length + 1 /* space */;

            // ".SomePropertyName"
            var typeNameSuffixLength = /* . */ 1 + info.PropertyName.Length;

            // "SomeTypeName"
            var typeNameLength = info.DisplayName.Length - typeNamePrefixLength - typeNameSuffixLength;
            var tagHelperTypeName = info.DisplayName.Substring(typeNamePrefixLength, typeNameLength);
            return tagHelperTypeName;
        }

        // Internal for testing
        internal static string ReduceTypeName(string content) => ReduceFullName(content, reduceWhenDotCount: 1);

        // Internal for testing
        internal static string ReduceMemberName(string content) => ReduceFullName(content, reduceWhenDotCount: 2);

        private static string ReduceFullName(string content, int reduceWhenDotCount)
        {
            // Starts searching backwards and then substrings everything when it finds enough dots. i.e. 
            // ReduceFullName("Microsoft.AspNetCore.SomeTagHelpers.SomeTypeName", 1) == "SomeTypeName"
            //
            // ReduceFullName("Microsoft.AspNetCore.SomeTagHelpers.SomeTypeName.AspAction", 2) == "SomeTypeName.AspAction"
            //
            // This is also smart enough to ignore nested dots in type generics[<>], methods[()], cref generics[{}].

            if (reduceWhenDotCount <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(reduceWhenDotCount));
            }

            var dotsSeen = 0;
            var scope = 0;
            for (var i = content.Length - 1; i >= 0; i--)
            {
                do
                {
                    if (content[i] == '}')
                    {
                        scope++;
                    }
                    else if (content[i] == '{')
                    {
                        scope--;
                    }

                    if (scope > 0)
                    {
                        i--;
                    }
                } while (scope != 0 && i >= 0);

                if (i < 0)
                {
                    // Could not balance scope
                    return content;
                }

                do
                {
                    if (content[i] == ')')
                    {
                        scope++;
                    }
                    else if (content[i] == '(')
                    {
                        scope--;
                    }

                    if (scope > 0)
                    {
                        i--;
                    }
                } while (scope != 0 && i >= 0);

                if (i < 0)
                {
                    // Could not balance scope
                    return content;
                }

                do
                {
                    if (content[i] == '>')
                    {
                        scope++;
                    }
                    else if (content[i] == '<')
                    {
                        scope--;
                    }

                    if (scope > 0)
                    {
                        i--;
                    }
                } while (scope != 0 && i >= 0);

                if (i < 0)
                {
                    // Could not balance scope
                    return content;
                }

                if (content[i] == '.')
                {
                    dotsSeen++;
                }

                if (dotsSeen == reduceWhenDotCount)
                {
                    var piece = content.Substring(i + 1);
                    return piece;
                }
            }

            // Could not reduce name
            return content;
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

            if (supportedKinds.Contains(MarkupKind.Markdown))
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
