// Assembly 'System.Cloud.Messaging'

using System.Threading.Tasks;

namespace System.Cloud.Messaging;

/// <summary>
/// Interface for writing message to a destination.
/// </summary>
public interface IMessageDestination
{
    /// <summary>
    /// Write message asynchronously.
    /// </summary>
    /// <param name="context">The message context.</param>
    /// <returns><see cref="T:System.Threading.Tasks.ValueTask" />.</returns>
    ValueTask WriteAsync(MessageContext context);
}
