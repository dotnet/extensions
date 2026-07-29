using Microsoft.Extensions.AI;

#pragma warning disable MEAI001 // Mock provider uses experimental testing APIs

internal static class MockChatClientExtensions
{
    /// <summary>Adds a canned response for matching requests.</summary>
    /// <param name="client">The mock chat client to configure.</param>
    /// <param name="requestPredicate">Predicate used to select whether the response applies to a request.</param>
    /// <param name="response">The assistant response text.</param>
    /// <param name="minDelay">
    /// The optional minimum simulated response delay in milliseconds. When supplied alone, it is the fixed delay.
    /// </param>
    /// <param name="maxDelay">
    /// The optional maximum simulated response delay in milliseconds. When supplied alone, a delay from zero up to this
    /// value is selected.
    /// </param>
    /// <param name="singleUse">
    /// <see langword="true"/> to remove the response seed after its first matching request; otherwise it remains reusable.
    /// </param>
    /// <returns>The configured <paramref name="client"/>.</returns>
    internal static MockChatClient AddResponse(
        this MockChatClient client,
        Func<MockChatClientRequest, bool> requestPredicate,
        string response,
        int? minDelay = null,
        int? maxDelay = null,
        bool singleUse = false)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(requestPredicate);
        ArgumentNullException.ThrowIfNull(response);
        ValidateDelayRange(minDelay, maxDelay);

        return client.AddResponse(
            requestPredicate,
            async (_, cancellationToken) =>
            {
                await Task.Delay(GetDelay(minDelay, maxDelay), cancellationToken);
                return new ChatResponse(new ChatMessage(ChatRole.Assistant, response + "\n\n"));
            },
            singleUse);
    }

    private static int GetDelay(int? minDelay, int? maxDelay) =>
        (minDelay, maxDelay) switch
        {
            (null, null) => 0,
            ({ } min, null) => min,
            (null, { } max) => Random.Shared.Next(0, max),
            ({ } min, { } max) => Random.Shared.Next(min, max),
        };

    private static void ValidateDelayRange(int? minDelay, int? maxDelay)
    {
        if (minDelay is { } minimumDelay)
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(minimumDelay, 0, nameof(minDelay));
        }

        if (maxDelay is { } maximumDelay)
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(maximumDelay, 0, nameof(maxDelay));

            if (minDelay is { } configuredMinimumDelay)
            {
                ArgumentOutOfRangeException.ThrowIfLessThan(maximumDelay, configuredMinimumDelay, nameof(maxDelay));
            }
        }
    }
}
#pragma warning restore MEAI001
