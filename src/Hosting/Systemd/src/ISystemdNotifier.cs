// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.Extensions.Hosting.Systemd
{
    /// <summary>
    /// Provides support to notify systemd about the service status.
    /// </summary>
    public interface ISystemdNotifier
    {
        /// <summary>
        /// Sends a notification to systemd.
        /// </summary>
        void Notify(ServiceState state);
        /// <summary>
        /// Returns whether systemd is configured to receive service notifications.
        /// </summary>
        bool IsEnabled { get; }
    }
}
