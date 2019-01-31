// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.VisualStudio.LiveShare.Razor.Guest
{
    public abstract class LiveShareSessionAccessor
    {
        public abstract CollaborationSession Session { get; }

        public abstract bool IsGuestSessionActive { get; }
    }
}
