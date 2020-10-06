// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Legacy;
using Microsoft.AspNetCore.Razor.LanguageServer.Common;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;
using Microsoft.CodeAnalysis.Text;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Range = OmniSharp.Extensions.LanguageServer.Protocol.Models.Range;

namespace Microsoft.AspNetCore.Razor.LanguageServer.Formatting
{
    internal class FormattingContext : IDisposable
    {
        private Document _csharpWorkspaceDocument;

        public DocumentUri Uri { get; private set; }

        public DocumentSnapshot OriginalSnapshot { get; private set; }

        public RazorCodeDocument CodeDocument { get; private set; }

        public SourceText SourceText => CodeDocument.GetSourceText();

        public SourceText CSharpSourceText => CodeDocument.GetCSharpSourceText();

        public Document CSharpWorkspaceDocument
        {
            get
            {
                if (_csharpWorkspaceDocument == null)
                {
                    var adhocWorkspace = new AdhocWorkspace();
                    var csharpOptions = adhocWorkspace.Options
                        .WithChangedOption(CodeAnalysis.Formatting.FormattingOptions.TabSize, LanguageNames.CSharp, (int)Options.TabSize)
                        .WithChangedOption(CodeAnalysis.Formatting.FormattingOptions.IndentationSize, LanguageNames.CSharp, (int)Options.TabSize)
                        .WithChangedOption(CodeAnalysis.Formatting.FormattingOptions.UseTabs, LanguageNames.CSharp, !Options.InsertSpaces);
                    adhocWorkspace.TryApplyChanges(adhocWorkspace.CurrentSolution.WithOptions(csharpOptions));

                    var project = adhocWorkspace.AddProject("TestProject", LanguageNames.CSharp);
                    var csharpSourceText = CodeDocument.GetCSharpSourceText();
                    _csharpWorkspaceDocument = adhocWorkspace.AddDocument(project.Id, "TestDocument", csharpSourceText);
                }

                return _csharpWorkspaceDocument;
            }
        }

        public Workspace CSharpWorkspace => CSharpWorkspaceDocument.Project.Solution.Workspace;

        public FormattingOptions Options { get; private set; }

        public string NewLineString => Environment.NewLine;

        public bool IsFormatOnType { get; private set; }

        public Range Range { get; private set; }

        public IReadOnlyDictionary<int, IndentationContext> Indentations { get; private set; }

        /// <summary>
        /// Generates a string of indentation based on a specific indentation level. For instance, inside of a C# method represents 1 indentation level. A method within a class would have indentaiton level of 2 by default etc.
        /// </summary>
        /// <param name="indentationLevel">The indentation level to represent</param>
        /// <returns>A whitespace string representing the indentation level based on the configuration.</returns>
        public string GetIndentationLevelString(int indentationLevel)
        {
            var indentation = GetIndentationOffsetForLevel(indentationLevel);
            var indentationString = GetIndentationString(indentation);
            return indentationString;
        }

        /// <summary>
        /// Given a <paramref name="indentation"/> amount of characters, generate a string representing the configured indentation.
        /// </summary>
        /// <param name="indentation">An amount of characters to represent the indentation</param>
        /// <returns>A whitespace string representation indentation.</returns>
        public string GetIndentationString(int indentation)
        {
            if (Options.InsertSpaces)
            {
                return new string(' ', indentation);
            }
            else
            {
                var tabs = indentation / Options.TabSize;
                var tabPrefix = new string('\t', (int)tabs);

                var spaces = indentation % Options.TabSize;
                var spaceSuffix = new string(' ', (int)spaces);

                var combined = string.Concat(tabPrefix, spaceSuffix);
                return combined;
            }
        }

        /// <summary>
        /// Given an offset return the corresponding indent level.
        /// </summary>
        /// <param name="offset">A value represents the number of spaces/tabs at the start of a line.</param>
        /// <returns>The corresponding indent level.</returns>
        public int GetIndentationLevelForOffset(int offset)
        {
            if (Options.InsertSpaces)
            {
                offset /= (int)Options.TabSize;
            }

            return offset;
        }

        /// <summary>
        /// Given a level, returns the corresponding offset.
        /// </summary>
        /// <param name="level">A value representing the indentation level.</param>
        /// <returns></returns>
        public int GetIndentationOffsetForLevel(int level)
        {
            return level * (int)Options.TabSize;
        }

        public bool TryGetIndentationLevel(int position, out int indentationLevel)
        {
            var syntaxTree = CodeDocument.GetSyntaxTree();
            var formattingSpans = syntaxTree.GetFormattingSpans();
            if (TryGetFormattingSpan(position, formattingSpans, out var span))
            {
                indentationLevel = span.IndentationLevel;
                return true;
            }

            indentationLevel = 0;
            return false;
        }

        public void Dispose()
        {
            if (_csharpWorkspaceDocument != null)
            {
                CSharpWorkspace.Dispose();
                _csharpWorkspaceDocument = null;
            }
        }

