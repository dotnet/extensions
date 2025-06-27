// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Diagnostics;

public static class ProcessExtensions
{
    public static bool TryGetHasExited(this Process process)
    {
        try
        {
            return process.HasExited;
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("No process is associated with this object"))
        {
            return true;
        }
    }
}
