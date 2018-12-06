// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.LiveShare.Razor.Serialization;
using Newtonsoft.Json;

namespace Microsoft.VisualStudio.LiveShare.Razor.Guest
{
    [Export]
    [ExportCollaborationService(typeof(ICollaborationService), Scope = SessionScope.Guest)]
    public class LiveShareClientProvider : ICollaborationServiceFactory
    {
        private LiveShareClientService _liveShareClientService;

        public Task<ICollaborationService> CreateServiceAsync(CollaborationSession session, CancellationToken cancellationToken)
        {
            var serializer = (JsonSerializer)session.GetService(typeof(JsonSerializer));
            serializer.Converters.RegisterRazorLiveShareConverters();

            var liveShareClientService = new LiveShareClientService(session);
            liveShareClientService.Disposed += (s, e) =>
            {
                _liveShareClientService = null;
            };

            _liveShareClientService = liveShareClientService;
            return Task.FromResult<ICollaborationService>(liveShareClientService);
        }

        internal Task<TProxy> CreateServiceProxyAsync<TProxy>() where TProxy : class
        {
            if (_liveShareClientService == null)
            {
                return Task.FromResult<TProxy>(null);
            }

            return _liveShareClientService.CreateServiceProxyAsync<TProxy>();
        }

        internal string ConvertToLocalPath(Uri sharedUri)
        {
            return _liveShareClientService?.ConvertToLocalPath(sharedUri);
        }

        internal Uri ConvertToSharedUri(string localPath)
        {
            return _liveShareClientService?.ConvertToSharedUri(localPath);
        }

        private class LiveShareClientService : ICollaborationService, IDisposable
        {
            private CollaborationSession _session;

            public LiveShareClientService(CollaborationSession session)
            {
                _session = session;
            }

            public event EventHandler Disposed;

            public void Dispose()
            {
                Disposed?.Invoke(this, null);
            }

            internal Task<TProxy> CreateServiceProxyAsync<TProxy>() where TProxy : class
            {
                return _session.GetRemoteServiceAsync<TProxy>(typeof(TProxy).Name, CancellationToken.None);
            }

            internal string ConvertToLocalPath(Uri sharedUri)
            {
                return _session.ConvertSharedUriToLocalPath(sharedUri);
            }

            internal Uri ConvertToSharedUri(string localPath)
            {
                return _session.ConvertLocalPathToSharedUri(localPath);
            }
        }
    }
}
