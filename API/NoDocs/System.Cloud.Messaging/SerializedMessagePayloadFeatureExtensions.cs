// Assembly 'System.Cloud.Messaging'

using System.Diagnostics.CodeAnalysis;

namespace System.Cloud.Messaging;

public static class SerializedMessagePayloadFeatureExtensions
{
    public static T GetSerializedPayload<T>(this MessageContext context) where T : notnull;
    public static void SetSerializedPayload<T>(this MessageContext context, T payload) where T : notnull;
    public static bool TryGetSerializedPayload<T>(this MessageContext context, [NotNullWhen(true)] out T? payload) where T : notnull;
}
