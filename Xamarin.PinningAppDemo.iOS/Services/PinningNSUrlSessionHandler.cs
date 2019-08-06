﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CoreFoundation;
using Foundation;
using Security;
using UIKit;

// 
// Copied from NSUrlSessionHandler.cs at commit (4a704e448e117b46f1636565e0e43f0b81500bc2) where a TrustOverride callback was added and fix included
// for incorrect error thrown on connectivity error. Modified only to add hostName into the delegate. Merged to master of
// https://github.com/xamarin/xamarin-macios (on (4/7/2019) but awaiting release (as of 5/7/2019)
// 
namespace Xamarin.PinningAppDemo.iOS.Services
{
    public delegate bool NSUrlSessionHandlerTrustOverrideCallback(PinningNSUrlSessionHandler sender, SecTrust trust, string hostName);

    // useful extensions for the class in order to set it in a header
	static class NSHttpCookieExtensions
	{
		static void AppendSegment(StringBuilder builder, string name, string value)
		{
			if (builder.Length > 0)
				builder.Append ("; ");

			builder.Append (name);
			if (value != null)
				builder.Append ("=").Append (value);
		}

		// returns the header for a cookie
		public static string GetHeaderValue (this NSHttpCookie cookie)
		{
			var header = new StringBuilder();
			AppendSegment (header, cookie.Name, cookie.Value);
			AppendSegment (header, NSHttpCookie.KeyPath.ToString (), cookie.Path.ToString ());
			AppendSegment (header, NSHttpCookie.KeyDomain.ToString (), cookie.Domain.ToString ());
			AppendSegment (header, NSHttpCookie.KeyVersion.ToString (), cookie.Version.ToString ());

			if (cookie.Comment != null)
				AppendSegment (header, NSHttpCookie.KeyComment.ToString (), cookie.Comment.ToString());

			if (cookie.CommentUrl != null)
				AppendSegment (header, NSHttpCookie.KeyCommentUrl.ToString (), cookie.CommentUrl.ToString());

			if (cookie.Properties.ContainsKey (NSHttpCookie.KeyDiscard))
				AppendSegment (header, NSHttpCookie.KeyDiscard.ToString (), null);

			if (cookie.ExpiresDate != null) {
				// Format according to RFC1123; 'r' uses invariant info (DateTimeFormatInfo.InvariantInfo)
				var dateStr = ((DateTime) cookie.ExpiresDate).ToUniversalTime ().ToString("r", CultureInfo.InvariantCulture);
				AppendSegment (header, NSHttpCookie.KeyExpires.ToString (), dateStr);
			}

			if (cookie.Properties.ContainsKey (NSHttpCookie.KeyMaximumAge)) {
				var timeStampString = (NSString) cookie.Properties[NSHttpCookie.KeyMaximumAge];
				AppendSegment (header, NSHttpCookie.KeyMaximumAge.ToString (), timeStampString);
			}

			if (cookie.IsSecure)
				AppendSegment (header, NSHttpCookie.KeySecure.ToString(), null);

			if (cookie.IsHttpOnly)
				AppendSegment (header, "httponly", null); // Apple does not show the key for the httponly

			return header.ToString ();
		}
	}

	public partial class PinningNSUrlSessionHandler : HttpMessageHandler
	{
		private const string SetCookie = "Set-Cookie";
		readonly Dictionary<string, string> headerSeparators = new Dictionary<string, string> {
			["User-Agent"] = " ",
			["Server"] = " "
		};

		readonly NSUrlSession session;
		readonly Dictionary<NSUrlSessionTask, InflightData> inflightRequests;
		readonly object inflightRequestsLock = new object ();
#if !MONOMAC && !__WATCHOS__
		readonly bool isBackgroundSession = false;
		NSObject notificationToken;  // needed to make sure we do not hang if not using a background session
#endif

		static NSUrlSessionConfiguration CreateConfig ()
		{
			// modifying the configuration does not affect future calls
			var config = NSUrlSessionConfiguration.DefaultSessionConfiguration;
			// but we want, by default, the timeout from HttpClient to have precedence over the one from NSUrlSession
			// Double.MaxValue does not work, so default to 24 hours
			config.TimeoutIntervalForRequest = 24 * 60 * 60;
			config.TimeoutIntervalForResource = 24 * 60 * 60;
			return config;
		}

		public PinningNSUrlSessionHandler () : this (CreateConfig ())
		{
		}

