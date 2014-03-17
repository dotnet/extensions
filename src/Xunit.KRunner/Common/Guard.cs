using System;

/// <summary>
/// Guard class, used for guard clauses and argument validation
/// </summary>
internal static class Guard
{
    /// <summary/>
    public static void ArgumentNotNull(string argName, object argValue)
    {
        if (argValue == null)
            throw new ArgumentNullException(argName);
    }
}