// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Razor.Workspaces;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Internal.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.LanguageServerClient.Razor
{
    [Shared]
    [Export(typeof(LSPEditorFeatureDetector))]
    internal class DefaultLSPEditorFeatureDetector : LSPEditorFeatureDetector
    {
        private const string DotNetCoreCSharpCapability = "CSharp&CPS";
        private const string RazorLSPEditorFeatureFlag = "Razor.LSP.Editor";

        private static readonly Guid LiveShareHostUIContextGuid = Guid.Parse("62de1aa5-70b0-4934-9324-680896466fe1");
        private static readonly Guid LiveShareGuestUIContextGuid = Guid.Parse("fd93f3eb-60da-49cd-af15-acda729e357e");

        private readonly ProjectHierarchyInspector _projectHierarchyInspector;
        private readonly Lazy<IVsUIShellOpenDocument> _vsUIShellOpenDocument;
        private readonly IVsFeatureFlags _featureFlags;

        private readonly MemoryCache _projectSupportsRazorLSPCache;

        private bool? _featureFlagEnabled;
        private bool? _environmentFeatureEnabled;
        private bool? _isVSServer;

        [ImportingConstructor]
        public DefaultLSPEditorFeatureDetector(ProjectHierarchyInspector projectHierarchyInspector)
        {
            if (projectHierarchyInspector is null)
            {
                throw new ArgumentNullException(nameof(projectHierarchyInspector));
            }

            _projectHierarchyInspector = projectHierarchyInspector;
            _featureFlags = (IVsFeatureFlags)AsyncPackage.GetGlobalService(typeof(SVsFeatureFlags));
            _vsUIShellOpenDocument = new Lazy<IVsUIShellOpenDocument>(() =>
            {
                var shellOpenDocument = (IVsUIShellOpenDocument)ServiceProvider.GlobalProvider.GetService(typeof(SVsUIShellOpenDocument));
                Assumes.Present(shellOpenDocument);

                return shellOpenDocument;
            });

            // 40 files * max 260 chars/file path => ~10 KB; likely lower as most paths aren't 260 chars
            _projectSupportsRazorLSPCache = new MemoryCache(
                    new MemoryCacheOptions()
                    {
                        SizeLimit = 40
                    }
                );
        }

        // Test constructor
        internal DefaultLSPEditorFeatureDetector()
        {
        }

        public override bool IsLSPEditorAvailable(string documentMoniker, object hierarchy)
        {
            if (documentMoniker == null)
            {
                return false;
            }

            var ivsHierarchy = hierarchy as IVsHierarchy;

            if (!IsLSPEditorFeatureEnabled())
            {
                // Razor LSP feature is not enabled
                return false;
            }

            if (!ProjectSupportsRazorLSPEditor(documentMoniker, ivsHierarchy))
            {
                // Current project hierarchy doesn't support the LSP Razor editor
                return false;
            }

            return true;
        }

        public override bool IsRemoteClient()
        {
            if (IsVSRemoteClient() || IsLiveShareGuest())
            {
                return true;
            }

            return false;
        }

        public override bool IsLSPEditorFeatureEnabled()
        {
            if (EnvironmentFeatureEnabled())
            {
                return true;
            }

            if (IsFeatureFlagEnabledCached())
            {
                return true;
            }

            if (IsVSServer())
            {
                // We default to "on" in Visual Studio server cloud environments
                return true;
            }

            if (IsVSRemoteClient())
            {
                // We default to "on" in Visual Studio remotely joined cloud environment clients
                return true;
            }

            if (IsLiveShareHost())
            {
                // Placeholder for when we turn on LiveShare support by default
                return false;
            }

            if (IsLiveShareGuest())
            {
                // Placeholder for when we turn on LiveShare support by default
                return false;
            }

            return false;
        }

        // Private protected virtual for testing
        private protected virtual bool ProjectSupportsRazorLSPEditor(string documentMoniker, IVsHierarchy hierarchy)
        {
            if (_projectSupportsRazorLSPCache.TryGetValue(documentMoniker, out bool isSupported))
            {
                return isSupported;
            }

            if (hierarchy == null)
            {
                var hr = _vsUIShellOpenDocument.Value.IsDocumentInAProject(documentMoniker, out var uiHierarchy, out _, out _, out _);
                hierarchy = uiHierarchy;
                if (!ErrorHandler.Succeeded(hr) || hierarchy == null)
                {
                    return CacheProjectRazorLSPEditorSupport(documentMoniker, false);
                }
            }

            if (_projectHierarchyInspector.HasCapability(documentMoniker, hierarchy, DotNetCoreCSharpCapability))
            {
                // .NET Core project that supports C#
                return CacheProjectRazorLSPEditorSupport(documentMoniker, true);
            }

            // Not a C# .NET Core project. This typically happens for legacy Razor scenarios
            return CacheProjectRazorLSPEditorSupport(documentMoniker, false);
        }

        private bool CacheProjectRazorLSPEditorSupport(string documentMoniker, bool isSupported)
        {
            _projectSupportsRazorLSPCache.Set(
                documentMoniker,
                isSupported,
                new MemoryCacheEntryOptions() { Size = 1 });
            return isSupported;
        }

        // Private protected virtual for testing
        private protected virtual bool EnvironmentFeatureEnabled()
        {
            if (!_environmentFeatureEnabled.HasValue)
            {
                var lspRazorEnabledString = Environment.GetEnvironmentVariable(RazorLSPEditorFeatureFlag);
                _ = bool.TryParse(lspRazorEnabledString, out var enabled);
                _environmentFeatureEnabled = enabled;
            }

            return _environmentFeatureEnabled.Value;
        }

        // Private protected virtual for testing
        private protected virtual bool IsFeatureFlagEnabledCached()
        {
            if (!_featureFlagEnabled.HasValue)
            {
                _featureFlagEnabled = _featureFlags.IsFeatureEnabled(RazorLSPEditorFeatureFlag, defaultValue: false);
            }

            return _featureFlagEnabled.Value;
        }

        // Private protected virtual for testing
        private protected virtual bool IsVSServer()
        {
            if (!_isVSServer.HasValue)
            {
                var shell = AsyncPackage.GetGlobalService(typeof(SVsShell)) as IVsShell;
                var result = shell.GetProperty((int)__VSSPROPID11.VSSPROPID_ShellMode, out var mode);

                if (ErrorHandler.Succeeded(result))
                {
                    // VSSPROPID_ShellMode is set to VSSM_Server when /server is used in devenv command
                    _isVSServer = ((int)mode == (int)__VSShellMode.VSSM_Server);
                }
                else
                {
                    _isVSServer = false;
                }
            }

            return _isVSServer.Value;
        }

        // Private protected virtual for testing
        private protected virtual bool IsVSRemoteClient()
        {
            var context = UIContext.FromUIContextGuid(VSConstants.UICONTEXT.CloudEnvironmentConnected_guid);
            return context.IsActive;
        }

        // Private protected virtual for testing
        private protected virtual bool IsLiveShareGuest()
        {
            var context = UIContext.FromUIContextGuid(LiveShareGuestUIContextGuid);
            return context.IsActive;
        }

        // Private protected virtual for testing
        private protected virtual bool IsLiveShareHost()
        {
            var context = UIContext.FromUIContextGuid(LiveShareHostUIContextGuid);
            return context.IsActive;
        }
    }
}
