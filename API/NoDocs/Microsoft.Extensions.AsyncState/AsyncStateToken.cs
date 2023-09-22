// Assembly 'Microsoft.Extensions.AsyncState'

using System;
using System.Runtime.CompilerServices;

namespace Microsoft.Extensions.AsyncState;

public readonly struct AsyncStateToken : IEquatable<AsyncStateToken>
{
    public override bool Equals(object? obj);
    public bool Equals(AsyncStateToken other);
    public override int GetHashCode();
    public static bool operator ==(AsyncStateToken left, AsyncStateToken right);
    public static bool operator !=(AsyncStateToken left, AsyncStateToken right);
}
