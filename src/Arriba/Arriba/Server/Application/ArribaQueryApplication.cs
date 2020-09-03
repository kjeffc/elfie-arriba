// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Security.Principal;
using System.Threading.Tasks;
using Arriba.Communication;
using Arriba.Communication.Application;
using Arriba.Configuration;
using Arriba.Model;
using Arriba.Model.Query;
using Arriba.Server.Authentication;
using Arriba.Server.Hosting;
using System.Collections.Specialized;
using Arriba.Communication.Server.Application;
using Arriba.Structures;
using Arriba.Serialization;
using Arriba.Serialization.Csv;
using Arriba.Model.Column;
using System.Collections.Generic;

namespace Arriba.Server
{
    /// <summary>
    /// Arriba restful application for query operations.
    /// </summary>
    public class ArribaQueryApplication : ArribaApplication
    {
        private const string DefaultFormat = "dictionary";
        private readonly IArribaQueryServices _service;
        private static readonly ActivitySource ArribaQueryApplicationSource = new ActivitySource(".Arriba.Server.ArribaQueryApplication");

        public ArribaQueryApplication(DatabaseFactory f, ClaimsAuthenticationService auth, ISecurityConfiguration securityConfiguration, IArribaQueryServices queryServices)
            : base(f, auth, securityConfiguration)
        {
            _service = queryServices;

            // /table/foo?type=select
            this.GetAsync(new RouteSpecification("/table/:tableName", new UrlParameter("action", "select")), this.Select);
            this.PostAsync(new RouteSpecification("/table/:tableName", new UrlParameter("action", "select")), this.Select);

            // /table/foo?type=distinct
            this.GetAsync(new RouteSpecification("/table/:tableName", new UrlParameter("action", "distinct")), this.Distinct);
            this.PostAsync(new RouteSpecification("/table/:tableName", new UrlParameter("action", "distinct")), this.Distinct);

            // /table/foo?type=aggregate
            this.GetAsync(new RouteSpecification("/table/:tableName", new UrlParameter("action", "aggregate")), this.Aggregate);
            this.PostAsync(new RouteSpecification("/table/:tableName", new UrlParameter("action", "aggregate")), this.Aggregate);

            this.GetAsync(new RouteSpecification("/allCount"), this.AllCount);
            this.GetAsync(new RouteSpecification("/suggest"), this.Suggest);
        }

        private async Task<IResponse> Select(IRequestContext ctx, Route route)
        {
            string tableName = GetAndValidateTableName(route);
            var user = ctx.Request.User;
            NameValueCollection p = await ParametersFromQueryStringAndBody(ctx);
            string outputFormat = ctx.Request.ResourceParameters["fmt"] ?? "";
            string rssIURL = ctx.Request.ResourceParameters["iURL"] ?? "";
            Table table = this.Database[tableName];

            try
            {
                var result = _service.QueryTableForUser(tableName, p, ctx, user);
                var query = result.Query as SelectQuery;

                // Format the result in the return format
                switch ((outputFormat ?? "").ToLowerInvariant())
                {
                    case "":
                    case "json":
                        return ArribaResponse.Ok(result);
                    case "csv":
                        return ToCsvResponse(result, $"{tableName}-{DateTime.Now:yyyyMMdd}.csv");
                    case "rss":
                        // If output rss only the ID column
                        query.Columns = new string[] { table.IDColumn.Name };
                        return ToRssResponse(result, "", $"{query.TableName} : {query.Where}", rssIURL);
                    default:
                        throw new ArgumentException($"OutputFormat [fmt] passed, '{outputFormat}', was invalid.");
                }
            }
            catch (Exception ex)
            {
                return ExceptionToArribaResponse(ex);
            }

        }

        private static IResponse ToCsvResponse(SelectResult result, string fileName)
        {
            const string outputMimeType = "text/csv; encoding=utf-8";

            var resp = new StreamWriterResponse(outputMimeType, async (s) =>
            {
                SerializationContext context = new SerializationContext(s);
                var items = result.Values;

                // ***Crazy Excel Business***
                // This is pretty ugly. If the first 2 chars in a CSV file as ID, then excel is  thinks the file is a SYLK 
                // file not a CSV File (!) and will alert the user. Excel does not care about output mime types. 
                // 
                // To work around this, and have a _nice_ experience for csv export, we'll modify 
                // the first column name to " ID" to trick Excel. It's not perfect, but it'll do.
                // 
                // As a mitigation for round-tripping, the CsvReader will trim column names. Sigh. 
                List<string> columns = new List<string>();

                foreach (ColumnDetails column in items.Columns)
                {
                    if (columns.Count == 0 && column.Name.Equals("ID", StringComparison.OrdinalIgnoreCase))
                    {
                        columns.Add(" ID");
                    }
                    else
                    {
                        columns.Add(column.Name);
                    }
                }

                CsvWriter writer = new CsvWriter(context, columns);

                for (int row = 0; row < items.RowCount; ++row)
                {
                    for (int col = 0; col < items.ColumnCount; ++col)
                    {
                        writer.AppendValue(items[row, col]);
                    }

                    writer.AppendRowSeparator();
                }

                context.Writer.Flush();
                await s.FlushAsync();
            });

            resp.AddHeader("Content-Disposition", String.Concat("attachment;filename=\"", fileName, "\";"));

            return resp;
        }

