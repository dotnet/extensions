// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.RazorExtension
{
    [PackageRegistration(UseManagedResourcesOnly = true)]
    [Guid(PackageGuidString)]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [ProvideToolWindow(typeof(Microsoft.VisualStudio.RazorExtension.RazorInfo.RazorInfoToolWindow))]
    [ProvideToolWindow(typeof(Microsoft.VisualStudio.RazorExtension.DocumentInfo.RazorDocumentInfoWindow))]
    public sealed class RazorDeveloperToolsPackage : Package
    {
        public const string PackageGuidString = "13b72f58-279e-49e0-a56d-296be02f0805";

        protected override void Initialize()
        {
            base.Initialize();

            ThreadHelper.ThrowIfNotOnUIThread();

            // Force the Razor package to load.
            var shell = GetService(typeof(SVsShell)) as IVsShell;
            if (shell == null)
            {
                return;
            }

            IVsPackage package = null;
            var packageGuid = new Guid("13b72f58-279e-49e0-a56d-296be02f0805");
            shell.LoadPackage(ref packageGuid, out package);

            RazorInfo.RazorInfoToolWindowCommand.Initialize(this);
            DocumentInfo.RazorDocumentInfoWindowCommand.Initialize(this);
        }
    }
}
