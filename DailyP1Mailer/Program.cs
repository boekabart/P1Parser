using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using P1Parsing;

namespace DailyP1Mailer
{
    class Program
    {
        private static readonly string CacheTemplate;
        private static readonly string PathTemplate;

        static Program()
        {
            var dataBase = @"\\legendary\p1data$";
            const string localDataBase = @"c:\Util\P1Logger\Data";
            if (Directory.Exists(localDataBase))
                dataBase = localDataBase;
            PathTemplate = dataBase + @"\{0:yyyy-MM-dd}";

            var cacheBase = @"\\legendary\p1datacache$";
            const string localCacheBase = @"c:\Util\P1Logger\DataCache";
            if (Directory.Exists(localCacheBase))
                cacheBase = localCacheBase;
            CacheTemplate = cacheBase + @"\{0:yyyy\\MM\\P1_yyyy-MM-dd}.xml";
        }

        class Usage
        {
            public double kWh { get; set; }
            public double M3 { get; set; }

            public static Usage FromRecords(P1Improved first, P1Improved last)
            {
                return new Usage { kWh = last.kWhTotal - first.kWhTotal, M3 = last.M3Total - first.M3Total };
            }
        }

        class DailyUsage
        {
            public Usage[] Hourly { get; set; }
            public Usage Total { get; set; }
            public DateTime Day { get; set; }
        }

        static void Main(string[] args)
        {
            if (args.Any() && args[0].Equals("updateall"))
            {
                UpdateAllCaches();
                return;
            }
            var oneDay = TimeSpan.FromDays(1);
            var today = DateTime.UtcNow.Subtract(oneDay);
            var yesterday = DateTime.UtcNow.Subtract(oneDay);
            // Update the cache only for today, yesterday
            Console.WriteLine("Reading today's raw data");
            UpdateCachedP1RecordsForDay(today);
            Console.WriteLine("Reading yesterdays's raw data");
            UpdateCachedP1RecordsForDay(yesterday);
            Console.WriteLine("Reading and processing older data");

            const string header =
                "<html><head><style>table,th,td{border:2px solid black;border-collapse:collapse;padding-left: 10px; padding-right: 10px;}th {background-color:#ccccdd;}</style></head><body>";
            const string tableHeader =
                "<table><tr><th>Dag</th><th>kWh</th><th>Stroom</th><th>kWh &euro;/mnd</th><th>m&#179;</th><th>m&#179; &euro;</th><th>m&#179; &euro;/mnd</th></tr>";
            const string tableFooter = "</table>";
            const string footer = "</html>";
            const string rowTemplate =
                "<tr><td>{0: ddd d MMM}</td><td>{1:F1}</td><td>&euro;{2:F2}</td><td>&euro;{3:F0}</td><td>{4:F1}</td><td>&euro; {5:F2}</td><td>&euro; {6:F0}</td></tr>";
            var daysBack = Enumerable.Range(1, 8);
            var days = daysBack.Select(d => DateTime.UtcNow.Subtract(TimeSpan.FromDays(d)));
            var dayData = days.Select(TryGetDailyUsage).Where(n => n != null);
            double euroPerkWh = 0.20879;
            double euroPerM3 = 0.60803;
            double daysPerMonth = 365.25 / 12.0;
            var dailyUsages = dayData as DailyUsage[] ?? dayData.ToArray();
            Console.WriteLine("Read all data");
            var rowStrings = dailyUsages.Select(u => string.Format(rowTemplate,
                u.Day, u.Total.kWh, u.Total.kWh * euroPerkWh, u.Total.kWh * euroPerkWh * daysPerMonth, u.Total.M3,
                u.Total.M3 * euroPerM3, u.Total.M3 * euroPerM3 * daysPerMonth));
            var rowsString = string.Concat(rowStrings);
            string extra = "<pre>";
            {
                var yesterData = dailyUsages.First();
                for (int h = 0; h < 24; h++)
                {
                    var hourlyUsage = yesterData.Hourly[h];
                    extra += string.Format("{0}:00-{1}:00: {2:F2} m&#179;, {3:F0} W avg<br/>", h, h + 1, hourlyUsage.M3,
                        hourlyUsage.kWh*1000.0);

                }
                extra += "</pre>";
            }

            using (var message = new MailMessage())
            {
                message.To.Add("loreleideboer@gmail.com");
                message.To.Add("bartdb@gmail.com");
                message.Subject = string.Format("Daily Energy Report for {0:ddd d MMM}", yesterday);
                message.IsBodyHtml = true;
                message.Body = header + tableHeader + rowsString + tableFooter +extra+ footer;
                using (var smtp = new SmtpClient())
                {
                    smtp.Send(message);
                }
            }
        }

