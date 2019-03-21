using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Android.App;
using Android.Content.PM;
using Android.Net.Http;
using Java.Net;
using Java.Security;
using Java.Security.Cert;
using Javax.Net.Ssl;

namespace Xamarin.PinningAppDemo.Droid.Services
{
    // We implement our own handler to perform pinning as there (currently) is no callback to perform pinning in the native client handlers
    //
    // This implementation is based upon the Xamarin.Android source code for AndroidClientHandler in order to provide
    // the necessary callbacks to support the pinning validation.
    //
    public sealed class PinningClientHandler : AndroidClientHandler.AndroidClientHandler
    {
        private const string ApiHost = "jsonplaceholder.typicode.com";
        private const string ExpectedFingerprint = "77a0b67182fbf225b979df0226255925cba0ce89e5a0c4a1ba16bd43d6a75752";
        private const string ExpectedSignature = "g6YYBfNix5BRnZrkxlh4s76oDys=";
        private const string PlayStoreInstallerPrefix = "com.android.vending";

        private X509TrustManagerExtensions _trustManagerExt;
        
        protected override SSLSocketFactory ConfigureCustomSSLSocketFactory(HttpsURLConnection connection)
        {
            var algorithm = TrustManagerFactory.DefaultAlgorithm;
            var trustManagerFactory = TrustManagerFactory.GetInstance(algorithm);
            trustManagerFactory.Init((KeyStore) null);

            var trustManagers = trustManagerFactory.GetTrustManagers();
            var context = SSLContext.GetInstance("TLS");
            context.Init(null, trustManagers, null);
            SSLContext.Default = context;

            if (_trustManagerExt == null)
            {
                var x509TrustManager = trustManagers.FirstOrDefault(x => x is IX509TrustManager) as IX509TrustManager;
                _trustManagerExt = new X509TrustManagerExtensions(x509TrustManager);                       
            }
          
            return context.SocketFactory;
        }

        protected override Task OnAfterConnectAsync(HttpURLConnection httpConnection, CancellationToken ct)
        {
            if (!(httpConnection is HttpsURLConnection httpsConnection))
            {
                // not an SSL connection
                return Task.CompletedTask;
            }

            if (_trustManagerExt == null)
            {
                throw new Exception("Unable to validate SSL connection");
            }


            ValidatePinning(_trustManagerExt, httpsConnection);
            TamperChecks();

            return Task.CompletedTask;
        }

        private void ValidatePinning(
            X509TrustManagerExtensions trustManagerExt,
            HttpsURLConnection conn)
        {
            var host = conn.URL.Host;
            if (host != ApiHost)
            {
                // no pinning against other hosts
                return;
            }
            
            var trustedChain = TrustedChain(trustManagerExt, conn);
            var leaf = trustedChain[0];
            var thumbprint = GetThumbprintSha256(leaf);

            if (!ExpectedFingerprint.Equals(thumbprint))
            {
                throw new SSLPeerUnverifiedException("Certificate chain not trusted.");
            }
        }
        
        private IList<X509Certificate> TrustedChain(
            X509TrustManagerExtensions trustManagerExt,
            HttpsURLConnection conn) 
        {
            var serverCerts = conn.GetServerCertificates();
            var untrustedCerts = serverCerts.Where(x => x is X509Certificate).Cast<X509Certificate>().ToArray();
            var host = conn.URL.Host;
            
            try
            {
                return trustManagerExt.CheckServerTrusted(untrustedCerts,
                    "RSA", host);
            } 
            catch (CertificateException e) 
            {
                throw new SSLException(e);
            }
        }

        private string GetThumbprintSha256(X509Certificate certificate)
        {
            var messageDigest = MessageDigest.GetInstance("SHA-256");
            var bytes = certificate.GetEncoded();
            messageDigest.Update(bytes);

            var digest = messageDigest.Digest();
            return digest.Aggregate(String.Empty, (str, hashByte) => str + hashByte.ToString("x2"));
        }

        #region Tamper checks

        private void TamperChecks()
        {
            CheckNotDebuggable();
            VerifyInstaller();
            ValidateSignature();
        }

        private void CheckNotDebuggable()
        {
#if !DEBUG
            var context = Application.Context;
            if (context.ApplicationInfo.Flags.HasFlag(ApplicationInfoFlags.Debuggable))
            {
                throw new Exception("Application is debuggable.");
            }
#endif
        }

        private void VerifyInstaller()
        {
#if !DEBUG
            var context = Application.Context;
            var packageManager = context.PackageManager;
            var installer = packageManager.GetInstallerPackageName(context.PackageName);

            if (installer == null || !installer.StartsWith(PlayStoreInstallerPrefix, StringComparison.Ordinal))
            {
                throw new Exception("Not installed from Play Store.");
            }
#endif
        }

        private void ValidateSignature()
        {
            var context = Application.Context;
            var packageInfo = context.PackageManager.GetPackageInfo(context.PackageName, PackageInfoFlags.Signatures);

            foreach (var signature in packageInfo.Signatures)
            {
                var signatureBytes = signature.ToByteArray();
                var md = MessageDigest.GetInstance("SHA");
                md.Update(signatureBytes);

                var currentSignature = Convert.ToBase64String(md.Digest());

                if (!ExpectedSignature.Equals(currentSignature))
                {
                    throw new Exception("Code has been tampered with.");
                }
            }
        }

        #endregion
    }
}
