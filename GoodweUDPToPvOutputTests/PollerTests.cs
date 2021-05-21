using System;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Threading;
using Xunit;
using Xunit.Abstractions;

namespace GoodweUdpPoller
{
    public class PollerTests
    {
        private readonly ITestOutputHelper _output;

        //                                        
        private static readonly string replyAsString =
            //        0  1  2  3  4  5  6  7  8  9  a b  c d  e f
            /* 0 */  "AA-55-7F-03-92-15-05-12-08-1B-1C-08-44-00-2C-00-" +
            /*16 */  "00-00-00-FF-FF-FF-FF-FF-FF-FF-FF-FF-FF-FF-FF-FF-" +
            /*32 */  "FF-FF-FF-FF-FF-FF-FF-FF-FF-09-83-FF-FF-FF-FF-00-" +
            /*48 */  "27-FF-FF-FF-FF-13-88-FF-FF-FF-FF-00-00-03-AC-00-" +
            /*64 */  "01-00-00-00-00-00-00-FF-FF-FF-FF-FF-FF-FF-FF-FF-" +
            /*80 */  "FF-FF-FF-FF-FF-FF-FF-00-D1-FF-FF-FF-FF-00-03-00-" +
            /*96 */  "00-0E-E4-00-00-06-93-00-14-00-00-FF-FF-00-00-FF-" +
            /*112*/  "FF-00-00-FF-FF-00-00-FF-FF-FF-FF-FF-FF-FF-FF-FF-" +
            /*128*/  "FF-01-42-0F-56-FF-FF-FF-FF-FF-FF-05-E1-FF-FF-FF-" +
            /*144*/  "FF-FF-FF-01-2A-00-39-04-1F";

        private static byte[] replyBytes = ReplyBytes(replyAsString);

        public PollerTests(ITestOutputHelper output)
        {
            _output = output;
        }

        private static byte[] ReplyBytes(string asString)
        {
            return asString.Split("-")
                .Select(s => byte.Parse(s, NumberStyles.AllowHexSpecifier)).ToArray();
        }

        [Fact]
        public void CrcOfReplyMatches()
        {
            var result = InverterTelemetry.GoodweCrc(replyBytes.Skip(2).SkipLast(2).ToArray());
            Assert.Equal(new byte[] { 0x04, 0x1F }, result);
        }

        [Fact]
        public void Given_bytes_When_Create_Then_a_valid_InverterTelemetry_is_created()
        {
            var result = GoodwePoller.CreateTelemetryFrom(replyBytes, null);
            WriteObject(result);
            Assert.Equal(20.9, result.Temperature, 2);
            Assert.Equal(50.0, result.GridFrequency, 3);
            Assert.Equal(381.2, result.EnergyLifetime);
            Assert.Equal(0.3, result.EnergyToday);
            Assert.Equal(940, result.Power);
            Assert.Equal(3.9, result.Iac);
            Assert.Equal(243.5, result.Vac);
            Assert.Equal(4.4, result.Ipv);
            Assert.Equal(211.6, result.Vpv);
            Assert.Equal("2021-05-18 08:27:28", result.Timestamp.ToString("yyyy-MM-dd HH:mm:ss"));
        }

        private void WriteObject(object toWrite)
        {
            var serialized = JsonSerializer.Serialize(toWrite, new JsonSerializerOptions { WriteIndented = true });
            _output.WriteLine(serialized);
        }
    }
}

