// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Extensions.AI;

public class RoutingChatClientTests
{
    [Fact]
    public void RoutingContext_CarriesRequestInputs()
    {
        var messages = new List<ChatMessage> { new(ChatRole.User, "initial") };
        var options = new ChatOptions { ModelId = "initial" };
        var context = new RoutingContext(messages, options);

        Assert.Same(messages, context.Messages);
        Assert.NotSame(options, context.ChatOptions);
        Assert.Equal("initial", context.ChatOptions!.ModelId);
        context.ChatOptions.ModelId = "changed";
        Assert.Equal("initial", options.ModelId);
        Assert.Throws<ArgumentNullException>(() => new RoutingContext(null!, options));
    }

    [Fact]
    public void Create_RejectsNullSelector()
    {
        Assert.Throws<ArgumentNullException>(() => RoutingChatClient.Create(null!));
    }

    [Fact]
    public async Task Create_SelectsClientForRequest()
    {
        var messages = new ChatMessage[] { new(ChatRole.User, "hi") };
        var options = new ChatOptions { ModelId = "request" };
        using var cancellationSource = new CancellationTokenSource();
        ChatResponse expected = new(new ChatMessage(ChatRole.Assistant, "ok"));
        ChatOptions? forwardedOptions = null;
        using var selected = new TestChatClient
        {
            GetResponseAsyncCallback = (_, selectedOptions, _) =>
            {
                forwardedOptions = selectedOptions;
                return Task.FromResult(expected);
            },
        };
        RoutingContext? observedContext = null;
        CancellationToken observedToken = default;
        int selectionCount = 0;
        using RoutingChatClient router = RoutingChatClient.Create((context, cancellationToken) =>
        {
            observedContext = context;
            observedToken = cancellationToken;
            selectionCount++;
            context.ChatOptions!.ModelId = "selected";
            return new(selected);
        });

        ChatResponse response = await router.GetResponseAsync(messages, options, cancellationSource.Token);

        Assert.Same(expected, response);
        Assert.Same(messages, observedContext!.Messages);
        Assert.NotSame(options, observedContext.ChatOptions);
        Assert.NotSame(observedContext.ChatOptions, forwardedOptions);
        Assert.Equal("selected", observedContext.ChatOptions!.ModelId);
        Assert.Equal("request", forwardedOptions!.ModelId);
        Assert.Equal("request", options.ModelId);
        Assert.Equal(cancellationSource.Token, observedToken);
        Assert.Equal(1, selectionCount);
    }

    [Fact]
    public async Task Create_DoesNotReselectAfterFailure()
    {
        var expected = new InvalidOperationException("failed");
        using var selected = new TestChatClient
        {
            GetResponseAsyncCallback = (_, _, _) => throw expected,
        };
        int selectionCount = 0;
        using RoutingChatClient router = RoutingChatClient.Create((_, _) =>
        {
            selectionCount++;
            return new(selected);
        });

        InvalidOperationException actual = await Assert.ThrowsAsync<InvalidOperationException>(
            () => router.GetResponseAsync([new(ChatRole.User, "hi")]));

        Assert.Same(expected, actual);
        Assert.Equal(1, selectionCount);
    }

    [Fact]
    public async Task Create_NullSelectionThrowsForNonStreamingAndStreaming()
    {
        using RoutingChatClient router = RoutingChatClient.Create((_, _) => new((IChatClient)null!));

        InvalidOperationException nonStreaming = await Assert.ThrowsAsync<InvalidOperationException>(
            () => router.GetResponseAsync([new(ChatRole.User, "hi")]));
        InvalidOperationException streaming = await Assert.ThrowsAsync<InvalidOperationException>(
            () => CollectAsync(router.GetStreamingResponseAsync([new(ChatRole.User, "hi")])));

        Assert.Contains("SelectClientAsync", nonStreaming.Message, StringComparison.Ordinal);
        Assert.Contains("SelectClientAsync", streaming.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Create_RejectsNullMessagesForNonStreamingAndStreaming()
    {
        using var selected = new TestChatClient();
        using RoutingChatClient router = RoutingChatClient.Create((_, _) => new(selected));

        await Assert.ThrowsAsync<ArgumentNullException>(() => router.GetResponseAsync(null!));
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => CollectAsync(router.GetStreamingResponseAsync(null!)));
    }

    [Fact]
    public async Task Create_DoesNotDisposeSelectedClient()
    {
        using var selected = new CountingDisposeClient();
        using (RoutingChatClient router = RoutingChatClient.Create((_, _) => new(selected)))
        {
            _ = await router.GetResponseAsync([new(ChatRole.User, "hi")]);
        }

        Assert.Equal(0, selected.DisposeCount);
    }

    [Fact]
    public void GetService_ReturnsSelfAndNullForUnknownOrKeyed()
    {
        using var client = new DelegatingTestRouter(_ => throw new NotSupportedException());

        Assert.Same(client, client.GetService(typeof(DelegatingTestRouter)));
        Assert.Same(client, client.GetService(typeof(RoutingChatClient)));
        Assert.Same(client, client.GetService(typeof(IChatClient)));
        Assert.Null(client.GetService(typeof(DelegatingTestRouter), serviceKey: "key"));
        Assert.Null(client.GetService(typeof(string)));
    }

    [Fact]
    public void MaximumAttemptsPerRequest_ValidatesValue()
    {
        using var client = new DelegatingFailoverTestRouter(
            _ => throw new NotSupportedException());

        Assert.Null(client.MaximumAttemptsPerRequest);
        client.MaximumAttemptsPerRequest = 2;
        Assert.Equal(2, client.MaximumAttemptsPerRequest);
        client.MaximumAttemptsPerRequest = null;
        Assert.Null(client.MaximumAttemptsPerRequest);
        Assert.Throws<ArgumentOutOfRangeException>(() => client.MaximumAttemptsPerRequest = 0);
        Assert.Throws<ArgumentOutOfRangeException>(() => client.MaximumAttemptsPerRequest = -1);
    }

    [Fact]
    public async Task Failover_RejectsNullMessagesForNonStreamingAndStreaming()
    {
        using var selected = new TestChatClient();
        using var router = new DelegatingFailoverTestRouter(_ => selected);

        await Assert.ThrowsAsync<ArgumentNullException>(() => router.GetResponseAsync(null!));
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => CollectAsync(router.GetStreamingResponseAsync(null!)));
    }

    [Fact]
    public async Task InitialSelectionFailureDoesNotReportAttempt()
    {
        var expected = new InvalidOperationException("selection failed");
        RoutingContext? selectedContext = null;
        int updateCount = 0;
        using var router = new DelegatingFailoverTestRouter(
            context =>
            {
                selectedContext = context;
                throw expected;
            },
            (_, _, _) => updateCount++);

        InvalidOperationException actual = await Assert.ThrowsAsync<InvalidOperationException>(
            () => router.GetResponseAsync([new(ChatRole.User, "hi")]));

        Assert.Same(expected, actual);
        Assert.NotNull(selectedContext);
        Assert.Equal(0, updateCount);
    }

    [Fact]
    public async Task InitialSelectionFailureWithCallerCancellationPropagatesSelectionFailure()
    {
        using var cancellationSource = new CancellationTokenSource();
        cancellationSource.Cancel();
        var selectionException = new InvalidOperationException("selection failed");
        int updateCount = 0;
        using var router = new DelegatingFailoverTestRouter(
            _ => throw selectionException,
            (_, _, _) => updateCount++);

        InvalidOperationException actual = await Assert.ThrowsAsync<InvalidOperationException>(
            () => router.GetResponseAsync(
                [new(ChatRole.User, "hi")],
                cancellationToken: cancellationSource.Token));

        Assert.Same(selectionException, actual);
        Assert.Equal(0, updateCount);
    }

    [Fact]
    public async Task StreamingInitialSelectionFailureDoesNotReportAttempt()
    {
        var expected = new InvalidOperationException("selection failed");
        RoutingContext? selectedContext = null;
        int updateCount = 0;
        using var router = new DelegatingFailoverTestRouter(
            context =>
            {
                selectedContext = context;
                throw expected;
            },
            (_, _, _) => updateCount++);

        InvalidOperationException actual = await Assert.ThrowsAsync<InvalidOperationException>(
            () => CollectAsync(router.GetStreamingResponseAsync([new(ChatRole.User, "hi")])));

        Assert.Same(expected, actual);
        Assert.NotNull(selectedContext);
        Assert.Equal(0, updateCount);
    }

