﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Arriba.ParametersCheckers;
using System;

namespace Arriba.ItemConsumers
{
    public static class ItemConsumerUtilities
    {
        public static IItemConsumer Build(CrawlerConfiguration config)
        {
            ParamChecker.ThrowIfNull(config, nameof(config));

            switch (config.ItemConsumer.ToLowerInvariant())
            {
                case "":
                case "arribaclient":
                    return new ArribaClientIndexerItemConsumer(config, config.ArribaServiceUrl ?? "http://localhost:42784");
                case "arribadirect":
                    return new ArribaDirectIndexerItemConsumer(config);
                case "csvwriter":
                    return new CsvWriterItemConsumer(config.ArribaTable, "Changed Date");
                default:
                    throw new InvalidOperationException(String.Format("{0} is an unknown Item Consumer", config.ItemConsumer));
            }
        }
    }
}
