// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;

namespace Microsoft.Extensions.Http.Logging.Test.Internal;

public static class RandomStringGenerator
{
    private static readonly Random _random = new();

    public static string Generate(int length)
    {
        const string Chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

        return new string(
            Enumerable
                .Repeat(Chars, length)
                .Select(s => s[_random.Next(s.Length)]).ToArray());
    }
}
