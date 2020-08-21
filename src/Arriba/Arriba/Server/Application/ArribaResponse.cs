// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Arriba.Communication;

namespace Arriba.Server
{
    public class ArribaResponse : Response<ArribaResponseEnvelope>
    {
        private ArribaResponseEnvelope _envelope = null;

        private ArribaResponse(ResponseStatus status, object body)
            : base(status)
        {
            _envelope = new ArribaResponseEnvelope(success: status == ResponseStatus.Ok, content: body);
        }

        protected override object GetResponseBody()
        {
            return _envelope;
        }

        public static ArribaResponse Ok(object body)
        {
            return new ArribaResponse(ResponseStatus.Ok, body);
        }

        public static ArribaResponse Created(object body)
        {
            return new ArribaResponse(ResponseStatus.Created, body);
        }

        public static ArribaResponse Forbidden(object body)
        {
            return new ArribaResponse(ResponseStatus.Forbidden, body);
        }

        public static ArribaResponse BadRequest(string format, params object[] args)
        {
            return new ArribaResponse(ResponseStatus.Error, string.Format(format, args));
        }

        // Replace Response.Error, Response.NotFound with ArribaResponseEnvelope-returning-versions
        public static new ArribaResponse Error(object body)
        {
            return new ArribaResponse(ResponseStatus.Error, body);
        }

        public static new ArribaResponse NotFound(object body)
        {
            return new ArribaResponse(ResponseStatus.NotFound, body);
        }
    }
}
