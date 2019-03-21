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
using System.Collections.Generic;
using System.Net;
using Java.Net;
using Xamarin.Android.Net;

namespace Xamarin.PinningAppDemo.Droid.Services.AndroidClientHandler
{
sealed class AuthModuleDigest : IAndroidAuthenticationModule
	{
		static readonly object cache_lock = new object ();
		static readonly Dictionary <int, AuthDigestSession> cache = new Dictionary <int, AuthDigestSession> ();

		public AuthenticationScheme Scheme { get; } = AuthenticationScheme.Digest;
		public string AuthenticationType { get; } = "Digest";
		public bool CanPreAuthenticate { get; } = true;
		
		static Dictionary <int, AuthDigestSession> Cache {
			get {
				lock (cache_lock) {
					CheckExpired (cache.Count);
				}
				
				return cache;
			}
		}

		static void CheckExpired (int count)
		{
			if (count < 10)
				return;

			DateTime t = DateTime.MaxValue;
			DateTime now = DateTime.Now;
			List <int> list = null;
			foreach (KeyValuePair <int, AuthDigestSession> kvp in cache) {
				AuthDigestSession elem = kvp.Value;
				if (elem.LastUse < t && (elem.LastUse - now).Ticks > TimeSpan.TicksPerMinute * 10) {
					t = elem.LastUse;
					if (list == null)
						list = new List <int> ();

					list.Add (kvp.Key);
				}
			}

			if (list != null) {
				foreach (int k in list)
					cache.Remove (k);
			}
		}
		
		public Authorization Authenticate (string challenge, HttpURLConnection request, ICredentials credentials) 
		{
			if (credentials == null || challenge == null) {
				return null;
			}
	
			string header = challenge.Trim ();
			if (header.IndexOf ("digest", StringComparison.OrdinalIgnoreCase) == -1) {
				return null;
			}

			var currDS = new AuthDigestSession();
			if (!currDS.Parse (challenge)) {
				return null;
			}

			var uri = new Uri (request.URL.ToString ());
			int hashcode = uri.GetHashCode () ^ credentials.GetHashCode () ^ currDS.Nonce.GetHashCode ();
			AuthDigestSession ds = null;
			bool addDS = false;
			if (!Cache.TryGetValue (hashcode, out ds) || ds == null)
				addDS = true;

			if (addDS)
				ds = currDS;
			else if (!ds.Parse (challenge)) {
				return null;
			}

			if (addDS)
				Cache.Add (hashcode, ds);

			return ds.Authenticate (request, credentials);
		}

		public Authorization PreAuthenticate (HttpURLConnection request, ICredentials credentials) 
		{
			if (request == null || credentials == null) {
				return null;
			}

			var uri = new Uri (request.URL.ToString ());
			int hashcode = uri.GetHashCode () ^ credentials.GetHashCode ();
			AuthDigestSession ds = null;
			if (!Cache.TryGetValue (hashcode, out ds) || ds == null)
				return null;

			return ds.Authenticate (request, credentials);
		}
	}
}