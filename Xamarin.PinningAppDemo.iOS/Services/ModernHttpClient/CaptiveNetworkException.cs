//
// Copyright(c) 2013 Paul Betts
//
// Permission is hereby granted, free of charge, to any person obtaining a copy of
// this software and associated documentation files (the "Software"), to deal in
// the Software without restriction, including without limitation the rights to
// use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of
// the Software, and to permit persons to whom the Software is furnished to do so,
// subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS
// FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR
// COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER
// IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN
// CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//
using System;
using System.Net;

namespace Xamarin.PinningAppDemo.iOS.Services.ModernHttpClient
{
    /// <summary>
    /// Thrown when the request goes to a captive network. This happens
    /// usually with wifi networks where an authentication html form is shown
    /// instead of the real content.
    /// </summary>
    public class CaptiveNetworkException : WebException
    {
        const string DefaultCaptiveNetworkErrorMessage = "Hostnames don't match, you are probably on a captive network";

        /// <summary>
        /// Gets the source URI.
        /// </summary>
        /// <value>The source URI.</value>
        public Uri SourceUri { get; private set; }

        /// <summary>
        /// Gets the destination URI.
        /// </summary>
        /// <value>The destination URI.</value>
        public Uri DestinationUri { get; private set; }

        public CaptiveNetworkException(Uri sourceUri, Uri destinationUri) : base(DefaultCaptiveNetworkErrorMessage)
        {
            SourceUri = sourceUri;
            DestinationUri = destinationUri;
        }
    }
}