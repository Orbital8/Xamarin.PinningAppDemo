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
using Java.Net;
using Javax.Net.Ssl;

namespace Xamarin.PinningAppDemo.Droid.Services.AndroidClientHandler
{
    // Context: https://github.com/xamarin/xamarin-android/issues/1615
    //
    // Code based on the code provided in the issue above
    //
    internal class OldAndroidSSLSocketFactory : SSLSocketFactory
    {
        readonly SSLSocketFactory factory = (SSLSocketFactory)Default;

        public override string[] GetDefaultCipherSuites ()
        {
            return factory.GetDefaultCipherSuites ();
        }

        public override string[] GetSupportedCipherSuites ()
        {
            return factory.GetSupportedCipherSuites ();
        }
        public override Socket CreateSocket (InetAddress address, int port, InetAddress localAddress, int localPort)
        {
            return EnableTlsOnSocket (factory.CreateSocket (address, port, localAddress, localPort));
        }

        public override Socket CreateSocket (InetAddress host, int port)
        {
            return EnableTlsOnSocket (factory.CreateSocket (host, port));
        }

        public override Socket CreateSocket (string host, int port, InetAddress localHost, int localPort)
        {
            return EnableTlsOnSocket (factory.CreateSocket (host, port, localHost, localPort));
        }

        public override Socket CreateSocket (string host, int port)
        {
            return EnableTlsOnSocket (factory.CreateSocket (host, port));
        }

        public override Socket CreateSocket (Socket s, string host, int port, bool autoClose)
        {
            return EnableTlsOnSocket (factory.CreateSocket (s, host, port, autoClose));
        }

        public override Socket CreateSocket ()
        {
            return EnableTlsOnSocket (factory.CreateSocket ());
        }

        private Socket EnableTlsOnSocket (Socket socket)
        {
            if (socket is SSLSocket sslSocket) {
                sslSocket.SetEnabledProtocols (sslSocket.GetSupportedProtocols ());
            }
            return socket;
        }
    }
}