using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RSTransfer.ReportingServices;
using RSTransfer.Store;

namespace RSTransfer.ReportingServiceStore
{
    public class ReportsStore : IReportsStore
    {
        private const char FolderSeparator = '/';
        private const string FolderItemType = "Folder";
        private const string ReportItemType = "Report";
        private const string DataSourceItemType = "DataSource";

        public ReportsStore(string url, string rootPath)
        {
            if (string.IsNullOrEmpty(url))
            {
                throw new ArgumentNullException("url");
            }

            this.Url = url;
            this.RootPath = rootPath;
        }

        public string Url { get; private set; }

        public string RootPath { get; private set; }

        public IEnumerable<PathItem> GetFolders(string path)
        {
            var fullPath = NormalizePath(path);
            var client = this.CreateWebClient();
            var items = client.ListChildren(fullPath, false);
            return items.Where(i => IsFolderItemType(i.TypeName)).Select(ConvertToPathItem).ToList();
        }

        public IEnumerable<PathItem> GetReports(string path)
        {
            var fullPath = NormalizePath(path);
            var client = this.CreateWebClient();
            var items = client.ListChildren(fullPath, false);
            return items.Where(i => IsReportItemType(i.TypeName)).Select(ConvertToPathItem).ToList();
        }

        public byte[] GetReport(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentNullException("path");
            }

            var fullPath = NormalizePath(path);
            var client = this.CreateWebClient();
            return client.GetItemDefinition(fullPath);
        }

        public IEnumerable<Store.ItemReference> GetDataSourceReferences(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentNullException("path");
            }

            var fullPath = NormalizePath(path);
            var client = this.CreateWebClient();
            var references = client.GetItemReferences(fullPath, DataSourceItemType);
            var result = references.Select(r => new Store.ItemReference() { Name = r.Name, Reference = r.Reference }).ToList();
            return result.Count != 0 ? result : null;
        }

        public ILookup<string, string> GetACL(string path)
        {
            var fullPath = NormalizePath(path);
            var client = this.CreateWebClient();

            bool inheritParent = false;
            var policies = client.GetPolicies(fullPath, out inheritParent);
            if (inheritParent)
            {
                return null;
            }
            else
            {
                var roles = policies.SelectMany(p => p.Roles.Select(r => new { Policy = p, Role = r }));
                return roles.ToLookup(r => r.Policy.GroupUserName, r => r.Role.Name, StringComparer.OrdinalIgnoreCase);
            }
        }

        public PathItem CreateFolder(string foldername, string path)
        {
            if (string.IsNullOrEmpty(foldername))
            {
                throw new ArgumentNullException("foldername");
            }

            path = NormalizePath(path);
            var children = this.GetFolders(path);
            var item = children.FirstOrDefault(c => string.Equals(c.Name, foldername, StringComparison.OrdinalIgnoreCase));
            if (item != null)
            {
                return item;
            }
            else
            {
                var client = this.CreateWebClient();
                var catalogItem = client.CreateFolder(foldername, path, null);
                return ConvertToPathItem(catalogItem);
            }
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

            path = NormalizePath(path);
            var children = this.GetReports(path);
            var item = children.FirstOrDefault(c => string.Equals(c.Name, name, StringComparison.OrdinalIgnoreCase));
            if (item != null)
            {
                return item;
            }
            else
            {
                var client = this.CreateWebClient();
                Warning[] warnings = null;
                var catalogItem = client.CreateCatalogItem(ReportItemType, name, path, false, rdl, null, out warnings);
                return ConvertToPathItem(catalogItem);
            }
        }

        public void SetDataSourceReferences(string path, IEnumerable<Store.ItemReference> references)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentNullException("path");
            }
            if (references == null)
            {
                throw new ArgumentNullException("references");
            }

            var fullPath = NormalizePath(path);
            var client = this.CreateWebClient();
            var itemReferences = references.Select(r => new ReportingServices.ItemReference() { Name = r.Name, Reference = r.Reference }).ToArray();
            client.SetItemReferences(fullPath, itemReferences);
        }

        public void SetACL(string path, ILookup<string, string> acl)
        {
            var fullPath = NormalizePath(path);
            var client = this.CreateWebClient();

            if (acl != null)
            {
                var policies = acl.Select(g => new Policy
                    {
                        GroupUserName = g.Key,
                        Roles = g.Select(r => new Role() { Name = r }).ToArray()
                    }).ToArray();
                client.SetPolicies(fullPath, policies);
            }
            else
            {
                client.InheritParentSecurity(fullPath);
            }
        }

        private ReportingService2010 CreateWebClient()
        {
            var result = new ReportingService2010();
            result.Url = this.Url;
            result.UseDefaultCredentials = true;
            result.PreAuthenticate = true;
            return result;
        }

        private static string NormalizePath(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return "" + FolderSeparator;
            }
            else if (path[0] != FolderSeparator)
            {
                return FolderSeparator + path;
            }
            else
            {
                return path;
            }
        }

        private static bool IsFolderItemType(string itemType)
        {
            return string.Equals(itemType, FolderItemType, StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsReportItemType(string itemType)
        {
            return string.Equals(itemType, ReportItemType, StringComparison.OrdinalIgnoreCase);
        }

        private static PathItem ConvertToPathItem(CatalogItem item)
        {
            return new PathItem()
            {
                Name = item.Name,
                Path = item.Path
            };
        }
    }
}
