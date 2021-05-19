using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace GoodweUdpPoller
{
    public class GoodwePoller
    {
        public TimeSpan ListenTimeout { get; }

        public GoodwePoller(TimeSpan listenTimeout)
        {
            ListenTimeout = listenTimeout;
        }

        public async IAsyncEnumerable<Inverter> DiscoverInvertersAsync()
        {
            using var channel = new UdpClient { EnableBroadcast = true };
            await SendHello(channel);

            Console.WriteLine("Waiting for greetings back");
            var timeout = Task.Delay(ListenTimeout);
            while (true)
            {
                var greeting = RecieveGreetings(channel);
                var finishedTask = await Task.WhenAny(timeout, greeting);
                if (finishedTask == greeting)
                    yield return greeting.Result;
                else
                    yield break;
            }
        }

        private async Task SendHello(UdpClient client)
        {
            var payload = Encoding.ASCII.GetBytes("WIFIKIT-214028-READ");
            //await client.SendAsync(payload, payload.Length, "255.255.255.255", 48899);
            await client.SendAsync(payload, payload.Length, "192.168.2.255", 48899);
        }

        private async Task<Inverter> RecieveGreetings(UdpClient client)
        {
            var helloBack = await client.ReceiveAsync();
            return new Inverter
            {
                ResponseIp = helloBack.RemoteEndPoint.Address.ToString(),
                DiscoverData = helloBack.Buffer
            };
        }

        public async Task<InverterTelemetry> QueryInverter(string host)
        {
            using var client = new UdpClient();
            var payload = new byte[] { 0x7f, 0x03, 0x75, 0x94, 0x00, 0x49, 0xd5, 0xc2 };
            await client.SendAsync(payload, payload.Length, host, port: 8899);
            var result = await client.ReceiveAsync(ListenTimeout);
            var response = CreateTelemetryFrom(result.Buffer, result.RemoteEndPoint.Address.ToString());
            return response;
        }

        public static InverterTelemetry CreateTelemetryFrom(ReadOnlySpan<byte> data, string responseIp)
        {
            const int expectedLength = 153;
            if (data.Length != expectedLength)
                throw new InvalidDataException($"Got size {data.Length}, expected {expectedLength}");
            var header = data.Slice(0, 2);
            if (!header.SequenceEqual(new byte[] { 0xaa, 0x55 }))
            {
                throw new InvalidDataException($"Wrong header");
            }

            var receivedCrc = data.Slice(data.Length - 2);
            var payload = data.Slice(2, data.Length - 4);
            if (!receivedCrc.SequenceEqual(InverterTelemetry.GoodweCrc(payload)))
            {
                throw new InvalidDataException($"CRC mismatch");
            }

            return new InverterTelemetry
            {
                Timestamp = new DateTimeOffset(new DateTime(kind: DateTimeKind.Local,
                    year: data[5] + 2000, month: data[6], day: data[7],
                    hour: data[8], minute: data[9], second: data[10], millisecond: 0)),
                Vpv = data.To16BitScale10(11),
                Ipv = data.To16BitScale10(13),
                Vac = data.To16BitScale10(41),
                Iac = data.To16BitScale10(47),
                GridFrequency = data.To16BitScale100(53),
                Power = data.To16Bit(61),
                Status = (InverterTelemetry.InverterStatus)data[63],
                Temperature = data.To16BitScale10(87),
                EnergyToday = data.To16BitScale10(93),
                EnergyLifetime = data.To16BitScale10(97)/*probably 32 bit*/,
                ResponseIp = responseIp
            };
        }
    }
}
