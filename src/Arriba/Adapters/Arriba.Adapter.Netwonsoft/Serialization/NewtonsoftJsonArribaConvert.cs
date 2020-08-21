using Arriba.Serialization;
using Newtonsoft.Json;

namespace Arriba.Serialization
{
    public class NewtonsoftJsonArribaConvert : IArribaConvert
    {
        private readonly JsonSerializerSettings _settings;

        public NewtonsoftJsonArribaConvert()
        {
            _settings = ArribaSerializationConfig.GetConfiguredSettings();
        }

        public string ToJson<T>(T content)
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

        public T FromJson<T>(string content)
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
