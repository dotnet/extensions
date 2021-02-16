// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.
#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Components;
using Microsoft.AspNetCore.Razor.Test.Common;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;
using Microsoft.VisualStudio.Editor.Razor;
using Moq;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using DefaultRazorTagHelperCompletionService = Microsoft.VisualStudio.Editor.Razor.DefaultTagHelperCompletionService;
using RazorTagHelperCompletionService = Microsoft.VisualStudio.Editor.Razor.TagHelperCompletionService;

namespace Microsoft.AspNetCore.Razor.LanguageServer.Completion
{
    public abstract class TagHelperServiceTestBase : LanguageServerTestBase
    {
        protected const string CSHtmlFile = "test.cshtml";
        protected const string RazorFile = "test.razor";

        public TagHelperServiceTestBase()
        {
            var builder1 = TagHelperDescriptorBuilder.Create("Test1TagHelper", "TestAssembly");
            builder1.TagMatchingRule(rule => rule.TagName = "test1");
            builder1.SetTypeName("Test1TagHelper");
            builder1.BindAttribute(attribute =>
            {
                attribute.Name = "bool-val";
                attribute.SetPropertyName("BoolVal");
                attribute.TypeName = typeof(bool).FullName;
            });
            builder1.BindAttribute(attribute =>
            {
                attribute.Name = "int-val";
                attribute.SetPropertyName("IntVal");
                attribute.TypeName = typeof(int).FullName;
            });

            var builder2 = TagHelperDescriptorBuilder.Create("Test2TagHelper", "TestAssembly");
            builder2.TagMatchingRule(rule => rule.TagName = "test2");
            builder2.SetTypeName("Test2TagHelper");
            builder2.BindAttribute(attribute =>
            {
                attribute.Name = "bool-val";
                attribute.SetPropertyName("BoolVal");
                attribute.TypeName = typeof(bool).FullName;
            });
            builder2.BindAttribute(attribute =>
            {
                attribute.Name = "int-val";
                attribute.SetPropertyName("IntVal");
                attribute.TypeName = typeof(int).FullName;
            });

            var builder3 = TagHelperDescriptorBuilder.Create(ComponentMetadata.Component.TagHelperKind, "Component1TagHelper", "TestAssembly");
            builder3.TagMatchingRule(rule => rule.TagName = "Component1");
            builder3.SetTypeName("Component1");
            builder3.Metadata[ComponentMetadata.Component.NameMatchKey] = ComponentMetadata.Component.FullyQualifiedNameMatch;
            builder3.BindAttribute(attribute =>
            {
                attribute.Name = "bool-val";
                attribute.SetPropertyName("BoolVal");
                attribute.TypeName = typeof(bool).FullName;
            });
            builder3.BindAttribute(attribute =>
            {
                attribute.Name = "int-val";
                attribute.SetPropertyName("IntVal");
                attribute.TypeName = typeof(int).FullName;
            });

            var directiveAttribute1 = TagHelperDescriptorBuilder.Create(ComponentMetadata.Component.TagHelperKind, "TestDirectiveAttribute", "TestAssembly");
            directiveAttribute1.TagMatchingRule(rule =>
            {
                rule.TagName = "*";
                rule.RequireAttributeDescriptor(b =>
                {
                    b.Name = "@test";
                    b.NameComparisonMode = RequiredAttributeDescriptor.NameComparisonMode.PrefixMatch;
                });
            });
            directiveAttribute1.TagMatchingRule(rule =>
            {
                rule.TagName = "*";
                rule.RequireAttributeDescriptor(b =>
                {
                    b.Name = "@test";
                    b.NameComparisonMode = RequiredAttributeDescriptor.NameComparisonMode.FullMatch;
                });
            });
            directiveAttribute1.BindAttribute(attribute =>
            {
                attribute.Metadata[ComponentMetadata.Common.DirectiveAttribute] = bool.TrueString;
                attribute.Name = "@test";
                attribute.SetPropertyName("Test");
                attribute.TypeName = typeof(string).FullName;

                attribute.BindAttributeParameter(parameter =>
                {
                    parameter.Name = "something";
                    parameter.TypeName = typeof(string).FullName;

                    parameter.SetPropertyName("Something");
                });
            });
            directiveAttribute1.Metadata[TagHelperMetadata.Common.ClassifyAttributesOnly] = bool.TrueString;
            directiveAttribute1.Metadata[ComponentMetadata.Component.NameMatchKey] = ComponentMetadata.Component.FullyQualifiedNameMatch;
            directiveAttribute1.SetTypeName("TestDirectiveAttribute");

            var directiveAttribute2 = TagHelperDescriptorBuilder.Create(ComponentMetadata.Component.TagHelperKind, "MinimizedDirectiveAttribute", "TestAssembly");
            directiveAttribute2.TagMatchingRule(rule =>
            {
                rule.TagName = "*";
                rule.RequireAttributeDescriptor(b =>
                {
                    b.Name = "@minimized";
                    b.NameComparisonMode = RequiredAttributeDescriptor.NameComparisonMode.PrefixMatch;
                });
            });
            directiveAttribute2.TagMatchingRule(rule =>
            {
                rule.TagName = "*";
                rule.RequireAttributeDescriptor(b =>
                {
                    b.Name = "@minimized";
                    b.NameComparisonMode = RequiredAttributeDescriptor.NameComparisonMode.FullMatch;
                });
            });
            directiveAttribute2.BindAttribute(attribute =>
            {
                attribute.Metadata[ComponentMetadata.Common.DirectiveAttribute] = bool.TrueString;
                attribute.Name = "@minimized";
                attribute.SetPropertyName("Minimized");
                attribute.TypeName = typeof(bool).FullName;

                attribute.BindAttributeParameter(parameter =>
                {
                    parameter.Name = "something";
                    parameter.TypeName = typeof(string).FullName;

                    parameter.SetPropertyName("Something");
                });
            });
            directiveAttribute2.Metadata[TagHelperMetadata.Common.ClassifyAttributesOnly] = bool.TrueString;
            directiveAttribute2.Metadata[ComponentMetadata.Component.NameMatchKey] = ComponentMetadata.Component.FullyQualifiedNameMatch;
            directiveAttribute2.SetTypeName("TestDirectiveAttribute");

            DefaultTagHelpers = new[] { builder1.Build(), builder2.Build(), builder3.Build(), directiveAttribute1.Build(), directiveAttribute2.Build() };

            HtmlFactsService = new DefaultHtmlFactsService();
            TagHelperFactsService = new DefaultTagHelperFactsService();
            RazorTagHelperCompletionService = new DefaultRazorTagHelperCompletionService(TagHelperFactsService);
        }

