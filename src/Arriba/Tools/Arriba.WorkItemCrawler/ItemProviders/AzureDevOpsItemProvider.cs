using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Arriba.Extensions;
using Arriba.Model.Column;
using Arriba.Structures;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Arriba.ItemProviders
{
    public class AzureDevOpsItemProvider : IItemProvider
    {
        public AzureDevOpsItemProvider(CrawlerConfiguration config)
            : this(config.ItemDatabaseName, config.ItemProject, config.AzPat, config.WorkItemTypes)
        {
        }

        public AzureDevOpsItemProvider(string organization, string project, string pat, IList<string> workitemTypes = null)
        {
            BaseUri = new Uri($"https://dev.azure.com/{organization}/{project}/");
            AnalyticsUri = new Uri($"https://analytics.dev.azure.com/{organization}/{project}/");

            Organization = organization;
            Project = project;
            WorkItemConstraint = GetWorkItemConstraint(workitemTypes);

            Http = GetClient(ToBase64($"Basic:{pat}"));
            Columns = new Lazy<Task<IList<ColumnDetails>>>(ReadColumnsAsync);
        }

        public string Organization { get; }

        public string Project { get; }

        private Uri BaseUri { get; }

        private Uri AnalyticsUri { get; }

        private HttpClient Http { get; }

        private string WorkItemConstraint { get; }

        private Lazy<Task<IList<ColumnDetails>>> Columns { get; }

        public void Dispose()
        {
            // Do nothing
        }

        public async Task<IList<ColumnDetails>> GetColumnsAsync()
        {
            return await Columns.Value;
        }


        public async Task<DataBlock> GetItemBlockAsync(IEnumerable<ItemIdentity> items, IEnumerable<string> columnNames)
        {
            var query = await GetWorkItems(items);

            //// Copy the item field values into a DataBlock and track the last cutoff per group
            DataBlock result = new DataBlock(await GetColumnsAsync(), query.Count);

            for (int itemIndex = 0; itemIndex < result.RowCount; ++itemIndex)
            {
                var item = query[itemIndex];

                int fieldIndex = 0;
                foreach (var columnName in columnNames)
                {
                    var c = await GetColumnFromName(columnName);

                    try
                    {
                        if (c.Alias == "System.Id")
                        {
                            result[itemIndex, fieldIndex] = ItemProviderUtilities.Canonicalize(item.Id);
                        }
                        else
                        {
                            if (item.Fields.ContainsKey(c.Alias))
                            {
                                var fieldValue = GetFieldValue(item.Fields[c.Alias]);
                                result[itemIndex, fieldIndex] = ItemProviderUtilities.Canonicalize(fieldValue);
                            }
                            else
                            {
                                result[itemIndex, fieldIndex] = null;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        result[itemIndex, fieldIndex] = null;
                        Trace.WriteLine(string.Format("Error Getting '{0}' from item {1}. Skipping field. Detail: {2}", c.Name, item.Id, ex.ToString()));
                    }

                    fieldIndex++;
                }
            }

            return result;
        }

        private object GetFieldValue(JToken item)
        {
            var simpleValue = (item as JValue)?.Value;

            if (simpleValue == null)
            {
                var obj = item as JObject;
                JToken result;
                if (obj.TryGetValue("uniqueName", out result))
                {
                    simpleValue = (result as JValue)?.Value;
                }
            }

            return simpleValue ?? item;
        }

        private async Task<ColumnDetails> GetColumnFromName(string columnName)
        {
            var set = await Columns.Value;
            return set.First(x => string.Equals(x.Name, columnName, StringComparison.OrdinalIgnoreCase));
        }

        public async Task<IList<ItemIdentity>> GetItemsChangedBetweenAsync(DateTimeOffset start, DateTimeOffset end)
        {
            Uri fieldsUri = GetAnalyticsUri($"_odata/v3.0-preview/WorkItems?$select=WorkItemId,ChangedDate,Revision&$orderby=ChangedDate asc&$filter=ChangedDate gt {Format(start)} and ChangedDate lt {Format(end)} {WorkItemConstraint}");
            var webRequest = await Http.GetAsync(fieldsUri);
            if (!IsSuccessFull(webRequest.StatusCode))
                throw new ArribaException($"Error performing request: Status code: {webRequest.StatusCode}");
            var json = await webRequest.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<ValueRequest<IList<AzureDevOpsChangedWorkItem>>>(json);

            if (result != null)
                return result.Value
                    .Select(x => new ItemIdentity(x.WorkItemId, x.ChangedDate))
                    .ToList();

            return null;
        }

        private bool IsSuccessFull(HttpStatusCode statusCode)
        {
            return statusCode != HttpStatusCode.BadRequest &&
                   statusCode != HttpStatusCode.Unauthorized &&
                   statusCode != HttpStatusCode.Forbidden;
        }

        private async Task<IList<ColumnDetails>> ReadColumnsAsync()
        {
            return (await GetSystemFieldsAsync())
                .Select(x =>
                {
                    bool primaryKey = string.Equals(x.ReferenceName, "System.Id", StringComparison.OrdinalIgnoreCase);
                    return new ColumnDetails(x.Name, MapType(x), null, x.ReferenceName, primaryKey);
                })
                .ToList()
                .AsReadOnly();
        }

        private static string Format(DateTimeOffset input)
        {
            return input.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ");
        }

        private string ToBase64(string toEncode)
        {
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(toEncode));
        }

        private static string MapType(AzureDevOpsFieldDefinition column)
        {
            if (column.Name == "Attachments" || column.Name == "Links") return "json";

            if (column.Name == "Tags") return "stringset";

            switch (column.Type.ToLowerInvariant())
            {
                case "boolean":
                    return "bool";
                case "integer":
                    return "int";
                case "history":
                    return "json";
                case "plaintext":
                case "treepath":
                case "string":
                    return "string";
                case "double":
                    return "double";
                case "datetime":
                    return "datetime";
                case "html":
                    return "html";
                default:
                    return column.Type;
            }
        }

        private static HttpClient GetClient(string pat)
        {
            var client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", pat);
            return client;
        }

        private async Task<IList<AzureDevOpsFieldDefinition>> GetSystemFieldsAsync()
        {
            Uri fieldsUri = GetUri("_apis/wit/fields");
            var webRequest = await Http.GetAsync(fieldsUri);
            var json = await webRequest.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<ValueRequest<IList<AzureDevOpsFieldDefinition>>>(json);
            return result.Value;
        }

        private async Task<IList<AzWorkItem>> GetWorkItems(IEnumerable<ItemIdentity> items)
        {
            List<AzWorkItem> result = new List<AzWorkItem>();

            foreach (var batch in items.Page(200))
            {
                var uri = GetUri($"_apis/wit/workitems/?ids={string.Join(",", batch.Select(x => x.ID))}");
                var webRequest = await Http.GetAsync(uri);
                var json = await webRequest.Content.ReadAsStringAsync();
                var jsonResult = JsonConvert.DeserializeObject<ValueRequest<IList<AzWorkItem>>>(json);
                result.AddRange(jsonResult.Value);
            }

            return result;
        }

        private Uri GetUri(string uriPart)
        {
            var uri = new Uri(BaseUri, uriPart);

            const string apiVersion = "api-version=5.0";

            if (string.IsNullOrEmpty(uri.Query))
            {
                uri = new Uri($"{uri.AbsoluteUri}?{apiVersion}");
            }
            else
            {
                uri = new Uri($"{uri.AbsoluteUri}&{apiVersion}");
            }

            return uri;
        }

        private Uri GetAnalyticsUri(string uriPart)
        {
            return new Uri(AnalyticsUri, uriPart);
        }

        private string GetWorkItemConstraint(IList<string> workitemTypes)
        {
            var result = string.Empty;

            if (workitemTypes != null && workitemTypes.Count > 0)
            {
                var list = workitemTypes.Select(x => $"WorkItemType eq '{x}'");
                result = $"and({string.Join(" or ", list)})";
            }

            return result;
        }

        private class AzureDevOpsFieldDefinition
        {
            public string Name { get; set; }
            public string ReferenceName { get; set; }
            public string Type { get; set; }
        }

        private class AzureDevOpsChangedWorkItem
        {
            public int WorkItemId { get; set; }
            public int Revision { get; set; }
            public DateTimeOffset ChangedDate { get; set; }
        }
    }

    public class ValueRequest<T>
    {
        public T Value { get; set; }
    }

    public class AzWorkItem
    {
        public string Id { get; set; }
        public string Rev { get; set; }
        public IDictionary<string, JToken> Fields { get; set; }
    }
}



