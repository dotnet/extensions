// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Messaging;

namespace SampleMsmqHost
{
    public class MsmqOptions
    {
        public string Path { get; set; }

        public bool SharedModeDenyReceive { get; set; } = false;

        public bool EnableCache { get; set; } = false;

        public QueueAccessMode AccessMode { get; set; } = QueueAccessMode.SendAndReceive;
    }
}