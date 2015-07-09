using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using RSTransfer.Store;

namespace RSTransfer.LocalStore
{
    public class LocalReportsStore : IReportsStore
    {
        private const string ReportExtension = ".rdl";
        private const string DataSourceReferencesExtension = ".dsref";
        private const string ReportsSearchPattern = "*.rdl";

        private const string ItemReferenceElementName = "ItemReference";
        private const string NameAttributeName = "Name";
        private const string ReferenceAttributeName = "Reference";

        private const string PolicyElementName = "Policy";
        private const string RoleElementName = "Role";
        private const string GroupUserNameAttributeName = "GroupUserName";

        public LocalReportsStore(string rootPath)
        {
            if (string.IsNullOrEmpty(rootPath))
            {
                throw new ArgumentNullException("rootPath");
            }

            this.RootPath = rootPath;
        }

        public string RootPath { get; private set; }

        public IEnumerable<PathItem> GetFolders(string path)
        {
            var directory = new DirectoryInfo(path);
            return directory.GetDirectories().Select(ConvertToPathItem).ToList();
        }

        public IEnumerable<PathItem> GetReports(string path)
        {
            var directory = new DirectoryInfo(path);
            return directory.GetFiles(ReportsSearchPattern).Select(ConvertReportToPathItem).ToList();
        }

        public byte[] GetReport(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentNullException("path");
            }

            var reportPath = GetReportPath(path);
            return File.ReadAllBytes(reportPath);
        }

        public IEnumerable<ItemReference> GetDataSourceReferences(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentNullException("path");
            }

            var referencesPath = GetDataSourceReferencesPath(path);
            if (!File.Exists(referencesPath))
            {
                return null;
            }

            var xml = XDocument.Load(referencesPath);
            return xml.Root.Elements(ItemReferenceElementName).Select(e => new ItemReference()
                {
                    Name = (string)e.Attribute(NameAttributeName),
                    Reference = (string)e.Attribute(ReferenceAttributeName)
                }).ToList();
        }

        public ILookup<string, string> GetACL(string path)
        {
            var aclPath = GetAclPath(path);
            if (!File.Exists(aclPath))
            {
                return null;
            }

            var xml = XDocument.Load(aclPath);
            var roles = xml.Root.Elements(PolicyElementName).SelectMany(p => p.Elements(RoleElementName).Select(r => new { Policy = p, Role = r }));
            return roles.ToLookup(r => (string)r.Policy.Attribute(GroupUserNameAttributeName), r => (string)r.Role, StringComparer.OrdinalIgnoreCase);
        }

        public PathItem CreateFolder(string foldername, string path)
        {
            if (string.IsNullOrEmpty(foldername))
            {
                throw new ArgumentNullException("foldername");
            }

            var directory = new DirectoryInfo(Path.Combine(path, foldername));
            if (!directory.Exists)
            {
                directory.Create();
            }

            return ConvertToPathItem(directory);
        }

        public PathItem CreateReport(string name, string path, byte[] rdl)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentNullException("name");
            }
            if (rdl == null)
            {
                throw new ArgumentNullException("rdl");
            }

            var fileName = GetReportPath(name);
            var file = new FileInfo(Path.Combine(path, fileName));
            if (!file.Exists)
            {
                using (var stream = file.OpenWrite())
                {
                    stream.Write(rdl, 0, rdl.Length);
                }
            }

            return ConvertReportToPathItem(file);
        }

        public void SetDataSourceReferences(string path, IEnumerable<ItemReference> references)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentNullException("path");
            }
            if (references == null)
            {
                throw new ArgumentNullException("references");
            }

            var referencesPath = GetDataSourceReferencesPath(path);
            var rootElement = new XElement("ItemReferences", references.Select(r => new XElement(ItemReferenceElementName,
                new XAttribute(NameAttributeName, r.Name),
                new XAttribute(ReferenceAttributeName, r.Reference))));
            var xml = new XDocument(rootElement);
            xml.Save(referencesPath);
        }

        public void SetACL(string path, ILookup<string, string> acl)
        {
            var aclPath = GetAclPath(path);
            if (acl != null)
            {
                var rootElement = new XElement("Policies", acl.Select(p => new XElement(PolicyElementName,
                    new XAttribute(GroupUserNameAttributeName, p.Key),
                    p.Select(r => new XElement(RoleElementName, r)))));
                var xml = new XDocument(rootElement);
                xml.Save(aclPath);
            }
            else
            {
                File.Delete(aclPath);
            }
        }

        private static PathItem ConvertToPathItem(FileSystemInfo item)
        {
            return new PathItem()
            {
                Name = item.Name,
                Path = item.FullName
            };
        }

        private static PathItem ConvertReportToPathItem(FileSystemInfo item)
        {
            var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(item.Name);
            var parent = Path.GetDirectoryName(item.FullName);
            return new PathItem()
            {
                Name = fileNameWithoutExtension,
                Path = Path.Combine(parent, fileNameWithoutExtension)
            };
        }

        private static string GetReportPath(string path)
        {
            if (path.EndsWith(ReportExtension, StringComparison.OrdinalIgnoreCase))
            {
                return path;
            }
            else
            {
                return path + ReportExtension;
            }
        }

        private static string GetDataSourceReferencesPath(string path)
        {
            if (path.EndsWith(ReportExtension, StringComparison.OrdinalIgnoreCase))
            {
                var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(path);
                var parent = Path.GetDirectoryName(path);
                path = Path.Combine(parent, fileNameWithoutExtension);
            }

            return path + DataSourceReferencesExtension;
        }

        private static string GetAclPath(string path)
        {
            return Path.Combine(path, "rssecurity.xml");
        }
    }
}
