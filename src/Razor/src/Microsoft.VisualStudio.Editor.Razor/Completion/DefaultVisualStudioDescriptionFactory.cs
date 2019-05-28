// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Composition;
using Microsoft.CodeAnalysis.Razor.Completion;
using Microsoft.VisualStudio.Core.Imaging;
using Microsoft.VisualStudio.Text.Adornments;

namespace Microsoft.VisualStudio.Editor.Razor.Completion
{
    [Shared]
    [Export(typeof(VisualStudioDescriptionFactory))]
    internal class DefaultVisualStudioDescriptionFactory : VisualStudioDescriptionFactory
    {
        // Internal for testing
        internal static readonly ContainerElement SeparatorElement = new ContainerElement(
            ContainerElementStyle.Wrapped,
            new ClassifiedTextElement(
                new ClassifiedTextRun(PredefinedClassificationNames.Comment, "------------")));

        private static readonly IReadOnlyDictionary<string, string> KeywordTypeNameLookups = new Dictionary<string, string>(StringComparer.Ordinal)
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

        // Hardcoding the Guid here to avoid a reference to Microsoft.VisualStudio.ImageCatalog.dll
        // that is not present in Visual Studio for Mac
        private static readonly Guid ImageCatalogGuid = new Guid("{ae27a6b0-e345-4288-96df-5eaf394ee369}");
        private static readonly ImageElement PropertyGlyph = new ImageElement(
            new ImageId(ImageCatalogGuid, 2429), // KnownImageIds.Type = 2429
            "Razor Attribute Glyph");
        private static readonly ClassifiedTextRun SpaceLiteral = new ClassifiedTextRun(PredefinedClassificationNames.Literal, " ");
        private static readonly ClassifiedTextRun DotLiteral = new ClassifiedTextRun(PredefinedClassificationNames.Literal, ".");

        public override ContainerElement CreateClassifiedDescription(AttributeCompletionDescription completionDescription)
        {
            if (completionDescription is null)
            {
                throw new ArgumentNullException(nameof(completionDescription));
            }

            var descriptionElements = new List<object>();
            foreach (var descriptionInfo in completionDescription.DescriptionInfos)
            {
                if (descriptionElements.Count > 0)
                {
                    descriptionElements.Add(SeparatorElement);
                }

                var returnTypeClassification = PredefinedClassificationNames.Type;
                if (KeywordTypeNameLookups.TryGetValue(descriptionInfo.ReturnTypeName, out var returnTypeName))
                {
                    returnTypeClassification = PredefinedClassificationNames.Keyword;
                }
                else
                {
                    returnTypeName = descriptionInfo.ReturnTypeName;
                }

                var tagHelperTypeName = descriptionInfo.TypeName;
                var tagHelperTypeNamePrefix = string.Empty;
                var tagHelperTypeNameProper = tagHelperTypeName;

                var lastDot = tagHelperTypeName.LastIndexOf('.');
                if (lastDot > 0)
                {
                    var afterLastDot = lastDot + 1;

                    // We're pulling apart the type name so the prefix looks like:
                    //
                    // Microsoft.AspnetCore.Components.
                    tagHelperTypeNamePrefix = tagHelperTypeName.Substring(0, afterLastDot);

                    // And the type name looks like BindBinds
                    tagHelperTypeNameProper = tagHelperTypeName.Substring(afterLastDot);
                }

                descriptionElements.Add(
                    new ContainerElement(
                        ContainerElementStyle.Wrapped,
                        PropertyGlyph,
                        new ClassifiedTextElement(
                            new ClassifiedTextRun(returnTypeClassification, returnTypeName),
                            SpaceLiteral,
                            new ClassifiedTextRun(PredefinedClassificationNames.Literal, tagHelperTypeNamePrefix),
                            new ClassifiedTextRun(PredefinedClassificationNames.Type, tagHelperTypeNameProper),
                            DotLiteral,
                            new ClassifiedTextRun(PredefinedClassificationNames.Identifier, descriptionInfo.PropertyName))));

                if (descriptionInfo.Documentation != null)
                {
                    descriptionElements.Add(
                        new ContainerElement(
                            ContainerElementStyle.Wrapped,
                            new ClassifiedTextElement(
                                new ClassifiedTextRun(PredefinedClassificationNames.NaturalLanguage, descriptionInfo.Documentation))));
                }
            }

            var descriptionContainer = new ContainerElement(ContainerElementStyle.Stacked, descriptionElements);
            return descriptionContainer;
        }

        private static class PredefinedClassificationNames
        {
            public const string Keyword = "keyword";

            public const string Literal = "literal";

            public const string Type = "Type";

            public const string Identifier = "identifier";

            public const string Comment = "comment";

            public const string NaturalLanguage = "natural language";
        }
    }
}