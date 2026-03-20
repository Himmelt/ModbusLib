using FluentModbus;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System.Net;
using System.Net.Sockets;

namespace ModbusLib.Tests.FluentServer;

public class ModbusRtuOverTcpServer : ModbusServer, IDisposable {
    #region Fields

    private readonly ILogger _logger;
    private bool _leaveOpen;
    private ITcpClientProvider? _tcpClientProvider;
    private readonly List<ModbusRtuOverTcpRequestHandler> _requestHandlers = [];
    private readonly object _lock = new();

    #endregion

    #region Constructors

    public ModbusRtuOverTcpServer(bool isAsynchronous = true, ILogger? logger = null) : base(isAsynchronous, logger ?? NullLogger.Instance) {
        _logger = logger ?? NullLogger.Instance;
    }

    #endregion

    #region Properties

    public TimeSpan ConnectionTimeout { get; set; } = TimeSpan.FromMinutes(1);

    public int MaxConnections { get; set; } = 0;

    public int ConnectionCount {
        get {
            lock (_lock) { return _requestHandlers.Count; }
        }
    }

    #endregion

    #region Methods

    public void Start() {
        Start(new IPEndPoint(IPAddress.Any, 502));
    }

    public void Start(IPAddress ipAddress) {
        Start(new IPEndPoint(ipAddress, 502));
    }

    public void Start(IPEndPoint localEndpoint) {
        Start(new DefaultTcpClientProvider(localEndpoint));
    }

    public void Start(ITcpClientProvider tcpClientProvider, bool leaveOpen = false) {
        _tcpClientProvider = tcpClientProvider;
        _leaveOpen = leaveOpen;

        base.StopProcessing();
        base.StartProcessing();

        _requestHandlers.Clear();

        Task.Run(AcceptLoopAsync, CTS!.Token);
        Task.Run(CleanupLoopAsync, CTS!.Token);
    }

    public void Start(TcpClient tcpClient) {
        base.StopProcessing();
        base.StartProcessing();

        _requestHandlers.Clear();
        _requestHandlers.Add(new ModbusRtuOverTcpRequestHandler(tcpClient, this, _logger));
    }

    public new void Stop() {
        base.StopProcessing();

        lock (_lock) {
            foreach (var handler in _requestHandlers.ToList()) {
                try { handler.Dispose(); } catch { }
            }
            _requestHandlers.Clear();
        }

        if (!_leaveOpen) {
            _tcpClientProvider?.Dispose();
            _tcpClientProvider = null;
        }
    }

    protected override void ProcessRequests() {
        lock (_lock) {
            foreach (var handler in _requestHandlers) {
                if (handler.IsReady) {
                    if (handler.Length > 0)
                        handler.WriteResponse();

                    if (!IsAsynchronous)
                        _ = handler.ReceiveRequestAsync();
                }
            }
        }
    }

    private async Task AcceptLoopAsync() {
        var token = CTS!.Token;
        try {
            while (!token.IsCancellationRequested) {
                var tcpClient = await _tcpClientProvider!.AcceptTcpClientAsync().ConfigureAwait(false);

                lock (_lock) {
                    if (MaxConnections > 0 && _requestHandlers.Count + 1 > MaxConnections) {
                        try { tcpClient.Close(); } catch { }
                        continue;
                    }

                    var handler = new ModbusRtuOverTcpRequestHandler(tcpClient, this, _logger);
                    _requestHandlers.Add(handler);
                    var clientText = _requestHandlers.Count == 1 ? "client is" : "clients are";
                    _logger.LogInformation($" {_requestHandlers.Count} {clientText} connected");
                }
            }
        } catch (OperationCanceledException) { } catch (Exception ex) {
            _logger.LogError(ex, "AcceptLoop error");
        }
    }

    private async Task CleanupLoopAsync() {
        var token = CTS!.Token;
        try {
            while (!token.IsCancellationRequested) {
                List<ModbusRtuOverTcpRequestHandler> toRemove = [];

                lock (_lock) {
                    foreach (var handler in _requestHandlers.ToList()) {
                        if (handler.CancellationToken.IsCancellationRequested ||
                            handler.LastRequest.Elapsed > ConnectionTimeout) {
                            toRemove.Add(handler);
                        }
                    }

                    foreach (var handler in toRemove) {
                        try {
                            _requestHandlers.Remove(handler);
                            handler.Dispose();
                            _logger.LogInformation($"Connection {handler.DisplayName} timed out or closed");
                        } catch { }
                    }

                    if (_requestHandlers.Count > 0) {
                        var clientText = _requestHandlers.Count == 1 ? "client is" : "clients are";
                        _logger.LogInformation($" {_requestHandlers.Count} {clientText} connected");
                    }
                }

                await Task.Delay(TimeSpan.FromSeconds(1), token).ConfigureAwait(false);
            }
        } catch (OperationCanceledException) { }
    }

    public new void Dispose() {
        Stop();
        GC.SuppressFinalize(this);
    }

    #endregion
}
