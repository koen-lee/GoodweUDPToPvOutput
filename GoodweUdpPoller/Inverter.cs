using System;
using System.Text;
using System.Text.Json.Serialization;

namespace GoodweUdpPoller
{
    public class Inverter
    {
        private byte[] _discoverData;
        public string ResponseIp { get; set; }

        [JsonIgnore]
        public byte[] DiscoverData
        {
            get => _discoverData;
            set
            {
                _discoverData = value;
                SetProperties(value);
            }
        }

        private void SetProperties(byte[] value)
        {
            try
            {
                var stringData = Encoding.UTF8.GetString(value);
                var segments = stringData.Split(",");
                if (segments.Length < 3) return;
                Host = segments[0];
                Mac = segments[1];
                Ssid = segments[2];
            }
            catch (Exception) { /*we have no control over what devices reply, so if the properties are unset that's a sign it is not a Goodwe inverter.*/ }
        }

        public string Ssid { get; set; }

        public string Mac { get; set; }

        public string Host { get; set; }
    }
}
