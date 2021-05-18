using System;
using System.Globalization;
using System.Linq;
using Xunit;

namespace GoodweUdpPoller
{
    public class PollerTests
    {
        //                                        0  1  2  3  4  5  6  7  8  9  a  b  c  d  e  f
        private static readonly string replyAsString = "AA-55-7F-03-92-15-05-12-08-1B-1C-08-44-00-2C-00-" +
                                                "00-00-00-FF-FF-FF-FF-FF-FF-FF-FF-FF-FF-FF-FF-FF-" +
                                                "FF-FF-FF-FF-FF-FF-FF-FF-FF-09-83-FF-FF-FF-FF-00-" +
                                                "27-FF-FF-FF-FF-13-88-FF-FF-FF-FF-00-00-03-AC-00-" +
                                                "01-00-00-00-00-00-00-FF-FF-FF-FF-FF-FF-FF-FF-FF-" +
                                                "FF-FF-FF-FF-FF-FF-FF-00-D1-FF-FF-FF-FF-00-03-00-" +
                                                "00-0E-E4-00-00-06-93-00-14-00-00-FF-FF-00-00-FF-" +
                                                "FF-00-00-FF-FF-00-00-FF-FF-FF-FF-FF-FF-FF-FF-FF-" +
                                                "FF-01-42-0F-56-FF-FF-FF-FF-FF-FF-05-E1-FF-FF-FF-" +
                                                "FF-FF-FF-01-2A-00-39-04-1F";

        private static byte[] replyBytes = replyAsString.Split("-")
            .Select(s => byte.Parse(s, NumberStyles.AllowHexSpecifier)).ToArray();

        [Fact]
        public void CrcOfReplyMatches()
        {
            var result = InverterTelemetry.GoodweCrc(replyBytes.Skip(2).SkipLast(2).ToArray());
            Assert.Equal(new byte[] { 0x04, 0x1F }, result);
        }
    }
}
