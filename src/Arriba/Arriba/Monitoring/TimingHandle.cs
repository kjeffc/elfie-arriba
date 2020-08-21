// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Arriba.Monitoring
{
    /// <summary>
    /// Provides a mechanism to time a scope of execution in fractional milliseconds.
    /// </summary>
    internal class TimingHandle : IDisposable
    {
        private Stopwatch _stopwatch;
        private double _accumulated;

        internal TimingHandle()
        {
            // TODO: since QueryPerformanceCounter/QueryPerformanceFrequency are not available on dotnetcore
            // we will use Stopwatch for now.  Determine if we can remove in favor of Stopwatch permantly of if
            // timing is sensative enough we need a replacement
            _stopwatch = Stopwatch.StartNew();
        }

        internal double ElapsedMiliseconds
        {
            get
            {
                return _stopwatch.ElapsedMilliseconds;
            }
        }

        internal void Start()
        {
            _stopwatch.Restart();
        }

        public void Dispose()
        {
            // Old behavior was to restart so do that
            _stopwatch.Stop();
            _accumulated += this.ElapsedMiliseconds;
        }
    }
}
