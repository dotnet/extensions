// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.VisualStudio.LanguageServerClient.Razor;
using Microsoft.VisualStudio.Shell;
using Task = System.Threading.Tasks.Task;

namespace Microsoft.VisualStudio.RazorExtension
{
    // We attach to the 51st priority order because the traditional Web + XML editors have priority 50. We need to be loaded prior to them
    // since we want to have the option to own the experience for Razor files
    [ProvideEditorExtension(typeof(RazorEditorFactory), RazorLSPContentTypeDefinition.CSHTMLFileExtension, 52, NameResourceID = 101)]
    [ProvideEditorExtension(typeof(RazorEditorFactory), RazorLSPContentTypeDefinition.RazorFileExtension, 52, NameResourceID = 101)]
    [ProvideEditorFactory(typeof(RazorEditorFactory), 101)]
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
        }
    }
}
