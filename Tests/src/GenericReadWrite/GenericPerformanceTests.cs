using Xunit;
using Xunit.Abstractions;
using ModbusLib.Models;
using ModbusLib.Enums;
using System.Diagnostics;

namespace ModbusLib.Tests.GenericReadWrite
{
    /// <summary>
    /// 泛型功能性能基准测试
    /// </summary>
    public class GenericPerformanceTests
    {
        private readonly ITestOutputHelper _output;
        
        public GenericPerformanceTests(ITestOutputHelper output)
        {
            _output = output;
        }
        
        [Fact]
        public void ModbusDataConverter_LargeByteArray_Performance()
        {
            // Arrange
            const int arraySize = 10000;
            var data = new byte[arraySize];
            for (int i = 0; i < arraySize; i++)
            {
                data[i] = (byte)(i % 256);
            }
            
            // Act & Measure
            var stopwatch = Stopwatch.StartNew();
            
            for (int iteration = 0; iteration < 100; iteration++)
            {
                var bytes = ModbusDataConverter.ToBytes(data, ModbusEndianness.BigEndian);
                var result = ModbusDataConverter.FromBytes<byte>(bytes, arraySize, ModbusEndianness.BigEndian);
            }
            
            stopwatch.Stop();
            
            // Assert & Report
            _output.WriteLine($"Large byte array (size: {arraySize}, iterations: 100) took {stopwatch.ElapsedMilliseconds}ms");
            _output.WriteLine($"Average per operation: {stopwatch.ElapsedMilliseconds / 100.0}ms");
            
            // 性能断言：100次操作应该在合理时间内完成（比如5秒以内）
            Assert.True(stopwatch.ElapsedMilliseconds < 5000, 
                $"Performance test took too long: {stopwatch.ElapsedMilliseconds}ms");
        }
        
        [Fact]
        public void ModbusDataConverter_IntArray_Performance()
        {
            // Arrange
            const int arraySize = 2500; // 2500个int = 10000字节
            var data = new int[arraySize];
            for (int i = 0; i < arraySize; i++)
            {
                data[i] = i * 12345;
            }
            
            // Act & Measure
            var stopwatch = Stopwatch.StartNew();
            
            for (int iteration = 0; iteration < 100; iteration++)
            {
                var bytes = ModbusDataConverter.ToBytes(data, ModbusEndianness.BigEndian);
                var result = ModbusDataConverter.FromBytes<int>(bytes, arraySize, ModbusEndianness.BigEndian);
            }
            
            stopwatch.Stop();
            
            // Assert & Report
            _output.WriteLine($"Int array (size: {arraySize}, iterations: 100) took {stopwatch.ElapsedMilliseconds}ms");
            _output.WriteLine($"Average per operation: {stopwatch.ElapsedMilliseconds / 100.0}ms");
            
            Assert.True(stopwatch.ElapsedMilliseconds < 5000, 
                $"Performance test took too long: {stopwatch.ElapsedMilliseconds}ms");
        }
        
        [Fact]
        public void ModbusDataConverter_FloatArray_Performance()
        {
            // Arrange  
            const int arraySize = 2500; // 2500个float = 10000字节
            var data = new float[arraySize];
            for (int i = 0; i < arraySize; i++)
            {
                data[i] = i * 3.14159f;
            }
            
            // Act & Measure
            var stopwatch = Stopwatch.StartNew();
            
            for (int iteration = 0; iteration < 100; iteration++)
            {
                var bytes = ModbusDataConverter.ToBytes(data, ModbusEndianness.BigEndian);
                var result = ModbusDataConverter.FromBytes<float>(bytes, arraySize, ModbusEndianness.BigEndian);
            }
            
            stopwatch.Stop();
            
            // Assert & Report
            _output.WriteLine($"Float array (size: {arraySize}, iterations: 100) took {stopwatch.ElapsedMilliseconds}ms");
            _output.WriteLine($"Average per operation: {stopwatch.ElapsedMilliseconds / 100.0}ms");
            
            Assert.True(stopwatch.ElapsedMilliseconds < 5000, 
                $"Performance test took too long: {stopwatch.ElapsedMilliseconds}ms");
        }
        
