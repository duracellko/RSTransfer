using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RSTransfer.Store;

namespace RSTransfer
{
    public class ReportsTransfer
    {
        private readonly TraceSource trace;

        public ReportsTransfer(TraceSource trace)
        {
            if (trace == null)
            {
                throw new ArgumentNullException("trace");
            }

            this.trace = trace;
        }

        public void Execute(IReportsStore source, IReportsStore target)
        {
            var traverse = new Stack<Tuple<string, string>>();
            traverse.Push(Tuple.Create(source.RootPath, target.RootPath));

            while (traverse.Count != 0)
            {
                var directory = traverse.Pop();
                this.CopyReports(source, directory.Item1, target, directory.Item2);
                this.CopyFolders(source, directory.Item1, target, directory.Item2, traverse);
            }
        }

        private void CopyReports(IReportsStore source, string sourceFolder, IReportsStore target, string targetFolder)
        {
            this.trace.TraceInformation("Copying reports from: {0} -> {1}", sourceFolder, targetFolder);
            try
            {
                var reports = source.GetReports(sourceFolder);
                var existingReports = target.GetReports(targetFolder);
                var newReports = reports.Where(r => !existingReports.Any(er => string.Equals(er.Name, r.Name, StringComparison.OrdinalIgnoreCase)));
                foreach (var report in newReports)
                {
                    this.CopyReport(source, report.Path, target, targetFolder, report.Name);
                }
            }
            catch (Exception ex)
            {
                this.HandleException(ex);
            }
        }

        private PathItem CopyReport(IReportsStore source, string sourceReport, IReportsStore target, string targetFolder, string targetReport)
        {
            this.trace.TraceInformation("Copying report from: {0} -> {1}", sourceReport, targetReport);
            try
            {
                var reportDefinition = source.GetReport(sourceReport);
                var dataSourceReferences = source.GetDataSourceReferences(sourceReport);
                var result = target.CreateReport(targetReport, targetFolder, reportDefinition);
                if (dataSourceReferences != null && dataSourceReferences.Any())
                {
                    target.SetDataSourceReferences(result.Path, dataSourceReferences);
                }
                return result;
            }
            catch (Exception ex)
            {
                this.HandleException(ex);
            }

            return null;
        }

        private void CopyFolders(IReportsStore source, string sourceFolder, IReportsStore target, string targetFolder, Stack<Tuple<string, string>> traverse)
        {
            this.trace.TraceInformation("Copying folders from: {0} -> {1}", sourceFolder, targetFolder);
            try
            {
                var folders = source.GetFolders(sourceFolder);
                foreach (var folder in folders)
                {
                    var newFolder = this.CopyFolder(source, folder.Path, target, targetFolder, folder.Name);
                    if (newFolder != null)
                    {
                        traverse.Push(Tuple.Create(folder.Path, newFolder.Path));
                    }
                }
            }
            catch (Exception ex)
            {
                this.HandleException(ex);
            }
        }

        private PathItem CopyFolder(IReportsStore source, string sourceFolder, IReportsStore target, string targetFolder, string name)
        {
            this.trace.TraceInformation("Copying folder from: {0} -> {1}", sourceFolder, name);
            try
            {
                var result = target.CreateFolder(name, targetFolder);
                var acl = source.GetACL(sourceFolder);
                if (acl != null)
                {
                    target.SetACL(result.Path, acl);
                }

                return result;
            }
            catch (Exception ex)
            {
                this.HandleException(ex);
            }

            return null;
        }

        private void HandleException(Exception exception)
        {
            this.trace.TraceEvent(TraceEventType.Error, 0, exception.ToString());
        }
    }
}
