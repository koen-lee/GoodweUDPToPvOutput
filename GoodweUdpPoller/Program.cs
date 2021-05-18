using System;
using System.Text.Json;
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
        /// <param name="timeout">Listen timeout for replies</param>
        public static async Task Main(string host = null, int timeout = 1000)
        {
            var listenTimeout = TimeSpan.FromMilliseconds(timeout);
            var poller = new GoodwePoller(listenTimeout);
            if (host == null)
            {
                var foundInverters = poller.DiscoverInvertersAsync();
                await foreach (var foundInverter in foundInverters)
                {
                    Console.WriteLine($"Found: {foundInverter.Ip}");
                }
            }
            else
            {
                Console.WriteLine($"Querying {host}");
                var response = await poller.QueryInverter(host);
                var serialized = JsonSerializer.Serialize(response, new JsonSerializerOptions { WriteIndented = true });
                Console.WriteLine(serialized);
            }
        }
    }
}
