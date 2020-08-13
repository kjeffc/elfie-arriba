// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Arriba.Configuration;
using Arriba.ItemConsumers;
using Arriba.ItemProviders;
using Arriba.Model.Column;

namespace Arriba
{
    internal class Program
    {
        private static async Task<int> Main(string[] args)
        {
            Trace.Listeners.Add(new TextWriterTraceListener(Console.Out));

            if (args.Length < 2)
            {
                Usage();
                return -1;
            }

            var configLoader = new ArribaConfigurationLoader(args);
            var configurationName = configLoader.GetStringValue("configName");
            var mode = configLoader.GetStringValue("mode", "-i").ToLowerInvariant();
            Trace.WriteLine("Launching Crawler");
            Trace.WriteLine($"Using configName: {configurationName} mode:{mode}");

            using (FileLock locker = FileLock.TryGet(string.Format("Arriba.TfsWorkItemCrawler.{0}.lock", configurationName)))
            {
                try
                {
                    // Ensure we got the file lock (no duplicate crawlers
                    if (locker == null)
                    {
                        Console.WriteLine("Another instance running. Stopping.");
                        return -2;
                    }

                    var databaseConfig = GetJsonPath(configurationName);
                    if (!string.IsNullOrEmpty(databaseConfig))
                    {
                        configLoader.AddJsonSource(databaseConfig);
                    }

                    var config = configLoader.Bind<CrawlerConfiguration>("Arriba");

                    // Build the item consumer
                    IItemConsumer consumer = ItemConsumerUtilities.Build(config);

                    // Build the item provider
                    IItemProvider provider = ItemProviderUtilities.Build(config);

                    // Determine the list of columns to crawl
                    IEnumerable<ColumnDetails> columns = await provider.GetColumnsAsync();
                    if (config.ColumnsToInclude.Count > 0) columns = columns.Where(cd => config.ColumnsToInclude.Contains(cd.Name));
                    if (config.ColumnsToExclude.Count > 0) columns = columns.Where(cd => !config.ColumnsToExclude.Contains(cd.Name));
                    List<ColumnDetails> columnsToAdd = new List<ColumnDetails>(columns);

                    // Create the target table (if it doesn't already exist)
                    consumer.CreateTable(columnsToAdd, config.LoadPermissions());

                    // Build a crawler and crawl the items in restartable order
                    DefaultCrawler crawler = new DefaultCrawler(config, columnsToAdd.Select((cd) => cd.Name), configurationName, !mode.Equals("-i"));
                    await crawler.Crawl(provider, consumer);

                    return 0;
                }
                catch (AggregateException ex)
                {
                    foreach (Exception inner in ex.InnerExceptions)
                    {
                        Trace.TraceError(string.Format("ERROR: {0}\r\n{1}", Environment.CommandLine, inner));
                    }

                    return -2;
                }
                catch (Exception ex)
                {
                    Trace.TraceError(string.Format("ERROR: {0}\r\n{1}", Environment.CommandLine, ex));
                    return -2;
                }
            }
        }

        private static string GetJsonPath(string configurationName)
        {
            var d = new DirectoryInfo(Directory.GetCurrentDirectory());
            var configPath = Path.Combine("Databases", configurationName, "appsettings.json");
            var result = string.Empty;

            while (d != null)
            {
                result = Path.Combine(d.FullName, configPath);
                if (File.Exists(result))
                {
                    break;
                }
                else
                {
                    Trace.WriteLine($"Probing {result}");
                    result = null;
                    d = d.Parent;
                }
            }

            return result;
        }

        private static void Usage()
        {
            Console.WriteLine(
@" Usage: Arriba.TfsWorkItemCrawler configName=<configName> mode=<mode> [<modeArguments>]
     'Arriba.TfsWorkItemCrawler configName=MyDatabase mode=-i' -> Append updated MyDatabase items from primary provider.
     'Arriba.TfsWorkItemCrawler configName=MyDatabase mode=-r' -> Rebuild all MyDatabase items from primary provider.
     'Arriba.TfsWorkItemCrawler configName=MyDatabase mode=-password -> Local User Encrypt a TFS online password for config.
");
        }
    }
}
