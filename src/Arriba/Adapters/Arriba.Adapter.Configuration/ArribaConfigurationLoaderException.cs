// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.Serialization;

namespace Arriba.Configuration
{
    [Serializable]
    public class ArribaConfigurationLoaderException : Exception
    {
        public ArribaConfigurationLoaderException()
        {
        }

        public ArribaConfigurationLoaderException(string message) : base(message)
        {
        }

        public ArribaConfigurationLoaderException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected ArribaConfigurationLoaderException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}