		[CLSCompliant (false)]
		public PinningNSUrlSessionHandler (NSUrlSessionConfiguration configuration)
		{
			if (configuration == null)
				throw new ArgumentNullException (nameof (configuration));

#if !MONOMAC  && !__WATCHOS__ 
			// if the configuration has an identifier, we are dealing with a background session, 
			// therefore, we do not have to listen to the notifications.
			isBackgroundSession = !string.IsNullOrEmpty (configuration.Identifier);
#endif

			AllowAutoRedirect = true;

			// we cannot do a bitmask but we can set the minimum based on ServicePointManager.SecurityProtocol minimum
			var sp = ServicePointManager.SecurityProtocol;
			if ((sp & SecurityProtocolType.Ssl3) != 0)
				configuration.TLSMinimumSupportedProtocol = SslProtocol.Ssl_3_0;
			else if ((sp & SecurityProtocolType.Tls) != 0)
				configuration.TLSMinimumSupportedProtocol = SslProtocol.Tls_1_0;
			else if ((sp & SecurityProtocolType.Tls11) != 0)
				configuration.TLSMinimumSupportedProtocol = SslProtocol.Tls_1_1;
			else if ((sp & SecurityProtocolType.Tls12) != 0)
				configuration.TLSMinimumSupportedProtocol = SslProtocol.Tls_1_2;
			else if ((sp & (SecurityProtocolType) 12288) != 0) // Tls13 value not yet in monno
				configuration.TLSMinimumSupportedProtocol = SslProtocol.Tls_1_3;
				
			session = NSUrlSession.FromConfiguration (configuration, (INSUrlSessionDelegate) new NSUrlSessionHandlerDelegate (this), null);
			inflightRequests = new Dictionary<NSUrlSessionTask, InflightData> ();
		}

#if !MONOMAC  && !__WATCHOS__

		void AddNotification ()
		{
			if (!isBackgroundSession && notificationToken == null)
				notificationToken = NSNotificationCenter.DefaultCenter.AddObserver (UIApplication.WillResignActiveNotification, BackgroundNotificationCb);
		}

		void RemoveNotification ()
		{
			if (notificationToken != null) {
				NSNotificationCenter.DefaultCenter.RemoveObserver (notificationToken);
				notificationToken = null;
			}
		}

		void BackgroundNotificationCb (NSNotification obj)
		{
			// we do not need to call the lock, we call cancel on the source, that will trigger all the needed code to 
			// clean the resources and such
			foreach (var r in inflightRequests.Values) {
				r.CompletionSource.TrySetCanceled ();
			}
		}
#endif

		public long MaxInputInMemory { get; set; } = long.MaxValue;

		void RemoveInflightData (NSUrlSessionTask task, bool cancel = true)
		{
			lock (inflightRequestsLock) {
				if (inflightRequests.TryGetValue (task, out var data)) {
					if (cancel)
						data.CancellationTokenSource.Cancel ();
					data.Dispose ();
					inflightRequests.Remove (task);
				}
#if !MONOMAC  && !__WATCHOS__
				// do we need to be notified? If we have not inflightData, we do not
				if (inflightRequests.Count == 0)
					RemoveNotification ();
#endif
			}

			if (cancel)
				task?.Cancel ();

			task?.Dispose ();
		}

		protected override void Dispose (bool disposing)
		{
#if !MONOMAC  && !__WATCHOS__
			// remove the notification if present, method checks against null
			RemoveNotification ();
#endif
			lock (inflightRequestsLock) {
				foreach (var pair in inflightRequests) {
					pair.Key?.Cancel ();
					pair.Key?.Dispose ();
					pair.Value?.Dispose ();
				}

				inflightRequests.Clear ();
			}
			base.Dispose (disposing);
		}

		bool disableCaching;

		public bool DisableCaching {
			get {
				return disableCaching;
			}
			set {
				EnsureModifiability ();
				disableCaching = value;
			}
		}

		bool allowAutoRedirect;

		public bool AllowAutoRedirect {
			get {
				return allowAutoRedirect;
			}
			set {
				EnsureModifiability ();
				allowAutoRedirect = value;
			}
		}

		bool allowsCellularAccess;

		public bool AllowsCellularAccess {
			get {
				return allowsCellularAccess;
			}
			set {
				EnsureModifiability ();
				allowsCellularAccess = value;
			}
		}

		ICredentials credentials;

		public ICredentials Credentials {
			get {
				return credentials;
			}
			set {
				EnsureModifiability ();
				credentials = value;
			}
		}

		NSUrlSessionHandlerTrustOverrideCallback trustOverride;

		public NSUrlSessionHandlerTrustOverrideCallback TrustOverride {
			get {
				return trustOverride;
			}
			set {
				EnsureModifiability ();
				trustOverride = value;
			}
		}

