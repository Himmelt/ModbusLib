using FluentModbus;
using System.Net;
using System.Net.Sockets;

namespace ModbusLib.Tests.FluentServer;

/// <summary>
/// Wraps an <see cref="ITcpClientProvider"/> so that the accept loop of a
/// FluentModbus server never produces an unobserved task exception when the
/// server is stopped. FluentModbus starts the accept loop with
/// <c>Task.Run(...)</c> and never awaits it; when the listener is disposed,
/// the pending <c>AcceptTcpClientAsync</c> faults (e.g. SocketException 995 on
/// Windows), leaving a faulted task that nobody observes. This wrapper observes
/// that exception and returns a task that stays pending, so the accept loop
/// simply exits via its cancellation token.
/// </summary>
public sealed class NonFaultingTcpClientProvider : ITcpClientProvider
{
    private readonly ITcpClientProvider _inner;
    private readonly object _lock = new();
    private bool _stopped;

    public NonFaultingTcpClientProvider(IPEndPoint endPoint)
        : this(new DefaultTcpClientProvider(endPoint))
    {
    }

    public NonFaultingTcpClientProvider(ITcpClientProvider inner)
    {
        _inner = inner;
    }

    public Task<TcpClient> AcceptTcpClientAsync()
    {
        lock (_lock)
        {
            if (_stopped)
            {
                // After stop, return a task that never completes and never faults.
                return new TaskCompletionSource<TcpClient>(TaskCreationOptions.RunContinuationsAsynchronously).Task;
            }

            var tcs = new TaskCompletionSource<TcpClient>(TaskCreationOptions.RunContinuationsAsynchronously);

            _inner.AcceptTcpClientAsync().ContinueWith(innerTask =>
            {
                // Observe the exception (e.g. SocketException 995) so it is never
                // reported as an unobserved task exception.
                _ = innerTask.Exception;

                if (innerTask.IsCompletedSuccessfully)
                    tcs.TrySetResult(innerTask.Result);

                // On fault/cancellation the TCS intentionally stays pending: the
                // server is being shut down and the loop exits via its token.
            }, TaskContinuationOptions.ExecuteSynchronously);

            return tcs.Task;
        }
    }

    public void Dispose()
    {
        lock (_lock)
        {
            _stopped = true;
            _inner.Dispose();
        }
    }
}
