// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace ThrowingLibrary
{
    public static class Thrower
    {
        public static void Throw()
        {
            throw new DivideByZeroException();
        }
    }
}
