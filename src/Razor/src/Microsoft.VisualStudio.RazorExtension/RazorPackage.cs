// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.LanguageServerClient.Razor;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Task = System.Threading.Tasks.Task;

namespace Microsoft.VisualStudio.RazorExtension
{
    // We attach to the 52nd priority order because the traditional Web + XML editors have priority 51. We need to be loaded prior to them
    // since we want to have the option to own the experience for Razor files
    [ProvideEditorExtension(typeof(RazorEditorFactory), RazorLSPContentTypeDefinition.CSHTMLFileExtension, 52, NameResourceID = 101)]
    [ProvideEditorExtension(typeof(RazorEditorFactory), RazorLSPContentTypeDefinition.RazorFileExtension, 52, NameResourceID = 101)]
    [ProvideEditorFactory(typeof(RazorEditorFactory), 101, CommonPhysicalViewAttributes = (int)__VSPHYSICALVIEWATTRIBUTES.PVA_SupportsPreview)]
    [ProvideEditorLogicalView(typeof(RazorEditorFactory), VSConstants.LOGVIEWID.TextView_string)]
    [PackageRegistration(UseManagedResourcesOnly = true)]
    [AboutDialogInfo(PackageGuidString, "ASP.NET Core Razor Language Services", "#110", "#112", IconResourceID = "#400")]
    [Guid(PackageGuidString)]
    public sealed class RazorPackage : AsyncPackage
    {
        public const string PackageGuidString = "13b72f58-279e-49e0-a56d-296be02f0805";

        private RazorEditorFactory _editorFactory;

        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            _editorFactory = new RazorEditorFactory(this);
            RegisterEditorFactory(_editorFactory);

            var componentModel = (IComponentModel)AsyncPackage.GetGlobalService(typeof(SComponentModel));

            // This type listens to ITextDocumentFactoryService created and disposed events. We want to tie into these as soon as possible to ensure we don't miss
            // and relevant Razor documents.
            _ = componentModel.GetService<RazorLSPTextDocumentCreatedListener>();
        }
    }
}
