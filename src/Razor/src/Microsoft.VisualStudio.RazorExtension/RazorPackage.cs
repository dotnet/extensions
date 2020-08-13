// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Task = System.Threading.Tasks.Task;

namespace Microsoft.VisualStudio.RazorExtension
{
    [PackageRegistration(UseManagedResourcesOnly = true)]
    [AboutDialogInfo(PackageGuidString, "ASP.NET Core Razor Language Services", "#110", "#112", IconResourceID = "#400")]
    [Guid(PackageGuidString)]
    public sealed class RazorPackage : AsyncPackage
    {
        public const string PackageGuidString = "13b72f58-279e-49e0-a56d-296be02f0805";
        public const string CSharpPackageGuidString = "13c3bbb4-f18f-4111-9f54-a0fb010d9194";

        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            cancellationToken.ThrowIfCancellationRequested();

            // Explicitly trigger the load of the CSharp package. This ensures that UI-bound services are appropriately prefetched. Ideally, we shouldn't need this but until Roslyn fixes it on their side, we have to live with it.
            var shellService = (IVsShell7)AsyncPackage.GetGlobalService(typeof(SVsShell));
            await shellService.LoadPackageAsync(new Guid(CSharpPackageGuidString));
        }
    }
}
