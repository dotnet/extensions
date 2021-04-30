// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis.Razor.Tooltip;

namespace Microsoft.AspNetCore.Razor.LanguageServer.Tooltip
{
    internal class DefaultVSLSPTagHelperTooltipFactory : VSLSPTagHelperTooltipFactory
    {
        private static readonly IReadOnlyList<string> CSharpPrimitiveTypes =
            new string[] { "bool", "byte", "sbyte", "char", "decimal", "double", "float", "int", "uint",
                "nint", "nuint", "long", "ulong", "short", "ushort", "object", "string", "dynamic" };

        private static readonly IReadOnlyDictionary<string, string> TypeNameToAlias = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            { "Int32", "int" },
            { "Int64", "long" },
            { "Int16", "short" },
            { "Single", "float" },
            { "Double", "double" },
            { "Decimal", "decimal" },
            { "Boolean", "bool" },
            { "String", "string" },
            { "Char", "char" }
        };

        private static readonly VSClassifiedTextRun Space = new(VSPredefinedClassificationTypeNames.WhiteSpace, " ");
        private static readonly VSClassifiedTextRun Dot = new(VSPredefinedClassificationTypeNames.Punctuation, ".");
        private static readonly VSClassifiedTextRun NewLine = new(VSPredefinedClassificationTypeNames.WhiteSpace, Environment.NewLine);
        private static readonly VSClassifiedTextRun NullableType = new(VSPredefinedClassificationTypeNames.Punctuation, "?");

        public override bool TryCreateTooltip(AggregateBoundElementDescription elementDescriptionInfo, out VSClassifiedTextElement tagHelperDescription)
        {
            if (elementDescriptionInfo is null)
            {
                throw new ArgumentNullException(nameof(elementDescriptionInfo));
            }

            var associatedTagHelperInfos = elementDescriptionInfo.AssociatedTagHelperDescriptions;
            if (associatedTagHelperInfos.Count == 0)
            {
                tagHelperDescription = null;
                return false;
            }

            // Generates a ClassifiedTextElement that looks something like:
            //     Namespace.TypeName
            //     Summary description
            // with the specific element parts classified appropriately.
            // Additional entries are separated by two new lines.

            var runs = new List<VSClassifiedTextRun>();
            foreach (var descriptionInfo in associatedTagHelperInfos)
            {
                if (runs.Count > 0)
                {
                    runs.Add(NewLine);
                    runs.Add(NewLine);
                }

                ClassifyTypeName(runs, descriptionInfo.TagHelperTypeName);
                TryClassifySummary(runs, descriptionInfo.Documentation);
            }

            tagHelperDescription = new VSClassifiedTextElement(runs.ToArray());
            return true;
        }

        public override bool TryCreateTooltip(AggregateBoundAttributeDescription descriptionInfos, out VSClassifiedTextElement tagHelperDescription)
        {
            if (descriptionInfos is null)
            {
                throw new ArgumentNullException(nameof(descriptionInfos));
            }

            var associatedAttributeInfos = descriptionInfos.DescriptionInfos;
            if (associatedAttributeInfos.Count == 0)
            {
                tagHelperDescription = null;
                return false;
            }

            // Generates a ClassifiedTextElement that looks something like:
            //     ReturnType Namespace.TypeName.Property
            //     Summary description
            // with the specific element parts classified appropriately.
            // Additional entries are separated by two new lines.

            var runs = new List<VSClassifiedTextRun>();
            foreach (var descriptionInfo in associatedAttributeInfos)
            {
                if (runs.Count > 0)
                {
                    runs.Add(NewLine);
                    runs.Add(NewLine);
                }

                if (!TypeNameStringResolver.TryGetSimpleName(descriptionInfo.ReturnTypeName, out var returnTypeName))
                {
                    returnTypeName = descriptionInfo.ReturnTypeName;
                }

                var reducedReturnTypeName = ReduceTypeName(returnTypeName);
                ClassifyReducedTypeName(runs, reducedReturnTypeName);
                runs.Add(Space);
                ClassifyTypeName(runs, descriptionInfo.TypeName);
                runs.Add(Dot);
                runs.Add(new VSClassifiedTextRun(VSPredefinedClassificationTypeNames.Identifier, descriptionInfo.PropertyName));
                TryClassifySummary(runs, descriptionInfo.Documentation);
            }

            tagHelperDescription = new VSClassifiedTextElement(runs.ToArray());
            return true;
        }

