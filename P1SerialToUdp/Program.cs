using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Reactive.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using Microsoft.AspNet.SignalR;
using Newtonsoft.Json;
using P1Parsing;
using Polly;
using Pushqa.Server.SignalR;

namespace P1SerialToUdp
{
    internal class Program
    {
        static Program()
        {
            StartFolder =
                Path.GetDirectoryName(new Uri(System.Reflection.Assembly.GetEntryAssembly().CodeBase).LocalPath);
            DataFolder = Path.Combine(StartFolder, "Data");
            LogFolder = Path.Combine(StartFolder, "Logs");
        }

        public static readonly string StartFolder;
        public static readonly string DataFolder;
        public static readonly string LogFolder;

        private static void SaveBytes(byte[] packet)
        {
            if (packet.Length == 0)
                return;
            LogFunc("Saving {0} bytes", packet.Length);
            var fileName = DateTime.UtcNow.ToString("s").Replace(':', '-');
            var folderName = fileName.Substring(0, 10);
            var dirPath = Path.Combine(DataFolder, folderName);
            if (!Directory.Exists(dirPath))
                Directory.CreateDirectory(dirPath);
            var fullPath = Path.Combine(dirPath, fileName);
            File.WriteAllBytes(fullPath, packet);
        }

        private static void Main(string[] args)
        {
            if (!System.IO.Directory.Exists(DataFolder))
                System.IO.Directory.CreateDirectory(DataFolder);

            if (!System.IO.Directory.Exists(LogFolder))
                System.IO.Directory.CreateDirectory(LogFolder);

            _logFunc = LogToFile;

            if (Environment.UserInteractive)
            {
                _logFunc = s =>
                {
                    LogToFile(s);
                    Console.WriteLine(s);
                };

                Start();
                while (!Console.KeyAvailable)
                    Thread.Sleep(250);
                Stop();
            }
            else
            {
                var servicesToRun = new ServiceBase[] {new P1BroadcastService()};
                ServiceBase.Run(servicesToRun);
            }
        }

        public static void Stop()
        {
            _logFunc("Stop.");
            try
            {
                while (Disposables.Any())
                    Disposables.Pop().Dispose();
            }
            catch (Exception e)
            {
                _logFunc("Exception4: " + e.Message);
                throw;
            }
            _logFunc("Stopped.");
        }

        private static Action<string> _logFunc;

        private static void LogFunc(string format, params object[] args)
        {
            _logFunc(string.Format(format, args));
        }

        private static void LogToFile(string xx)
        {
            var lines = xx.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).Select(x => string.Format("{0:r} {1}", DateTime.UtcNow, x)).ToArray();
            try
            {
                File.AppendAllLines(Path.Combine(LogFolder, "P1Service.log"), lines);
            }
            catch
            {
            }
        }

        public static void Start()
        {
            try
            {
                _logFunc("Start.");
                const string url = "http://localhost:8080/p1/";
                var c = new MyPushContext();
                //Using(WebApp.Start<Startup>(url));

                var serialPort = new SerialPort("COM3", 9600, Parity.Even, 7);
                serialPort.Open();
                var serialObservable = serialPort.ToObservable().Publish();

                var messagesObservable =
                    serialObservable
                        .SkipWhile(b => b != '/')
                        .TakeUntil(b => b == '!')
                        .ToArray()
                        .Repeat().Publish();

                Using(messagesObservable.Subscribe(SaveBytes, e => _logFunc(e.Message),
                    () => _logFunc("Doneth")
                    ));

                var p1StringObservable = messagesObservable.Select(Encoding.ASCII.GetString);
                var recordObservable = p1StringObservable.Select(P1Record.TryFromDataAndNow).Where(_ => _ != null).Publish();

                //c.P1Message = p1StringObservable.AsQbservable();
                //c.P1Record = p1StringObservable.Select(P1Parsing.P1Record.FromDataAndNow).AsQbservable();
                Using(recordObservable.Subscribe(WriteLastRecordJson));
                Using(recordObservable.Buffer(20,1).Subscribe(WriteLastRecordsJson));

                Using(recordObservable.Connect());
                Using(messagesObservable.Connect());
                Using(serialObservable.Connect());
                _logFunc("Started.");
            }
            catch (Exception e)
            {
                _logFunc("Exception1: " + e.Message);
                throw;
            }
        }

        private static readonly Policy IoRetryPolicy = Policy.Handle<IOException>()
            .WaitAndRetry(3, _ => TimeSpan.FromMilliseconds(100));

        private static void WriteLastRecordJson(P1Record record)
        {
            var json = JsonConvert.SerializeObject(record, Formatting.None);
            IoRetryPolicy.Execute(() => File.WriteAllText(@"c:\inetpub\home.debb.nl\P1\API\LastP1Record.json",
                json));
        }

        private static void WriteLastRecordsJson(IEnumerable<P1Record> records)
        {
            var json = JsonConvert.SerializeObject(records.Reverse().ToArray(), Formatting.None);
            IoRetryPolicy.Execute(() => File.WriteAllText(@"c:\inetpub\home.debb.nl\P1\API\LastP1Records.json",
                json));
        }

        private static void Using(IDisposable disp)
        {
            Disposables.Push(disp);
        }

        private static readonly Stack<IDisposable> Disposables = new Stack<IDisposable>();
    }
    /*
    public class Startup
    {
        public void Configuration(Owin.IAppBuilder app)
        {
            Owin.OwinExtensions.MapConnection<QueryablePushService<MyPushContext>>( "/events", new ConnectionConfiguration());
        }
    }
    */
    public class MyPushContext
    {
        /// <summary>
        /// Gets a one second timer that produces a new message each second with an incrementing id and timestamp.
        /// </summary>
        /// <value>The one second timer.</value>
        public IQbservable<P1Record> P1Record
        {
            get ; set;
        }
        public IQbservable<string> P1Message
        {
            get;
            set;
        }
    }
}
