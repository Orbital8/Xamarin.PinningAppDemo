using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Android.App;
using Android.Content.PM;
using Android.Net.Http;
using Java.Net;
using Java.Security;
using Java.Security.Cert;
using Javax.Net.Ssl;
using Xamarin.PinningAppDemo.Core.Extensions;
using X509Certificate = Java.Security.Cert.X509Certificate;

namespace Xamarin.PinningAppDemo.Droid.Services
{
    // We implement our own handler to perform pinning as there (currently) is no callback to perform pinning in the native client handlers
    //
    // This implementation is based upon the Xamarin.Android source code for AndroidClientHandler in order to provide
    // the necessary callbacks to support the pinning validation.
    //
    public sealed class PinningClientHandler : AndroidClientHandler.AndroidClientHandler
    {
        private static readonly string[] ApiHosts = { "jsonplaceholder.typicode.com"};
        private static readonly string[] ExpectedFingerprints = {  "763ba7a333a9ef4c57ccb559eabc0df64542d582f5b35a92ce9919c55205484d","...add other certificate fingerprints"};
        private static readonly string[] ExpectedPublicKeys = { "6fe77a111e3e23b428108bb0104b22365ff368c7241d2aec0f718a54b5443948","...add other certificate public key SHA256"};
        private static readonly string[] ExpectedSignatures = { "Your-signing-keystore-sha256" };

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
            var httpsConnection = httpConnection as HttpsURLConnection;
            if (httpsConnection == null)
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
            if ( !ApiHosts.Any(x=>string.Equals(host, x, StringComparison.InvariantCultureIgnoreCase)))
            {
                // no pinning against other hosts
                return;
            }
            
            var trustedChain = TrustedChain(trustManagerExt, conn);
            var leaf = trustedChain[0];
            X509Certificate2 cert = new X509Certificate2(leaf.GetEncoded());

            var thumbprint = GetThumbprintSha256(cert);

            // TODO thumbprint or public key

            if ( ExpectedFingerprints.Length > 0 && !ExpectedFingerprints.Any(x=>string.Equals(thumbprint, x, StringComparison.InvariantCulture)))
            {
                //System.Diagnostics.Debug.WriteLine($"Thumbprint not in expected {string.Join(", ", ExpectedFingerprints)} was {thumbprint}");
                throw new SSLPeerUnverifiedException("Certificate chain not trusted.");
            }
            var publicKey = GetPublicKeySha256(cert);
            if ( ExpectedPublicKeys.Length > 0 && !ExpectedPublicKeys.Any(x=>string.Equals(publicKey, x, StringComparison.InvariantCulture)))
            {
                //System.Diagnostics.Debug.WriteLine($"PublicKey not in expected {string.Join(", ", ExpectedPublicKeys)} was {publicKey}");
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

 
        private string GetThumbprintSha256(X509Certificate2 cert)
        {
            return cert.ThumbprintSha256();
        }



        private string GetPublicKeySha256(X509Certificate2 cert)
        {
            return cert.PublicKeySha256();
        }


        #region Tamper checks

        private void TamperChecks()
        {
#if !DEBUG
            
            CheckNotDebuggable();
#endif
#if !DEBUG
                VerifyInstaller();
#endif
            ValidateKeystoreSignature();
        }

        private void CheckNotDebuggable()
        {
            var context = Application.Context;
            if (context.ApplicationInfo.Flags.HasFlag(ApplicationInfoFlags.Debuggable))
            {
                throw new Exception("Application is debuggable.");
            }
        }

        private void VerifyInstaller()
        {
            var context = Application.Context;
            var packageManager = context.PackageManager;
            var installer = packageManager.GetInstallerPackageName(context.PackageName);

            if (installer == null || !installer.StartsWith(PlayStoreInstallerPrefix, StringComparison.Ordinal))
            {
                throw new Exception("Not installed from Play Store.");
            }
        }

        /// <summary>
        /// Check that the app has been signed using the expected keystore
        /// </summary>
        private void ValidateKeystoreSignature()
        {
            var context = Application.Context;
            var packageInfo = context.PackageManager.GetPackageInfo(context.PackageName, PackageInfoFlags.Signatures);

            foreach (var signature in packageInfo.Signatures)
            {

                var signatureBytes = signature.ToByteArray();
                var md = MessageDigest.GetInstance("SHA");
                md.Update(signatureBytes);

                var currentSignature = Convert.ToBase64String(md.Digest());

                if ( ExpectedSignatures.Length > 0 && !ExpectedSignatures.Any(x=>string.Equals(currentSignature, x, StringComparison.InvariantCulture)))
                {
                    //System.Diagnostics.Debug.WriteLine($"ExpectedSignature not in expected {string.Join(", ", ExpectedSignatures)} was {currentSignature}");
                    throw new Exception("Code has been tampered with.");
                }
            }
        }

        #endregion
    }
}
