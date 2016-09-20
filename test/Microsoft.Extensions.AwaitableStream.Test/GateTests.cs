using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.AwaitableStream.Internal;
using Xunit;

namespace Microsoft.Extensions.AwaitableStream.Test
{
    /// <summary>
    /// Tests for the <see cref="Gate"/> type. Although it is internal, it is a critical
    /// synchronization primitive so we want to know it works :).
    /// </summary>
    public class GateTests
    {
        [Fact]
        public async Task ItBlocksOneTaskUntilTheGateIsReleased()
        {
            // Use TCS for more "traditional" synchronization that is already known to work :).
            var initTcs = new TaskCompletionSource<object>();

            // The gate we're testing
            var gate = new Gate();

            // Set up the producer thread.
            var producer = Task.Run(async () =>
            {
                // We're ready, task initialization has finished, etc.
                initTcs.SetResult(null);

                // Wait for the gate to release
                await gate;
            });

            // Wait for the produce to start
            await initTcs.Task;

            // Open the gate
            gate.Open();

            // Wait for the producer to finish, with a 200ms timeout
            var finished = await Task.WhenAny(producer, Task.Delay(200));

            // Assert.True/False are the only one that take a custom message :(
            Assert.True(ReferenceEquals(finished, producer), "Timeout elapsed!");
        }

        [Fact]
        public async Task ItImmediatelyClosesAfterOpening()
        {
            // Use TCS for more "traditional" synchronization that is already known to work :).
            var initTcs = new TaskCompletionSource<object>();
            var secondTcs = new TaskCompletionSource<object>();
            var cts = new CancellationTokenSource();

            // The gate we're testing
            var gate = new Gate();

            // Set up the producer thread.
            var producer = Task.Run(async () =>
            {
                // We're ready, task initialization has finished, etc.
                initTcs.SetResult(null);

                // Wait for the gate to release
                await gate;
                Assert.False(gate.IsCompleted, "The gate did not close immediately after opening!");

                // Signal that the gate has closed again
                secondTcs.SetResult(null);

                // Wait for a second release (it's never going to happen!)
                await gate;
            }, cts.Token);

            // Wait for the produce to start
            await initTcs.Task;

            // Open the gate
            gate.Open();

            // Wait for the gate to close again
            // (without this wait, we don't know if the gate will actually have closed again! the produce may not have run yet)
            var finished = await Task.WhenAny(secondTcs.Task, Task.Delay(200));

            // Assert.True/False are the only one that take a custom message :(
            Assert.True(ReferenceEquals(finished, secondTcs.Task), "Timeout elapsed!");

            // Open the gate
            gate.Open();

            // Wait for the producer to finish, with a 200ms timeout
            finished = await Task.WhenAny(producer, Task.Delay(200));

            // Assert.True/False are the only one that take a custom message :(
            Assert.True(ReferenceEquals(finished, producer), "Timeout elapsed!");
        }
    }
}