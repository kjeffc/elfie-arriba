using Arriba.Serialization;

namespace Arriba
{
    public static class ArribaConvert
    {
        private static IArribaConvert _convert;

        public static void Assign(IArribaConvert convert)
        {
            if (_convert == null)
            {
                _convert = convert;
            }
        }

        public static T FromJson<T>(string content)
        {
            return _convert.FromJson<T>(content);
        }

        public static string ToJson<T>(T content)
        {
            return _convert.ToJson<T>(content);
        }
    }
}