        private static void ClassifyTypeName(List<VSClassifiedTextRun> runs, string tagHelperTypeName)
        {
            var reducedTypeName = ReduceTypeName(tagHelperTypeName);
            if (reducedTypeName == tagHelperTypeName)
            {
                ClassifyReducedTypeName(runs, reducedTypeName);
                return;
            }

            // If we reach this point, the type is prefixed by a namespace so we have to do a little extra work.
            var typeNameParts = tagHelperTypeName.Split('.');

            var reducedTypeIndex = Array.LastIndexOf(typeNameParts, reducedTypeName);

            for (var partIndex = 0; partIndex < typeNameParts.Length; partIndex++)
            {
                if (partIndex != 0)
                {
                    runs.Add(Dot);
                }

                var typeNamePart = typeNameParts[partIndex];

                // Only the reduced type name should be classified as non-plain text. We also need to check
                // for a matching index since other parts of the full type name may include the reduced type
                // name (e.g. Namespace.Pages.Pages).
                if (typeNamePart == reducedTypeName && partIndex == reducedTypeIndex)
                {
                    ClassifyReducedTypeName(runs, typeNamePart);
                }
                else
                {
                    runs.Add(new VSClassifiedTextRun(VSPredefinedClassificationTypeNames.Text, typeNamePart));
                }
            }
        }

        private static void ClassifyReducedTypeName(List<VSClassifiedTextRun> runs, string reducedTypeName)
        {
            var currentTextRun = new StringBuilder();
            for (var i = 0; i < reducedTypeName.Length; i++)
            {
                var ch = reducedTypeName[i];

                // There are certain characters that should be classified as plain text. For example,
                // in 'TypeName<T, T2>', the characters '<', ',' and '>' should be classified as plain
                // text while the rest should be classified as a keyword or type.
                if (ch == '<' || ch == '>' || ch == '[' || ch == ']' || ch == ',')
                {
                    if (currentTextRun.Length != 0)
                    {
                        var currentRunTextStr = currentTextRun.ToString();

                        // The type we're working with could contain a nested type, in which case we may
                        // also need to reduce the inner type name(s), e.g. 'List<NamespaceName.TypeName>'
                        if ((ch == '<' || ch == '>' || ch == '[' || ch == ']') && currentRunTextStr.Contains("."))
                        {
                            var reducedName = ReduceTypeName(currentRunTextStr);
                            ClassifyShortName(runs, reducedName);
                        }
                        else
                        {
                            ClassifyShortName(runs, currentRunTextStr);
                        }

                        currentTextRun.Clear();
                    }

                    runs.Add(new VSClassifiedTextRun(VSPredefinedClassificationTypeNames.Punctuation, ch.ToString()));
                }
                else
                {
                    currentTextRun.Append(ch);
                }
            }

            if (currentTextRun.Length != 0)
            {
                ClassifyShortName(runs, currentTextRun.ToString());
            }
        }

        private static void ClassifyShortName(List<VSClassifiedTextRun> runs, string typeName)
        {
            var nullableType = typeName.EndsWith("?");
            if (nullableType)
            {
                // Classify the '?' symbol separately from the rest of the type since it's considered punctuation.
                typeName = typeName.Substring(0, typeName.Length - 1);
            }

            // Case 1: Type can be aliased as a C# built-in type (e.g. Boolean -> bool, Int32 -> int, etc.).
            if (TypeNameToAlias.TryGetValue(typeName, out var aliasedTypeName))
            {
                runs.Add(new VSClassifiedTextRun(VSPredefinedClassificationTypeNames.Keyword, aliasedTypeName));
            }
            // Case 2: Type is a C# built-in type (e.g. bool, int, etc.).
            else if (CSharpPrimitiveTypes.Contains(typeName))
            {
                runs.Add(new VSClassifiedTextRun(VSPredefinedClassificationTypeNames.Keyword, typeName));
            }
            // Case 3: All other types.
            else
            {
                runs.Add(new VSClassifiedTextRun(VSPredefinedClassificationTypeNames.Type, typeName));
            }

            if (nullableType)
            {
                runs.Add(NullableType);
            }
        }