        protected TagHelperDescriptor[] DefaultTagHelpers { get; }

        protected RazorTagHelperCompletionService RazorTagHelperCompletionService { get; }

        internal HtmlFactsService HtmlFactsService { get; }

        protected TagHelperFactsService TagHelperFactsService { get; }

        internal static RazorCodeDocument CreateCodeDocument(string text, params TagHelperDescriptor[] tagHelpers)
        {
            return CreateCodeDocument(text, CSHtmlFile, tagHelpers);
        }

        protected TextDocumentIdentifier GetIdentifier(bool isRazor)
        {
            var file = isRazor ? RazorFile : CSHtmlFile;
            return new TextDocumentIdentifier(new Uri($"c:\\${file}"));
        }

        internal (Queue<DocumentSnapshot>, Queue<TextDocumentIdentifier>) CreateDocumentSnapshot(string?[] textArray, bool[] isRazorArray, TagHelperDescriptor[] tagHelpers, VersionStamp projectVersion = default)
        {
            var documentSnapshots = new Queue<DocumentSnapshot>();
            var identifiers = new Queue<TextDocumentIdentifier>();
            foreach (var (text, isRazor) in textArray.Zip(isRazorArray, (t, r) => (t, r)))
            {
                var file = isRazor ? RazorFile : CSHtmlFile;
                var document = CreateCodeDocument(text, file, tagHelpers);

                var projectSnapshot = new Mock<ProjectSnapshot>(MockBehavior.Strict);
                projectSnapshot
                    .Setup(p => p.Version)
                    .Returns(projectVersion);

                var documentSnapshot = new Mock<DocumentSnapshot>(MockBehavior.Strict);
                documentSnapshot.Setup(d => d.GetGeneratedOutputAsync())
                    .ReturnsAsync(document);

                var version = VersionStamp.Create();
                documentSnapshot.Setup(d => d.GetTextVersionAsync())
                    .ReturnsAsync(version);

                documentSnapshot.Setup(d => d.Project)
                    .Returns(projectSnapshot.Object);

                documentSnapshots.Enqueue(documentSnapshot.Object);
                var identifier = GetIdentifier(isRazor);
                identifiers.Enqueue(identifier);
            }

            return (documentSnapshots, identifiers);
        }

        internal static RazorCodeDocument CreateCodeDocument(string text, string filePath, params TagHelperDescriptor[] tagHelpers)
        {
            tagHelpers ??= Array.Empty<TagHelperDescriptor>();
            var sourceDocument = TestRazorSourceDocument.Create(text, filePath: filePath, relativePath: filePath);
            var projectEngine = RazorProjectEngine.Create(builder => { });
            var fileKind = filePath.EndsWith(".razor", StringComparison.Ordinal) ? FileKinds.Component : FileKinds.Legacy;
            var codeDocument = projectEngine.ProcessDesignTime(sourceDocument, fileKind, Array.Empty<RazorSourceDocument>(), tagHelpers);

            return codeDocument;
        }
    }
}
