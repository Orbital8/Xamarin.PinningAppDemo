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
using Xamarin.Android.Net;

namespace Xamarin.PinningAppDemo.Droid.Services.AndroidClientHandler
{
    public class AuthenticationData
    {
        /// <summary>
        /// Gets the authentication scheme. If instance of AuthenticationData comes from the <see cref="AndroidClientHandler.RequestedAuthentication"/>
        /// collection it will have this property set to the type of authentication as requested by the server, or to <c>AuthenticationScheme.Unsupported</c>/>. 
        /// In the latter case the application is required to provide the authentication module in <see cref="AuthModule"/>.
        /// </summary>
        /// <value>The authentication scheme.</value>
        public AuthenticationScheme Scheme { get; set; } = AuthenticationScheme.None;

        /// <summary>
        /// Contains the full authentication challenge (full value of the WWW-Authenticate HTTP header). This information can be used by the custom
        /// authentication module (<see cref="AuthModule"/>)
        /// </summary>
        /// <value>The challenge.</value>
        public string Challenge { get; internal set; }

        /// <summary>
        /// Indicates whether authentication performed using data in this instance should be done for the end server or a proxy. If instance of 
        /// AuthenticationData comes from the <see cref="AndroidClientHandler.RequestedAuthentication"/> collection it will have this property set to
        /// <c>true</c> if authentication request came from a proxy, <c>false</c> otherwise.
        /// </summary>
        /// <value><c>true</c> to use proxy authentication.</value>
        public bool UseProxyAuthentication { get; set; }

        /// <summary>
        /// If the <see cref="Scheme"/> property is set to <c>AuthenticationScheme.Unsupported</c>, this property must be set to an instance of
        /// a class that implements the <see cref="IAndroidAuthenticationModule"/> interface and which understands the authentication challenge contained
        /// in the <see cref="Challenge"/> property.
        /// </summary>
        /// <value>The auth module.</value>
        public IAndroidAuthenticationModule AuthModule { get; set; }
    }
}