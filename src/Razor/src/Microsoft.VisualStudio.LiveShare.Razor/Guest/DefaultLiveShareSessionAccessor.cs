// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;

namespace Microsoft.VisualStudio.LiveShare.Razor.Guest
{
    [System.Composition.Shared]
    [Export(typeof(LiveShareSessionAccessor))]
    internal class DefaultLiveShareSessionAccessor : LiveShareSessionAccessor
    {
        private CollaborationSession _currentSession;
        private bool _guestSessionIsActive;

        // We have a separate IsGuestSessionActive to avoid loading LiveShare dlls unnecessarily.
        public override bool IsGuestSessionActive => _guestSessionIsActive;

        public override CollaborationSession Session => _currentSession;

        public void SetSession(CollaborationSession session)
        {
            _guestSessionIsActive = session != null;
            _currentSession = session;
        }
    }
}
