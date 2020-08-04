// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Intermediate;
using Microsoft.AspNetCore.Razor.LanguageServer.Common;
using Microsoft.AspNetCore.Razor.LanguageServer.ProjectSystem;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Razor;
using Newtonsoft.Json.Linq;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using CSharpSyntaxFactory = Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using CSharpSyntaxKind = Microsoft.CodeAnalysis.CSharp.SyntaxKind;

namespace Microsoft.AspNetCore.Razor.LanguageServer.CodeActions
{
    internal class ExtractToCodeBehindCodeActionResolver : RazorCodeActionResolver
    {
        private readonly ForegroundDispatcher _foregroundDispatcher;
        private readonly DocumentResolver _documentResolver;
        private readonly FilePathNormalizer _filePathNormalizer;

        private static readonly Range StartOfDocumentRange = new Range(new Position(0, 0), new Position(0, 0));

        public ExtractToCodeBehindCodeActionResolver(
            ForegroundDispatcher foregroundDispatcher,
            DocumentResolver documentResolver,
            FilePathNormalizer filePathNormalizer)
        {
            _foregroundDispatcher = foregroundDispatcher ?? throw new ArgumentNullException(nameof(foregroundDispatcher));
            _documentResolver = documentResolver ?? throw new ArgumentNullException(nameof(documentResolver));
            _filePathNormalizer = filePathNormalizer ?? throw new ArgumentNullException(nameof(filePathNormalizer));
        }

        public override string Action => LanguageServerConstants.CodeActions.ExtractToCodeBehindAction;

        public override async Task<WorkspaceEdit> ResolveAsync(JObject data, CancellationToken cancellationToken)
        {
            if (data is null)
            {
                return null;
            }

            var actionParams = data.ToObject<ExtractToCodeBehindCodeActionParams>();
            var path = _filePathNormalizer.Normalize(actionParams.Uri.GetAbsoluteOrUNCPath());

            var document = await Task.Factory.StartNew(() =>
            {
                _documentResolver.TryResolveDocument(path, out var documentSnapshot);
                return documentSnapshot;
            }, cancellationToken, TaskCreationOptions.None, _foregroundDispatcher.ForegroundScheduler).ConfigureAwait(false);
            if (document is null)
            {
                return null;
            }

            var codeDocument = await document.GetGeneratedOutputAsync().ConfigureAwait(false);
            if (codeDocument.IsUnsupported())
            {
                return null;
            }

            if (!FileKinds.IsComponent(codeDocument.GetFileKind()))
            {
                return null;
            }

            var codeBehindPath = GenerateCodeBehindPath(path);
            var codeBehindUri = new UriBuilder
            {
                Scheme = Uri.UriSchemeFile,
                Path = codeBehindPath,
                Host = string.Empty,
            }.Uri;

            var text = await document.GetTextAsync().ConfigureAwait(false);
            if (text is null)
            {
                return null;
            }

            var className = Path.GetFileNameWithoutExtension(path);
            var codeBlockContent = text.GetSubTextString(new CodeAnalysis.Text.TextSpan(actionParams.ExtractStart, actionParams.ExtractEnd - actionParams.ExtractStart));
            var codeBehindContent = GenerateCodeBehindClass(className, codeBlockContent, codeDocument);

            var start = codeDocument.Source.Lines.GetLocation(actionParams.RemoveStart);
            var end = codeDocument.Source.Lines.GetLocation(actionParams.RemoveEnd);
            var removeRange = new Range(
                new Position(start.LineIndex, start.CharacterIndex),
                new Position(end.LineIndex, end.CharacterIndex));

            var codeDocumentIdentifier = new VersionedTextDocumentIdentifier { Uri = actionParams.Uri };
            var codeBehindDocumentIdentifier = new VersionedTextDocumentIdentifier { Uri = codeBehindUri };

            var documentChanges = new List<WorkspaceEditDocumentChange>
            {
                new WorkspaceEditDocumentChange(new CreateFile { Uri = codeBehindUri.ToString() }),
                new WorkspaceEditDocumentChange(new TextDocumentEdit
                {
                    TextDocument = codeDocumentIdentifier,
                    Edits = new[]
                    {
                        new TextEdit
                        {
                            NewText = string.Empty,
                            Range = removeRange,
                        }
                    },
                }),
                new WorkspaceEditDocumentChange(new TextDocumentEdit
                {
                    TextDocument = codeBehindDocumentIdentifier,
                    Edits  = new[]
                    {
                        new TextEdit
                        {
                            NewText = codeBehindContent,
                            Range = StartOfDocumentRange,
                        }
                    },
                })
            };

            return new WorkspaceEdit
            {
                DocumentChanges = documentChanges,
            };
        }

