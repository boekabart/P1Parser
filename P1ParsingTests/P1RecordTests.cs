using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using P1Parsing;
using Xunit;

namespace P1ParsingTests
{
    public class P1RecordTests
    {
        private string[] data1 = {"2013-12-16T20-48-52", @"/KMP5 ZABF001670954712

0-0:96.1.1(205A414246303031363730393534373132)
1-0:1.8.1(01871.000*kWh)
1-0:1.8.2(01737.000*kWh)
1-0:2.8.1(00000.000*kWh)
1-0:2.8.2(00000.000*kWh)
0-0:96.14.0(0001)
1-0:1.7.0(0000.39*kW)
1-0:2.7.0(0000.00*kW)
0-0:17.0.0(999*A)
0-0:96.3.10(1)
0-0:96.13.1()
0-0:96.13.0()
0-1:24.1.0(3)
0-1:96.1.0(3238303131303031323438323831363132)
0-1:24.3.0(131216210000)(00)(60)(1)(0-1:24.2.1)(m3)
(01609.335)
0-1:24.4.0(1)
!
"};

        [Fact]
        public void TestData1()
        {
            var subject = P1Record.FromNameData(data1[0], data1[1]);
            Assert.Equal(2013, subject.DateTime.Year);
            Assert.Equal(12, subject.DateTime.Month);
            Assert.Equal(16, subject.DateTime.Day);
            Assert.Equal(20, subject.DateTime.ToUniversalTime().Hour);
            Assert.Equal(48, subject.DateTime.ToUniversalTime().Minute);
            Assert.Equal(52, subject.DateTime.ToUniversalTime().Second);
            Assert.Equal(1871, subject.kWh1);
            Assert.Equal(1737, subject.kWh2);
            Assert.Equal(3608, subject.kWhTotal);
            Assert.Equal(1, subject.ActiveMeter);
            Assert.Equal(0.39, subject.kW,3);
            Assert.Equal(1609.335, subject.M3Total, 3);
        }
    }
}
