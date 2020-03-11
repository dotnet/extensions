// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Composition;
using System.Linq;
using System.Text;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Components;
using Microsoft.AspNetCore.Razor.Language.Extensions;
using Microsoft.AspNetCore.Razor.Language.Syntax;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using DocumentSnapshot = Microsoft.CodeAnalysis.Razor.ProjectSystem.DocumentSnapshot;
using SourceText = Microsoft.CodeAnalysis.Text.SourceText;
using TextLineCollection = Microsoft.CodeAnalysis.Text.TextLineCollection;
using TextSpan = Microsoft.CodeAnalysis.Text.TextSpan;

namespace Microsoft.VisualStudio.Editor.Razor
{
    [Shared]
    [Export(typeof(DocumentDivergenceChecker))]
    internal class DefaultDocumentDivergenceChecker : DocumentDivergenceChecker
    {
        private static readonly RazorParserOptions ParserOptions = RazorParserOptions.Create(builder =>
        {
            builder.Directives.Add(FunctionsDirective.Directive);
            builder.Directives.Add(ComponentCodeDirective.Directive);
        });

        // The goal of this method is to decide if the older and newer snapshots have diverged in a way that could result in
        // impactful changes to other Razor files. Detecting divergence is an optimization which allows us to not always re-parse
        // dependent files and therefore not not notify anyone else listening (Roslyn).
        //
        // Here's how we calculate "divergence":
        //
        // 1. Get the SyntaxTree for each document. If one has not been calculated yet, create a one-off syntax tree that only cares about
        //    capturing Razor directives.
        // 2. Extract @code and @functions directive blocks
        // 3. Map those directive blocks back to the original source document and extract their inner content.
        //    Aka @code { private int _foo; } => private int _foo;
        // 4. Do a light-weight C# parse on the content of each captured @code/@functions content in order to build a C# SyntaxTree. The
        //    SyntaxTree is not meant to be full-fidelity or error free, it's just meant to represent the key pieces of the content like
        //    methods, fields, properties etc.
        // 5. Extract all properties for the new and old documents.
        // 6. Compare the old documents properties to the new documents properties, if they've changed structurally then we've diverged;
        //    otherwise no divergence!
        //
        // At any point in this flow if we're unable to calculate one of the requirements such as the original Razor documents source text,
        // assume divergence.
        public override bool PossibleDivergence(DocumentSnapshot old, DocumentSnapshot @new)
        {
            if (old is null)
            {
                throw new ArgumentNullException(nameof(old));
            }

            if (@new is null)
            {

                throw new ArgumentNullException(nameof(@new));
            }

            if (!string.Equals(@new.FileKind, FileKinds.Component, StringComparison.OrdinalIgnoreCase))
            {
                // Component import or ordinary cshtml file.
                return true;
            }

            if (!TryGetSyntaxTreeAndSource(old, out var oldSyntaxTree, out var oldText))
            {
                return true;
            }

            if (!TryGetSyntaxTreeAndSource(@new, out var newSyntaxTree, out var newText))
            {
                return true;
            }

            var newWalker = new CodeFunctionsExtractor();
            newWalker.Visit(newSyntaxTree.Root);

            var oldWalker = new CodeFunctionsExtractor();
            oldWalker.Visit(oldSyntaxTree.Root);

            if (newWalker.CodeBlocks.Count == 0 && oldWalker.CodeBlocks.Count == 0)
            {
                // No directive code blocks, therefore no properties to analyze.
                return false;
            }

            var newProperties = ExtractCSharpProperties(newText, newWalker.CodeBlocks);
            var oldProperties = ExtractCSharpProperties(oldText, oldWalker.CodeBlocks);

            if (newProperties.Count != oldProperties.Count)
            {
                return true;
            }

            for (var i = 0; i < newProperties.Count; i++)
            {
                var newProperty = newProperties[i];
                var oldProperty = oldProperties[i];
                if (!CSharpPropertiesEqual(newProperty, oldProperty))
                {
                    return true;
                }
            }

            // Properties before and after document change are equivalent
            return false;
        }