        public async Task<FormattingContext> WithTextAsync(SourceText changedText)
        {
            if (changedText is null)
            {
                throw new ArgumentNullException(nameof(changedText));
            }

            var engine = OriginalSnapshot.Project.GetProjectEngine();
            var importSources = new List<RazorSourceDocument>();

            if (OriginalSnapshot is DefaultDocumentSnapshot documentSnapshot)
            {
                var imports = documentSnapshot.State.GetImports((DefaultProjectSnapshot)OriginalSnapshot.Project);
                foreach (var import in imports)
                {
                    var sourceText = await import.GetTextAsync();
                    var source = sourceText.GetRazorSourceDocument(import.FilePath, import.TargetPath);
                    importSources.Add(source);
                }
            }

            var changedSourceDocument = changedText.GetRazorSourceDocument(OriginalSnapshot.FilePath, OriginalSnapshot.TargetPath);

            var codeDocument = engine.ProcessDesignTime(changedSourceDocument, OriginalSnapshot.FileKind, importSources, OriginalSnapshot.Project.TagHelpers);

            var newContext = Create(Uri, OriginalSnapshot, codeDocument, Options, Range, IsFormatOnType);
            return newContext;
        }

        public static FormattingContext Create(
            DocumentUri uri,
            DocumentSnapshot originalSnapshot,
            RazorCodeDocument codeDocument,
            FormattingOptions options,
            Range range = null,
            bool isFormatOnType = false)
        {
            if (uri is null)
            {
                throw new ArgumentNullException(nameof(uri));
            }

            if (originalSnapshot is null)
            {
                throw new ArgumentNullException(nameof(originalSnapshot));
            }

            if (codeDocument is null)
            {
                throw new ArgumentNullException(nameof(codeDocument));
            }

            if (options is null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            var text = codeDocument.GetSourceText();
            range ??= TextSpan.FromBounds(0, text.Length).AsRange(text);

            var result = new FormattingContext()
            {
                Uri = uri,
                OriginalSnapshot = originalSnapshot,
                CodeDocument = codeDocument,
                Range = range,
                Options = options,
                IsFormatOnType = isFormatOnType
            };

            var sourceText = codeDocument.GetSourceText();
            var syntaxTree = codeDocument.GetSyntaxTree();
            var formattingSpans = syntaxTree.GetFormattingSpans();
            var indentations = new Dictionary<int, IndentationContext>();

            var previousIndentationLevel = 0;
            for (var i = 0; i < sourceText.Lines.Count; i++)
            {
                // Get first non-whitespace character position
                var nonWsPos = sourceText.Lines[i].GetFirstNonWhitespacePosition();
                var existingIndentation = (nonWsPos ?? sourceText.Lines[i].End) - sourceText.Lines[i].Start;
                var emptyOrWhitespaceLine = false;
                if (nonWsPos == null)
                {
                    emptyOrWhitespaceLine = true;
                    nonWsPos = sourceText.Lines[i].Start;
                }

                // position now contains the first non-whitespace character or 0. Get the corresponding FormattingSpan.
                if (TryGetFormattingSpan(nonWsPos.Value, formattingSpans, out var span))
                {
                    indentations[i] = new IndentationContext
                    {
                        Line = i,
                        RazorIndentationLevel = span.RazorIndentationLevel,
                        HtmlIndentationLevel = span.HtmlIndentationLevel,
                        RelativeIndentationLevel = span.IndentationLevel - previousIndentationLevel,
                        ExistingIndentation = existingIndentation,
                        FirstSpan = span,
                        EmptyOrWhitespaceLine = emptyOrWhitespaceLine,
                    };
                    previousIndentationLevel = span.IndentationLevel;
                }
                else
                {
                    // Couldn't find a corresponding FormattingSpan. Happens if it is a 0 length line.
                    // Let's create a 0 length span to represent this and default it to HTML.
                    var placeholderSpan = new FormattingSpan(
                        new Language.Syntax.TextSpan(nonWsPos.Value, 0),
                        new Language.Syntax.TextSpan(nonWsPos.Value, 0),
                        FormattingSpanKind.Markup,
                        FormattingBlockKind.Markup,
                        razorIndentationLevel: 0,
                        htmlIndentationLevel: 0,
                        isInClassBody: false);

                    indentations[i] = new IndentationContext
                    {
                        Line = i,
                        RazorIndentationLevel = 0,
                        HtmlIndentationLevel = 0,
                        RelativeIndentationLevel = previousIndentationLevel,
                        ExistingIndentation = existingIndentation,
                        FirstSpan = placeholderSpan,
                        EmptyOrWhitespaceLine = emptyOrWhitespaceLine,
                    };
                }
            }

            result.Indentations = indentations;

            return result;
        }

        private static bool TryGetFormattingSpan(int absoluteIndex, IReadOnlyList<FormattingSpan> formattingspans, out FormattingSpan result)
        {
            result = null;
            for (var i = 0; i < formattingspans.Count; i++)
            {
                var formattingspan = formattingspans[i];
                var span = formattingspan.Span;

                if (span.Start <= absoluteIndex)
                {
                    if (span.End >= absoluteIndex)
                    {
                        if (span.End == absoluteIndex && span.Length > 0)
                        {
                            // We're at an edge.
                            // Non-marker spans (spans.length == 0) do not own the edges after it
                            continue;
                        }

                        result = formattingspan;
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
