// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.Extensions.Hosting.SystemdServices
{
    /// <summary>
    /// Describes a service state change.
    /// </summary>
    public struct ServiceState
    {
        private string _state;

        /// <summary>
        /// Service startup is finished.
        /// </summary>
        public static ServiceState Ready => new ServiceState("READY=1");

        /// <summary>
        /// Service is reloading its configuration.
        /// </summary>
        public static ServiceState Reloading => new ServiceState("RELOADING=1");

        /// <summary>
        /// Service is beginning its shutdown.
        /// </summary>
        public static ServiceState Stopping => new ServiceState("STOPPING=1");

        /// <summary>
        /// Update the watchdog timestamp.
        /// </summary>
        public static ServiceState Watchdog => new ServiceState("WATCHDOG=1");

        /// <summary>
        /// Describes the service state.
        /// </summary>
        public static ServiceState Status(string value) => new ServiceState($"STATUS={value}");

        /// <summary>
        /// Describes the service failure (errno-style).
        /// </summary>
        public static ServiceState Errno(int value) => new ServiceState($"ERRNO={value}");

        /// <summary>
        /// Describes the service failure (D-Bus error).
        /// </summary>
        public static ServiceState BusError(string value) => new ServiceState($"BUSERROR={value}");

        /// <summary>
        /// Main process ID (PID) of the service, in case the service manager did not fork off the process itself.
        /// </summary>
        public static ServiceState MainPid(int value) => new ServiceState($"MAINPID={value}");

        /// <summary>
        /// Create custom ServiceState.
        /// </summary>
        public ServiceState(string state)
        {
            _state = state ?? throw new ArgumentNullException(nameof(state));
        }

        /// <summary>
        /// String representation of service state.
        /// </summary>
        public override string ToString() => _state;
    }
}