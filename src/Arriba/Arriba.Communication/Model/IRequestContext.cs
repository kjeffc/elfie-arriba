// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

using Arriba.Monitoring;

namespace Arriba.Communication
{
    /// <summary>
    /// Respresents the lifetime of a request including internal processing meta data. 
    /// </summary>
    public interface IRequestContext : ITelemetry
    {
        /// <summary>
        /// Gets the original request. 
        /// </summary>
        IRequest Request { get; }

        /// <summary>
        /// Gets timing information for the request. 
        /// </summary>
        IDictionary<string, double> TraceTimings { get; }
    }
}