        private static void UpdateAllCaches()
        {
            var today = DateTime.UtcNow;
            for (int daysBack = 0; daysBack < 300; daysBack++)
            {
                var day = today.AddDays(-daysBack);
                Console.WriteLine(day);
                try
                {
                    UpdateCachedP1RecordsForDay(day);
                }
                catch (Exception e)
                {
                    Console.Error.WriteLine(e.Message);
                }
            }
        }

        private static DailyUsage TryGetDailyUsage(DateTime yesterday)
        {
            try
            {
                return GetDailyUsageImproved(yesterday);
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static void WriteCachedP1RecordsForDay(IEnumerable<P1Record> records, DateTime yesterday)
        {
            var outPath = string.Format(CacheTemplate, yesterday);
            var p1RecordsForDay = records as P1Record[] ?? records.ToArray();
            var ser = new XmlSerializer(typeof (P1Record[]));
            var outDir = Path.GetDirectoryName(outPath);
            if (!Directory.Exists(outDir))
                Directory.CreateDirectory(outDir);
            using (var fs = new FileStream(outPath, FileMode.Create))
            {
                ser.Serialize(fs, p1RecordsForDay);
            }
        }

        private static void UpdateCachedP1RecordsForDay(DateTime yesterday)
        {
            var records = ReadRawP1RecordsForDay(yesterday);
            var p1RecordsForDay = records as P1Record[] ?? records.ToArray();
            var key = yesterday.ToString("YYYYMMdd");
            P1RecordsForDay[key] = p1RecordsForDay;
            if (p1RecordsForDay.Any())
                WriteCachedP1RecordsForDay(p1RecordsForDay, yesterday);
        }

        private static readonly Dictionary<string, P1Record[]> P1RecordsForDay = new Dictionary<string, P1Record[]>();
        private static IEnumerable<P1Record> GetP1RecordsForDay(DateTime yesterday)
        {
            var key = yesterday.ToString("YYYYMMdd");
            if (P1RecordsForDay.ContainsKey(key))
                return P1RecordsForDay[key];
            var readRecords = ReadP1RecordsForDay(yesterday).ToArray();
            P1RecordsForDay[key] = readRecords;
            return readRecords;
        }

        private static IEnumerable<P1Record> ReadP1RecordsForDay(DateTime yesterday)
        {
            try
            {
                return ReadCachedP1RecordsForDay(yesterday);
            }
            catch
            {
                return ReadRawP1RecordsForDay(yesterday);
            }
        }

        private static IEnumerable<P1Record> ReadCachedP1RecordsForDay(DateTime yesterday)
        {
            var cachePath = string.Format(CacheTemplate, yesterday);
            var ser = new XmlSerializer(typeof (P1Record[]));

            using (var fs = new FileStream(cachePath, FileMode.Open))
            {
                return (P1Record[])ser.Deserialize(fs);
            }
        }

        private static IEnumerable<P1Record> ReadRawP1RecordsForDay(DateTime yesterday)
        {
            var yesterdayPath = string.Format(PathTemplate, yesterday);
            var files = Directory.EnumerateFiles(yesterdayPath, "????-??-??T??-??-??").OrderBy(a => a);
            return files.Select(P1Record.FromFile);
        }

        private static DailyUsage GetDailyUsageImproved(DateTime yesterday)
        {
            yesterday = yesterday.Date;
            var today = yesterday.AddDays(1);

            var todayRecords = GetP1RecordsForDay(today);
            var yesterdayRecords = GetP1RecordsForDay(yesterday);
            var dayBeforeYesterdayRecords = GetP1RecordsForDay(yesterday.AddDays(-1));

            var yesterdayLocal = yesterday.AddHours(12).ToLocalTime().Date;
            var todayLocal = yesterdayLocal.AddDays(1);

            var records = dayBeforeYesterdayRecords.Concat(yesterdayRecords).Concat(todayRecords);
            var improved = P1Improved.FromP1Records(records).ToArray();
            var firstRecord = improved.First(p1I => p1I.DateTime >= yesterdayLocal);
            var lastRecord = improved.First(p1I => p1I.DateTime >= todayLocal);
            var usageTotal = Usage.FromRecords(firstRecord, lastRecord);
            var range = Enumerable.Range(0, 25).ToArray();
            var hours = range.Select(i => yesterdayLocal.AddHours(i)).ToArray();
            var hourRecords = hours.Select(h => improved.First(p1I => p1I.DateTime >= h)).ToArray();
            var hourly = range.Skip(1).Select(r => new Usage
            {
                kWh = hourRecords[r].kWhTotal - hourRecords[r - 1].kWhTotal,
                M3 = hourRecords[r].M3Total - hourRecords[r - 1].M3Total,
            }).ToArray();
            var dailyUsage = new DailyUsage {Total = usageTotal, Day = yesterday, Hourly = hourly};
            return dailyUsage;
        }
    }
}
