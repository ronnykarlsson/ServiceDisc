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

            var result = echoService.Send(input);
            Assert.Equal(input, result);
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
        string Send(string input);
    }

    public class EchoService : IEchoService
    {
        public string Send(string input)
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