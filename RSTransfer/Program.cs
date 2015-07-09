using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RSTransfer.LocalStore;
using RSTransfer.ReportingServiceStore;
using RSTransfer.Store;

namespace RSTransfer
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var trace = new TraceSource("RSTransfer", SourceLevels.Verbose);
            trace.Listeners.Add(new ConsoleTraceListener());
            trace.Listeners.Add(new TextWriterTraceListener("RSTransfer.log"));

            if (args == null || args.Length < 2)
            {
                Console.WriteLine("Please specify source and target for reports. Use URL or local directory path.");
            }
            else
            {
                var sourcePath = args[0];
                var targetPath = args[1];

                try
                {
                    trace.TraceInformation("RS transfer starting: \"{0}\" -> \"{1}\"", sourcePath, targetPath);

                    var source = CreateReportsStore(sourcePath);
                    var target = CreateReportsStore(targetPath);

                    var transfer = new ReportsTransfer(trace);
                    transfer.Execute(source, target);
                    trace.TraceInformation("Completed :)");
                }
                catch (Exception ex)
                {
                    trace.TraceEvent(TraceEventType.Error, 1, ex.ToString());
                }
            }
        }

        private static IReportsStore CreateReportsStore(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentNullException("path");
            }

            if (path.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                path.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                var url = path;
                if (!url.EndsWith("/"))
                {
                    url += "/";
                }

                url += "ReportService2010.asmx";
                return new ReportsStore(url, null);
            }
            else
            {
                return new LocalReportsStore(path);
            }
        }
    }
}
