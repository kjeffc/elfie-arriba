﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Arriba.Serialization;
using Arriba.Server;

namespace Arriba.Client
{
    internal static class HttpExtensions
    {
        public static async Task EnsureArribaSuccess(this HttpResponseMessage message)
        {
            if (message.IsSuccessStatusCode)
            {
                return;
            }

            string exceptionMessage = String.Format("({0}) {1}", (int)message.StatusCode, message.ReasonPhrase);
            Exception inner = null;
            bool foundSpecificContent = false;

            // Try and read the response body. 
            if (message.Content.Headers.ContentLength > 0)
            {
                try
                {
                    // Is it JSON?
                    if (message.Content.Headers.ContentType.MediaType == "application/json")
                    {
                        var contentString = await message.Content.ReadAsStringAsync();

                        IEnumerable<string> exceptionType;
                        if (message.Headers.TryGetValues("Runtime-Unhandled-Exception", out exceptionType) && exceptionType != null && exceptionType.Any())
                        {
                            // Can we create a matching type? 
                            string type = exceptionType.First();
                            inner = ArribaConvert.FromJson<Exception>(contentString);

                            exceptionMessage += ": Server threw unhandled " + type + ". See inner exception for details";
                            foundSpecificContent = true;
                        }
                        else
                        {
                            var envalope = ArribaConvert.FromJson<ArribaResponseEnvelope>(contentString);

                            if (envalope.Content != null)
                            {
                                exceptionMessage += ": " + envalope.Content.ToString();
                            }

                            foundSpecificContent = true;
                        }
                    }
                }
                catch (ArribaSerializationException)
                { }
            }

            if (!foundSpecificContent)
            {
                try
                {
                    message.EnsureSuccessStatusCode();
                }
                catch (Exception e)
                {
                    inner = e;
                }
            }
            else
            {
                message.Dispose();
            }

            ArribaClientException clientException;

            if (inner != null)
            {
                clientException = new ArribaClientException(exceptionMessage, inner);
            }
            else
            {
                clientException = new ArribaClientException(exceptionMessage);
            }

            throw clientException;
        }
    }
}
