using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Arriba.Client.Serialization.Json;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Arriba
{
    public class ArribaSerializationConfig
    {
        public static JsonSerializerSettings GetConfiguredSettings()
        {
            return GetConfiguredSettings(null);
        }

        public static JsonSerializerSettings GetConfiguredSettings(IEnumerable<JsonConverter> converters = null)
        {
            var settings = new JsonSerializerSettings()
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver() { NamingStrategy = new CamelCaseNamingStrategy() { ProcessDictionaryKeys = false } },
                Formatting = Debugger.IsAttached ? Formatting.Indented : Formatting.None
            };

            // TODO: Use composition to import Converters
            if (converters == null)
            {
                converters = Enumerable.Empty<JsonConverter>();
            }

            converters = converters.Union(ConverterFactory.GetArribaConverters()).Distinct();

            foreach (JsonConverter converter in converters)
            {
                settings.Converters.Add(converter);
            }

            return settings;
        }
    }
}
