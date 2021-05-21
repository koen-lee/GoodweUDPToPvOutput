using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace GoodweUdpPoller
{
    public static class Extensions
    {
        public static async Task<UdpReceiveResult> ReceiveAsync(this UdpClient client, TimeSpan timeout)
        {
            var listenTask = client.ReceiveAsync();
            if (await Task.WhenAny(listenTask, Task.Delay(timeout)) == listenTask)
                return await listenTask;
            throw new TimeoutException();
        }


        public static ushort To16Bit(this ReadOnlySpan<byte> buffer, int offset)
        {
            return (ushort)IPAddress.NetworkToHostOrder(BitConverter.ToInt16(buffer.Slice(offset)));
        }
        public static uint To32Bit(this ReadOnlySpan<byte> buffer, int offset)
        {
            return (uint)IPAddress.NetworkToHostOrder(BitConverter.ToInt32(buffer.Slice(offset)));
        }

        public static double To16BitScale10(this ReadOnlySpan<byte> buffer, int offset)
        {
            return Math.Round(To16Bit(buffer, offset) * 0.1, 1);
        }

        public static double To32BitScale10(this ReadOnlySpan<byte> buffer, int offset)
        {
            return Math.Round(To32Bit(buffer, offset) * 0.1, 1);
        }

        public static double To16BitScale100(this ReadOnlySpan<byte> buffer, int offset)
        {
            return Math.Round(To16Bit(buffer, offset) * 0.01, 2);
        }
    }
}
