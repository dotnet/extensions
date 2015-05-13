// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.Framework.Notification
{
    public interface INotifier
    {
        void EnlistTarget(object target);

        bool ShouldNotify(string notificationName);

        void Notify(string notificationName, object parameters);
    }
}