        private static IResponse ToRssResponse(SelectResult result, string rssUrl, string query, string itemUrlWithoutId)
        {
            DateTime utcNow = DateTime.UtcNow;

            const string outputMimeType = "application/rss+xml; encoding=utf-8";

            var resp = new StreamWriterResponse(outputMimeType, async (s) =>
            {
                SerializationContext context = new SerializationContext(s);
                RssWriter w = new RssWriter(context);

                ByteBlock queryBB = (ByteBlock)query;
                w.WriteRssHeader(queryBB, queryBB, rssUrl, utcNow, TimeSpan.FromHours(1));

                ByteBlock baseLink = itemUrlWithoutId;
                var items = result.Values;
                for (int row = 0; row < items.RowCount; ++row)
                {
                    ByteBlock id = ConvertToByteBlock(items[row, 0]);
                    w.WriteItem(id, id, id, baseLink, utcNow);
                }

                w.WriteRssFooter();

                context.Writer.Flush();
                await s.FlushAsync();
            });

            return resp;
        }

        private static ByteBlock ConvertToByteBlock(object value)
        {
            if (value == null) return ByteBlock.Zero;

            if (value is ByteBlock)
            {
                return (ByteBlock)value;
            }
            else if (value is string)
            {
                return (ByteBlock)value;
            }
            else
            {
                return (ByteBlock)(value.ToString());
            }
        }


        private async Task<IResponse> AllCount(IRequestContext ctx, Route route)
        {
            NameValueCollection p = await ParametersFromQueryStringAndBody(ctx);
            var user = ctx.Request.User;
            AllCountResult result;

            try
            {
                result = _service.AllCountForUser(p, ctx, user);
            }
            catch (Exception ex)
            {
                return ExceptionToArribaResponse(ex);
            }

            return ArribaResponse.Ok(result);
        }

        // According to https://github.com/open-telemetry/opentelemetry-dotnet/blob/master/examples/Console/TestRedis.cs
        // if another activity was already started, it'll use it as a source, so this should use Server activity as a parent
        private async Task<IResponse> Suggest(IRequestContext ctx, Route route)
        {
            // You're supposed to put all the logic of your method in the using statement
            using (var activity = ArribaQueryApplicationSource.StartActivity("Suggest"))
            {
                activity?.SetTag("Context", ctx);
            }

            NameValueCollection p = await ParametersFromQueryStringAndBody(ctx);
            IPrincipal user = ctx.Request.User;
            IntelliSenseResult result;

            try
            {
                result = _service.IntelliSenseTableForUser(p, ctx, user);
            }
            catch (Exception ex)
            {
                return ExceptionToArribaResponse(ex);
            }

            return ArribaResponse.Ok(result);
        }

        private async Task<IResponse> Aggregate(IRequestContext ctx, Route route)
        {
            string tableName = GetAndValidateTableName(route);
            var user = ctx.Request.User;
            NameValueCollection p = await ParametersFromQueryStringAndBody(ctx);
            AggregationResult result;
            try
            {
                result = _service.AggregateQueryTableForUser(tableName, p, ctx, user);
            }
            catch (Exception ex)
            {
                return ExceptionToArribaResponse(ex);
            }

            return ArribaResponse.Ok(result);
        }

        private async Task<IResponse> Distinct(IRequestContext ctx, Route route)
        {
            string tableName = GetAndValidateTableName(route);
            var user = ctx.Request.User;
            NameValueCollection p = await ParametersFromQueryStringAndBody(ctx);
            DistinctResult result;
            try
            {
                result = _service.DistinctQueryTableForUser(tableName, p, ctx, user);
            }
            catch (Exception ex)
            {
                return ExceptionToArribaResponse(ex);
            }

            return ArribaResponse.Ok(result);
        }

    }
}