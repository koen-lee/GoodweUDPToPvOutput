using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace GoodweUdpPoller
{
    class Program
    {
        private static readonly HttpClient _client = new HttpClient();

        /// <summary>
        /// Tool for querying Goodwe inverters over the local network.
        /// Without options set, will try to discover any responding inverters on the network and display current telemetry for the last one.
        /// Intended use is to invoke in a cronjob every 5 minutes to update PVOutput.org, but can be adapted to other uses as it outputs json by default.
        /// </summary>
        /// <param name="host">IP address, hostname or subnet broadcast address of the inverter.
        /// If unset, will broadcast a discovery packet to find any compatible inverter.</param>
        /// <param name="timeout">Listen timeout for replies</param>
        /// <param name="pvoutputSystemId">System Id for API access on pvoutput.org, see https://pvoutput.org/help/api_specification.html </param>
        /// <param name="pvoutputApikey">API key for API access on pvoutput.org, see https://pvoutput.org/help/api_specification.html </param>
        /// <param name="pvoutputRequestUrl">optional url to post to</param>
        public static async Task Main(
            string host = null,
            int timeout = 1000,
            int pvoutputSystemId = 0,
            string pvoutputApikey = "",
            string pvoutputRequestUrl = "https://pvoutput.org/service/r2/addstatus.jsp")
        {
            var listenTimeout = TimeSpan.FromMilliseconds(timeout);
            var poller = new GoodwePoller(listenTimeout);
            if (host == null)
            {
                await foreach (var foundInverter in poller.DiscoverInvertersAsync())
                {
                    if (foundInverter.Ssid == null /*== not a Goodwe inverter*/)
                        continue;

                    WriteObject(foundInverter);
                    host = foundInverter.ResponseIp;
                }
            }

            if (host == null)
                throw new ArgumentException("No host given on command line and none discovered, nothing to do. Either make sure your router doesn't block broadcasts or discover the IP with, for example, the Goodwe app");
            if (pvoutputSystemId <= 0 ^ string.IsNullOrEmpty(pvoutputApikey))
                throw new ArgumentException("Both systemid and apikey need to be set to upload to pvoutput");

            var response = await poller.QueryInverter(host);
            WriteObject(response);

            if (pvoutputSystemId > 0)
                await PostToPvOutput(response, pvoutputSystemId, pvoutputApikey, pvoutputRequestUrl);
        }

        private static async Task PostToPvOutput(InverterTelemetry inverterStatus, int pvOutputSystemId,
            string pvOutputApikey, string pvOutputRequestUrl)
        {
            var values = new Dictionary<string, string>
            {
                { "d", inverterStatus.Timestamp.ToString("yyyyMMdd") },
                { "t", inverterStatus.Timestamp.ToString("HH:mm") },
                { "v1", (inverterStatus.EnergyLifetime*1000).ToString(CultureInfo.InvariantCulture) },
                { "c1", "1" /*Lifetime energy is cumulative*/},
                { "v2", inverterStatus.Power.ToString(CultureInfo.InvariantCulture) },
                { "v5", inverterStatus.Temperature.ToString(CultureInfo.InvariantCulture) },
                { "v6", inverterStatus.Vac.ToString(CultureInfo.InvariantCulture) },
            };

            var content = new FormUrlEncodedContent(values);
            _client.DefaultRequestHeaders.Add("X-Pvoutput-Apikey", pvOutputApikey);
            _client.DefaultRequestHeaders.Add("X-Pvoutput-SystemId", pvOutputSystemId.ToString(CultureInfo.InvariantCulture));
            var response = await _client.PostAsync(pvOutputRequestUrl, content);

            var responseString = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"<={responseString}");
            response.EnsureSuccessStatusCode();
        }

        private static void WriteObject(object toWrite)
        {
            var serialized = JsonSerializer.Serialize(toWrite, new JsonSerializerOptions { WriteIndented = true });
            Console.WriteLine(serialized);
        }
    }
}
