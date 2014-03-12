using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace P1SerialToUdp
{
    class Program
    {
        static Program()
        {
            StartFolder = System.IO.Path.GetDirectoryName(new Uri(System.Reflection.Assembly.GetEntryAssembly().CodeBase).LocalPath);
            DataFolder = Path.Combine(StartFolder, "Data");
            LogFolder = Path.Combine(StartFolder, "Logs");
        }

        public static readonly string StartFolder;
        public static readonly string DataFolder;
        public static readonly string LogFolder;
        private static UdpClient _udp;
        private const int Port = 9191;
        private static UdpClient Udp
        {

            get { return _udp ?? (_udp = new UdpClient()); }
        }

        private static void SaveBytes(byte[] packet)
        {
            var fileName = DateTime.UtcNow.ToString("s").Replace(':','-');
            var folderName = fileName.Substring(0, 10);
            var dirPath = Path.Combine(DataFolder, folderName);
            if (!System.IO.Directory.Exists(dirPath))
                System.IO.Directory.CreateDirectory(dirPath);
            var fullPath = Path.Combine(dirPath, fileName);
            File.WriteAllBytes(fullPath, packet);
        }

        static void BroadcastBytes(byte[] packet)
        {
            Udp.Send(packet, packet.Length, IPAddress.Broadcast.ToString(), Port);
        }

        private static void Main(string[] args)
        {
            if (!System.IO.Directory.Exists(DataFolder))
                System.IO.Directory.CreateDirectory(DataFolder);

            if (!System.IO.Directory.Exists(LogFolder))
                System.IO.Directory.CreateDirectory(LogFolder);

            if (Environment.UserInteractive)
            {
                Start(Console.WriteLine);
                while (!Console.KeyAvailable)
                {
                    Thread.Sleep(250);
                }
                Stop(Console.WriteLine);
            }
            else
            {
                var servicesToRun = new ServiceBase[] { new P1BroadcastService() };
                ServiceBase.Run(servicesToRun);
            }
        }

        public static void Stop(Action<string> logFunc)
        {
            logFunc("Stop.");
            try
            {
                if (SerialPort != null)
                {
                    logFunc("Closing Serial Port");
                    SerialPort.Close();
                    SerialPort = null;
                }
                if (TheThread != null)
                {
                    logFunc("Joining Thread");
                    TheThread.Join();
                    TheThread = null;
                }
            }
            catch (Exception e)
            {
                logFunc("Exception4: " + e.Message);
                throw;
            }
            logFunc("Stopped.");
        }

        public static void Start(Action<string> logFunc)
        {
            try
            {
                logFunc("Start.");
                SerialPort = new SerialPort("COM3", 9600, Parity.Even, 7);
                SerialPort.Open();
                logFunc("Serial port open");
                TheThread = new Thread(() => ThreadFunc(logFunc, SerialPort));
                TheThread.Priority = ThreadPriority.BelowNormal;
                TheThread.Start();
                logFunc("Started.");
            }
            catch (Exception e)
            {
                logFunc("Exception1: " + e.Message);
                throw;
            }
        }

        private static void ThreadFunc(Action<string> logFunc, SerialPort serialPort)
        {
            try
            {
                while (serialPort.IsOpen)
                {
                    try
                    {
                        logFunc("Waiting for message...");
                        var message = ReadP1Message(serialPort, logFunc);
                        logFunc("Received message!");
                        SaveBytes(message);
                    }
                    catch (Exception e)
                    {
                        logFunc("Exception3: " + e.Message);
                    }
                }
            }
            catch (Exception e)
            {
                logFunc("Exception2: " + e.Message);
            }
            logFunc("Thread Exit");
        }

        static readonly byte[] Buffer = new byte[4096];
        private static Thread TheThread;
        private static SerialPort SerialPort;

        private static byte[] ReadP1Message(SerialPort sp, Action<string> logFunc)
        {
            var read = 0;
            while (true)
            {
                read += sp.Read(Buffer, read, Buffer.Length - read);
                if (!BufferStartsWithP1StartPhrase(Buffer, read))
                {
                    logFunc(string.Format("Message ({1}) didn't start correctly ({0} '{2}'), resetting", Buffer[0], read, (Char)Buffer[0]));
                    read = 0;
                }
                if (BufferEndsInP1EndPhrase(Buffer, read))
                {
                    return Buffer.Take(read).ToArray();
                }
                if (read == Buffer.Length || read > 1000)
                {
                    logFunc(string.Format("Message longer ({0}) than expected, resetting ({1} '{2}')", read, Buffer[read - 1], (Char)Buffer[read - 1]));
                    read = 0;
                }
            }
        }

        private static bool BufferEndsInP1EndPhrase(byte[] bytes, int read)
        {
            return read >= 3 && bytes[read - 1] == '\n' && bytes[read - 2] == '\r' && bytes[read - 3] == '!';
        }

        private static bool BufferStartsWithP1StartPhrase(byte[] bytes, int read)
        {
            return read > 0 && bytes[0] == '/';
        }
    }
}
