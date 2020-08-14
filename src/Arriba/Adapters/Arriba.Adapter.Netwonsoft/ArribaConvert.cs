using Newtonsoft.Json;

namespace Arriba
{
    public static class ArribaConvert
    {
        private readonly static JsonSerializerSettings _settings;

        static ArribaConvert()
        {
            _settings = ArribaSerializationConfig.GetConfiguredSettings();
        }

        public static string ToJson(object content)
        {
            return JsonConvert.SerializeObject(content, _settings);
        }

        public static T FromJson<T>(string content)
        {
            return JsonConvert.DeserializeObject<T>(content, _settings);
        }
    }
}
