using FluentModbus;
using Microsoft.Extensions.Logging;
using System.Net.Sockets;

namespace ModbusLib.Tests.FluentServer;

internal class ModbusRtuOverTcpRequestHandler : ModbusRequestHandler, IDisposable {
    #region Fields

    private readonly IModbusRtuSerialPort _serialPort;
    private readonly ILogger _logger;

    #endregion

    #region Constructors

    public ModbusRtuOverTcpRequestHandler(TcpClient tcpClient, ModbusRtuOverTcpServer rtuOverTcpServer, ILogger logger)
        : base(rtuOverTcpServer, 256) {
        _logger = logger;
        _serialPort = new NetworkStreamRtuSerialPort(tcpClient, () => LastRequest.Restart());
        _serialPort.Open();

        DisplayName = ((System.Net.IPEndPoint?)tcpClient.Client.RemoteEndPoint)?.ToString() ?? "tcp";

        base.Start();
    }

    #endregion

    #region Properties

    public override string DisplayName { get; }

    protected override bool IsResponseRequired => ModbusServer.UnitIdentifiers.Contains(UnitIdentifier);

    #endregion

    #region Methods

    internal override async Task ReceiveRequestAsync() {
        if (CancellationToken.IsCancellationRequested)
            return;

        IsReady = false;

        try {
            if (await TryReceiveRequestAsync()) {
                IsReady = true;

                if (ModbusServer.IsAsynchronous)
                    WriteResponse();
            }
        } catch (Exception ex) {
            _logger.LogDebug(ex, "The connection will be closed");

            CancelToken();
        }
    }

    protected override int WriteFrame(Action extendFrame) {
        int frameLength;
        ushort crc;

        FrameBuffer.Writer.Seek(0, SeekOrigin.Begin);

        FrameBuffer.Writer.Write(UnitIdentifier);

        extendFrame();

        frameLength = unchecked((int)FrameBuffer.Writer.BaseStream.Position);
        crc = ModbusUtils.CalculateCRC(FrameBuffer.Buffer.AsMemory(0, frameLength));
        FrameBuffer.Writer.Write(crc);

        return frameLength + 2;
    }

    protected override void OnResponseReady(int frameLength) {
        _serialPort.Write(FrameBuffer.Buffer, 0, frameLength);
    }

    private async Task<bool> TryReceiveRequestAsync() {
        Length = 0;

        try {
            while (true) {
                Length += await _serialPort.ReadAsync(FrameBuffer.Buffer, Length, FrameBuffer.Buffer.Length - Length, CancellationToken);

                if (ModbusUtils.DetectRequestFrame(255, FrameBuffer.Buffer.AsMemory(0, Length))) {
                    FrameBuffer.Reader.BaseStream.Seek(0, SeekOrigin.Begin);

                    UnitIdentifier = FrameBuffer.Reader.ReadByte();

                    break;
                } else {
                    if (Length == FrameBuffer.Buffer.Length)
                        Length = 0;
                }
            }
        } catch (TimeoutException) {
            return false;
        }

        if (ModbusServer.UnitIdentifiers.Contains(UnitIdentifier)) {
            LastRequest.Restart();
            return true;
        } else {
            return false;
        }
    }

    #endregion

    #region IDisposable Support

    private bool _disposedValue = false;

    protected override void Dispose(bool disposing) {
        if (!_disposedValue) {
            if (disposing)
                _serialPort.Close();

            _disposedValue = true;
        }

        base.Dispose(disposing);
    }

    #endregion
}