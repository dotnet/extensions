// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Components;
using Microsoft.AspNetCore.Razor.Language.Legacy;
using Microsoft.AspNetCore.Razor.Language.Syntax;
using Microsoft.AspNetCore.Razor.LanguageServer.CodeActions.Models;
using Microsoft.AspNetCore.Razor.LanguageServer.Common;
using Microsoft.VisualStudio.Editor.Razor;
using Newtonsoft.Json.Linq;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace Microsoft.AspNetCore.Razor.LanguageServer.CodeActions
{
    internal class ComponentAccessibilityCodeActionProvider : RazorCodeActionProvider
    {
        private static readonly string CreateComponentFromTagTitle = "Create component from tag";
        private static readonly Task<IReadOnlyList<RazorCodeAction>> EmptyResult = Task.FromResult<IReadOnlyList<RazorCodeAction>>(null);

        private readonly TagHelperFactsService _tagHelperFactsService;
        private readonly FilePathNormalizer _filePathNormalizer;

        public ComponentAccessibilityCodeActionProvider(
            TagHelperFactsService tagHelperFactsService,
            FilePathNormalizer filePathNormalizer)
        {
            _tagHelperFactsService = tagHelperFactsService ?? throw new ArgumentNullException(nameof(tagHelperFactsService));
            _filePathNormalizer = filePathNormalizer ?? throw new ArgumentNullException(nameof(filePathNormalizer));
        }

        public override Task<IReadOnlyList<RazorCodeAction>> ProvideAsync(RazorCodeActionContext context, CancellationToken cancellationToken)
        {
            var codeActions = new List<RazorCodeAction>();

            // Locate cursor
            var change = new SourceChange(context.Location.AbsoluteIndex, length: 0, newText: string.Empty);
            var node = context.CodeDocument.GetSyntaxTree().Root.LocateOwner(change);
            if (node is null)
            {
                return EmptyResult;
            }

            // Find start tag
            var startTag = (MarkupStartTagSyntax)node.Ancestors().FirstOrDefault(n => n is MarkupStartTagSyntax);
            if (startTag is null)
            {
                return EmptyResult;
            }

            // Ignore if start tag has dots, as we only handle short tags
            if (startTag.Name.Content.Contains("."))
            {
                return EmptyResult;
            }

            if (IsTagUnknown(startTag, context))
            {
                AddComponentAccessFromTag(context, startTag, codeActions);
                AddCreateComponentFromTag(context, startTag, codeActions);
            }

            return Task.FromResult(codeActions as IReadOnlyList<RazorCodeAction>);
        }

        private void AddCreateComponentFromTag(RazorCodeActionContext context, MarkupStartTagSyntax startTag, List<RazorCodeAction> container)
        {
            if (context is null)
            {
                return;
            }

            if (!context.SupportsFileCreation)
            {
                return;
            }

            var path = context.Request.TextDocument.Uri.GetAbsoluteOrUNCPath();
            path = _filePathNormalizer.Normalize(path);
            var newComponentPath = Path.Combine(Path.GetDirectoryName(path), $"{startTag.Name.Content}.razor");
            if (File.Exists(newComponentPath))
            {
                return;
            }

            var actionParams = new CreateComponentCodeActionParams
            {
                Uri = context.Request.TextDocument.Uri,
                Path = newComponentPath,
            };

            var resolutionParams = new RazorCodeActionResolutionParams
            {
                Action = LanguageServerConstants.CodeActions.CreateComponentFromTag,
                Language = LanguageServerConstants.CodeActions.Languages.Razor,
                Data = actionParams,
            };

            container.Add(new RazorCodeAction()
            {
                Title = CreateComponentFromTagTitle,
                Data = JToken.FromObject(resolutionParams)
            });
        }

        private void AddComponentAccessFromTag(RazorCodeActionContext context, MarkupStartTagSyntax startTag, List<RazorCodeAction> container)
        {
            var matching = FindMatchingTagHelpers(context, startTag);

            // For all the matches, add options for add @using and fully qualify
            foreach (var tagHelperPair in matching.Values)
            {
                if (tagHelperPair.FullyQualified is null)
                {
                    continue;
                }

                var fullyQualifiedName = tagHelperPair.Short.Name;

                // Insert @using
                var addUsingCodeAction = AddUsingsCodeActionProviderFactory.CreateAddUsingCodeAction(
                    fullyQualifiedName,
                    context.Request.TextDocument.Uri);
                if (addUsingCodeAction != null)
                {
                    container.Add(addUsingCodeAction);
                }

                // Fully qualify
                container.Add(new RazorCodeAction()
                {
                    Title = $"{fullyQualifiedName}",
                    Edit = CreateRenameTagEdit(context, startTag, fullyQualifiedName),
                });
            }
        }

        private Dictionary<string, TagHelperPair> FindMatchingTagHelpers(RazorCodeActionContext context, MarkupStartTagSyntax startTag)
        {
            // Get all data necessary for matching
            var tagName = startTag.Name.Content;
            string parentTagName = null;
            if (startTag.Parent?.Parent is MarkupElementSyntax parentElement)
            {
                parentTagName = parentElement.StartTag.Name.Content;
            }
            else if (startTag.Parent?.Parent is MarkupTagHelperElementSyntax parentTagHelperElement)
            {
                parentTagName = parentTagHelperElement.StartTag.Name.Content;
            }

            var attributes = _tagHelperFactsService.StringifyAttributes(startTag.Attributes).ToList();

            // Find all matching tag helpers
            var matching = new Dictionary<string, TagHelperPair>();
            foreach (var tagHelper in context.DocumentSnapshot.Project.TagHelpers)
            {
                if (tagHelper.TagMatchingRules.All(rule => TagHelperMatchingConventions.SatisfiesRule(tagName, parentTagName, attributes, rule)))
                {
                    matching.Add(tagHelper.Name, new TagHelperPair { Short = tagHelper });
                }
            }

            // Iterate and find the fully qualified version
            foreach (var tagHelper in context.DocumentSnapshot.Project.TagHelpers)
            {
                if (matching.TryGetValue(tagHelper.Name, out var tagHelperPair))
                {
                    if (tagHelperPair != null && tagHelper != tagHelperPair.Short)
                    {
                        tagHelperPair.FullyQualified = tagHelper;
                    }
                }
            }

            return matching;
        }

        private static WorkspaceEdit CreateRenameTagEdit(RazorCodeActionContext context, MarkupStartTagSyntax startTag, string newTagName)
        {
            var textEdits = new List<TextEdit>();
            var codeDocumentIdentifier = new VersionedTextDocumentIdentifier() { Uri = context.Request.TextDocument.Uri };

            var startTagTextEdit = new TextEdit
            {
                Range = startTag.Name.GetRange(context.CodeDocument.Source),
                NewText = newTagName,
            };

            textEdits.Add(startTagTextEdit);

            var endTag = (startTag.Parent as MarkupElementSyntax).EndTag;
            if (endTag != null)
            {
                var endTagTextEdit = new TextEdit
                {
                    Range = endTag.Name.GetRange(context.CodeDocument.Source),
                    NewText = newTagName,
                };

                textEdits.Add(endTagTextEdit);
            }

            return new WorkspaceEdit
            {
                DocumentChanges = new List<WorkspaceEditDocumentChange>()
                {
                    new WorkspaceEditDocumentChange(
                        new TextDocumentEdit()
                        {
                            TextDocument = codeDocumentIdentifier,
                            Edits = textEdits,
                        }
                    )
                }
            };
        }

        private static bool IsTagUnknown(MarkupStartTagSyntax startTag, RazorCodeActionContext context)
        {
            foreach (var diagnostic in context.CodeDocument.GetCSharpDocument().Diagnostics)
            {
                // Check that the diagnostic is to do with our start tag
                if (!(diagnostic.Span.AbsoluteIndex > startTag.Span.End
                    || startTag.Span.Start > diagnostic.Span.AbsoluteIndex + diagnostic.Span.Length))
                {
                    // Component is not recognized in environment
                    if (diagnostic.Id == ComponentDiagnosticFactory.UnexpectedMarkupElement.Id)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        private class TagHelperPair
        {
            public TagHelperDescriptor Short = null;
            public TagHelperDescriptor FullyQualified = null;
        }
    }
}
