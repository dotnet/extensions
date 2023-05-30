// Assembly 'Microsoft.AspNetCore.HeaderParsing'

using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Microsoft.AspNetCore.HeaderParsing;

public readonly struct HostHeaderValue : IEquatable<HostHeaderValue>
{
    public string Host { get; }
    public int? Port { get; }
    public HostHeaderValue(string host, int? port);
    public static bool operator ==(HostHeaderValue left, HostHeaderValue right);
    public static bool operator !=(HostHeaderValue left, HostHeaderValue right);
    public static bool TryParse(string value, [NotNullWhen(true)] out HostHeaderValue result);
    public bool Equals(HostHeaderValue other);
    public override bool Equals(object? obj);
    public override int GetHashCode();
    public override string ToString();
}
