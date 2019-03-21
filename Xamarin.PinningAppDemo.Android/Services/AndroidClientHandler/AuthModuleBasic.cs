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
using System;
using System.Net;
using System.Text;
using Java.Net;
using Xamarin.Android.Net;

namespace Xamarin.PinningAppDemo.Droid.Services.AndroidClientHandler
{
    sealed class AuthModuleBasic : IAndroidAuthenticationModule
    {
        public AuthenticationScheme Scheme { get; } = AuthenticationScheme.Basic;
        public string AuthenticationType { get; } = "Basic";
        public bool CanPreAuthenticate { get; } = true;

        public Authorization Authenticate (string challenge, HttpURLConnection request, ICredentials credentials)
        {
            string header = challenge?.Trim ();
            if (credentials == null || String.IsNullOrEmpty (header))
                return null;

            if (header.IndexOf ("basic", StringComparison.OrdinalIgnoreCase) == -1)
                return null;

            return InternalAuthenticate (request, credentials);
        }
		
        public Authorization PreAuthenticate (HttpURLConnection request, ICredentials credentials)
        {
            return InternalAuthenticate (request, credentials);
        }

        Authorization InternalAuthenticate (HttpURLConnection request, ICredentials credentials)
        {
            if (request == null || credentials == null)
                return null;

            NetworkCredential cred = credentials.GetCredential (new Uri (request.URL.ToString ()), AuthenticationType.ToLowerInvariant ());
            if (cred == null)
                return null;

            if (String.IsNullOrEmpty (cred.UserName))
                return null;

            string domain = cred.Domain?.Trim ();
            string response = String.Empty;

            // If domain is set, MS sends "domain\user:password".
            if (!String.IsNullOrEmpty (domain))
                response = domain + "\\";
            response += cred.UserName + ":" + cred.Password;

            return new Authorization ($"{AuthenticationType} {Convert.ToBase64String (Encoding.ASCII.GetBytes (response))}");
        }
    }
}