﻿// ----------------------------------------------------------------------------------
//
// Copyright Microsoft Corporation
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// http://www.apache.org/licenses/LICENSE-2.0
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// ----------------------------------------------------------------------------------

namespace Microsoft.WindowsAzure.Commands.Utilities.Common
{
    using System;
    using System.Collections.Specialized;
    using System.IO;
    using System.Net;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.ServiceModel;
    using System.ServiceModel.Channels;
    using System.ServiceModel.Description;
    using System.ServiceModel.Dispatcher;
    using System.Threading;
    using System.Xml.Linq;

    public class HttpRestMessageHandler : MessageProcessingHandler
    {
        private Action<string> logger;

        public HttpRestMessageHandler(Action<string> logger)
        {
            this.logger = logger;
        }

        protected override HttpRequestMessage ProcessRequest(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            string body = String.Empty;
            if (request.Content != null)
            {
                var stream = new MemoryStream();
                request.Content.CopyToAsync(stream).Wait();
                stream.Position = 0;
                body = XElement.Load(stream).ToString();
                stream.Position = 0;
                request.Content = new StreamContent(stream);
            }
            logger(General.GetHttpRequestLog(request.Method.ToString(), request.RequestUri.AbsoluteUri, request.Headers, body));

            return request;
        }

        protected override HttpResponseMessage ProcessResponse(HttpResponseMessage response, CancellationToken cancellationToken)
        {
            string body = String.Empty;
            if(response.Content != null)
            {
                var stream = new MemoryStream();
                response.Content.CopyToAsync(stream).Wait();
                stream.Position = 0;
                body = XElement.Load(stream).ToString();
                stream.Position = 0;
                response.Content = new StreamContent(stream);
            }
            logger(General.GetHttpResponseLog(response.StatusCode.ToString(), response.Headers, body));
            return response;
        }
    }

    public class HttpRestMessageInspector : IClientMessageInspector, IEndpointBehavior
    {
        private Action<string> logger;

        public HttpRestMessageInspector(Action<string> logger)
        {
            this.logger = logger;
        }

        #region IClientMessageInspector

        public virtual void AfterReceiveReply(ref Message reply, object correlationState)
        {
            HttpResponseMessageProperty prop = (HttpResponseMessageProperty)reply.Properties[HttpResponseMessageProperty.Name];
            string body = General.ReadMessageBody(ref reply);
            logger(General.GetHttpResponseLog(prop.StatusCode.ToString(), prop.Headers, body));
        }

        public virtual object BeforeSendRequest(ref Message request, IClientChannel channel)
        {
            HttpRequestMessageProperty prop = (HttpRequestMessageProperty)request.Properties[HttpRequestMessageProperty.Name];
            string body = General.ReadMessageBody(ref request);
            logger(General.GetHttpRequestLog(prop.Method, request.Headers.To.AbsoluteUri, prop.Headers, body));

            return request;
        }

        #endregion

        #region IEndpointBehavior Members

        public void AddBindingParameters(ServiceEndpoint endpoint, BindingParameterCollection bindingParameters) { }

        public void ApplyClientBehavior(ServiceEndpoint endpoint, ClientRuntime clientRuntime)
        {
            clientRuntime.MessageInspectors.Add(this);
        }

        public void ApplyDispatchBehavior(ServiceEndpoint endpoint, EndpointDispatcher endpointDispatcher) { }

        public void Validate(ServiceEndpoint endpoint) { }

        #endregion
    }
}