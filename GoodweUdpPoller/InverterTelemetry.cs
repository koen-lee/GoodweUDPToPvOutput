using System;
using System.IO;

namespace GoodweUdpPoller
{
    public class InverterTelemetry
    {
        public static InverterTelemetry Create(ReadOnlySpan<byte> data)
        {
            var expectedLength = 160;
            if (data.Length != expectedLength)
                throw new InvalidDataException($"Got size {data.Length}, expected {expectedLength}");
            var header = data.Slice(0, 2);
            if (!header.SequenceEqual(new byte[] { 0x55, 0xaa }))
            {
                throw new InvalidDataException($"Wrong header");
            }

            var receivedCrc = data.Slice(data.Length - 2);
            var payload = data.Slice(2, data.Length - 4);
            if (!receivedCrc.SequenceEqual(GoodweCrc(payload)))
            {
                throw new InvalidDataException($"CRC mismatch");
            }

            throw new NotImplementedException();
        }

        public static byte[] GoodweCrc(ReadOnlySpan<byte> payload)
        {
            var crc = 0xFFFF;
            bool odd;

            for (var i = 0; i < payload.Length; i++)
            {
                crc = crc ^ payload[i];

                for (var j = 0; j < 8; j++)
                {
                    odd = (crc & 0x0001) != 0;
                    crc = crc >> 1;
                    if (odd)
                    {
                        crc = crc ^ 0xA001;
                    }
                }
            }

            return BitConverter.GetBytes((ushort)crc);
        }
    }
}
