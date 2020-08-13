using System;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Server;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Microsoft.AspNetCore.Razor.LanguageServer.ProjectSystem;
using Microsoft.CodeAnalysis.Razor;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.LanguageServer.Common;
using Microsoft.CodeAnalysis.Text;
using Microsoft.AspNetCore.Razor.Language.Syntax;
using Microsoft.AspNetCore.Razor.Language.Legacy;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;

namespace Microsoft.AspNetCore.Razor.LanguageServer.Definition
{
    internal class RazorDefinitionEndpoint : IDefinitionHandler
    {
        private readonly ForegroundDispatcher _foregroundDispatcher;
        private readonly DocumentResolver _documentResolver;
        private readonly RazorComponentSearchEngine _componentSearchEngine;

        public DefinitionCapability _capability { get; private set; }

        public RazorDefinitionEndpoint(
            ForegroundDispatcher foregroundDispatcher,
            DocumentResolver documentResolver,
            RazorComponentSearchEngine componentSearchEngine)
        {
            _foregroundDispatcher = foregroundDispatcher ?? throw new ArgumentNullException(nameof(foregroundDispatcher));
            _documentResolver = documentResolver ?? throw new ArgumentNullException(nameof(documentResolver));
            _componentSearchEngine = componentSearchEngine ?? throw new ArgumentNullException(nameof(componentSearchEngine));
        }

        public TextDocumentRegistrationOptions GetRegistrationOptions()
        {
            return new TextDocumentRegistrationOptions
            {
                DocumentSelector = RazorDefaults.Selector
            };
        }

        public async Task<LocationOrLocationLinks> Handle(DefinitionParams request, CancellationToken cancellationToken)
        {
            if (request is null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            var documentSnapshot = await Task.Factory.StartNew(() =>
            {
                var path = request.TextDocument.Uri.GetAbsoluteOrUNCPath();
                _documentResolver.TryResolveDocument(path, out var documentSnapshot);
                return documentSnapshot;
            }, cancellationToken, TaskCreationOptions.None, _foregroundDispatcher.ForegroundScheduler).ConfigureAwait(false);

            if (documentSnapshot is null)
            {
                return null;
            }

            if (!FileKinds.IsComponent(documentSnapshot.FileKind))
            {
                return null;
            }

            var codeDocument = await documentSnapshot.GetGeneratedOutputAsync().ConfigureAwait(false);
            if (codeDocument.IsUnsupported())
            {
                return null;
            }

            var originTagHelperBinding = await GetOriginTagHelperBindingAsync(documentSnapshot, codeDocument, request.Position).ConfigureAwait(false);
            if (originTagHelperBinding is null)
            {
                return null;
            }

            var originTagDescriptor = originTagHelperBinding.Descriptors.SingleOrDefault();
            if (originTagDescriptor is null)
            {
                return null;
            }

            var originComponentDocumentSnapshot = await _componentSearchEngine.TryLocateComponentAsync(originTagDescriptor).ConfigureAwait(false);
            if (originComponentDocumentSnapshot is null)
            {
                return null;
            }

            var originComponentUri = new UriBuilder
            {
                Path = originComponentDocumentSnapshot.FilePath,
                Scheme = Uri.UriSchemeFile,
                Host = string.Empty,
            }.Uri;

            return new LocationOrLocationLinks(new[]
            {
                new LocationOrLocationLink(new Location
                {
                    Uri = originComponentUri,
                    Range = new Range(new Position(0, 0), new Position(0, 0)),
                }),
            });
        }

        public void SetCapability(DefinitionCapability capability)
        {
            _capability = capability;
        }

        private async Task<TagHelperBinding> GetOriginTagHelperBindingAsync(DocumentSnapshot documentSnapshot, RazorCodeDocument codeDocument, Position position)
        {
            var sourceText = await documentSnapshot.GetTextAsync().ConfigureAwait(false);
            var linePosition = new LinePosition((int)position.Line, (int)position.Character);
            var hostDocumentIndex = sourceText.Lines.GetPosition(linePosition);
            var location = new SourceLocation(hostDocumentIndex, (int)position.Line, (int)position.Character);

            var change = new SourceChange(location.AbsoluteIndex, length: 0, newText: string.Empty);
            var syntaxTree = codeDocument.GetSyntaxTree();
            if (syntaxTree?.Root is null)
            {
                return null;
            }

            var owner = syntaxTree.Root.LocateOwner(change);
            if (owner is null)
            {
                return null;
            }

            var node = owner.Ancestors().FirstOrDefault(n => n.Kind == SyntaxKind.MarkupTagHelperStartTag);
            if (node == null || !(node is MarkupTagHelperStartTagSyntax tagHelperStartTag))
            {
                return null;
            }
            
            if (!tagHelperStartTag.Name.Span.Contains(location.AbsoluteIndex))
            {
                return null;
            }

            if (!(tagHelperStartTag?.Parent is MarkupTagHelperElementSyntax tagHelperElement))
            {
                return null;
            }

            return tagHelperElement.TagHelperInfo.BindingResult;
        }
    }
}
