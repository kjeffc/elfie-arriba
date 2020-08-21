using System;
using System.Collections.Generic;
using System.Text;

namespace Arriba.Serialization
{
    public interface IArribaConvert
    {
        string ToJson<T>(T content);

        T FromJson<T>(string content);
    }
}