        [Theory]
        [InlineData(ModbusEndianness.BigEndian)]
        [InlineData(ModbusEndianness.LittleEndian)]
        [InlineData(ModbusEndianness.MidLittleEndian)]
        public void ModbusDataConverter_DifferentEndianness_Performance(ModbusEndianness endianness)
        {
            // Arrange
            const int arraySize = 1000;
            var data = new int[arraySize];
            for (int i = 0; i < arraySize; i++)
            {
                data[i] = i * 54321;
            }
            
            // Act & Measure
            var stopwatch = Stopwatch.StartNew();
            
            for (int iteration = 0; iteration < 50; iteration++)
            {
                var bytes = ModbusDataConverter.ToBytes(data, endianness);
                var result = ModbusDataConverter.FromBytes<int>(bytes, arraySize, endianness);
            }
            
            stopwatch.Stop();
            
            // Assert & Report
            _output.WriteLine($"Endianness {endianness} (size: {arraySize}, iterations: 50) took {stopwatch.ElapsedMilliseconds}ms");
            _output.WriteLine($"Average per operation: {stopwatch.ElapsedMilliseconds / 50.0}ms");
            
            Assert.True(stopwatch.ElapsedMilliseconds < 3000, 
                $"Endianness performance test took too long: {stopwatch.ElapsedMilliseconds}ms");
        }
        
        [Fact]
        public void ModbusSpanExtensions_BigEndian_Performance()
        {
            // Arrange
            const int bufferSize = 1000;
            var buffer = new ushort[bufferSize];
            var span = buffer.AsSpan();
            
            // Act & Measure
            var stopwatch = Stopwatch.StartNew();
            
            for (int iteration = 0; iteration < 1000; iteration++)
            {
                for (int i = 0; i < bufferSize - 1; i += 2)
                {
                    span.SetBigEndian<int>(i, iteration * i);
                    var value = span.GetBigEndian<int>(i);
                }
            }
            
            stopwatch.Stop();
            
            // Assert & Report
            _output.WriteLine($"Span BigEndian operations (buffer: {bufferSize}, iterations: 1000) took {stopwatch.ElapsedMilliseconds}ms");
            _output.WriteLine($"Average per operation: {stopwatch.ElapsedMilliseconds / 1000.0}ms");
            
            Assert.True(stopwatch.ElapsedMilliseconds < 2000, 
                $"Span operations took too long: {stopwatch.ElapsedMilliseconds}ms");
        }
        
        [Fact]
        public void RegisterCountCalculation_Performance()
        {
            // Act & Measure
            var stopwatch = Stopwatch.StartNew();
            
            for (int iteration = 0; iteration < 100000; iteration++)
            {
                var byteCount = ModbusDataConverter.GetRegisterCount<byte>();
                var ushortCount = ModbusDataConverter.GetRegisterCount<ushort>();
                var intCount = ModbusDataConverter.GetRegisterCount<int>();
                var floatCount = ModbusDataConverter.GetRegisterCount<float>();
                var doubleCount = ModbusDataConverter.GetRegisterCount<double>();
                
                var totalByteCount = ModbusDataConverter.GetTotalRegisterCount<byte>(100);
                var totalIntCount = ModbusDataConverter.GetTotalRegisterCount<int>(25);
            }
            
            stopwatch.Stop();
            
            // Assert & Report
            _output.WriteLine($"Register count calculations (iterations: 100000) took {stopwatch.ElapsedMilliseconds}ms");
            _output.WriteLine($"Average per operation: {stopwatch.ElapsedMilliseconds / 100000.0}ms");
            
            Assert.True(stopwatch.ElapsedMilliseconds < 1000, 
                $"Register count calculations took too long: {stopwatch.ElapsedMilliseconds}ms");
        }
    }
}