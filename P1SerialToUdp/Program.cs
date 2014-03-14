using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reactive.Threading.Tasks;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

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
                if (_messageSubscription != null)
                {
                    _messageSubscription.Dispose();
                    _messageSubscription = null;
                }
                if (_serialConnection != null)
                {
                    _serialConnection.Dispose();
                    _serialConnection = null;
                }
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
                var serialPort = new SerialPort("COM3", 9600, Parity.Even, 7);
                serialPort.Open();
                var serialObservable = serialPort.ToObservable().Publish();
                
                var messagesObservable =
                    serialObservable
                        .SkipWhile(b => b != '/')
                        .TakeUntil(b => b == '!')
                        .ToArray()
                        .Repeat();

                _messageSubscription = messagesObservable.Subscribe(SaveBytes, e => _logFunc(e.Message),
                    () => _logFunc("Doneth")
                    );

                _serialConnection = serialObservable.Connect();
                _logFunc("Started.");
            }
            catch (Exception e)
            {
                _logFunc("Exception1: " + e.Message);
                throw;
            }
        }

        private static IDisposable _messageSubscription;
        private static IDisposable _serialConnection;
    }

    public static class Extensions
    {
        public static IObservable<TSource> TakeUntil<TSource>(
                this IObservable<TSource> source, Func<TSource, bool> predicate)
        {
            return Observable
                .Create<TSource>(o => source.Subscribe(x =>
                {
                    o.OnNext(x);
                    if (predicate(x))
                        o.OnCompleted();
                },
                o.OnError,
                o.OnCompleted
            ));
        }
        
        public static IObservable<byte> ToObservable(this SerialPort openPort)
        {
            return openPort.BaseStream.ToObservable();
        }

        public static async Task<byte[]> ReadAsync(this Stream stream, int bufSize = 1024)
        {
            var buffer = new byte[bufSize];
            var read = await stream.ReadAsync(buffer, 0, bufSize);
            return new ArraySegment<byte>(buffer, 0, read).ToArray();
        }

        public static IObservable<byte> ToObservable(this Stream stream)
        {
            return
                Observable.FromAsync(() => stream.ReadAsync())
                    .Repeat()
                    .TakeWhile(_ => _.Length != 0)
                    .SelectMany(arr => arr.ToObservable());
        }
    }
}
