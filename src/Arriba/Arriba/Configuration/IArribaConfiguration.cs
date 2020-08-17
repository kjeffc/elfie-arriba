using System;
using System.Collections.Generic;
using System.Text;

namespace Arriba.Configuration
{
    public interface IArribaConfiguration
    {
        string ApplicationVersion { get; }

        string ServiceName { get; }
    }
}
