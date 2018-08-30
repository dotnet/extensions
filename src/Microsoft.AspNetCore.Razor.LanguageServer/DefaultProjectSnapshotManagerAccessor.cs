// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Razor;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;
using Microsoft.VisualStudio.Editor.Razor;

namespace Microsoft.AspNetCore.Razor.LanguageServer
{
    internal class DefaultProjectSnapshotManagerAccessor : ProjectSnapshotManagerAccessor
    {
        private readonly ForegroundDispatcher _foregroundDispatcher;
        private readonly IEnumerable<ProjectSnapshotChangeTrigger> _changeTriggers;
        private ProjectSnapshotManagerBase _instance;

        public DefaultProjectSnapshotManagerAccessor(
            ForegroundDispatcher foregroundDispatcher,
            IEnumerable<ProjectSnapshotChangeTrigger> changeTriggers)
        {
            if (foregroundDispatcher == null)
            {
                throw new ArgumentNullException(nameof(foregroundDispatcher));
            }

            if (changeTriggers == null)
            {
                throw new ArgumentNullException(nameof(changeTriggers));
            }

            _foregroundDispatcher = foregroundDispatcher;
            _changeTriggers = changeTriggers;
        }

        public override ProjectSnapshotManagerBase Instance
        {
            get
            {
                if (_instance == null)
                {
                    var projectEngineFactories = new Lazy<IProjectEngineFactory, ICustomProjectEngineFactoryMetadata>[]
                    {
                        new Lazy<IProjectEngineFactory, ICustomProjectEngineFactoryMetadata>(
                            () => new LegacyProjectEngineFactory_1_0(),
                            new ExportCustomProjectEngineFactoryAttribute("MVC-1.0") { SupportsSerialization = true }),
                        new Lazy<IProjectEngineFactory, ICustomProjectEngineFactoryMetadata>(
                            () => new LegacyProjectEngineFactory_1_1(),
                            new ExportCustomProjectEngineFactoryAttribute("MVC-1.1") { SupportsSerialization = true }),
                        new Lazy<IProjectEngineFactory, ICustomProjectEngineFactoryMetadata>(
                            () => new LegacyProjectEngineFactory_2_0(),
                            new ExportCustomProjectEngineFactoryAttribute("MVC-2.0") { SupportsSerialization = true }),
                        new Lazy<IProjectEngineFactory, ICustomProjectEngineFactoryMetadata>(
                            () => new LegacyProjectEngineFactory_2_1(),
                            new ExportCustomProjectEngineFactoryAttribute("MVC-2.1") { SupportsSerialization = true }),
                    };
                    var services = AdhocServices.Create(
                        workspaceServices: new[]
                        {
                            new DefaultProjectSnapshotProjectEngineFactory(
                                new FallbackProjectEngineFactory(),
                                projectEngineFactories)
                        },
                        razorLanguageServices: new[]
                        {
                            NoopTagHelperResolver.Instance
                        });
                    var workspace = new AdhocWorkspace(services);
                    _instance = new DefaultProjectSnapshotManager(
                        _foregroundDispatcher,
                        new DefaultErrorReporter(),
                        _changeTriggers,
                        workspace);
                }

                return _instance;
            }
        }

        private class NoopTagHelperResolver : TagHelperResolver
        {
            public static readonly NoopTagHelperResolver Instance = new NoopTagHelperResolver();
            private NoopTagHelperResolver()
            {
            }

            public override Task<TagHelperResolutionResult> GetTagHelpersAsync(ProjectSnapshot project, CancellationToken cancellationToken = default)
            {
                return Task.FromResult(TagHelperResolutionResult.Empty);
            }
        }

        private class AdhocServices : HostServices
        {
            private readonly IEnumerable<IWorkspaceService> _workspaceServices;
            private readonly IEnumerable<ILanguageService> _razorLanguageServices;

            private AdhocServices(IEnumerable<IWorkspaceService> workspaceServices, IEnumerable<ILanguageService> razorLanguageServices)
            {
                if (workspaceServices == null)
                {
                    throw new ArgumentNullException(nameof(workspaceServices));
                }

                if (razorLanguageServices == null)
                {
                    throw new ArgumentNullException(nameof(razorLanguageServices));
                }

                _workspaceServices = workspaceServices;
                _razorLanguageServices = razorLanguageServices;
            }

