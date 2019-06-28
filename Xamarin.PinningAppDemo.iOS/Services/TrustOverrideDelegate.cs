using System;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using Security;
using Xamarin.PinningAppDemo.Core.Extensions;

namespace Xamarin.PinningAppDemo.iOS.Services
{


    public sealed class TrustOverrideDelegate 
    {



        private static readonly string[] ApiHosts = { "jsonplaceholder.typicode.com" };
        private static readonly string[] ExpectedFingerprints = {  "763ba7a333a9ef4c57ccb559eabc0df64542d582f5b35a92ce9919c55205484d"};
        private static readonly string[] ExpectedPublicKeys = { "6fe77a111e3e23b428108bb0104b22365ff368c7241d2aec0f718a54b5443948" };


        public bool ValidateTrustChain(PinningNSUrlSessionHandler sender, SecTrust trust, string hostName)
        {

            return ValidateTrustChain(hostName, trust);

        }



        private bool ValidateTrustChain(string host, SecTrust serverCertChain)
        {

            if (!ApiHosts.Any(x => string.Equals(host, x, StringComparison.InvariantCultureIgnoreCase)))
            {
                // no pinning against other hosts
                return true;
            }

            var certificate = serverCertChain[0];
            var cert = certificate.ToX509Certificate2();
            var thumbprint = GetThumbprintSha256(cert);
            if (ExpectedFingerprints.Length > 0 && !ExpectedFingerprints.Any(x => string.Equals(thumbprint, x, StringComparison.InvariantCulture)))
            {
                //System.Diagnostics.Debug.WriteLine($"Thumbprint not in expected {string.Join(", ", ExpectedFingerprints)} was {thumbprint}");
                return false;
            }

            var publicKey = GetPublicKeySha256(cert);
            if (ExpectedPublicKeys.Length > 0 && !ExpectedPublicKeys.Any(x => string.Equals(publicKey, x, StringComparison.InvariantCulture)))
            {
                //System.Diagnostics.Debug.WriteLine($"PublicKey not in expected {string.Join(", ", ExpectedPublicKeys)} was {publicKey}");
                return false;
            }


            return true;
        }


        private string GetThumbprintSha256(X509Certificate2 certificate)
        {
            return certificate.ThumbprintSha256();
        }



        private string GetPublicKeySha256(X509Certificate2 certificate)
        {
            return certificate.PublicKeySha256();

        }


    }


}