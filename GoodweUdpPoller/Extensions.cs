using System;
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
    }
}
