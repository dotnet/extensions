// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.Framework.Notification
{
    public class NotificationNameAttribute : Attribute
    {
        public NotificationNameAttribute(string name)
        {
            Name = name;
        }

        public string Name { get; }
    }
}
