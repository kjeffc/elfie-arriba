// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Text;
using Arriba.Extensions;
using Arriba.ParametersCheckers;
using Arriba.Serialization;

namespace Arriba.ItemProviders
{
    public static class ItemProviderUtilities
    {
        // Field length limit is 10MB
        public const int FieldLengthLimitBytes = 10 * 1024 * 1024;

        public static IItemProvider Build(CrawlerConfiguration config)
        {
            ParamChecker.ThrowIfNull(config, nameof(config));

            switch (config.ItemProvider.ToLowerInvariant())
            {
                case "":
                case "azdo":
                    return new AzureDevOpsItemProvider(config);
                default:
                    throw new InvalidOperationException(string.Format("{0} is an unknown Item Provider", config.ItemProvider));
            }
        }

        public const string CutoffLocationFormatString = @"Tables/{0}/Cutoff.{1}.txt";
        public static DateTimeOffset LoadLastCutoff(string tableName, string configurationName, bool rebuild)
        {
            if (string.IsNullOrEmpty(tableName))
            {
                throw new ArgumentException("tableName is null or empty.", "tableName");
            }

            if (string.IsNullOrEmpty(configurationName))
            {
                throw new ArgumentException("configurationName is null or empty.", "configurationName");
            }

            DateTimeOffset allItemsCutoff = DateTimeOffset.UtcNow.AddYears(-20);
            DateTimeOffset cutoff;

            if (rebuild)
            {
                cutoff = allItemsCutoff;
            }
            else
            {
                cutoff = TextSerializer.ReadDateTime(string.Format(CutoffLocationFormatString, tableName, configurationName), allItemsCutoff);
            }

            return cutoff;
        }

        public static void SaveLastCutoff(string tableName, string configurationName, DateTimeOffset cutoff)
        {
            // Write the new cutoff as long as it's not still the default one
            if (cutoff.Year > DateTime.UtcNow.Year - 19)
            {
                TextSerializer.Write(cutoff, string.Format(CutoffLocationFormatString, tableName, configurationName));
            }
        }

        public static object Canonicalize(object value)
        {
            if (value is DateTime) return CanonicalizeDateTime((DateTime)value);

            if (value is string)
            {
                string valueString = (string)value;
                if (valueString.Length > FieldLengthLimitBytes)
                {
                    valueString = valueString.Substring(0, FieldLengthLimitBytes);
                }

                return valueString.CanonicalizeNewlines();
            }

            return value;
        }

        public static DateTime CanonicalizeDateTime(DateTime value)
        {
            // Convert all DateTimes to UTC
            DateTime result = value.ToUniversalTime();

            // Truncate to previous whole second
            return new DateTime(result.Year, result.Month, result.Day, result.Hour, result.Minute, result.Second, 0, DateTimeKind.Utc);
        }

        public static string ConvertLineBreaksToHtml(string value)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;
            StringBuilder result = new StringBuilder();

            char last = '\0';
            for (int i = 0; i < value.Length; ++i)
            {
                char c = value[i];

                if (c == '\n')
                {
                    // \n without \r? Replace with <br />
                    if (last != '\r')
                    {
                        result.Append("<br />");
                    }
                }
                else if (c == '\r')
                {
                    //replace all carriage return with <br />
                    result.Append("<br />");
                }
                else
                {
                    //output everything except \r or \n
                    result.Append(c);
                }

                last = c;
            }

            return result.ToString();
        }
    }

}