		bool sentRequest;

		internal void EnsureModifiability ()
		{
			if (sentRequest)
				throw new InvalidOperationException (
					"This instance has already started one or more requests. " +
					"Properties can only be modified before sending the first request.");
		}

		static Exception createExceptionForNSError(NSError error)
		{
			var innerException = new NSErrorException(error);

			// errors that exists in both share the same error code, so we can use a single switch/case
			// this also ease watchOS integration as if does not expose CFNetwork but (I would not be 
			// surprised if it)could return some of it's error codes
#if __WATCHOS__
			if (error.Domain == NSError.NSUrlErrorDomain) {
#else
			if ((error.Domain == NSError.NSUrlErrorDomain) || (error.Domain == NSError.CFNetworkErrorDomain)) {
#endif
				// Apple docs: https://developer.apple.com/library/mac/documentation/Cocoa/Reference/Foundation/Miscellaneous/Foundation_Constants/index.html#//apple_ref/doc/constant_group/URL_Loading_System_Error_Codes
				// .NET docs: http://msdn.microsoft.com/en-us/library/system.net.webexceptionstatus(v=vs.110).aspx
				switch ((NSUrlError) (long) error.Code) {
				case NSUrlError.Cancelled:
				case NSUrlError.UserCancelledAuthentication:
#if !__WATCHOS__
				case (NSUrlError) NSNetServicesStatus.CancelledError:
#endif
					// No more processing is required so just return.
					return new OperationCanceledException(error.LocalizedDescription, innerException);
				}
			}

			return new HttpRequestException (error.LocalizedDescription, innerException);
 		}

		string GetHeaderSeparator (string name)
		{
			string value;
			if (!headerSeparators.TryGetValue (name, out value))
				value = ",";
			return value;
		}

		async Task<NSUrlRequest> CreateRequest (HttpRequestMessage request)
		{
			var stream = Stream.Null;
			var headers = request.Headers as IEnumerable<KeyValuePair<string, IEnumerable<string>>>;

			if (request.Content != null) {
				stream = await request.Content.ReadAsStreamAsync ().ConfigureAwait (false);
				headers = System.Linq.Enumerable.ToArray(headers.Union (request.Content.Headers));
			}

			var nsrequest = new NSMutableUrlRequest {
				AllowsCellularAccess = allowsCellularAccess,
				CachePolicy = DisableCaching ? NSUrlRequestCachePolicy.ReloadIgnoringCacheData : NSUrlRequestCachePolicy.UseProtocolCachePolicy,
				HttpMethod = request.Method.ToString ().ToUpperInvariant (),
				Url = NSUrl.FromString (request.RequestUri.AbsoluteUri),
				Headers = headers.Aggregate (new NSMutableDictionary (), (acc, x) => {
					acc.Add (new NSString (x.Key), new NSString (string.Join (GetHeaderSeparator (x.Key), x.Value)));
					return acc;
				})
			};
			if (stream != Stream.Null) {
				// HttpContent.TryComputeLength is `protected internal` :-( but it's indirectly called by headers
				var length = request.Content.Headers.ContentLength;
				if (length.HasValue && (length <= MaxInputInMemory))
					nsrequest.Body = NSData.FromStream (stream);
				else
					nsrequest.BodyStream = new WrappedNSInputStream (stream);
			}
			return nsrequest;
		}

#if SYSTEM_NET_HTTP || MONOMAC
		internal
#endif
		protected override async Task<HttpResponseMessage> SendAsync (HttpRequestMessage request, CancellationToken cancellationToken)
		{
            System.Diagnostics.Debug.WriteLine($"{GetType().Name} SendAsync {request.RequestUri}");

			Volatile.Write (ref sentRequest, true);
			var nsrequest = await CreateRequest (request).ConfigureAwait(false);
			var dataTask = session.CreateDataTask (nsrequest);

			var tcs = new TaskCompletionSource<HttpResponseMessage> ();

			lock (inflightRequestsLock) {
#if !MONOMAC  && !__WATCHOS__
				// Add the notification whenever needed
				AddNotification ();
#endif
				inflightRequests.Add (dataTask, new InflightData {
					RequestUrl = request.RequestUri.AbsoluteUri,
					CompletionSource = tcs,
					CancellationToken = cancellationToken,
					CancellationTokenSource = new CancellationTokenSource (),
					Stream = new NSUrlSessionDataTaskStream (),
					Request = request
				});
			}

			if (dataTask.State == NSUrlSessionTaskState.Suspended)
				dataTask.Resume ();

			// as per documentation: 
			// If this token is already in the canceled state, the 
			// delegate will be run immediately and synchronously.
			// Any exception the delegate generates will be 
			// propagated out of this method call.
			//
			// The execution of the register ensures that if we 
			// receive a already cancelled token or it is cancelled
			// just before this call, we will cancel the task. 
			// Other approaches are harder, since querying the state
			// of the token does not guarantee that in the next
			// execution a threads cancels it.
			cancellationToken.Register (() => {
				RemoveInflightData (dataTask);
				tcs.TrySetCanceled ();
			});

			return await tcs.Task.ConfigureAwait (false);
		}

		// Needed since we strip during linking since we're inside a product assembly.
		[Preserve (AllMembers = true)]
		partial class NSUrlSessionHandlerDelegate : NSUrlSessionDataDelegate
		{
			readonly PinningNSUrlSessionHandler sessionHandler;

			public NSUrlSessionHandlerDelegate (PinningNSUrlSessionHandler handler)
			{
				sessionHandler = handler;
			}

			InflightData GetInflightData (NSUrlSessionTask task)
			{
				var inflight = default (InflightData);

				lock (sessionHandler.inflightRequestsLock)
					if (sessionHandler.inflightRequests.TryGetValue (task, out inflight)) {
						// ensure that we did not cancel the request, if we did, do cancel the task
						if (inflight.CancellationToken.IsCancellationRequested)
							task?.Cancel ();
						return inflight;
					}

				// if we did not manage to get the inflight data, we either got an error or have been canceled, lets cancel the task, that will execute DidCompleteWithError
				task?.Cancel ();
				return null;
			}

			public override void DidReceiveResponse (NSUrlSession session, NSUrlSessionDataTask dataTask, NSUrlResponse response, Action<NSUrlSessionResponseDisposition> completionHandler)
			{
				var inflight = GetInflightData (dataTask);

				if (inflight == null)
					return;

				try {
					var urlResponse = (NSHttpUrlResponse)response;
					var status = (int)urlResponse.StatusCode;

					var content = new NSUrlSessionDataTaskStreamContent (inflight.Stream, () => {
						if (!inflight.Completed) {
							dataTask.Cancel ();
						}

						inflight.Disposed = true;
						inflight.Stream.TrySetException (new ObjectDisposedException ("The content stream was disposed."));

						sessionHandler.RemoveInflightData (dataTask);
					}, inflight.CancellationTokenSource.Token);

					// NB: The double cast is because of a Xamarin compiler bug
					var httpResponse = new HttpResponseMessage ((HttpStatusCode)status) {
						Content = content,
						RequestMessage = inflight.Request
					};
					httpResponse.RequestMessage.RequestUri = new Uri (urlResponse.Url.AbsoluteString);

					foreach (var v in urlResponse.AllHeaderFields) {
						// NB: Cocoa trolling us so hard by giving us back dummy dictionary entries
						if (v.Key == null || v.Value == null) continue;
						// NSUrlSession tries to be smart with cookies, we will not use the raw value but the ones provided by the cookie storage
						if (v.Key.ToString () == SetCookie) continue;

						httpResponse.Headers.TryAddWithoutValidation (v.Key.ToString (), v.Value.ToString ());
						httpResponse.Content.Headers.TryAddWithoutValidation (v.Key.ToString (), v.Value.ToString ());
					}

					var cookies = session.Configuration.HttpCookieStorage.CookiesForUrl (response.Url);
					for (var index = 0; index < cookies.Length; index++) {
						httpResponse.Headers.TryAddWithoutValidation (SetCookie, cookies [index].GetHeaderValue ());
					}

					inflight.Response = httpResponse;

					// We don't want to send the response back to the task just yet.  Because we want to mimic .NET behavior
					// as much as possible.  When the response is sent back in .NET, the content stream is ready to read or the
					// request has completed, because of this we want to send back the response in DidReceiveData or DidCompleteWithError
					if (dataTask.State == NSUrlSessionTaskState.Suspended)
						dataTask.Resume ();

				} catch (Exception ex) {
					inflight.CompletionSource.TrySetException (ex);
					inflight.Stream.TrySetException (ex);

					sessionHandler.RemoveInflightData (dataTask);
				}

				completionHandler (NSUrlSessionResponseDisposition.Allow);
			}

			public override void DidReceiveData (NSUrlSession session, NSUrlSessionDataTask dataTask, NSData data)
			{
				var inflight = GetInflightData (dataTask);

				if (inflight == null)
					return;

				inflight.Stream.Add (data);
				SetResponse (inflight);
			}

			public override void DidCompleteWithError (NSUrlSession session, NSUrlSessionTask task, NSError error)
			{
				var inflight = GetInflightData (task);

				// this can happen if the HTTP request times out and it is removed as part of the cancellation process
				if (inflight != null) {
					// set the stream as finished
					inflight.Stream.TrySetReceivedAllData ();

					// send the error or send the response back
					if (error != null) {
						// got an error, cancel the stream operatios before we do anything
						inflight.CancellationTokenSource.Cancel (); 
						inflight.Errored = true;

						var exc = inflight.Exception ?? createExceptionForNSError (error);
						inflight.CompletionSource.TrySetException (exc);
						inflight.Stream.TrySetException (exc);
					} else {
						inflight.Completed = true;
						SetResponse (inflight);
					}

					sessionHandler.RemoveInflightData (task, cancel: false);
				}
			}

			void SetResponse (InflightData inflight)
			{
				lock (inflight.Lock) {
					if (inflight.ResponseSent)
						return;

					if (inflight.CancellationTokenSource.Token.IsCancellationRequested)
						return;

					if (inflight.CompletionSource.Task.IsCompleted)
						return;

					var httpResponse = inflight.Response;

					inflight.ResponseSent = true;

					// EVIL HACK: having TrySetResult inline was blocking the request from completing
					Task.Run (() => inflight.CompletionSource.TrySetResult (httpResponse));
				}
			}

			public override void WillCacheResponse (NSUrlSession session, NSUrlSessionDataTask dataTask, NSCachedUrlResponse proposedResponse, Action<NSCachedUrlResponse> completionHandler)
			{
				completionHandler (sessionHandler.DisableCaching ? null : proposedResponse);
			}

			public override void WillPerformHttpRedirection (NSUrlSession session, NSUrlSessionTask task, NSHttpUrlResponse response, NSUrlRequest newRequest, Action<NSUrlRequest> completionHandler)
			{
				completionHandler (sessionHandler.AllowAutoRedirect ? newRequest : null);
			}

			public override void DidReceiveChallenge (NSUrlSession session, NSUrlSessionTask task, NSUrlAuthenticationChallenge challenge, Action<NSUrlSessionAuthChallengeDisposition, NSUrlCredential> completionHandler)
			{
                System.Diagnostics.Debug.WriteLine($"{GetType().Name} DidReceiveChallenge");

				var inflight = GetInflightData (task);

				if (inflight == null)
					return;

				// ToCToU for the callback
				var trustCallback = sessionHandler.TrustOverride;
				if (trustCallback != null && challenge.ProtectionSpace.AuthenticationMethod == NSUrlProtectionSpace.AuthenticationMethodServerTrust) {
                    {
                        // <MODIFICATION FROM ORIGINAL>
                        // add hostname to allow selective pinning
                        var hostname = task.CurrentRequest.Url.Host;
                        System.Diagnostics.Debug.WriteLine($"{GetType().Name} call trustCallback {hostname}");

                        if (trustCallback (sessionHandler, challenge.ProtectionSpace.ServerSecTrust, hostname)) {
                            System.Diagnostics.Debug.WriteLine($"{GetType().Name} user callback accepted the certificate");
                            var credential = new NSUrlCredential (challenge.ProtectionSpace.ServerSecTrust);
                            completionHandler (NSUrlSessionAuthChallengeDisposition.UseCredential, credential);
                        } else {
                            // user callback rejected the certificate, we want to set the exception, else the user will
                            // see as if the request was cancelled.
                            System.Diagnostics.Debug.WriteLine($"{GetType().Name} user callback rejected the certificate");

                            lock (inflight.Lock) {
                                inflight.Exception = new HttpRequestException ("An error occurred while sending the request.", new WebException ("Error: TrustFailure"));
                            }
                            completionHandler (NSUrlSessionAuthChallengeDisposition.CancelAuthenticationChallenge, null);
                        }
                        return;
                    }
				}
				// case for the basic auth failing up front. As per apple documentation:
				// The URL Loading System is designed to handle various aspects of the HTTP protocol for you. As a result, you should not modify the following headers using
				// the addValue(_:forHTTPHeaderField:) or setValue(_:forHTTPHeaderField:) methods:
				// 	Authorization
				// 	Connection
				// 	Host
				// 	Proxy-Authenticate
				// 	Proxy-Authorization
				// 	WWW-Authenticate
				// but we are hiding such a situation from our users, we can nevertheless know if the header was added and deal with it. The idea is as follows,
				// check if we are in the first attempt, if we are (PreviousFailureCount == 0), we check the headers of the request and if we do have the Auth 
				// header, it means that we do not have the correct credentials, in any other case just do what it is expected.

				if (challenge.PreviousFailureCount == 0) {
					var authHeader = inflight.Request?.Headers?.Authorization;
					if (!(string.IsNullOrEmpty (authHeader?.Scheme) && string.IsNullOrEmpty (authHeader?.Parameter))) {
						completionHandler (NSUrlSessionAuthChallengeDisposition.RejectProtectionSpace, null);
						return;
					}
				}

				if (sessionHandler.Credentials != null && TryGetAuthenticationType (challenge.ProtectionSpace, out string authType)) {
					NetworkCredential credentialsToUse = null;
					if (authType != RejectProtectionSpaceAuthType) {
						var uri = inflight.Request.RequestUri;
						credentialsToUse = sessionHandler.Credentials.GetCredential (uri, authType);
					}

					if (credentialsToUse != null) {
						var credential = new NSUrlCredential (credentialsToUse.UserName, credentialsToUse.Password, NSUrlCredentialPersistence.ForSession);
						completionHandler (NSUrlSessionAuthChallengeDisposition.UseCredential, credential);
					} else {
						// Rejecting the challenge allows the next authentication method in the request to be delivered to
						// the DidReceiveChallenge method. Another authentication method may have credentials available.
						completionHandler (NSUrlSessionAuthChallengeDisposition.RejectProtectionSpace, null);
					}
				} else {
					completionHandler (NSUrlSessionAuthChallengeDisposition.PerformDefaultHandling, challenge.ProposedCredential);
				}
			}

			static readonly string RejectProtectionSpaceAuthType = "reject";

			static bool TryGetAuthenticationType (NSUrlProtectionSpace protectionSpace, out string authenticationType)
			{
				if (protectionSpace.AuthenticationMethod == NSUrlProtectionSpace.AuthenticationMethodNTLM) {
					authenticationType = "NTLM";
				} else if (protectionSpace.AuthenticationMethod == NSUrlProtectionSpace.AuthenticationMethodHTTPBasic) {
					authenticationType = "basic";
				} else if (protectionSpace.AuthenticationMethod == NSUrlProtectionSpace.AuthenticationMethodNegotiate ||
					protectionSpace.AuthenticationMethod == NSUrlProtectionSpace.AuthenticationMethodHTMLForm ||
					protectionSpace.AuthenticationMethod == NSUrlProtectionSpace.AuthenticationMethodHTTPDigest) {
					// Want to reject this authentication type to allow the next authentication method in the request to
					// be used.
					authenticationType = RejectProtectionSpaceAuthType;
				} else {
					// ServerTrust, ClientCertificate or Default.
					authenticationType = null;
					return false;
				}
				return true;
			}
		}

		// Needed since we strip during linking since we're inside a product assembly.
		[Preserve (AllMembers = true)]
		class InflightData : IDisposable
		{
			public readonly object Lock = new object ();
			public string RequestUrl { get; set; }

			public TaskCompletionSource<HttpResponseMessage> CompletionSource { get; set; }
			public CancellationToken CancellationToken { get; set; }
			public CancellationTokenSource CancellationTokenSource { get; set; }
			public NSUrlSessionDataTaskStream Stream { get; set; }
			public HttpRequestMessage Request { get; set; }
			public HttpResponseMessage Response { get; set; }

			public Exception Exception { get; set; }
			public bool ResponseSent { get; set; }
			public bool Errored { get; set; }
			public bool Disposed { get; set; }
			public bool Completed { get; set; }
			public bool Done { get { return Errored || Disposed || Completed || CancellationToken.IsCancellationRequested; } }

			public void Dispose()
			{
				Dispose (true);
				GC.SuppressFinalize(this);
			}

			// The bulk of the clean-up code is implemented in Dispose(bool)
			protected virtual void Dispose (bool disposing)
			{
				if (disposing) {
					if (CancellationTokenSource != null) {
						CancellationTokenSource.Dispose ();
						CancellationTokenSource = null;
					}
				}
			}

		}

		// Needed since we strip during linking since we're inside a product assembly.
		[Preserve (AllMembers = true)]
		class NSUrlSessionDataTaskStreamContent : MonoStreamContent
		{
			Action disposed;

			public NSUrlSessionDataTaskStreamContent (NSUrlSessionDataTaskStream source, Action onDisposed, CancellationToken token) : base (source, token)
			{
				disposed = onDisposed;
			}

			protected override void Dispose (bool disposing)
			{
				var action = Interlocked.Exchange (ref disposed, null);
				action?.Invoke ();

				base.Dispose (disposing);
			}
		}

		//
		// Copied from https://github.com/mono/mono/blob/2019-02/mcs/class/System.Net.Http/System.Net.Http/StreamContent.cs.
		//
		// This is not a perfect solution, but the most robust and risk-free approach.
		//
		// The implementation depends on Mono-specific behavior, which makes SerializeToStreamAsync() cancellable.
		// Unfortunately, the CoreFX implementation of HttpClient does not support this.
		//
		// By copying Mono's old implementation here, we ensure that we're compatible with both HttpClient implementations,
		// so when we eventually adopt the CoreFX version in all of Mono's profiles, we don't regress here.
		//
		class MonoStreamContent : HttpContent
		{
			readonly Stream content;
			readonly int bufferSize;
			readonly CancellationToken cancellationToken;
			readonly long startPosition;
			bool contentCopied;

			public MonoStreamContent (Stream content)
				: this (content, 16 * 1024)
			{
			}

			public MonoStreamContent (Stream content, int bufferSize)
			{
				if (content == null)
					throw new ArgumentNullException ("content");

				if (bufferSize <= 0)
					throw new ArgumentOutOfRangeException ("bufferSize");

				this.content = content;
				this.bufferSize = bufferSize;

				if (content.CanSeek) {
					startPosition = content.Position;
				}
			}

			//
			// Workarounds for poor .NET API
			// Instead of having SerializeToStreamAsync with CancellationToken as public API. Only LoadIntoBufferAsync
			// called internally from the send worker can be cancelled and user cannot see/do it
			//
			internal MonoStreamContent (Stream content, CancellationToken cancellationToken)
				: this (content)
			{
				// We don't own the token so don't worry about disposing it
				this.cancellationToken = cancellationToken;
			}

			protected override Task<Stream> CreateContentReadStreamAsync ()
			{
				return Task.FromResult (content);
			}

			protected override void Dispose (bool disposing)
			{
				if (disposing) {
					content.Dispose ();
				}

				base.Dispose (disposing);
			}

			protected override Task SerializeToStreamAsync (Stream stream, TransportContext context)
			{
				if (contentCopied) {
					if (!content.CanSeek) {
						throw new InvalidOperationException ("The stream was already consumed. It cannot be read again.");
					}

					content.Seek (startPosition, SeekOrigin.Begin);
				} else {
					contentCopied = true;
				}

				return content.CopyToAsync (stream, bufferSize, cancellationToken);
			}

			protected override bool TryComputeLength (out long length)
			{
				if (!content.CanSeek) {
					length = 0;
					return false;
				}
				length = content.Length - startPosition;
				return true;
			}
		}

		// Needed since we strip during linking since we're inside a product assembly.
		[Preserve (AllMembers = true)]
		class NSUrlSessionDataTaskStream : Stream
		{
			readonly Queue<NSData> data;
			readonly object dataLock = new object ();

			long position;
			long length;

			bool receivedAllData;
			Exception exc;

			NSData current;
			Stream currentStream;

			public NSUrlSessionDataTaskStream ()
			{
				data = new Queue<NSData> ();
			}

			public void Add (NSData d)
			{
				lock (dataLock) {
					data.Enqueue (d);
					length += (int)d.Length;
				}
			}

			public void TrySetReceivedAllData ()
			{
				receivedAllData = true;
			}

			public void TrySetException (Exception e)
			{
				exc = e;
				TrySetReceivedAllData ();
			}

			void ThrowIfNeeded (CancellationToken cancellationToken)
			{
				if (exc != null)
					throw exc;

				cancellationToken.ThrowIfCancellationRequested ();
			}

			public override int Read (byte [] buffer, int offset, int count)
			{
				return ReadAsync (buffer, offset, count).Result;
			}

			public override async Task<int> ReadAsync (byte [] buffer, int offset, int count, CancellationToken cancellationToken)
			{
				// try to throw on enter
				ThrowIfNeeded (cancellationToken);

				while (current == null) {
					lock (dataLock) {
						if (data.Count == 0 && receivedAllData && position == length)
							return 0;

						if (data.Count > 0 && current == null) {
							current = data.Peek ();
							currentStream = current.AsStream ();
							break;
						}
					}

					try {
						await Task.Delay (50, cancellationToken).ConfigureAwait (false);
					} catch (TaskCanceledException ex) {
						// add a nicer exception for the user to catch, add the cancelation exception
						// to have a decent stack
						throw new TimeoutException ("The request timed out.", ex);
					}
				}

				// try to throw again before read
				ThrowIfNeeded (cancellationToken);

				var d = currentStream;
				var bufferCount = Math.Min (count, (int)(d.Length - d.Position));
				var bytesRead = await d.ReadAsync (buffer, offset, bufferCount, cancellationToken).ConfigureAwait (false);

				// add the bytes read from the pointer to the position
				position += bytesRead;

				// remove the current primary reference if the current position has reached the end of the bytes
				if (d.Position == d.Length) {
					lock (dataLock) {
						// this is the same object, it was done to make the cleanup
						data.Dequeue ();
						currentStream?.Dispose ();
						// We cannot use current?.Dispose. The reason is the following one:
						// In the DidReceiveResponse, if iOS realizes that a buffer can be reused,
						// because the data is the same, it will do so. Such a situation does happen
						// between requests, that is, request A and request B will get the same NSData
						// (buffer) in the delegate. In this case, we cannot dispose the NSData because
						// it might be that a different request received it and it is present in
						// its NSUrlSessionDataTaskStream stream. We can only trust the gc to do the job
						// which is better than copying the data over. 
						current = null;
						currentStream = null;
					}
				}

				return bytesRead;
			}

			public override bool CanRead => true;

			public override bool CanSeek => false;

			public override bool CanWrite => false;

			public override bool CanTimeout => false;

			public override long Length => length;

			public override void SetLength (long value)
			{
				throw new InvalidOperationException ();
			}

			public override long Position {
				get { return position; }
				set { throw new InvalidOperationException (); }
			}

			public override long Seek (long offset, SeekOrigin origin)
			{
				throw new InvalidOperationException ();
			}

			public override void Flush ()
			{
				throw new InvalidOperationException ();
			}

			public override void Write (byte [] buffer, int offset, int count)
			{
				throw new InvalidOperationException ();
			}
		}

		// Needed since we strip during linking since we're inside a product assembly.
		[Preserve (AllMembers = true)]
		class WrappedNSInputStream : NSInputStream
		{
			NSStreamStatus status;
			CFRunLoopSource source;
			readonly Stream stream;
			bool notifying;

			public WrappedNSInputStream (Stream inputStream)
			{
				status = NSStreamStatus.NotOpen;
				stream = inputStream;
				source = new CFRunLoopSource (Handle);
			}

			public override NSStreamStatus Status => status;

			public override void Open ()
			{
				status = NSStreamStatus.Open;
				Notify (CFStreamEventType.OpenCompleted);
			}

			public override void Close ()
			{
				status = NSStreamStatus.Closed;
			}

			public override nint Read (IntPtr buffer, nuint len)
			{
				var sourceBytes = new byte [len];
				var read = stream.Read (sourceBytes, 0, (int)len);
				Marshal.Copy (sourceBytes, 0, buffer, (int)len);

				if (notifying)
					return read;

				notifying = true;
				if (stream.CanSeek && stream.Position == stream.Length) {
					Notify (CFStreamEventType.EndEncountered);
					status = NSStreamStatus.AtEnd;
				}
				notifying = false;

				return read;
			}

			public override bool HasBytesAvailable ()
			{
				return true;
			}

			protected override bool GetBuffer (out IntPtr buffer, out nuint len)
			{
				// Just call the base implemention (which will return false)
				return base.GetBuffer (out buffer, out len);
			}

			// NSInvalidArgumentException Reason: *** -propertyForKey: only defined for abstract class.  Define -[System_Net_Http_NSUrlSessionHandler_WrappedNSInputStream propertyForKey:]!
			protected override NSObject GetProperty (NSString key)
			{
				return null;
			}

			protected override bool SetProperty (NSObject property, NSString key)
			{
				return false;
			}

			protected override bool SetCFClientFlags (CFStreamEventType inFlags, IntPtr inCallback, IntPtr inContextPtr)
			{
				// Just call the base implementation, which knows how to handle everything.
				return base.SetCFClientFlags (inFlags, inCallback, inContextPtr);
			}

			public override void Schedule (NSRunLoop aRunLoop, string mode)
			{
				var cfRunLoop = aRunLoop.GetCFRunLoop ();
				var nsMode = new NSString (mode);

				cfRunLoop.AddSource (source, nsMode);

				if (notifying)
					return;

				notifying = true;
				Notify (CFStreamEventType.HasBytesAvailable);
				notifying = false;
			}

			public override void Unschedule (NSRunLoop aRunLoop, string mode)
			{
				var cfRunLoop = aRunLoop.GetCFRunLoop ();
				var nsMode = new NSString (mode);

				cfRunLoop.RemoveSource (source, nsMode);
			}

			protected override void Dispose (bool disposing)
			{
				stream?.Dispose ();
			}
		}
	}

}