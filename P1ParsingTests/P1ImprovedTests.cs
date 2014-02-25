using System;
using System.Linq;
using P1Parsing;
using Xunit;

namespace P1ParsingTests
{
    public class P1ImprovedTests
    {
        [Fact]
        public void TestWithStartAndEnd()
        {
            var refTime = DateTime.UtcNow;
            var prev = new P1Record { ActiveMeter = 1, DateTime = refTime.AddHours(-3), kWh1 = 999, kW = 1 };
            var first1k = new P1Record { ActiveMeter = 1, DateTime = refTime.AddHours(-2), kWh1 = 1000, kW = 1 };
            var next1k = new P1Record { ActiveMeter = 1, DateTime = refTime.AddHours(-1), kWh1 = 1000, kW = 0.4 };
            var first1k1 = new P1Record { ActiveMeter = 1, DateTime = refTime.AddHours(0), kWh1 = 1001, kW = 0.4 };
            var standen = new[] {prev, first1k, next1k, first1k1};
            var improvedStanden = P1Improved.FromP1Records(standen).ToArray();
            Assert.Equal(1000.5, improvedStanden[2].kWhTotal, 2);
        }

        [Fact]
        public void TestWithStartAndEndAsym()
        {
            var refTime = DateTime.UtcNow;
            var prev = new P1Record { ActiveMeter = 1, DateTime = refTime.AddHours(-3), kWh1 = 999, kW = 1 };
            var first1k = new P1Record { ActiveMeter = 1, DateTime = refTime.AddHours(-2), kWh1 = 1000, kW = 1 };
            var next1k = new P1Record { ActiveMeter = 1, DateTime = refTime.AddHours(-1), kWh1 = 1000, kW = 0.4 };
            var first1k1 = new P1Record { ActiveMeter = 1, DateTime = refTime.AddHours(0), kWh1 = 1001, kW = 0.6 };
            var standen = new[] { prev, first1k, next1k, first1k1 };
            var improvedStanden = P1Improved.FromP1Records(standen).ToArray();
            Assert.Equal(1000.4, improvedStanden[2].kWhTotal, 2);
        }

        [Fact]
        public void TestWithStartAndEndAsym2()
        {
            var refTime = DateTime.UtcNow;
            var prev = new P1Record { ActiveMeter = 1, DateTime = refTime.AddHours(-3), kWh1 = 999, kW = 1 };
            var first1k = new P1Record { ActiveMeter = 1, DateTime = refTime.AddHours(-1), kWh1 = 1000, kW = 1 };
            var next1k = new P1Record { ActiveMeter = 1, DateTime = refTime.AddHours(-0.6), kWh1 = 1000, kW = 1 };
            var first1k1 = new P1Record { ActiveMeter = 1, DateTime = refTime.AddHours(0), kWh1 = 1001, kW = 1 };
            var standen = new[] { prev, first1k, next1k, first1k1 };
            var improvedStanden = P1Improved.FromP1Records(standen).ToArray();
            Assert.Equal(1000.4, improvedStanden[2].kWhTotal, 2);
        }

        [Fact]
        public void TestWithStartNoEnd()
        {
            var refTime = DateTime.UtcNow;
            var prev = new P1Record { ActiveMeter = 1, DateTime = refTime.AddHours(-3), kWh1 = 999, kW = 1 };
            var first1k = new P1Record { ActiveMeter = 1, DateTime = refTime.AddHours(-2), kWh1 = 1000, kW = 1 };
            var next1k = new P1Record { ActiveMeter = 1, DateTime = refTime.AddHours(-1), kWh1 = 1000, kW = 0.4 };
            var first1k1 = new P1Record { ActiveMeter = 1, DateTime = refTime.AddHours(0), kWh1 = 1000, kW = 0.4 };
            var standen = new[] { prev, first1k, next1k, first1k1 };
            var improvedStanden = P1Improved.FromP1Records(standen).ToArray();
            Assert.Equal(1000.4, improvedStanden[2].kWhTotal, 2);
        }

        [Fact]
        public void TestWithNoStartButEnd()
        {
            var refTime = DateTime.UtcNow;
            var prev = new P1Record { ActiveMeter = 1, DateTime = refTime.AddHours(-3), kWh1 = 1000, kW = 0 };
            var first1k = new P1Record { ActiveMeter = 1, DateTime = refTime.AddHours(-2), kWh1 = 1000, kW = 0 };
            var next1k = new P1Record { ActiveMeter = 1, DateTime = refTime.AddHours(-1), kWh1 = 1000, kW = 0.4 };
            var first1k1 = new P1Record { ActiveMeter = 1, DateTime = refTime.AddHours(0), kWh1 = 1001, kW = 0.4 };
            var standen = new[] { prev, first1k, next1k, first1k1 };
            var improvedStanden = P1Improved.FromP1Records(standen).ToArray();
            Assert.Equal(1000.6, improvedStanden[2].kWhTotal, 2);
            Assert.Equal(1000.2, improvedStanden[1].kWhTotal, 2);
        }
    }
}