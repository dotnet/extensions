// Assembly 'System.Cloud.Messaging'

namespace System.Cloud.Messaging;

/// <summary>
/// Feature interface for setting/retrieving the serialized message payload.
/// </summary>
/// <typeparam name="T">Type of the message payload.</typeparam>
public interface ISerializedMessagePayloadFeature<out T> where T : notnull
{
    /// <summary>
    /// Gets the serialized message payload.
    /// </summary>
    T Payload { get; }
}
