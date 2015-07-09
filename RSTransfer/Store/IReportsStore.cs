using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RSTransfer.Store
{
    public interface IReportsStore
    {
        string RootPath { get; }

        IEnumerable<PathItem> GetFolders(string path);

        IEnumerable<PathItem> GetReports(string path);

        byte[] GetReport(string path);

        IEnumerable<ItemReference> GetDataSourceReferences(string path);

        ILookup<string, string> GetACL(string path);

        PathItem CreateFolder(string foldername, string path);

        PathItem CreateReport(string name, string path, byte[] rdl);

        void SetDataSourceReferences(string path, IEnumerable<ItemReference> references);

        void SetACL(string path, ILookup<string, string> acl);
    }
}
