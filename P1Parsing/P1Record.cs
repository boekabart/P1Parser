using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace P1Parsing
{
    public class P1Record
    {
        public DateTime DateTime { get; set; }
        public float kWh1 { get; set; }
        public float kWh2 { get; set; }

        public float kWhTotal
        {
            get { return kWh1 + kWh2; }
        }

        public float kW { get; set; }
        public int ActiveMeter { get; set; }
        public float M3Total { get; set; }

        public static P1Record FromFile(string path)
        {
            return FromNameData(System.IO.Path.GetFileName(path), System.IO.File.ReadAllText(path));
        }

        private static readonly Regex KWh1Regex = new Regex(@"^1-0:1\.8\.1\((\d\d\d\d\d\.\d\d\d)\*kWh\)$", RegexOptions.Multiline);
        private static readonly Regex KWh2Regex = new Regex(@"^1-0:1\.8\.2\((\d\d\d\d\d\.\d\d\d)\*kWh\)$", RegexOptions.Multiline);
        private static readonly Regex KWRegex = new Regex(@"^1-0:1\.7\.0\((\d\d\d\d\.\d\d)\*kW\)$", RegexOptions.Multiline);
        private static readonly Regex ActiveMeterRegex = new Regex(@"^0-0:96\.14\.0\(\d\d\d(\d)\)$", RegexOptions.Multiline);
        private static readonly Regex M3TotalRegex = new Regex(@"^\((\d\d\d\d\d\.\d\d\d)\)$", RegexOptions.Multiline);

        public static P1Record FromNameData(string fileName, string fileData)
        {
            fileData = fileData.Replace("\r\n", "\n");
            return new P1Record
            {
                DateTime = DateTime.ParseExact(fileName, "yyyy-MM-ddTHH-mm-ss", CultureInfo.CurrentCulture, DateTimeStyles.AssumeUniversal),
                kWh1 = float.Parse(FirstMatchValueOrNull(KWh1Regex, fileData)),
                kWh2 = float.Parse(FirstMatchValueOrNull(KWh2Regex, fileData)),
                kW = float.Parse(FirstMatchValueOrNull(KWRegex, fileData)),
                ActiveMeter = int.Parse(FirstMatchValueOrNull(ActiveMeterRegex, fileData)),
                M3Total = float.Parse(FirstMatchValueOrNull(M3TotalRegex, fileData)),
            };
        }

        private static string FirstMatchValueOrNull(Regex regex, string message)
        {
            var matchOrNot = regex.Match(message);
            return matchOrNot.Success ? matchOrNot.Groups[1].Value : null;
        }
    }
}