        private static bool CSharpPropertiesEqual(PropertyDeclarationSyntax newProperty, PropertyDeclarationSyntax oldProperty)
        {
            if (!newProperty.Identifier.IsEquivalentTo(oldProperty.Identifier))
            {
                return false;
            }

            if (!newProperty.Type.IsEquivalentTo(oldProperty.Type))
            {
                return false;
            }

            if (newProperty.Modifiers.Count != oldProperty.Modifiers.Count)
            {
                return false;
            }

            for (var i = 0; i < newProperty.Modifiers.Count; i++)
            {
                var newModifier = newProperty.Modifiers[i];
                var oldModifier = oldProperty.Modifiers[i];
                if (newModifier.Text != oldModifier.Text)
                {
                    return false;
                }
            }

            if (newProperty.AttributeLists.Count != oldProperty.AttributeLists.Count)
            {
                return false;
            }

            for (var i = 0; i < newProperty.AttributeLists.Count; i++)
            {
                var newAttributes = newProperty.AttributeLists[i].Attributes;
                var oldAttributes = oldProperty.AttributeLists[i].Attributes;

                if (newAttributes.Count != oldAttributes.Count)
                {
                    return false;
                }

                for (var j = 0; j < newAttributes.Count; j++)
                {
                    var newAttribute = newAttributes[j];
                    var oldAttribute = oldAttributes[j];

                    if (!newAttribute.IsEquivalentTo(oldAttribute))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private static List<PropertyDeclarationSyntax> ExtractCSharpProperties(SourceText newText, IReadOnlyList<RazorDirectiveSyntax> codeBlocks)
        {
            var propertyList = new List<PropertyDeclarationSyntax>();
            for (var i = 0; i < codeBlocks.Count; i++)
            {
                var bodyRange = codeBlocks[i].Body.Span;
                var bodyTextSpan = TextSpan.FromBounds(bodyRange.Start, bodyRange.End);
                var subText = newText.GetSubText(bodyTextSpan);
                var parsedText = CSharpSyntaxTree.ParseText(subText);
                var root = parsedText.GetRoot();
                var childNodes = root.ChildNodes();
                var properties = childNodes.Where(node => node.Kind() == CodeAnalysis.CSharp.SyntaxKind.PropertyDeclaration).OfType<PropertyDeclarationSyntax>();
                propertyList.AddRange(properties);
            }

            return propertyList;
        }

        private bool TryGetSyntaxTreeAndSource(DocumentSnapshot document, out RazorSyntaxTree syntaxTree, out SourceText sourceText)
        {
            if (document.TryGetGeneratedOutput(out var codeDocument) &&
                document.TryGetText(out sourceText))
            {
                syntaxTree = codeDocument.GetSyntaxTree();
                return true;
            }
            else
            {
                if (!document.TryGetText(out sourceText))
                {
                    // Can't get the source text synchronously
                    syntaxTree = null;
                    return false;
                }

                syntaxTree = ParseSourceText(document.FilePath, sourceText);
                return true;
            }
        }

        private RazorSyntaxTree ParseSourceText(string filePath, SourceText sourceText)
        {
            var sourceDocument = new SourceTextSourceDocument(filePath, sourceText);
            var syntaxTree = RazorSyntaxTree.Parse(sourceDocument, ParserOptions);
            return syntaxTree;
        }

        private class SourceTextSourceDocument : RazorSourceDocument
        {
            private readonly string _filePath;
            private readonly SourceText _sourceText;
            private readonly RazorSourceTextLineCollection _lines;

            public SourceTextSourceDocument(string filePath, SourceText sourceText)
            {
                if (filePath is null)
                {
                    throw new ArgumentNullException(nameof(filePath));
                }

                if (sourceText is null)
                {
                    throw new ArgumentNullException(nameof(sourceText));
                }

                _filePath = filePath;
                _sourceText = sourceText;
                _lines = new RazorSourceTextLineCollection(_sourceText.Lines);
            }

            public override char this[int position] => _sourceText[position];

            public override Encoding Encoding => _sourceText.Encoding;

            public override string FilePath => _filePath;

            public override int Length => _sourceText.Length;

            public override RazorSourceLineCollection Lines => _lines;

            public override void CopyTo(int sourceIndex, char[] destination, int destinationIndex, int count) => _sourceText.CopyTo(sourceIndex, destination, destinationIndex, count);

            public override byte[] GetChecksum() => Array.Empty<byte>();

            private class RazorSourceTextLineCollection : RazorSourceLineCollection
            {
                private readonly TextLineCollection _textLines;

                public RazorSourceTextLineCollection(TextLineCollection textLines)
                {
                    if (textLines is null)
                    {
                        throw new ArgumentNullException(nameof(textLines));
                    }

                    _textLines = textLines;
                }

                public override int Count => _textLines.Count;

                public override int GetLineLength(int index) => _textLines[index].Span.Length;

                internal override SourceLocation GetLocation(int position)
                {
                    var line = _textLines.GetLinePosition(position);
                    var sourceLocation = new SourceLocation(position, line.Line, line.Character);
                    return sourceLocation;
                }
            }
        }

        private class CodeFunctionsExtractor : SyntaxWalker
        {
            private readonly List<RazorDirectiveSyntax> _codeBlocks;

            public CodeFunctionsExtractor()
            {
                _codeBlocks = new List<RazorDirectiveSyntax>();
            }

            public IReadOnlyList<RazorDirectiveSyntax> CodeBlocks => _codeBlocks;

            public override void VisitRazorDirective(RazorDirectiveSyntax node)
            {
                if (node.DirectiveDescriptor == FunctionsDirective.Directive ||
                    node.DirectiveDescriptor == ComponentCodeDirective.Directive)
                {
                    _codeBlocks.Add(node);
                }
            }
        }
    }
}