    [Fact]
    public async Task StreamingInitialSelectionFailureWithCallerCancellationPropagatesSelectionFailure()
    {
        using var cancellationSource = new CancellationTokenSource();
        cancellationSource.Cancel();
        var selectionException = new InvalidOperationException("selection failed");
        int updateCount = 0;
        using var router = new DelegatingFailoverTestRouter(
            _ => throw selectionException,
            (_, _, _) => updateCount++);

        InvalidOperationException actual = await Assert.ThrowsAsync<InvalidOperationException>(
            () => CollectAsync(
                router.GetStreamingResponseAsync(
                    [new(ChatRole.User, "hi")],
                    cancellationToken: cancellationSource.Token)));

        Assert.Same(selectionException, actual);
        Assert.Equal(0, updateCount);
    }

    [Fact]
    public async Task NullSelectionDoesNotReportAttempt()
    {
        int updateCount = 0;
        using var router = new DelegatingFailoverTestRouter(
            _ => null!,
            (_, _, _) => updateCount++);

        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => router.GetResponseAsync([new(ChatRole.User, "hi")]));

        Assert.Contains("SelectClientAsync", exception.Message, StringComparison.Ordinal);
        Assert.Equal(0, updateCount);
    }

    [Fact]
    public async Task StreamingNullSelectionDoesNotReportAttempt()
    {
        int updateCount = 0;
        using var router = new DelegatingFailoverTestRouter(
            _ => null!,
            (_, _, _) => updateCount++);

        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => CollectAsync(router.GetStreamingResponseAsync([new(ChatRole.User, "hi")])));

        Assert.Contains("SelectClientAsync", exception.Message, StringComparison.Ordinal);
        Assert.Equal(0, updateCount);
    }

    [Fact]
    public async Task Failover_RetrySelectionFailureReportsOnlyCompletedAttempt()
    {
        var invocationException = new InvalidOperationException("invocation failed");
        var selectionException = new InvalidOperationException("selection failed");
        using var failing = new TestChatClient
        {
            GetResponseAsyncCallback = (_, _, _) => throw invocationException,
        };
        int selections = 0;
        var updates = new List<(FailoverChatClientAttempt attempt, bool isTerminal)>();
        using var router = new DelegatingFailoverTestRouter(
            _ => ++selections == 1 ? failing : throw selectionException,
            (_, attempt, isTerminal) =>
            {
                updates.Add((attempt, isTerminal));
            });

        InvalidOperationException actual = await Assert.ThrowsAsync<InvalidOperationException>(
            () => router.GetResponseAsync([new(ChatRole.User, "hi")]));

        Assert.Same(selectionException, actual);
        Assert.Equal(2, selections);
        (FailoverChatClientAttempt attempt, bool isTerminal) update = Assert.Single(updates);
        Assert.Same(failing, update.attempt.Client);
        Assert.Same(invocationException, update.attempt.Exception);
        Assert.False(update.isTerminal);
    }

    [Fact]
    public async Task TerminalRoutingUpdateFailureReplacesRequestFailure()
    {
        var invocationException = new InvalidOperationException("invocation failed");
        var completionException = new InvalidOperationException("completion failed");
        using var failing = new TestChatClient
        {
            GetResponseAsyncCallback = (_, _, _) => throw invocationException,
        };
        using var router = new DelegatingFailoverTestRouter(
            _ => failing,
            (_, _, isTerminal) =>
            {
                Assert.True(isTerminal);
                throw completionException;
            })
        {
            MaximumAttemptsPerRequest = 1,
        };

        InvalidOperationException actual = await Assert.ThrowsAsync<InvalidOperationException>(
            () => router.GetResponseAsync([new(ChatRole.User, "hi")]));

        Assert.Same(completionException, actual);
    }

    [Fact]
    public async Task NonterminalRoutingUpdateFailureStopsWithoutAnotherUpdate()
    {
        var invocationException = new InvalidOperationException("invocation failed");
        var updateException = new InvalidOperationException("update failed");
        using var failing = new TestChatClient
        {
            GetResponseAsyncCallback = (_, _, _) => throw invocationException,
        };
        int selections = 0;
        int updates = 0;
        using var router = new DelegatingFailoverTestRouter(
            _ =>
            {
                selections++;
                return failing;
            },
            (_, attempt, isTerminal) =>
            {
                updates++;
                Assert.Same(invocationException, attempt!.Exception);
                Assert.False(isTerminal);
                throw updateException;
            });

        InvalidOperationException actual = await Assert.ThrowsAsync<InvalidOperationException>(
            () => router.GetResponseAsync([new(ChatRole.User, "hi")]));

        Assert.Same(updateException, actual);
        Assert.Equal(1, selections);
        Assert.Equal(1, updates);
    }

    [Fact]
    public async Task SelectionFailureDoesNotInvokeRoutingUpdate()
    {
        var selectionException = new InvalidOperationException("selection failed");
        int updateCount = 0;
        using var router = new DelegatingFailoverTestRouter(
            _ => throw selectionException,
            (_, _, _) => updateCount++);

        InvalidOperationException actual = await Assert.ThrowsAsync<InvalidOperationException>(
            () => router.GetResponseAsync([new(ChatRole.User, "hi")]));

        Assert.Same(selectionException, actual);
        Assert.Equal(0, updateCount);
    }

    [Fact]
    public async Task Dispatch_ConfiguredClientPreservesAndOverridesRequestOptions()
    {
        ChatOptions? forwarded = null;
        using var inner = new TestChatClient
        {
            GetResponseAsyncCallback = (_, options, _) =>
            {
                forwarded = options;
                return Task.FromResult(new ChatResponse());
            },
        };
        using var configured = new ConfigureOptionsChatClient(
            inner,
            options => options.ModelId = "route");
        var requestOptions = new ChatOptions
        {
            Instructions = "caller",
            ModelId = "request",
        };
        using var router = new DelegatingTestRouter(_ => configured);

        _ = await router.GetResponseAsync([new(ChatRole.User, "hi")], requestOptions);

        Assert.NotSame(requestOptions, forwarded);
        Assert.Equal("route", forwarded!.ModelId);
        Assert.Equal("caller", forwarded.Instructions);
        Assert.Equal("request", requestOptions.ModelId);
    }

    [Fact]
    public async Task Failover_UsesFreshRequestOptionsForEachAttempt()
    {
        var requestOptions = new ChatOptions
        {
            Instructions = "caller",
            ModelId = "request",
        };
        var invokedOptions = new List<ChatOptions>();
        using var first = new TestChatClient
        {
            GetResponseAsyncCallback = (_, options, _) =>
            {
                invokedOptions.Add(options!);
                options!.ModelId = "changed by first client";
                throw new InvalidOperationException("failed");
            },
        };
        ChatResponse expected = new(new ChatMessage(ChatRole.Assistant, "ok"));
        using var second = new TestChatClient
        {
            GetResponseAsyncCallback = (_, options, _) =>
            {
                invokedOptions.Add(options!);
                return Task.FromResult(expected);
            },
        };
        int selections = 0;
        using var router = new DelegatingFailoverTestRouter(
            _ => ++selections == 1 ? first : second);

        ChatResponse response = await router.GetResponseAsync(
            [new(ChatRole.User, "hi")],
            requestOptions);

        Assert.Same(expected, response);
        Assert.Equal(2, selections);
        Assert.Equal(2, invokedOptions.Count);
        Assert.NotSame(invokedOptions[0], invokedOptions[1]);
        Assert.Equal("changed by first client", invokedOptions[0].ModelId);
        Assert.Equal("request", invokedOptions[1].ModelId);
        Assert.Equal("caller", invokedOptions[1].Instructions);
        Assert.Equal("request", requestOptions.ModelId);
    }

    [Fact]
    public async Task Failure_Propagates()
    {
        var expected = new InvalidOperationException("failed");
        using var inner = new TestChatClient
        {
            GetResponseAsyncCallback = (_, _, _) => throw expected,
        };
        FailoverChatClientAttempt? observed = null;
        using var router = new DelegatingFailoverTestRouter(
            _ => inner,
            (_, attempt, isTerminal) =>
            {
                Assert.True(isTerminal);
                observed = attempt;
            })
        {
            MaximumAttemptsPerRequest = 1,
        };

        InvalidOperationException actual = await Assert.ThrowsAsync<InvalidOperationException>(
            () => router.GetResponseAsync([new(ChatRole.User, "hi")]));

        Assert.Same(expected, actual);
        Assert.Same(inner, observed!.Client);
        Assert.Same(expected, observed.Exception);
        Assert.False(observed.ResponseCompleted);
    }

    [Fact]
    public async Task Failover_UpdateChangesStateBeforeNextSelection()
    {
        using var failing = new TestChatClient
        {
            GetResponseAsyncCallback = (_, _, _) => throw new InvalidOperationException("failed"),
        };
        ChatResponse expected = new(new ChatMessage(ChatRole.Assistant, "ok"));
        using var working = new TestChatClient
        {
            GetResponseAsyncCallback = (_, _, _) => Task.FromResult(expected),
        };
        RoutingContext? initialContext = null;
        FailoverChatClientAttempt? failedAttempt = null;
        FailoverChatClientAttempt? terminalAttempt = null;
        using var router = new DelegatingFailoverTestRouter(
            context =>
            {
                initialContext ??= context;
                Assert.Same(initialContext, context);
                return failedAttempt is null ? failing : working;
            },
            (context, attempt, isTerminal) =>
            {
                Assert.Same(initialContext, context);
                if (isTerminal)
                {
                    terminalAttempt = attempt;
                }
                else
                {
                    failedAttempt = attempt;
                }
            });

        ChatResponse response = await router.GetResponseAsync([new(ChatRole.User, "hi")]);

        Assert.Same(expected, response);
        Assert.Same(failing, failedAttempt!.Client);
        Assert.Equal("failed", Assert.IsType<InvalidOperationException>(failedAttempt.Exception).Message);
        Assert.True(failedAttempt.Duration >= TimeSpan.Zero);
        Assert.Null(failedAttempt.TimeToFirstUpdate);
        Assert.False(failedAttempt.OutputCommitted);
        Assert.False(failedAttempt.ResponseCompleted);
        Assert.Same(working, terminalAttempt!.Client);
        Assert.Null(terminalAttempt.Exception);
        Assert.True(terminalAttempt.ResponseCompleted);
    }

    [Fact]
    public async Task Failover_CanRetrySameClient()
    {
        int calls = 0;
        using var inner = new TestChatClient
        {
            GetResponseAsyncCallback = (_, _, _) =>
                ++calls == 1
                    ? throw new InvalidOperationException("transient")
                    : Task.FromResult(new ChatResponse()),
        };
        using var router = new DelegatingFailoverTestRouter(
            _ => inner);

        _ = await router.GetResponseAsync([new(ChatRole.User, "hi")]);

        Assert.Equal(2, calls);
    }

    [Fact]
    public async Task MaximumAttemptsPerRequest_AllowsSuccessAtLimit()
    {
        int calls = 0;
        ChatResponse expected = new(new ChatMessage(ChatRole.Assistant, "ok"));
        using var inner = new TestChatClient
        {
            GetResponseAsyncCallback = (_, _, _) =>
                ++calls == 1
                    ? throw new InvalidOperationException("transient")
                    : Task.FromResult(expected),
        };
        using var router = new DelegatingFailoverTestRouter(
            _ => inner)
        {
            MaximumAttemptsPerRequest = 2,
        };

        ChatResponse response = await router.GetResponseAsync([new(ChatRole.User, "hi")]);

        Assert.Same(expected, response);
        Assert.Equal(2, calls);
    }

    [Fact]
    public async Task MaximumAttemptsPerRequest_RethrowsLastFailure()
    {
        int calls = 0;
        var terminalUpdates = new List<bool>();
        using var inner = new TestChatClient
        {
            GetResponseAsyncCallback = (_, _, _) =>
                throw new InvalidOperationException($"failure {++calls}"),
        };
        using var router = new DelegatingFailoverTestRouter(
            _ => inner,
            (_, attempt, isTerminal) =>
            {
                Assert.NotNull(attempt);
                terminalUpdates.Add(isTerminal);
            })
        {
            MaximumAttemptsPerRequest = 2,
        };

        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => router.GetResponseAsync([new(ChatRole.User, "hi")]));

        Assert.Equal("failure 2", exception.Message);
        Assert.Equal(2, calls);
        Assert.Equal([false, true], terminalUpdates);
    }

    [Fact]
    public async Task Failover_NullResponseIsReturnedWithoutFailover()
    {
        using var nullClient = new TestChatClient
        {
            GetResponseAsyncCallback = (_, _, _) => Task.FromResult<ChatResponse>(null!),
        };
        int selections = 0;
        FailoverChatClientAttempt? terminalAttempt = null;
        using var router = new DelegatingFailoverTestRouter(
            _ =>
            {
                selections++;
                return nullClient;
            },
            (_, attempt, isTerminal) =>
            {
                Assert.True(isTerminal);
                terminalAttempt = attempt;
            });

        ChatResponse? response = await router.GetResponseAsync([new(ChatRole.User, "hi")]);

        Assert.Null(response);
        Assert.Equal(1, selections);
        Assert.Null(terminalAttempt!.Exception);
        Assert.True(terminalAttempt!.ResponseCompleted);
    }

    [Fact]
    public async Task Cancellation_DoesNotReselect()
    {
        using var cancellationSource = new CancellationTokenSource();
        cancellationSource.Cancel();
        int selections = 0;
        using var inner = new TestChatClient
        {
            GetResponseAsyncCallback = (_, _, cancellationToken) =>
                throw new OperationCanceledException(cancellationToken),
        };
        using var router = new DelegatingTestRouter(_ =>
        {
            selections++;
            return inner;
        });

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => router.GetResponseAsync(
                [new(ChatRole.User, "hi")],
                cancellationToken: cancellationSource.Token));

        Assert.Equal(1, selections);
    }

    [Fact]
    public async Task Failover_CancellationReportsTerminalUpdate()
    {
        using var cancellationSource = new CancellationTokenSource();
        cancellationSource.Cancel();
        var failure = new InvalidOperationException("failed");
        using var inner = new TestChatClient
        {
            GetResponseAsyncCallback = (_, _, _) => throw failure,
        };
        int updateCount = 0;
        using var router = new DelegatingFailoverTestRouter(
            _ => inner,
            (_, attempt, isTerminal) =>
            {
                updateCount++;
                Assert.Same(failure, attempt!.Exception);
                Assert.True(isTerminal);
            });

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => router.GetResponseAsync(
                [new(ChatRole.User, "hi")],
                cancellationToken: cancellationSource.Token));

        Assert.Equal(1, updateCount);
    }

    [Fact]
    public async Task Failover_CancellationDuringUpdateIsObservedByNextAttempt()
    {
        using var cancellationSource = new CancellationTokenSource();
        using var inner = new TestChatClient
        {
            GetResponseAsyncCallback = (_, _, _) => throw new InvalidOperationException("failed"),
        };
        int selections = 0;
        var updates = new List<(FailoverChatClientAttempt attempt, bool isTerminal)>();
        using var router = new DelegatingFailoverTestRouter(
            _ =>
            {
                selections++;
                return inner;
            },
            (_, attempt, isTerminal) =>
            {
                updates.Add((attempt, isTerminal));
                if (!isTerminal)
                {
                    cancellationSource.Cancel();
                }
            });

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => router.GetResponseAsync(
                [new(ChatRole.User, "hi")],
                cancellationToken: cancellationSource.Token));

        Assert.Equal(2, selections);
        Assert.Collection(
            updates,
            update =>
            {
                Assert.NotNull(update.attempt);
                Assert.False(update.isTerminal);
            },
            update =>
            {
                Assert.NotNull(update.attempt);
                Assert.True(update.isTerminal);
            });
    }

    [Fact]
    public async Task Streaming_PreOutputFailurePropagates()
    {
        using var inner = new TestChatClient
        {
            GetStreamingResponseAsyncCallback = (_, _, _) => ThrowingStream("failed"),
        };
        FailoverChatClientAttempt? observed = null;
        using var router = new DelegatingFailoverTestRouter(
            _ => inner,
            (_, attempt, isTerminal) =>
            {
                Assert.True(isTerminal);
                observed = attempt;
            })
        {
            MaximumAttemptsPerRequest = 1,
        };

        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => CollectAsync(router.GetStreamingResponseAsync([new(ChatRole.User, "hi")])));

        Assert.Equal("failed", exception.Message);
        Assert.Same(exception, observed!.Exception);
        Assert.False(observed.OutputCommitted);
        Assert.False(observed.ResponseCompleted);
    }

    [Fact]
    public async Task Streaming_FallsBackBeforeFirstUpdate()
    {
        using var failing = new TestChatClient
        {
            GetStreamingResponseAsyncCallback = (_, _, _) => ThrowingStream("failed"),
        };
        using var working = new TestChatClient
        {
            GetStreamingResponseAsyncCallback = (_, _, _) => YieldUpdates("ok"),
        };
        int selections = 0;
        using var router = new DelegatingFailoverTestRouter(
            _ => ++selections == 1 ? failing : working);

        List<ChatResponseUpdate> updates =
            await CollectAsync(router.GetStreamingResponseAsync([new(ChatRole.User, "hi")]));

        Assert.Equal("ok", Assert.Single(updates).Text);
        Assert.Equal(2, selections);
    }

    [Fact]
    public async Task Streaming_FailoverUsesFreshRequestOptionsForEachAttempt()
    {
        var requestOptions = new ChatOptions
        {
            Instructions = "caller",
            ModelId = "request",
        };
        var invokedOptions = new List<ChatOptions>();
        using var first = new TestChatClient
        {
            GetStreamingResponseAsyncCallback = (_, options, _) =>
            {
                invokedOptions.Add(options!);
                options!.ModelId = "changed by first client";
                return ThrowingStream("failed");
            },
        };
        using var second = new TestChatClient
        {
            GetStreamingResponseAsyncCallback = (_, options, _) =>
            {
                invokedOptions.Add(options!);
                return YieldUpdates("ok");
            },
        };
        int selections = 0;
        using var router = new DelegatingFailoverTestRouter(
            _ => ++selections == 1 ? first : second);

        List<ChatResponseUpdate> updates = await CollectAsync(
            router.GetStreamingResponseAsync(
                [new(ChatRole.User, "hi")],
                requestOptions));

        Assert.Equal("ok", Assert.Single(updates).Text);
        Assert.Equal(2, selections);
        Assert.NotSame(invokedOptions[0], invokedOptions[1]);
        Assert.Equal("changed by first client", invokedOptions[0].ModelId);
        Assert.Equal("request", invokedOptions[1].ModelId);
        Assert.Equal("caller", invokedOptions[1].Instructions);
        Assert.Equal("request", requestOptions.ModelId);
    }

    [Fact]
    public async Task Streaming_CancellationDuringUpdateIsObservedByNextAttempt()
    {
        using var cancellationSource = new CancellationTokenSource();
        using var inner = new TestChatClient
        {
            GetStreamingResponseAsyncCallback = (_, _, _) => ThrowingStream("failed"),
        };
        int selections = 0;
        var updates = new List<(FailoverChatClientAttempt attempt, bool isTerminal)>();
        using var router = new DelegatingFailoverTestRouter(
            _ =>
            {
                selections++;
                return inner;
            },
            (_, attempt, isTerminal) =>
            {
                updates.Add((attempt, isTerminal));
                if (!isTerminal)
                {
                    cancellationSource.Cancel();
                }
            });

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => CollectAsync(
                router.GetStreamingResponseAsync(
                    [new(ChatRole.User, "hi")],
                    cancellationToken: cancellationSource.Token)));

        Assert.Equal(2, selections);
        Assert.Collection(
            updates,
            update =>
            {
                Assert.NotNull(update.attempt);
                Assert.False(update.isTerminal);
            },
            update =>
            {
                Assert.NotNull(update.attempt);
                Assert.True(update.isTerminal);
            });
    }

    [Fact]
    public async Task Streaming_CancellationDoesNotSelectNext()
    {
        using var cancellationSource = new CancellationTokenSource();
        cancellationSource.Cancel();
        using var inner = new TestChatClient
        {
            GetStreamingResponseAsyncCallback = (_, _, _) => ThrowingStream("failed"),
        };
        int selections = 0;
        FailoverChatClientAttempt? observed = null;
        using var router = new DelegatingFailoverTestRouter(
            _ =>
            {
                selections++;
                return inner;
            },
            (_, attempt, isTerminal) =>
            {
                Assert.True(isTerminal);
                observed = attempt;
            });

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => CollectAsync(
                router.GetStreamingResponseAsync(
                    [new(ChatRole.User, "hi")],
                    cancellationToken: cancellationSource.Token)));

        Assert.Equal(1, selections);
        Assert.IsType<InvalidOperationException>(observed!.Exception);
        Assert.False(observed.OutputCommitted);
        Assert.False(observed.ResponseCompleted);
    }

    [Fact]
    public async Task OperationCanceledException_CanTriggerFailover()
    {
        bool secondCalled = false;
        ChatResponse expected = new(new ChatMessage(ChatRole.Assistant, "ok"));
        using var first = new TestChatClient
        {
            GetResponseAsyncCallback = (_, _, _) => throw new OperationCanceledException(),
        };
        using var second = new TestChatClient
        {
            GetResponseAsyncCallback = (_, _, _) =>
            {
                secondCalled = true;
                return Task.FromResult(expected);
            },
        };
        using var client = new OrderedFailoverChatClient([first, second]);

        ChatResponse response = await client.GetResponseAsync([new(ChatRole.User, "hi")]);

        Assert.Same(expected, response);
        Assert.True(secondCalled);
    }

    [Fact]
    public async Task Streaming_OperationCanceledExceptionCanTriggerFailover()
    {
        bool secondCalled = false;
        using var first = new TestChatClient
        {
            GetStreamingResponseAsyncCallback = (_, _, _) => CanceledStream(CancellationToken.None),
        };
        using var second = new TestChatClient
        {
            GetStreamingResponseAsyncCallback = (_, _, _) =>
            {
                secondCalled = true;
                return YieldUpdates("ok");
            },
        };
        using var client = new OrderedFailoverChatClient([first, second]);

        List<ChatResponseUpdate> updates =
            await CollectAsync(client.GetStreamingResponseAsync([new(ChatRole.User, "hi")]));

        Assert.Equal("ok", Assert.Single(updates).Text);
        Assert.True(secondCalled);
    }

    [Fact]
    public async Task Streaming_DisposalCancellationCanTriggerFailover()
    {
        bool secondCalled = false;
        var canceledStream = new TrackingAsyncEnumerable(
            [],
            disposeException: new OperationCanceledException());
        using var first = new TestChatClient
        {
            GetStreamingResponseAsyncCallback = (_, _, _) => canceledStream,
        };
        using var second = new TestChatClient
        {
            GetStreamingResponseAsyncCallback = (_, _, _) =>
            {
                secondCalled = true;
                return YieldUpdates("ok");
            },
        };
        using var client = new OrderedFailoverChatClient([first, second]);

        List<ChatResponseUpdate> updates =
            await CollectAsync(client.GetStreamingResponseAsync([new(ChatRole.User, "hi")]));

        Assert.Equal("ok", Assert.Single(updates).Text);
        Assert.True(secondCalled);
        Assert.Equal(1, canceledStream.DisposeCount);
    }

    [Fact]
    public async Task Streaming_MaximumAttemptsPerRequest_RethrowsLastFailure()
    {
        int calls = 0;
        var terminalUpdates = new List<bool>();
        using var inner = new TestChatClient
        {
            GetStreamingResponseAsyncCallback = (_, _, _) => ThrowingStream($"failure {++calls}"),
        };
        using var router = new DelegatingFailoverTestRouter(
            _ => inner,
            (_, attempt, isTerminal) =>
            {
                Assert.NotNull(attempt);
                terminalUpdates.Add(isTerminal);
            })
        {
            MaximumAttemptsPerRequest = 2,
        };

        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => CollectAsync(router.GetStreamingResponseAsync([new(ChatRole.User, "hi")])));

        Assert.Equal("failure 2", exception.Message);
        Assert.Equal(2, calls);
        Assert.Equal([false, true], terminalUpdates);
    }

    [Fact]
    public async Task Streaming_StreamCreationFailureFallsBack()
    {
        using var failing = new TestChatClient
        {
            GetStreamingResponseAsyncCallback = (_, _, _) => throw new InvalidOperationException("failed"),
        };
        using var working = new TestChatClient
        {
            GetStreamingResponseAsyncCallback = (_, _, _) => YieldUpdates("ok"),
        };
        int selections = 0;
        using var router = new DelegatingFailoverTestRouter(
            _ => ++selections == 1 ? failing : working);

        List<ChatResponseUpdate> updates =
            await CollectAsync(router.GetStreamingResponseAsync([new(ChatRole.User, "hi")]));

        Assert.Equal("ok", Assert.Single(updates).Text);
        Assert.Equal(2, selections);
    }

    [Fact]
    public async Task Streaming_EnumeratorCreationFailureFallsBack()
    {
        using var failing = new TestChatClient
        {
            GetStreamingResponseAsyncCallback = (_, _, _) =>
                new ThrowingGetAsyncEnumeratorEnumerable(new InvalidOperationException("failed")),
        };
        using var working = new TestChatClient
        {
            GetStreamingResponseAsyncCallback = (_, _, _) => YieldUpdates("ok"),
        };
        int selections = 0;
        using var router = new DelegatingFailoverTestRouter(
            _ => ++selections == 1 ? failing : working);

        List<ChatResponseUpdate> updates =
            await CollectAsync(router.GetStreamingResponseAsync([new(ChatRole.User, "hi")]));

        Assert.Equal("ok", Assert.Single(updates).Text);
        Assert.Equal(2, selections);
    }

    [Fact]
    public async Task Streaming_CurrentFailureFallsBackBeforeOutput()
    {
        var currentException = new InvalidOperationException("current failed");
        var failedStream = new ThrowingCurrentAsyncEnumerable(currentException);
        using var failing = new TestChatClient
        {
            GetStreamingResponseAsyncCallback = (_, _, _) => failedStream,
        };
        using var working = new TestChatClient
        {
            GetStreamingResponseAsyncCallback = (_, _, _) => YieldUpdates("ok"),
        };
        FailoverChatClientAttempt? failedAttempt = null;
        using var router = new DelegatingFailoverTestRouter(
            _ => failedAttempt is null ? failing : working,
            (_, attempt, isTerminal) =>
            {
                if (!isTerminal)
                {
                    failedAttempt = attempt;
                }
            });

        List<ChatResponseUpdate> updates =
            await CollectAsync(router.GetStreamingResponseAsync([new(ChatRole.User, "hi")]));

        Assert.Equal("ok", Assert.Single(updates).Text);
        Assert.Same(currentException, failedAttempt!.Exception);
        Assert.False(failedAttempt.OutputCommitted);
        Assert.False(failedAttempt.ResponseCompleted);
        Assert.Null(failedAttempt.TimeToFirstUpdate);
        Assert.Equal(1, failedStream.DisposeCount);
    }

    [Fact]
    public async Task Streaming_CurrentFailureCancellationIsObservedByNextAttempt()
    {
        using var cancellationSource = new CancellationTokenSource();
        var failedStream = new ThrowingCurrentAsyncEnumerable(new InvalidOperationException("current failed"));
        using var inner = new TestChatClient
        {
            GetStreamingResponseAsyncCallback = (_, _, _) => failedStream,
        };
        int selections = 0;
        var updates = new List<(FailoverChatClientAttempt attempt, bool isTerminal)>();
        using var router = new DelegatingFailoverTestRouter(
            _ =>
            {
                selections++;
                return inner;
            },
            (_, attempt, isTerminal) =>
            {
                updates.Add((attempt, isTerminal));
                if (!isTerminal)
                {
                    cancellationSource.Cancel();
                }
            });

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => CollectAsync(
                router.GetStreamingResponseAsync(
                    [new(ChatRole.User, "hi")],
                    cancellationToken: cancellationSource.Token)));

        Assert.Equal(2, selections);
        Assert.Collection(
            updates,
            update =>
            {
                Assert.NotNull(update.attempt);
                Assert.False(update.isTerminal);
            },
            update =>
            {
                Assert.NotNull(update.attempt);
                Assert.True(update.isTerminal);
            });
        Assert.Equal(2, failedStream.DisposeCount);
    }

    [Fact]
    public async Task Streaming_EmptyStreamDisposalFailureFallsBack()
    {
        var disposalException = new InvalidOperationException("dispose failed");
        var failedStream = new TrackingAsyncEnumerable([], disposeException: disposalException);
        using var failing = new TestChatClient
        {
            GetStreamingResponseAsyncCallback = (_, _, _) => failedStream,
        };
        using var working = new TestChatClient
        {
            GetStreamingResponseAsyncCallback = (_, _, _) => YieldUpdates("ok"),
        };
        FailoverChatClientAttempt? failedAttempt = null;
        FailoverChatClientAttempt? terminalAttempt = null;
        using var router = new DelegatingFailoverTestRouter(
            _ => failedAttempt is null ? failing : working,
            (_, attempt, isTerminal) =>
            {
                if (isTerminal)
                {
                    terminalAttempt = attempt;
                }
                else
                {
                    failedAttempt = attempt;
                }
            });

        List<ChatResponseUpdate> updates =
            await CollectAsync(router.GetStreamingResponseAsync([new(ChatRole.User, "hi")]));

        Assert.Equal("ok", Assert.Single(updates).Text);
        Assert.Same(disposalException, failedAttempt!.Exception);
        Assert.False(failedAttempt.OutputCommitted);
        Assert.False(failedAttempt.ResponseCompleted);
        Assert.True(terminalAttempt!.ResponseCompleted);
        Assert.Equal(1, failedStream.DisposeCount);
    }

    [Fact]
    public async Task Streaming_MidStreamFailureIsObservedAndDoesNotReselect()
    {
        var stream = new TrackingAsyncEnumerable(
            [new ChatResponseUpdate(ChatRole.Assistant, "first")],
            throwOnMove: 2,
            exception: new InvalidOperationException("mid-stream"));
        using var inner = new TestChatClient
        {
            GetStreamingResponseAsyncCallback = (_, _, _) => stream,
        };
        int selections = 0;
        FailoverChatClientAttempt? observed = null;
        using var router = new DelegatingFailoverTestRouter(
            _ =>
            {
                selections++;
                return inner;
            },
            (_, attempt, isTerminal) =>
            {
                Assert.True(isTerminal);
                observed = attempt;
            });
        var updates = new List<ChatResponseUpdate>();

        async Task ConsumeAsync()
        {
            await foreach (ChatResponseUpdate update in router.GetStreamingResponseAsync([new(ChatRole.User, "hi")]))
            {
                updates.Add(update);
            }
        }

        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(ConsumeAsync);

        Assert.Equal("mid-stream", exception.Message);
        Assert.Equal("first", Assert.Single(updates).Text);
        Assert.Equal(1, selections);
        Assert.Same(exception, observed!.Exception);
        Assert.True(observed.OutputCommitted);
        Assert.False(observed.ResponseCompleted);
        Assert.NotNull(observed.TimeToFirstUpdate);
        Assert.True(observed.Duration >= observed.TimeToFirstUpdate.Value);
        Assert.Equal(1, stream.DisposeCount);
    }

    [Fact]
    public async Task Streaming_CompletionNotifiesHook()
    {
        using var inner = new TestChatClient
        {
            GetStreamingResponseAsyncCallback = (_, _, _) => YieldUpdates("a", "b"),
        };
        int completions = 0;
        FailoverChatClientAttempt? observed = null;
        using var router = new DelegatingFailoverTestRouter(
            _ => inner,
            (_, attempt, isTerminal) =>
            {
                Assert.True(isTerminal);
                completions++;
                observed = attempt;
            });

        List<ChatResponseUpdate> updates =
            await CollectAsync(router.GetStreamingResponseAsync([new(ChatRole.User, "hi")]));

        Assert.Equal(2, updates.Count);
        Assert.Equal(1, completions);
        Assert.Null(observed!.Exception);
        Assert.True(observed.OutputCommitted);
        Assert.True(observed.ResponseCompleted);
        Assert.NotNull(observed.TimeToFirstUpdate);
    }

    [Fact]
    public async Task Streaming_EmptyCompletionNotifiesHook()
    {
        using var inner = new TestChatClient
        {
            GetStreamingResponseAsyncCallback = (_, _, _) => YieldUpdates(),
        };
        int updateCount = 0;
        using var router = new DelegatingFailoverTestRouter(
            _ => inner,
            (_, attempt, isTerminal) =>
            {
                updateCount++;
                Assert.NotNull(attempt);
                Assert.Null(attempt.Exception);
                Assert.False(attempt.OutputCommitted);
                Assert.True(attempt.ResponseCompleted);
                Assert.True(isTerminal);
            });

        List<ChatResponseUpdate> updates =
            await CollectAsync(router.GetStreamingResponseAsync([new(ChatRole.User, "hi")]));

        Assert.Empty(updates);
        Assert.Equal(1, updateCount);
    }

    [Fact]
    public async Task Streaming_CallerStopsEarlyNotifiesHookAsIncomplete()
    {
        using var inner = new TestChatClient
        {
            GetStreamingResponseAsyncCallback = (_, _, _) => YieldUpdates("a", "b"),
        };
        int completions = 0;
        FailoverChatClientAttempt? observed = null;
        using var router = new DelegatingFailoverTestRouter(
            _ => inner,
            (_, attempt, isTerminal) =>
            {
                Assert.True(isTerminal);
                completions++;
                observed = attempt;
            });

        await ConsumeOneAsync(router.GetStreamingResponseAsync([new(ChatRole.User, "hi")]));

        Assert.Equal(1, completions);
        Assert.Null(observed!.Exception);
        Assert.True(observed.OutputCommitted);
        Assert.False(observed.ResponseCompleted);
    }

    [Fact]
    public void SemanticRouting_RejectsInvalidConfiguration()
    {
        using var client = new TestChatClient();
        using var generator = new TestEmbeddingGenerator();
        var profiles = new Dictionary<IChatClient, IReadOnlyList<string>>(ChatClientReferenceComparer.Instance)
        {
            [client] = ["profile"],
        };

        Assert.Throws<ArgumentNullException>(() =>
            new SemanticRoutingChatClient(null!, profiles, client));
        Assert.Throws<ArgumentNullException>(() =>
            new SemanticRoutingChatClient(generator, null!, client));
        Assert.Throws<ArgumentNullException>(() =>
            new SemanticRoutingChatClient(generator, profiles, null!));
        Assert.Throws<ArgumentException>(() =>
            new SemanticRoutingChatClient(
                generator,
                new Dictionary<IChatClient, IReadOnlyList<string>>(),
                client));
        Assert.Throws<ArgumentException>(() =>
            new SemanticRoutingChatClient(
                generator,
                new Dictionary<IChatClient, IReadOnlyList<string>>
                {
                    [client] = [],
                },
                client));
        Assert.Throws<ArgumentException>(() =>
            new SemanticRoutingChatClient(
                generator,
                new Dictionary<IChatClient, IReadOnlyList<string>>
                {
                    [client] = [" "],
                },
                client));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new SemanticRoutingChatClient(generator, profiles, client, scoreThreshold: 1.1f));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new SemanticRoutingChatClient(generator, profiles, client, topK: 0));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new SemanticRoutingChatClient(
                generator,
                profiles,
                client,
                scoreAggregation: (SemanticRoutingChatClient.ScoreAggregation)(-1)));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new SemanticRoutingChatClient(
                generator,
                profiles,
                client,
                scoreThreshold: 2.1f,
                topK: 2,
                scoreAggregation: SemanticRoutingChatClient.ScoreAggregation.Sum));
    }

    [Fact]
    public async Task SemanticRouting_SelectsBestProfileAndCachesIndex()
    {
        var vectors = new Dictionary<string, float[]>
        {
            ["code profile"] = [1, 0],
            ["writing profile"] = [0, 1],
            ["debug this code"] = [1, 0],
        };
        int profileBatches = 0;
        using var generator = new TestEmbeddingGenerator
        {
            GenerateAsyncCallback = (values, _, _) =>
            {
                string[] inputs = [.. values];
                if (inputs.Length > 1)
                {
                    profileBatches++;
                }

                return Task.FromResult(new GeneratedEmbeddings<Embedding<float>>(
                    [.. inputs.Select(input => new Embedding<float>(vectors[input]))]));
            },
        };
        ChatResponse expected = new(new ChatMessage(ChatRole.Assistant, "code"));
        using var code = new TestChatClient
        {
            GetResponseAsyncCallback = (_, _, _) => Task.FromResult(expected),
        };
        using var writing = new TestChatClient
        {
            GetResponseAsyncCallback = (_, _, _) =>
                Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, "writing"))),
        };
        var profiles = new Dictionary<IChatClient, IReadOnlyList<string>>(ChatClientReferenceComparer.Instance)
        {
            [code] = ["code profile"],
            [writing] = ["writing profile"],
        };
        using var router = new SemanticRoutingChatClient(
            generator,
            profiles,
            defaultClient: writing,
            leaveOpen: true);

        ChatResponse first = await router.GetResponseAsync([new(ChatRole.User, "debug this code")]);
        ChatResponse second = await router.GetResponseAsync([new(ChatRole.User, "debug this code")]);

        Assert.Same(expected, first);
        Assert.Same(expected, second);
        Assert.Equal(1, profileBatches);
    }

    [Theory]
    [InlineData(SemanticRoutingChatClient.ScoreAggregation.Mean, "code")]
    [InlineData(SemanticRoutingChatClient.ScoreAggregation.Sum, "writing")]
    public async Task SemanticRouting_AggregatesGlobalTopKByClient(
        SemanticRoutingChatClient.ScoreAggregation scoreAggregation,
        string expectedResponse)
    {
        var vectors = new Dictionary<string, float[]>
        {
            ["code"] = [1, 0],
            ["writing one"] = [0.8f, 0.6f],
            ["writing two"] = [0.8f, -0.6f],
            ["query"] = [1, 0],
        };
        using var generator = new TestEmbeddingGenerator
        {
            GenerateAsyncCallback = (values, _, _) =>
                Task.FromResult(new GeneratedEmbeddings<Embedding<float>>(
                    [.. values.Select(input => new Embedding<float>(vectors[input]))])),
        };
        using var code = new TestChatClient
        {
            GetResponseAsyncCallback = (_, _, _) =>
                Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, "code"))),
        };
        using var writing = new TestChatClient
        {
            GetResponseAsyncCallback = (_, _, _) =>
                Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, "writing"))),
        };
        var profiles = new Dictionary<IChatClient, IReadOnlyList<string>>(ChatClientReferenceComparer.Instance)
        {
            [code] = ["code"],
            [writing] = ["writing one", "writing two"],
        };
        using var router = new SemanticRoutingChatClient(
            generator,
            profiles,
            defaultClient: code,
            scoreThreshold: scoreAggregation == SemanticRoutingChatClient.ScoreAggregation.Sum ? 1.5f : 0.3f,
            topK: 3,
            scoreAggregation: scoreAggregation,
            leaveOpen: true);

        ChatResponse response = await router.GetResponseAsync([new(ChatRole.User, "query")]);

        Assert.Equal(expectedResponse, response.Text);
    }

    [Fact]
    public async Task SemanticRouting_UsesDefaultBelowThreshold()
    {
        var vectors = new Dictionary<string, float[]>
        {
            ["code profile"] = [1, 0],
            ["unrelated query"] = [0, 1],
        };
        using var generator = new TestEmbeddingGenerator
        {
            GenerateAsyncCallback = (values, _, _) =>
                Task.FromResult(new GeneratedEmbeddings<Embedding<float>>(
                    [.. values.Select(input => new Embedding<float>(vectors[input]))])),
        };
        using var profiled = new TestChatClient
        {
            GetResponseAsyncCallback = (_, _, _) =>
                Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, "profiled"))),
        };
        ChatResponse expected = new(new ChatMessage(ChatRole.Assistant, "default"));
        using var defaultClient = new TestChatClient
        {
            GetResponseAsyncCallback = (_, _, _) => Task.FromResult(expected),
        };
        var profiles = new Dictionary<IChatClient, IReadOnlyList<string>>(ChatClientReferenceComparer.Instance)
        {
            [profiled] = ["code profile"],
        };
        using var router = new SemanticRoutingChatClient(
            generator,
            profiles,
            defaultClient,
            scoreThreshold: 0.5f,
            leaveOpen: true);

        ChatResponse response =
            await router.GetResponseAsync([new(ChatRole.User, "unrelated query")]);

        Assert.Same(expected, response);
    }

    [Fact]
    public void SemanticRouting_DisposesOwnedResourcesOnce()
    {
#pragma warning disable CA2000 // Dispose objects before losing scope
        var generator = new CountingEmbeddingGenerator();
        var profiled = new CountingDisposeClient();
        var defaultClient = new CountingDisposeClient();
        var profiles = new Dictionary<IChatClient, IReadOnlyList<string>>(ChatClientReferenceComparer.Instance)
        {
            [profiled] = ["profile"],
        };
        var router = new SemanticRoutingChatClient(generator, profiles, defaultClient);
#pragma warning restore CA2000

        router.Dispose();
        router.Dispose();

        Assert.Equal(1, generator.DisposeCount);
        Assert.Equal(1, profiled.DisposeCount);
        Assert.Equal(1, defaultClient.DisposeCount);
    }

    [Fact]
    public void OrderedFailover_RejectsMissingClients()
    {
        using var inner = new TestChatClient();

        Assert.Throws<ArgumentNullException>(() => new OrderedFailoverChatClient(null!));
        Assert.Throws<ArgumentException>(() => new OrderedFailoverChatClient([]));
        Assert.Throws<ArgumentException>(() => new OrderedFailoverChatClient([inner, null!]));
        Assert.Throws<ArgumentException>(() => new OrderedFailoverChatClient([inner, inner]));
    }

    [Fact]
    public async Task OrderedFailover_TriesClientsInOrderAndSkipsFailures()
    {
        var calls = new List<string>();
        using var first = new TestChatClient
        {
            GetResponseAsyncCallback = (_, _, _) =>
            {
                calls.Add("first");
                throw new InvalidOperationException("first failed");
            },
        };
        ChatResponse expected = new(new ChatMessage(ChatRole.Assistant, "ok"));
        using var second = new TestChatClient
        {
            GetResponseAsyncCallback = (_, _, _) =>
            {
                calls.Add("second");
                return Task.FromResult(expected);
            },
        };
        using var client = new OrderedFailoverChatClient([first, second]);

        ChatResponse response = await client.GetResponseAsync([new(ChatRole.User, "hi")]);

        Assert.Same(expected, response);
        Assert.Equal(["first", "second"], calls);
    }

    [Fact]
    public async Task OrderedFailover_DistinguishesValueEqualClients()
    {
        ChatResponse expected = new(new ChatMessage(ChatRole.Assistant, "ok"));
        using var first = new ValueEqualChatClient(
            () => throw new InvalidOperationException("failed"));
        using var second = new ValueEqualChatClient(
            () => Task.FromResult(expected));
        using var client = new OrderedFailoverChatClient([first, second]);

        ChatResponse response = await client.GetResponseAsync([new(ChatRole.User, "hi")]);

        Assert.Same(expected, response);
    }

    [Fact]
    public async Task OrderedFailover_ExhaustionRethrowsLastFailure()
    {
        using var first = new TestChatClient
        {
            GetResponseAsyncCallback = (_, _, _) => throw new InvalidOperationException("first"),
        };
        using var second = new TestChatClient
        {
            GetResponseAsyncCallback = (_, _, _) => throw new InvalidOperationException("second"),
        };
        using var client = new OrderedFailoverChatClient([first, second]);

        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => client.GetResponseAsync([new(ChatRole.User, "hi")]));

        Assert.Equal("second", exception.Message);
    }

    [Fact]
    public async Task OrderedFailover_StateIsScopedToOneRequest()
    {
        int firstCalls = 0;
        using var first = new TestChatClient
        {
            GetResponseAsyncCallback = (_, _, _) =>
            {
                firstCalls++;
                throw new InvalidOperationException("failed");
            },
        };
        using var second = new TestChatClient
        {
            GetResponseAsyncCallback = (_, _, _) => Task.FromResult(new ChatResponse()),
        };
        using var client = new OrderedFailoverChatClient([first, second]);

        _ = await client.GetResponseAsync([new(ChatRole.User, "one")]);
        _ = await client.GetResponseAsync([new(ChatRole.User, "two")]);

        Assert.Equal(2, firstCalls);
    }

    [Fact]
    public async Task OrderedFailover_ConcurrentRequestsHaveIndependentState()
    {
        const int RequestCount = 10;
        int firstCalls = 0;
        int secondCalls = 0;
        var allStarted = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var first = new TestChatClient
        {
            GetResponseAsyncCallback = async (_, _, _) =>
            {
                if (Interlocked.Increment(ref firstCalls) == RequestCount)
                {
                    allStarted.SetResult(true);
                }

                await allStarted.Task;
                throw new InvalidOperationException("failed");
            },
        };
        using var second = new TestChatClient
        {
            GetResponseAsyncCallback = (_, _, _) =>
            {
                Interlocked.Increment(ref secondCalls);
                return Task.FromResult(new ChatResponse());
            },
        };
        using var client = new OrderedFailoverChatClient([first, second]);

        Task<ChatResponse>[] requests =
        [
            .. Enumerable.Range(0, RequestCount).Select(index =>
                client.GetResponseAsync([new(ChatRole.User, index.ToString())])),
        ];
        _ = await Task.WhenAll(requests);

        Assert.Equal(RequestCount, firstCalls);
        Assert.Equal(RequestCount, secondCalls);
    }

    [Fact]
    public async Task OrderedFailover_DoesNotRetainStateWhileStreaming()
    {
        using var first = new TestChatClient
        {
            GetStreamingResponseAsyncCallback = (_, _, _) => ThrowingStream("failed"),
        };
        using var second = new TestChatClient
        {
            GetStreamingResponseAsyncCallback = (_, _, _) => YieldUpdates("first", "second"),
        };
        using var client = new OrderedFailoverChatClient([first, second]);
        await using IAsyncEnumerator<ChatResponseUpdate> enumerator =
            client.GetStreamingResponseAsync([new(ChatRole.User, "hi")]).GetAsyncEnumerator();

        Assert.True(await enumerator.MoveNextAsync());
        Assert.Equal("first", enumerator.Current.Text);
        Assert.Equal(0, GetOrderedFailoverRequestStateCount(client));
    }

    [Fact]
    public async Task OrderedFailover_SnapshotsConfiguredClients()
    {
        ChatResponse expected = new(new ChatMessage(ChatRole.Assistant, "ok"));
        using var inner = new TestChatClient
        {
            GetResponseAsyncCallback = (_, _, _) => Task.FromResult(expected),
        };
        var clients = new List<IChatClient> { inner };
        using var failover = new OrderedFailoverChatClient(clients, leaveOpen: true);
        clients.Clear();

        ChatResponse response = await failover.GetResponseAsync([new(ChatRole.User, "hi")]);

        Assert.Same(expected, response);
    }

    [Fact]
    public async Task OrderedFailover_UsesEachConfiguredClient()
    {
        var seenOptions = new List<(string? modelId, string? instructions)>();
        using var firstInner = new TestChatClient
        {
            GetResponseAsyncCallback = (_, options, _) =>
            {
                seenOptions.Add((options?.ModelId, options?.Instructions));
                throw new InvalidOperationException("failed");
            },
        };
        using var first = new ConfigureOptionsChatClient(
            firstInner,
            options => options.ModelId = "first");
        using var secondInner = new TestChatClient
        {
            GetResponseAsyncCallback = (_, options, _) =>
            {
                seenOptions.Add((options?.ModelId, options?.Instructions));
                return Task.FromResult(new ChatResponse());
            },
        };
        using var second = new ConfigureOptionsChatClient(
            secondInner,
            options => options.ModelId = "second");
        using var client = new OrderedFailoverChatClient(
            [first, second],
            leaveOpen: true);

        _ = await client.GetResponseAsync(
            [new(ChatRole.User, "hi")],
            new ChatOptions
            {
                Instructions = "caller",
                ModelId = "request",
            });

        Assert.Equal(
            [("first", "caller"), ("second", "caller")],
            seenOptions);
    }

    [Fact]
    public async Task OrderedFailover_StreamingFallsBackBeforeFirstUpdate()
    {
        using var first = new TestChatClient
        {
            GetStreamingResponseAsyncCallback = (_, _, _) => ThrowingStream("failed"),
        };
        using var second = new TestChatClient
        {
            GetStreamingResponseAsyncCallback = (_, _, _) => YieldUpdates("ok"),
        };
        using var client = new OrderedFailoverChatClient([first, second]);

        List<ChatResponseUpdate> updates =
            await CollectAsync(client.GetStreamingResponseAsync([new(ChatRole.User, "hi")]));

        Assert.Equal("ok", Assert.Single(updates).Text);
    }

    [Fact]
    public async Task OrderedFailover_CancellationDoesNotFallback()
    {
        using var cancellationSource = new CancellationTokenSource();
        cancellationSource.Cancel();
        bool secondCalled = false;
        using var first = new TestChatClient
        {
            GetResponseAsyncCallback = (_, _, _) => throw new InvalidOperationException("failed"),
        };
        using var second = new TestChatClient
        {
            GetResponseAsyncCallback = (_, _, _) =>
            {
                secondCalled = true;
                return Task.FromResult(new ChatResponse());
            },
        };
        using var client = new OrderedFailoverChatClient([first, second]);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => client.GetResponseAsync(
                [new(ChatRole.User, "hi")],
                cancellationToken: cancellationSource.Token));

        Assert.False(secondCalled);
    }

    [Fact]
    public void OrderedFailover_DisposesEachClientOnce()
    {
#pragma warning disable CA2000 // Dispose objects before losing scope
        var shared = new CountingDisposeClient();
        var other = new CountingDisposeClient();
        var client = new OrderedFailoverChatClient([shared, other]);
#pragma warning restore CA2000

        client.Dispose();
        client.Dispose();

        Assert.Equal(1, shared.DisposeCount);
        Assert.Equal(1, other.DisposeCount);
    }

    [Fact]
    public void OrderedFailover_LeaveOpenDoesNotDisposeInnerClients()
    {
#pragma warning disable CA2000 // Dispose objects before losing scope
        var inner = new CountingDisposeClient();
        var client = new OrderedFailoverChatClient([inner], leaveOpen: true);
#pragma warning restore CA2000

        client.Dispose();

        Assert.Equal(0, inner.DisposeCount);
        inner.Dispose();
    }

    [Fact]
    public async Task NestedRouters_ReturnLeafResponse()
    {
        ChatResponse expected = new(new ChatMessage(ChatRole.Assistant, "ok"));
        using var leaf = new TestChatClient
        {
            GetResponseAsyncCallback = (_, _, _) => Task.FromResult(expected),
        };
        using var inner = new OrderedFailoverChatClient([leaf]);
        using var outer = new OrderedFailoverChatClient([inner]);

        ChatResponse response = await outer.GetResponseAsync([new(ChatRole.User, "hi")]);

        Assert.Same(expected, response);
    }

    private static async Task<List<ChatResponseUpdate>> CollectAsync(
        IAsyncEnumerable<ChatResponseUpdate> updates)
    {
        var result = new List<ChatResponseUpdate>();
        await foreach (ChatResponseUpdate update in updates)
        {
            result.Add(update);
        }

        return result;
    }

    private static async Task ConsumeOneAsync(IAsyncEnumerable<ChatResponseUpdate> updates)
    {
        await using IAsyncEnumerator<ChatResponseUpdate> enumerator = updates.GetAsyncEnumerator();
        Assert.True(await enumerator.MoveNextAsync());
    }

    private static int GetOrderedFailoverRequestStateCount(OrderedFailoverChatClient client)
    {
        object requestStates = typeof(OrderedFailoverChatClient)
            .GetField("_requestStates", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
            .GetValue(client)!;
        return (int)requestStates.GetType().GetProperty("Count")!.GetValue(requestStates)!;
    }

    private static async IAsyncEnumerable<ChatResponseUpdate> YieldUpdates(params string[] texts)
    {
        foreach (string text in texts)
        {
            await Task.Yield();
            yield return new ChatResponseUpdate(ChatRole.Assistant, text);
        }
    }

    private static async IAsyncEnumerable<ChatResponseUpdate> ThrowingStream(string message)
    {
        await Task.Yield();
        foreach (int _ in Array.Empty<int>())
        {
            yield return new ChatResponseUpdate(ChatRole.Assistant, "never");
        }

        throw new InvalidOperationException(message);
    }

    private static async IAsyncEnumerable<ChatResponseUpdate> CanceledStream(
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await Task.Yield();
        foreach (int _ in Array.Empty<int>())
        {
            yield return new ChatResponseUpdate(ChatRole.Assistant, "never");
        }

        throw new OperationCanceledException(cancellationToken);
    }

    private sealed class DelegatingTestRouter : RoutingChatClient
    {
        private readonly Func<RoutingContext, IChatClient> _select;

        public DelegatingTestRouter(Func<RoutingContext, IChatClient> select)
        {
            _select = select;
        }

        protected override ValueTask<IChatClient> SelectClientAsync(
            RoutingContext context,
            CancellationToken cancellationToken) =>
            new(_select(context));
    }

    private sealed class DelegatingFailoverTestRouter : FailoverChatClient
    {
        private readonly Func<RoutingContext, IChatClient> _select;
        private readonly Action<RoutingContext, FailoverChatClientAttempt, bool>? _onRoutingUpdate;

        public DelegatingFailoverTestRouter(
            Func<RoutingContext, IChatClient> select,
            Action<RoutingContext, FailoverChatClientAttempt, bool>? onRoutingUpdate = null)
        {
            _select = select;
            _onRoutingUpdate = onRoutingUpdate;
        }

        protected override ValueTask<IChatClient> SelectClientAsync(
            RoutingContext context,
            CancellationToken cancellationToken) =>
            new(_select(context));

        protected override ValueTask OnRoutingUpdateAsync(
            RoutingContext context,
            FailoverChatClientAttempt attempt,
            bool isTerminal,
            CancellationToken cancellationToken)
        {
            _onRoutingUpdate?.Invoke(context, attempt, isTerminal);
            return default;
        }
    }

    private sealed class ThrowingGetAsyncEnumeratorEnumerable : IAsyncEnumerable<ChatResponseUpdate>
    {
        private readonly Exception _exception;

        public ThrowingGetAsyncEnumeratorEnumerable(Exception exception)
        {
            _exception = exception;
        }

        public IAsyncEnumerator<ChatResponseUpdate> GetAsyncEnumerator(
            CancellationToken cancellationToken = default) =>
            throw _exception;
    }

    private sealed class ThrowingCurrentAsyncEnumerable(Exception exception) : IAsyncEnumerable<ChatResponseUpdate>
    {
        public int DisposeCount { get; private set; }

        public IAsyncEnumerator<ChatResponseUpdate> GetAsyncEnumerator(
            CancellationToken cancellationToken = default) =>
            new Enumerator(this, exception);

        private sealed class Enumerator(
            ThrowingCurrentAsyncEnumerable owner,
            Exception exception) : IAsyncEnumerator<ChatResponseUpdate>
        {
            public ChatResponseUpdate Current => throw exception;

            public ValueTask DisposeAsync()
            {
                owner.DisposeCount++;
                return default;
            }

            public ValueTask<bool> MoveNextAsync() => new(true);
        }
    }

    private sealed class TrackingAsyncEnumerable : IAsyncEnumerable<ChatResponseUpdate>
    {
        private readonly IReadOnlyList<ChatResponseUpdate> _updates;
        private readonly int? _throwOnMove;
        private readonly Exception? _exception;
        private readonly Exception? _disposeException;

        public TrackingAsyncEnumerable(
            IReadOnlyList<ChatResponseUpdate> updates,
            int? throwOnMove = null,
            Exception? exception = null,
            Exception? disposeException = null)
        {
            _updates = updates;
            _throwOnMove = throwOnMove;
            _exception = exception;
            _disposeException = disposeException;
        }

        public int DisposeCount { get; private set; }

        public IAsyncEnumerator<ChatResponseUpdate> GetAsyncEnumerator(
            CancellationToken cancellationToken = default) =>
            new Enumerator(this);

        private sealed class Enumerator : IAsyncEnumerator<ChatResponseUpdate>
        {
            private readonly TrackingAsyncEnumerable _owner;
            private int _moveCount;

            public Enumerator(TrackingAsyncEnumerable owner)
            {
                _owner = owner;
            }

            public ChatResponseUpdate Current { get; private set; } = null!;

            public ValueTask<bool> MoveNextAsync()
            {
                _moveCount++;
                if (_moveCount == _owner._throwOnMove)
                {
                    throw _owner._exception!;
                }

                int index = _moveCount - 1;
                if (index >= _owner._updates.Count)
                {
                    return new(false);
                }

                Current = _owner._updates[index];
                return new(true);
            }

            public ValueTask DisposeAsync()
            {
                _owner.DisposeCount++;
                if (_owner._disposeException is { } exception)
                {
                    throw exception;
                }

                return default;
            }
        }
    }

    private sealed class CountingDisposeClient : IChatClient
    {
        public int DisposeCount { get; private set; }

        public Task<ChatResponse> GetResponseAsync(
            IEnumerable<ChatMessage> messages,
            ChatOptions? options = null,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new ChatResponse());

        public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
            IEnumerable<ChatMessage> messages,
            ChatOptions? options = null,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public object? GetService(Type serviceType, object? serviceKey = null) => null;

        public void Dispose() => DisposeCount++;

        public override bool Equals(object? obj) => obj is CountingDisposeClient;

        public override int GetHashCode() => 0;
    }

    private sealed class CountingEmbeddingGenerator :
        IEmbeddingGenerator<string, Embedding<float>>
    {
        public int DisposeCount { get; private set; }

        public Task<GeneratedEmbeddings<Embedding<float>>> GenerateAsync(
            IEnumerable<string> values,
            EmbeddingGenerationOptions? options = null,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public object? GetService(Type serviceType, object? serviceKey = null) => null;

        public void Dispose() => DisposeCount++;
    }

    private sealed class ChatClientReferenceComparer : IEqualityComparer<IChatClient>
    {
        public static ChatClientReferenceComparer Instance { get; } = new();

        public bool Equals(IChatClient? x, IChatClient? y) => ReferenceEquals(x, y);

        public int GetHashCode(IChatClient obj) => RuntimeHelpers.GetHashCode(obj);
    }

    private sealed class ValueEqualChatClient(Func<Task<ChatResponse>> getResponse) : IChatClient
    {
        public Task<ChatResponse> GetResponseAsync(
            IEnumerable<ChatMessage> messages,
            ChatOptions? options = null,
            CancellationToken cancellationToken = default) =>
            getResponse();

        public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
            IEnumerable<ChatMessage> messages,
            ChatOptions? options = null,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public object? GetService(Type serviceType, object? serviceKey = null) => null;

        public void Dispose()
        {
        }

        public override bool Equals(object? obj) => obj is ValueEqualChatClient;

        public override int GetHashCode() => 0;
    }
}
