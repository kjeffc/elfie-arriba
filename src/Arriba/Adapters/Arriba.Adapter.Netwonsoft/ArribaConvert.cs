using Arriba.Serialization;
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
            try
            {
                return JsonConvert.SerializeObject(content, _settings);
            }
            catch (JsonSerializationException ex)
            {
                throw new ArribaSerializationException("Object serialization failed", ex);
            }
        }

        public static T FromJson<T>(string content)
        {
            try
            {
                return JsonConvert.DeserializeObject<T>(content, _settings);
            }
            catch (JsonSerializationException ex)
            {
                throw new ArribaSerializationException("Object deserialization failed", ex);
            }
        }
    }
}
