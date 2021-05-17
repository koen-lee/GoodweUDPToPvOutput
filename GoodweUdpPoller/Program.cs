using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace GoodweUdpPoller
{
    class Program
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="host">IP address or hostname of the inverter.
        /// If unset, will broadcast a discovery packet to find any compatible inverter.</param>
        static void Main(string host = null)
        {
            if (host == null)
                DiscoverInverters().Wait(TimeSpan.FromMilliseconds(1000));
            else
            {
                QueryInverter(host);
            }
        }

        public static async Task DiscoverInverters()
        {
            var foundInverters = DiscoverInvertersAsync();
            await foreach (var foundInverter in foundInverters)
            {
                Console.WriteLine($"Found: {foundInverter.Ip}");
            }
        }

        private static async IAsyncEnumerable<Inverter> DiscoverInvertersAsync()
        {
            using var channel = new UdpClient(48899) { EnableBroadcast = true };
            SendHello(channel);

            Console.WriteLine("Waiting for greetings back");
            while (true)
            {
                var greeting = RecieveGreetings(channel);
                yield return greeting.Result;
            }
        }

        private static void SendHello(UdpClient client)
        {
            client.Connect("255.255.255.255", 48899);
            var payload = Encoding.ASCII.GetBytes("WIFIKIT-214028-READ");
            payload = new byte[] { 0x57, 0x49, 0x46, 0x49, 0x4b, 0x49, 0x54, 0x2d, 0x32, 0x31, 0x34, 0x30, 0x32, 0x38, 0x2d, 0x52, 0x45, 0x41, 0x44 };
            client.Send(payload, payload.Length);
        }

        private static void QueryInverter(string host)
        {
            using var client = new UdpClient(8899);
            client.Connect(host, 8899);
            var payload = new byte[] { 0x7f, 0x03, 0x75, 0x94, 0x00, 0x49, 0xd5, 0xc2 };
            client.Send(payload, payload.Length);
            IPEndPoint remote = null;
            var result = client.Receive(ref remote);
            Console.WriteLine(BitConverter.ToString(result));
        }

        private static async Task<Inverter> RecieveGreetings(UdpClient client)
        {
            var helloBack = await client.ReceiveAsync();

            Console.WriteLine(helloBack.RemoteEndPoint.Address);
            return new Inverter { Ip = helloBack.RemoteEndPoint.Address.ToString() };
        }
    }

    public class Inverter
    {
        public string Ip { get; set; }
    }
}