        private static bool TryClassifySummary(List<VSClassifiedTextRun> runs, string documentation)
        {
            if (!TryExtractSummary(documentation, out var summaryContent))
            {
                return false;
            }

            runs.Add(NewLine);
            CleanAndClassifySummaryContent(runs, summaryContent);
            return true;
        }

        // Internal for testing
        internal static void CleanAndClassifySummaryContent(List<VSClassifiedTextRun> runs, string summaryContent)
        {
            // Cleans out all <see cref="..." /> and <seealso cref="..." /> elements. It's possible to
            // have additional doc comment types in the summary but none that require cleaning. For instance
            // if there's a <para> in the summary element when it's shown in the completion description window
            // it'll be serialized as html (wont show).
            summaryContent = summaryContent.Trim();
            var lines = summaryContent.ToString().Split('\n').Select(line => line.Trim());
            summaryContent = string.Join(Environment.NewLine, lines);

            // There's a few edge cases we need to explicitly convert.
            summaryContent = summaryContent.Replace("&lt;", "<");
            summaryContent = summaryContent.Replace("&gt;", ">");
            summaryContent = summaryContent.Replace("<para>", Environment.NewLine);
            summaryContent = summaryContent.Replace("</para>", Environment.NewLine);

            var codeMatches = ExtractCodeMatches(summaryContent);
            var crefMatches = ExtractCrefMatches(summaryContent);

            if (codeMatches.Count == 0 && crefMatches.Count == 0)
            {
                runs.Add(new VSClassifiedTextRun(VSPredefinedClassificationTypeNames.Text, summaryContent));
                return;
            }

            var currentTextRun = new StringBuilder();
            var currentCrefMatchIndex = 0;
            var currentCodeMatchIndex = 0;
            for (var i = 0; i < summaryContent.Length; i++)
            {
                // If we made it through all the crefs and code matches, add the rest of the text and break out of the loop.
                if (currentCrefMatchIndex == crefMatches.Count && currentCodeMatchIndex == codeMatches.Count)
                {
                    currentTextRun.Append(summaryContent.Substring(i));
                    break;
                }

                var currentCodeMatch = currentCodeMatchIndex < codeMatches.Count ? codeMatches[currentCodeMatchIndex] : null;
                var currentCrefMatch = currentCrefMatchIndex < crefMatches.Count ? crefMatches[currentCrefMatchIndex] : null;

                if (currentCodeMatch != null && i == currentCodeMatch.Index)
                {
                    ClassifyExistingTextRun(runs, currentTextRun);

                    // We've processed the existing string, now we can process the code block.
                    var value = currentCodeMatch.Groups[1].Value;
                    if (value.Length != 0)
                    {
                        runs.Add(new VSClassifiedTextRun(VSPredefinedClassificationTypeNames.Text, value.ToString(), VSClassifiedTextRunStyle.UseClassificationFont));
                    }

                    i += currentCodeMatch.Length - 1;
                    currentCodeMatchIndex++;
                }
                else if (currentCrefMatch != null && i == currentCrefMatch.Index)
                {
                    ClassifyExistingTextRun(runs, currentTextRun);

                    // We've processed the existing string, now we can process the actual cref.
                    var value = currentCrefMatch.Groups[2].Value;
                    var reducedValue = ReduceCrefValue(value);
                    reducedValue = reducedValue.Replace("{", "<").Replace("}", ">").Replace("`1", "<>");
                    ClassifyTypeName(runs, reducedValue);

                    i += currentCrefMatch.Length - 1;
                    currentCrefMatchIndex++;
                }
                else
                {
                    currentTextRun.Append(summaryContent[i]);
                }
            }

            ClassifyExistingTextRun(runs, currentTextRun);

            static void ClassifyExistingTextRun(List<VSClassifiedTextRun> runs, StringBuilder currentTextRun)
            {
                if (currentTextRun.Length != 0)
                {

                    runs.Add(new VSClassifiedTextRun(VSPredefinedClassificationTypeNames.Text, currentTextRun.ToString()));
                    currentTextRun.Clear();
                }
            }
        }

        // Internal for testing
        // Adapted from VS' PredefinedClassificationTypeNames
        internal static class VSPredefinedClassificationTypeNames
        {
            public const string Identifier = "identifier";

            public const string Keyword = "keyword";

            public const string Punctuation = "punctuation";

            public const string Text = "text";

            public const string Type = "type";

            public const string WhiteSpace = "whitespace";
        }
    }
}
