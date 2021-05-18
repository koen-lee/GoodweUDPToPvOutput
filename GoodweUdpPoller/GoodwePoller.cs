using System;
using System.Collections.Generic;
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
            SendHello(channel);

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

        private async void SendHello(UdpClient client)
        {
            var payload = Encoding.ASCII.GetBytes("WIFIKIT-214028-READ");
            payload = new byte[] { 0x57, 0x49, 0x46, 0x49, 0x4b, 0x49, 0x54, 0x2d, 0x32, 0x31, 0x34, 0x30, 0x32, 0x38, 0x2d, 0x52, 0x45, 0x41, 0x44 };
            //     client.Send(payload, payload.Length, "255.255.255.255", 48899);
            client.Send(payload, payload.Length, "192.168.2.129", 48899);
        }

        private async Task<Inverter> RecieveGreetings(UdpClient client)
        {
            var helloBack = await client.ReceiveAsync();
            Console.WriteLine(helloBack.RemoteEndPoint.Address);
            return new Inverter
            {
                Ip = helloBack.RemoteEndPoint.Address.ToString(),
                DiscoverData = helloBack.Buffer
            };
        }

        public async Task QueryInverter(string host)
        {
            Console.WriteLine($"Querying {host}");
            using var client = new UdpClient();
            var payload = new byte[] { 0x7f, 0x03, 0x75, 0x94, 0x00, 0x49, 0xd5, 0xc2 };
            await client.SendAsync(payload, payload.Length, host, port: 8899);
            var result = await client.ReceiveAsync(ListenTimeout);
            var response = InverterTelemetry.Create(result.Buffer);

        }
    }
}
