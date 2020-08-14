// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Web;

using Arriba.Communication;

namespace Arriba.Server.Owin
{
    public class ArribaOwinRequest : ArribaRequest
    {
        private IDictionary<string, object> _environment;

        public ArribaOwinRequest(IDictionary<string, object> environment, IContentReaderWriterService readerWriter)
            : base(readerWriter)
        {
            _environment = environment;
        }

        public override IPrincipal User
        {
            get
            {
                return _environment.Get<IPrincipal>("server.User");
            }
        }

        public override string Origin
        {
            get
            {
                return _environment.Get<string>("server.RemoteIpAddress");
            }
        }
        public override Stream InputStream
        {
            get
            {
                return _environment.Get<Stream>("owin.RequestBody");
            }
        }

        protected override string HttpRequestVerb
        {
            get
            {
                return _environment.Get<string>("owin.RequestMethod");
            }
        }

        protected override string GetRequestPath()
        {
            return _environment.Get<string>("owin.RequestPath");
        }

        protected override IValueBag GetQueryString()
        {
            return new NameValueCollectionValueBag(HttpUtility.ParseQueryString(_environment.Get<string>("owin.RequestQueryString"), Encoding.UTF8));
        }

        protected override IValueBag GetRequestHeaders()
        {
            return new DictionaryValueBag(_environment.Get<IDictionary<string, string[]>>("owin.RequestHeaders"));
        }
    }
}
