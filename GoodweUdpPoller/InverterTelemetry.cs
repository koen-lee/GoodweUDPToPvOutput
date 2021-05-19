using System;
using System.IO;
using System.Text.Json.Serialization;

namespace GoodweUdpPoller
{
    public class InverterTelemetry
    {
        /// <summary>
        /// Temperature in degrees Celsius
        /// </summary>
        public double Temperature { get; set; }

        public InverterStatus Status { get; set; }

        public double EnergyLifetime { get; set; }

        public double EnergyToday { get; set; }

        /// <summary>
        /// Momentary power at timestamp, in W
        /// </summary>
        public double Power { get; set; }

        public double Iac { get; set; }

        public double Vac { get; set; }

        public double GridFrequency { get; set; }

        /// <summary>
        /// DC Current from the solar array, in A 
        /// </summary>
        public double Ipv { get; set; }

        /// <summary>
        /// DC Voltage from the solar array, in V
        /// </summary>
        public double Vpv { get; set; }

        /// <summary>
        /// Timestamp of the telemetry according to the inverter, second precision.
        /// </summary>
        public DateTimeOffset Timestamp { get; set; }

        public string ResponseIp { get; set; }

        public static byte[] GoodweCrc(ReadOnlySpan<byte> payload)
        {
            var crc = 0xFFFF;
            bool odd;

            for (var i = 0; i < payload.Length; i++)
            {
                crc ^= payload[i];

                for (var j = 0; j < 8; j++)
                {
                    odd = (crc & 0x0001) != 0;
                    crc >>= 1;
                    if (odd)
                    {
                        crc ^= 0xA001;
                    }
                }
            }
            return BitConverter.GetBytes((ushort)crc);
        }

        public enum InverterStatus
        {
            Waiting,
            Normal,
            Error,
            Checking
        }
    }
}
