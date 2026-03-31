using ModbusLib.Interfaces;
using ModbusLib.Models;
using ModbusLib.Protocols;
using ModbusLib.Transports;

namespace ModbusLib.Clients {
    public class ModbusRtuClient(SerialConfig config) : ModbusClientBase {

        public SerialConfig SerialConfig => config;

        protected override IModbusProtocol Protocol { get; set; } = new RtuProtocol();
        protected override IModbusTransport Transport { get; set; } = new SerialTransport(config);

        public ModbusRtuClient(string portName, int baudRate = 9600) : this(new SerialConfig { PortName = portName, BaudRate = baudRate }) { }
    }
}