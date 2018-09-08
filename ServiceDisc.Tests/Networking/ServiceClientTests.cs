using System;
using System.IO;
using System.Text;
using ServiceDisc.Networking;
using ServiceDisc.Networking.ServiceDiscConnection;
using Xunit;

namespace ServiceDisc.Tests.Networking
{
    public abstract class ServiceClientTests
    {
        [Theory]
        [InlineData("123abc\nA\0BC\tok\r\na\n\rb")]
        [InlineData("")]
        [InlineData(null)]
        [InlineData(@"test#¤€""'\TEST10´!a{0}/")]
        public void EchoStrings(string input)
        {
            var echoService = CreateEchoService();

            var result = echoService.EchoString(input);
            Assert.Equal(input, result);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(long.MaxValue)]
        [InlineData(long.MinValue)]
        public void EchoLongNumbers(long input)
        {
            var echoService = CreateEchoService();

            var result = echoService.EchoLong(input);
            Assert.Equal(input, result);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(ulong.MaxValue)]
        [InlineData(ulong.MinValue)]
        public void EchoUlongNumbers(ulong input)
        {
            var echoService = CreateEchoService();

            var result = echoService.EchoUlong(input);
            Assert.Equal(input, result);
        }

        [Theory]
        [InlineData(0f)]
        [InlineData(1f)]
        [InlineData(1.2f)]
        [InlineData(float.Epsilon)]
        [InlineData(float.NaN)]
        [InlineData(float.NegativeInfinity)]
        [InlineData(float.PositiveInfinity)]
        [InlineData(float.MaxValue/2 - float.Epsilon)]
        [InlineData(float.MaxValue)]
        [InlineData(float.MinValue)]
        public void EchoFloatNumbers(float input)
        {
            var echoService = CreateEchoService();

            var result = echoService.EchoFloat(input);
            Assert.Equal(input, result);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(1.2)]
        [InlineData(double.Epsilon)]
        [InlineData(double.NaN)]
        [InlineData(double.NegativeInfinity)]
        [InlineData(double.PositiveInfinity)]
        [InlineData(double.MaxValue/2 - double.Epsilon)]
        [InlineData(double.MaxValue)]
        [InlineData(double.MinValue)]
        public void EchoDoubleNumbers(double input)
        {
            var echoService = CreateEchoService();

            var result = echoService.EchoDouble(input);
            Assert.Equal(input, result);
        }

        [Fact]
        public void EchoDateTime()
        {
            var dateTime = new DateTime(2018, 9, 4);
            var echoService = CreateEchoService();

            var result = echoService.EchoDateTime(dateTime);
            Assert.Equal(dateTime, result);
        }

        [Fact]
        public void EchoDefaultDateTime()
        {
            var dateTime = default(DateTime);
            var echoService = CreateEchoService();

            var result = echoService.EchoDateTime(dateTime);
            Assert.Equal(dateTime, result);
        }

        [Fact]
        public void SendNullStream()
        {
            var streamService = CreateStreamService();

            var bytes = streamService.SendStream(null);

            Assert.Null(bytes);
        }

        [Fact]
        public void SendShortStream()
        {
            var streamService = CreateStreamService();
            var testBytes = Encoding.UTF8.GetBytes("Hello");

            var memoryStream = new MemoryStream(testBytes);
            var result = streamService.SendStream(memoryStream);

            Assert.Equal(testBytes, result);
        }

        [Fact]
        public void ReceiveShortStream()
        {
            var streamService = CreateStreamService();
            var testBytes = Encoding.UTF8.GetBytes("Hello");

            var stream = streamService.GetStream(testBytes);
            var memoryStream = new MemoryStream();
            stream.CopyTo(memoryStream);
            var result = memoryStream.ToArray();

            Assert.Equal(testBytes, result);
        }

        [Fact]
        public void SendAndReceiveShortStream()
        {
            var streamService = CreateStreamService();
            var testBytes = Encoding.UTF8.GetBytes("Hello");

            var memoryStream = new MemoryStream(testBytes);
            var echoStream = streamService.EchoStream(memoryStream);
            var resultStream = new MemoryStream();
            echoStream.CopyTo(resultStream);
            var result = resultStream.ToArray();

            Assert.Equal(testBytes, result);
        }

        [Fact]
        public void ReceiveNullStream()
        {
            var streamService = CreateStreamService();

            var stream = streamService.GetStream(null);

            Assert.Null(stream);
        }

        public IEchoService CreateEchoService()
        {
            var serviceDisc = new ServiceDiscClient(new InMemoryServiceDiscConnection()) { ServiceHostFactory = GetServiceHostFactory() };
            serviceDisc.HostAsync<IEchoService>(new EchoService()).GetAwaiter().GetResult();
            var service = serviceDisc.GetAsync<IEchoService>().GetAwaiter().GetResult();
            return service;
        }

        public IStreamService CreateStreamService()
        {
            var serviceDisc = new ServiceDiscClient(new InMemoryServiceDiscConnection()) { ServiceHostFactory = GetServiceHostFactory() };
            serviceDisc.HostAsync<IStreamService>(new StreamService()).GetAwaiter().GetResult();
            var service = serviceDisc.GetAsync<IStreamService>().GetAwaiter().GetResult();
            return service;
        }

        public abstract IServiceHostFactory GetServiceHostFactory();
    }

    public interface IEchoService
    {
        string EchoString(string input);
        long EchoLong(long input);
        ulong EchoUlong(ulong input);
        float EchoFloat(float input);
        double EchoDouble(double input);
        DateTime EchoDateTime(DateTime input);
    }

    public class EchoService : IEchoService
    {
        public string EchoString(string input)
        {
            return input;
        }

        public long EchoLong(long input)
        {
            return input;
        }

        public ulong EchoUlong(ulong input)
        {
            return input;
        }

        public float EchoFloat(float input)
        {
            return input;
        }

        public double EchoDouble(double input)
        {
            return input;
        }

        public DateTime EchoDateTime(DateTime input)
        {
            return input;
        }
    }

    public interface IStreamService
    {
        byte[] SendStream(Stream stream);
        Stream GetStream(byte[] bytes);
        Stream EchoStream(Stream stream);
    }

    public class StreamService : IStreamService
    {
        public byte[] SendStream(Stream stream)
        {
            if (stream == null) return null;
            var memoryStream = new MemoryStream();
            stream.CopyTo(memoryStream);
            return memoryStream.ToArray();
        }

        public Stream GetStream(byte[] bytes)
        {
            if (bytes == null) return null;
            return new MemoryStream(bytes);
        }

        public Stream EchoStream(Stream stream)
        {
            if (stream == null) return null;
            var memoryStream = new MemoryStream();
            stream.CopyTo(memoryStream);
            memoryStream.Position = 0;
            return memoryStream;
        }
    }
}