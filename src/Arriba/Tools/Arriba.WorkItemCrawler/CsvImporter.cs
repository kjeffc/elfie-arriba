// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using Arriba.ItemConsumers;
using Arriba.ItemProviders;
using Arriba.Structures;

namespace Arriba
{
    internal class CsvImporter
    {
        private const int BatchSize = 100;
        private const int WriteAfterMinutes = 20;

        private string ConfigurationName { get; set; }
        private CrawlerConfiguration Configuration { get; set; }
        private string ChangedDateColumn { get; set; }

        public CsvImporter(CrawlerConfiguration config, string configurationName, string changedDateColumn)
        {
            Configuration = config;
            ConfigurationName = configurationName;
            ChangedDateColumn = changedDateColumn;
        }

        public void Import(IItemConsumer consumer)
        {
            DateTimeOffset lastCutoffWritten = ItemProviderUtilities.LoadLastCutoff(Configuration.ArribaTable, ConfigurationName + ".CSV", false);
            Stopwatch saveWatch = null;

            CsvReaderItemProvider provider = null;

            try
            {
                provider = new CsvReaderItemProvider(Configuration.ArribaTable, ChangedDateColumn, lastCutoffWritten, DateTime.UtcNow);

                while (true)
                {
                    // Get another batch of items
                    Console.Write("[");
                    DataBlock block = provider.GetNextBlock(BatchSize);
                    if (block == null || block.RowCount == 0) break;

                    // Append them
                    Console.Write("]");
                    consumer.Append(block);

                    // Track the last item changed date
                    DateTime lastItemInBlock;
                    Value.Create(block[block.RowCount - 1, block.IndexOfColumn(ChangedDateColumn)]).TryConvert(out lastItemInBlock);
                    if (lastItemInBlock > lastCutoffWritten)
                    {
                        lastCutoffWritten = lastItemInBlock;
                    }

                    if (saveWatch == null) saveWatch = Stopwatch.StartNew();
                    if (saveWatch.Elapsed.TotalMinutes > WriteAfterMinutes)
                    {
                        Save(consumer, lastCutoffWritten);
                        saveWatch.Restart();
                    }
                }
            }
            finally
            {
                provider.Dispose();

                Save(consumer, lastCutoffWritten);
                consumer.Dispose();
            }
        }

        private void Save(IItemConsumer consumer, DateTimeOffset lastCutoffWritten)
        {
            Trace.WriteLine("Saving...");
            consumer.Save();
            Trace.WriteLine("Save Complete.");

            // Record the new last cutoff written
            ItemProviderUtilities.SaveLastCutoff(Configuration.ArribaTable, ConfigurationName + ".CSV", lastCutoffWritten);
        }
    }
}
