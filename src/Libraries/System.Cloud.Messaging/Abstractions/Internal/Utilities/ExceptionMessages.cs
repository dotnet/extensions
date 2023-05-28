// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Cloud.Messaging.Internal;

/// <summary>
/// Defines messages for exceptions thrown by the library.
/// </summary>
internal sealed class ExceptionMessages
{
    public const string NoMessagePostponeFeatureOnMessageContext = $"No {nameof(IMessagePostponeFeature)} is assigned to the provided {nameof(MessageContext)}.";
    public const string NoSerializedMessagePayloadFeatureOnMessageContext = $"No ISerializedMessagePayloadFeature is assigned to the provided {nameof(MessageContext)}.";
}
