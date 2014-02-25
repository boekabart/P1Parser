using System;
using P1Parsing;
using Xunit;

namespace P1ParsingTests
{
    public class P1ImprovedTests
    {
        [Fact]
        public void Test1()
        {
            var refTime = DateTime.UtcNow;
            var prev = new P1Record { ActiveMeter = 1, DateTime = refTime.AddHours(-3), kWh1 = 999, kW = 1 };
            var first1k = new P1Record { ActiveMeter = 1, DateTime = refTime.AddHours(-2), kWh1 = 1000, kW = 1 };
            var next1k = new P1Record { ActiveMeter = 1, DateTime = refTime.AddHours(-1), kWh1 = 1000, kW = 0.4 };
            var first1k1 = new P1Record { ActiveMeter = 1, DateTime = refTime.AddHours(0), kWh1 = 1001, kW = 0.4 };
            var standen = new[] {prev, first1k, next1k, first1k1};

        }
    }
}