using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;
using P1Parsing;

namespace DailyP1Mailer
{
    class Program
    {
        class Usage
        {
            public double kWh { get; set; }
            public double M3 { get; set; }

            public static Usage FromRecords(P1Record first, P1Record last)
            {
                return new Usage {kWh = last.kWhTotal - first.kWhTotal, M3 = last.M3Total - first.M3Total};
            }
        }

        class DailyUsage
        {
            public Usage AM { get; set; }
            public Usage PM { get; set; }
            public Usage Total { get; set; }
            public DateTime Day { get; set; }
        }

        static void Main(string[] args)
        {
            var oneDay = TimeSpan.FromDays(1);
            var yesterday = DateTime.UtcNow.Subtract(oneDay);
            var dailyUsage = GetDailyUsage(yesterday);

            using (var message = new MailMessage())
            {
                //message.To.Add("loreleideboer@gmail.com");
                message.To.Add("bartdb@gmail.com");
                message.Subject = string.Format("Daily Energy Report for {0:ddd d MMM}", yesterday);
                const string header =
                    "<html><head><style>table,th,td{border:2px solid black;border-collapse:collapse;padding-left: 10px; padding-right: 10px;}th {background-color:#ccccdd;}</style></head><body>";
                const string tableHeader =
                    "<table><tr><th>Dag</th><th>kWh</th><th>Stroom</th><th>kWh &euro;/mnd</th><th>m&#179;</th><th>m&#179; &euro;</th><th>m&#179; &euro;/mnd</th></tr>";
                const string tableFooter = "</table>";
                const string footer = "</html>";
                const string rowTemplate =
                    "<tr><td>{0: ddd d MM}</td><td>{1:F1}</td><td>&euro;{2:F2}</td><td>&euro;{3:F0}</td><td>{4:F1}</td><td>&euro; {5:F2}</td><td>&euro; {6:F0}</td></tr>";
                var daysBack = Enumerable.Range(1, 8);
                var days = daysBack.Select(d => DateTime.UtcNow.Subtract(TimeSpan.FromDays(d)));
                var dayData = days.Select(TryGetDailyUsage).Where(n => n != null);
                double euroPerkWh = 0.20879;
                double euroPerM3 = 0.60803;
                double daysPerMonth = 365.25/12.0;
                var rowStrings = dayData.Select(u => string.Format(rowTemplate,
                    u.Day, u.Total.kWh, u.Total.kWh*euroPerkWh, u.Total.kWh*euroPerkWh*daysPerMonth, u.Total.M3,
                    u.Total.M3*euroPerM3, u.Total.M3*euroPerM3*daysPerMonth));
                var rowsString = string.Concat(rowStrings);
                message.IsBodyHtml = true;
                message.Body = header + tableHeader + rowsString + tableFooter + footer;
                using (var smtp = new SmtpClient())
                {
                    smtp.Send(message);
                }
            }
        }

        private static DailyUsage TryGetDailyUsage(DateTime yesterday)
        {
            try
            {
                return GetDailyUsage(yesterday);
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static DailyUsage GetDailyUsage(DateTime yesterday)
        {
            const string pathTemplate = @"\\legendary\p1Data$\{0:yyyy-MM-dd}";
            var yesterdayPath = string.Format(pathTemplate, yesterday);
            var firstFile = Directory.GetFiles(yesterdayPath, "????-??-??T00-00-??").OrderBy(a => a).First();
            var noonFile = Directory.GetFiles(yesterdayPath, "????-??-??T12-00-??").OrderBy(a => a).First();
            var lastFile = Directory.GetFiles(yesterdayPath, "????-??-??T23-59-??").OrderByDescending(a => a).First();
            var firstRecord = P1Record.FromFile(firstFile);
            var noonRecord = P1Record.FromFile(noonFile);
            var lastRecord = P1Record.FromFile(lastFile);
            var usageAM = Usage.FromRecords(firstRecord, noonRecord);
            var usagePM = Usage.FromRecords(noonRecord, lastRecord);
            var usageTotal = Usage.FromRecords(firstRecord, lastRecord);
            var dailyUsage = new DailyUsage {AM = usageAM, PM = usagePM, Total = usageTotal, Day = yesterday};
            return dailyUsage;
        }
    }
}
