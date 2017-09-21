// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.
#if NETCOREAPP2_0

namespace Microsoft.AspNetCore.Certificates.Generation
{
    internal enum CertificatePurpose
    {
        All,
        HTTPS,
        Signing
    }
}

#endif