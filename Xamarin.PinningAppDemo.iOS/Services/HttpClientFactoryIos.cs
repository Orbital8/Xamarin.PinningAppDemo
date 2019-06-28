using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using Xamarin.PinningAppDemo.Core.Services;



namespace Xamarin.PinningAppDemo.iOS.Services
{
    public class HttpClientFactoryIos : IHttpClientFactory
    {
        public HttpClient GetClient()
        {
            return GetClient(null);
        }

        public HttpClient GetClient(Uri baseUri)
        {
            var handler = new PinningNSUrlSessionHandler();
            var trustDelegate = new TrustOverrideDelegate();
            handler.TrustOverride = trustDelegate.ValidateTrustChain;
            if (baseUri == null)
            {
                return new HttpClient(handler) { MaxResponseContentBufferSize = 25000 };
            }
            return new HttpClient(handler) { BaseAddress = baseUri, MaxResponseContentBufferSize = 25000 };
        }

    }

}