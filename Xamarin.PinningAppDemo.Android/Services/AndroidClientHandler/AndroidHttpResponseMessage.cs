//
// Xamarin.Android SDK
//
// The MIT License (MIT)
//
// Copyright (c) .NET Foundation Contributors
//
// All rights reserved.
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.
//
using System.Collections.Generic;
using System.Net.Http;
using Java.Net;

namespace Xamarin.PinningAppDemo.Droid.Services.AndroidClientHandler
{
    /// <summary>
    /// A convenience wrapper around <see cref="System.Net.Http.HttpResponseMessage"/> returned by <see cref="AndroidClientHandler.SendAsync"/>
    /// that allows easy access to authentication data as returned by the server, if any.
    /// </summary>
    public class AndroidHttpResponseMessageEx : HttpResponseMessage
    {
        URL javaUrl;
        HttpURLConnection httpConnection;

        /// <summary>
        /// Set to the same value as <see cref="AndroidClientHandler.RequestedAuthentication"/>.
        /// </summary>
        /// <value>The requested authentication.</value>
        public IList <AuthenticationData> RequestedAuthentication { get; internal set; }

        /// <summary>
        /// Set to the same value as <see cref="AndroidClientHandler.RequestNeedsAuthorization"/>
        /// </summary>
        /// <value>The request needs authorization.</value>
        public bool RequestNeedsAuthorization {
            get { return RequestedAuthentication?.Count > 0; }
        }

        public AndroidHttpResponseMessageEx ()
        {}

        public AndroidHttpResponseMessageEx (URL javaUrl, HttpURLConnection httpConnection) 
        {
            this.javaUrl = javaUrl;
            this.httpConnection = httpConnection;
        }

        protected override void Dispose (bool disposing)
        {
            base.Dispose(disposing);

            if (javaUrl != null) {
                javaUrl.Dispose ();
            }

            if (httpConnection != null) {
                httpConnection.Dispose ();
            }
        }
    }
}