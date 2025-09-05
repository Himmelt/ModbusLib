using ModbusLib.Factories;
using ModbusLib.Models;
using System.IO.Ports;

namespace ModbusLib.Demo;

class Program {
    static async Task Main(string[] args) {
        Console.WriteLine("=== Modbus Client Library Demo ===");

        var clinet = ModbusClientFactory.CreateUdpClient("192.168.61.65", 10123);

        await clinet.ConnectAsync();

        for (int i = 0; i < 30; i++) {
            var v = await clinet.ReadHoldingRegistersAsync(1, 0x1e, 1);

            Console.WriteLine(v[0]);

            await Task.Delay(1000);
        }

        await clinet.DisconnectAsync();
    }
}
