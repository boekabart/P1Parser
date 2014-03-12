using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace P1SerialToUdp
{
    partial class P1BroadcastService : ServiceBase
    {
        public P1BroadcastService()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            Program.Start(Log);
        }

        protected override void OnStop()
        {
            Program.Stop(Log);
        }

        private static void Log(string xx)
        {
            var lines = xx.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).Select(x => string.Format("{0:r} {1}", DateTime.UtcNow, x)).ToArray();
            File.AppendAllLines(Path.Combine(Program.LogFolder, "P1Service.log"), lines);
        }
    }
}