            protected override HostWorkspaceServices CreateWorkspaceServices(Workspace workspace)
            {
                if (workspace == null)
                {
                    throw new ArgumentNullException(nameof(workspace));
                }

                return new AdhocWorkspaceServices(this, _workspaceServices, _razorLanguageServices, workspace);
            }

            public static HostServices Create(IEnumerable<ILanguageService> razorLanguageServices)
                => Create(Enumerable.Empty<IWorkspaceService>(), razorLanguageServices);

            public static HostServices Create(IEnumerable<IWorkspaceService> workspaceServices, IEnumerable<ILanguageService> razorLanguageServices)
                => new AdhocServices(workspaceServices, razorLanguageServices);
        }

        private class AdhocWorkspaceServices : HostWorkspaceServices
        {
            private static readonly Workspace DefaultWorkspace = new AdhocWorkspace();

            private readonly HostServices _hostServices;
            private readonly HostLanguageServices _razorLanguageServices;
            private readonly IEnumerable<IWorkspaceService> _workspaceServices;
            private readonly Workspace _workspace;

            public AdhocWorkspaceServices(
                HostServices hostServices,
                IEnumerable<IWorkspaceService> workspaceServices,
                IEnumerable<ILanguageService> languageServices,
                Workspace workspace)
            {
                if (hostServices == null)
                {
                    throw new ArgumentNullException(nameof(hostServices));
                }

                if (workspaceServices == null)
                {
                    throw new ArgumentNullException(nameof(workspaceServices));
                }

                if (languageServices == null)
                {
                    throw new ArgumentNullException(nameof(languageServices));
                }

                if (workspace == null)
                {
                    throw new ArgumentNullException(nameof(workspace));
                }

                _hostServices = hostServices;
                _workspaceServices = workspaceServices;
                _workspace = workspace;

                _razorLanguageServices = new AdhocLanguageServices(this, languageServices);
            }

            public override HostServices HostServices => _hostServices;

            public override Workspace Workspace => _workspace;

            public override TWorkspaceService GetService<TWorkspaceService>()
            {
                var service = _workspaceServices.OfType<TWorkspaceService>().FirstOrDefault();

                if (service == null)
                {
                    // Fallback to default host services to resolve roslyn specific features.
                    service = DefaultWorkspace.Services.GetService<TWorkspaceService>();
                }

                return service;
            }

            public override HostLanguageServices GetLanguageServices(string languageName)
            {
                if (languageName == RazorLanguage.Name)
                {
                    return _razorLanguageServices;
                }

                // Fallback to default host services to resolve roslyn specific features.
                return DefaultWorkspace.Services.GetLanguageServices(languageName);
            }

            public override IEnumerable<string> SupportedLanguages => new[] { RazorLanguage.Name };

            public override bool IsSupported(string languageName) => languageName == RazorLanguage.Name;

            public override IEnumerable<TLanguageService> FindLanguageServices<TLanguageService>(MetadataFilter filter) => throw new NotImplementedException();
        }

        private class AdhocLanguageServices : HostLanguageServices
        {
            private readonly HostWorkspaceServices _workspaceServices;
            private readonly IEnumerable<ILanguageService> _languageServices;

            public AdhocLanguageServices(HostWorkspaceServices workspaceServices, IEnumerable<ILanguageService> languageServices)
            {
                if (workspaceServices == null)
                {
                    throw new ArgumentNullException(nameof(workspaceServices));
                }

                if (languageServices == null)
                {
                    throw new ArgumentNullException(nameof(languageServices));
                }

                _workspaceServices = workspaceServices;
                _languageServices = languageServices;
            }

            public override HostWorkspaceServices WorkspaceServices => _workspaceServices;

            public override string Language => RazorLanguage.Name;

            public override TLanguageService GetService<TLanguageService>()
            {
                var service = _languageServices.OfType<TLanguageService>().FirstOrDefault();

                if (service == null)
                {
                    throw new InvalidOperationException($"Razor language services not configured properly, missing language service '{typeof(TLanguageService).FullName}'.");
                }

                return service;
            }
        }
    }
}
