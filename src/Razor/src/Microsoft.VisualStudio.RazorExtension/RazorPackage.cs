// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.VisualStudio.Shell;
using Task = System.Threading.Tasks.Task;

namespace Microsoft.VisualStudio.RazorExtension
{
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [AboutDialogInfo(PackageGuidString, "ASP.NET Core Razor Language Services", "#110", "#112", IconResourceID = "#400")]
    [Guid(PackageGuidString)]
    public sealed class RazorPackage : AsyncPackage
    {
        public const string PackageGuidString = "13b72f58-279e-49e0-a56d-296be02f0805";

        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
        }
    }
}
