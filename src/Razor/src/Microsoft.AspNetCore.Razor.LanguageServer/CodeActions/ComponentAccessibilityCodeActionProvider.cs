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
using Microsoft.AspNetCore.Razor.LanguageServer.Common;
using Microsoft.CodeAnalysis.Razor;
using Microsoft.VisualStudio.Editor.Razor;
using Newtonsoft.Json.Linq;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace Microsoft.AspNetCore.Razor.LanguageServer.CodeActions
{
    internal class ComponentAccessibilityCodeActionProvider : RazorCodeActionProvider
    {
        private readonly TagHelperFactsService _tagHelperFactsService;
        private readonly FilePathNormalizer _filePathNormalizer;

        public ComponentAccessibilityCodeActionProvider(
            TagHelperFactsService tagHelperFactsService,
            FilePathNormalizer filePathNormalizer)
        {
            _tagHelperFactsService = tagHelperFactsService ?? throw new ArgumentNullException(nameof(tagHelperFactsService));
            _filePathNormalizer = filePathNormalizer ?? throw new ArgumentNullException(nameof(filePathNormalizer));
        }

        public override Task<CommandOrCodeActionContainer> ProvideAsync(RazorCodeActionContext context, CancellationToken cancellationToken)
        {
            var commandOrCodeActions = new List<CommandOrCodeAction>();

            // Locate cursor
            var change = new SourceChange(context.Location.AbsoluteIndex, length: 0, newText: string.Empty);
            var node = context.CodeDocument.GetSyntaxTree().Root.LocateOwner(change);
            if (node is null)
            {
                return Task.FromResult<CommandOrCodeActionContainer>(null);
            }

            // Find start tag
            var startTag = (MarkupStartTagSyntax)node.Ancestors().FirstOrDefault(n => n is MarkupStartTagSyntax);
            if (startTag is null)
            {
                return Task.FromResult<CommandOrCodeActionContainer>(null);
            }

            // Ignore if start tag has dots, as we only handle short tags
            if (startTag.Name.Content.Contains("."))
            {
                return Task.FromResult<CommandOrCodeActionContainer>(null);
            }

            if (IsTagUnknown(startTag, context))
            {
                AddComponentAccessFromTag(context, startTag, commandOrCodeActions);
                AddCreateComponentFromTag(context, startTag, commandOrCodeActions);
            }

            return Task.FromResult(new CommandOrCodeActionContainer(commandOrCodeActions));
        }

        private void AddCreateComponentFromTag(RazorCodeActionContext context, MarkupStartTagSyntax startTag, List<CommandOrCodeAction> container)
        {
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
            var data = JObject.FromObject(actionParams);

            var resolutionParams = new RazorCodeActionResolutionParams
            {
                Action = LanguageServerConstants.CodeActions.CreateComponentFromTag,
                Data = data,
            };
            var serializedParams = JToken.FromObject(resolutionParams);
            var arguments = new JArray(serializedParams);

            container.Add(new CommandOrCodeAction(new Command
            {
                Title = "Create component from tag",
                Name = LanguageServerConstants.RazorCodeActionRunnerCommand,
                Arguments = arguments,
            }));
        }

        private void AddComponentAccessFromTag(RazorCodeActionContext context, MarkupStartTagSyntax startTag, List<CommandOrCodeAction> container)
        {
            var matching = FindMatchingTagHelpers(context, startTag);

            // For all the matches, add options for add @using and fully qualify
            foreach (var tagHelperPair in matching.Values)
            {
                if (tagHelperPair.FullyQualified is null)
                {
                    continue;
                }

                var fullyQualifiedComponentName = tagHelperPair.Short.Name;  // We assume .Name is the fully qualified component name
                DefaultRazorTagHelperBinderPhase.ComponentDirectiveVisitor.TrySplitNamespaceAndType(fullyQualifiedComponentName, out var namespaceSpan, out var _);
                var namespaceName = tagHelperPair.Short.Name.Substring(namespaceSpan.Start, namespaceSpan.Length);
                var actionParams = new AddUsingsCodeActionParams
                {
                    Uri = context.Request.TextDocument.Uri,
                    Namespace = namespaceName,
                };
                var data = JObject.FromObject(actionParams);

                var resolutionParams = new RazorCodeActionResolutionParams
                {
                    Action = LanguageServerConstants.CodeActions.AddUsing,
                    Data = data,
                };
                var serializedParams = JToken.FromObject(resolutionParams);
                var arguments = new JArray(serializedParams);

                // Insert @using
                container.Add(new CommandOrCodeAction(new Command
                {
                    Title = $"@using {namespaceName}",
                    Name = LanguageServerConstants.RazorCodeActionRunnerCommand,
                    Arguments = arguments,
                }));

                // Fully qualify
                container.Add(new CommandOrCodeAction(new CodeAction
                {
                    Title = $"{tagHelperPair.Short.Name}",
                    Edit = CreateRenameTagEdit(context, startTag, tagHelperPair.Short.Name),
                }));
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
            var attributes = _tagHelperFactsService.StringifyAttributes(startTag.Attributes);

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
            var changes = new List<TextEdit>
            {
                new TextEdit
                {
                    Range = startTag.Name.GetRange(context.CodeDocument.Source),
                    NewText = newTagName,
                },
            };
            var endTag = (startTag.Parent as MarkupElementSyntax).EndTag;
            if (endTag != null)
            {
                changes.Add(new TextEdit
                {
                    Range = endTag.Name.GetRange(context.CodeDocument.Source),
                    NewText = newTagName,
                });
            }
            return new WorkspaceEdit
            {
                Changes = new Dictionary<Uri, IEnumerable<TextEdit>> {
                    [context.Request.TextDocument.Uri] = changes,
                }
            };
        }

        private sealed class TagHelperPair
        {
            public TagHelperDescriptor Short = null;
            public TagHelperDescriptor FullyQualified = null;
        }

        private bool IsTagUnknown(MarkupStartTagSyntax startTag, RazorCodeActionContext context)
        {
            foreach (var diagnostic in context.CodeDocument.GetCSharpDocument().Diagnostics)
            {
                // Check that the diagnostic is to do with our start tag
                if (!(diagnostic.Span.AbsoluteIndex > startTag.Span.End || startTag.Span.Start > diagnostic.Span.AbsoluteIndex + diagnostic.Span.Length))
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
    }
}