        /// <summary>
        /// Generate a file path with adjacent to our input path that has the
        /// correct codebehind extension, using numbers to differentiate from
        /// any collisions.
        /// </summary>
        /// <param name="path">The origin file path.</param>
        /// <returns>A non-existent file path with the same base name and a codebehind extension.</returns>
        private string GenerateCodeBehindPath(string path)
        {
            var n = 0;
            string codeBehindPath;
            do
            {
                var identifier = n > 0 ? n.ToString(CultureInfo.InvariantCulture) : string.Empty;  // Make it look nice
                codeBehindPath = Path.Combine(
                    Path.GetDirectoryName(path),
                    $"{Path.GetFileNameWithoutExtension(path)}{identifier}{Path.GetExtension(path)}.cs");
                n++;
            } while (File.Exists(codeBehindPath));
            return codeBehindPath;
        }

        /// <summary>
        /// Determine all explicit and implicit using statements in the code
        /// document using the intermediate node.
        /// </summary>
        /// <param name="razorCodeDocument">The code document to analyze.</param>
        /// <returns>An enumerable of the qualified namespaces.</returns>
        private static IEnumerable<string> FindUsings(RazorCodeDocument razorCodeDocument)
        {
            return razorCodeDocument
                .GetDocumentIntermediateNode()
                .FindDescendantNodes<UsingDirectiveIntermediateNode>()
                .Select(n => n.Content);
        }

        /// <summary>
        /// Generate a complete C# compilation unit containing a partial class
        /// with the given name, body contents, and the namespace and all
        /// usings from the existing code document.
        /// </summary>
        /// <param name="className">Name of the resultant partial class.</param>
        /// <param name="contents">Class body contents.</param>
        /// <param name="razorCodeDocument">Existing code document we're extracting from.</param>
        /// <returns></returns>
        private static string GenerateCodeBehindClass(string className, string contents, RazorCodeDocument razorCodeDocument)
        {
            var namespaceNode = (NamespaceDeclarationIntermediateNode)razorCodeDocument
                .GetDocumentIntermediateNode()
                .FindDescendantNodes<IntermediateNode>()
                .FirstOrDefault(n => n is NamespaceDeclarationIntermediateNode);

            var mock = (ClassDeclarationSyntax)CSharpSyntaxFactory.ParseMemberDeclaration($"class Class {contents}");
            var @class = CSharpSyntaxFactory
                .ClassDeclaration(className)
                .AddModifiers(CSharpSyntaxFactory.Token(CSharpSyntaxKind.PublicKeyword), CSharpSyntaxFactory.Token(CSharpSyntaxKind.PartialKeyword))
                .AddMembers(mock.Members.ToArray());

            var @namespace = CSharpSyntaxFactory
                .NamespaceDeclaration(CSharpSyntaxFactory.ParseName(namespaceNode.Content))
                .AddMembers(@class);

            var usings = FindUsings(razorCodeDocument)
                .Select(u => CSharpSyntaxFactory.UsingDirective(CSharpSyntaxFactory.ParseName(u)))
                .ToArray();
            var compilationUnit = CSharpSyntaxFactory
                .CompilationUnit()
                .AddUsings(usings)
                .AddMembers(@namespace);

            return compilationUnit.NormalizeWhitespace().ToFullString();
        }
    }
}
