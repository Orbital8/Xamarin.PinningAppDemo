using System.Diagnostics;
using System.Security.Cryptography.X509Certificates;
using Security;
using Xamarin.PinningAppDemo.Core.Extensions;
using Xamarin.PinningAppDemo.iOS.Services.ModernHttpClient;

namespace Xamarin.PinningAppDemo.iOS.Services
{
    // We implement our own handler to perform pinning as there (currently) is no callback to perform pinning in the native client handlers
    //
    // This sample pinning implementation is based upon the ModernHttpClient NativeMessageHandler
    // Ideally - this would be based upon the Xamarin.iOS source for NSUrlSessionHandler (I just have to find time to do this)
    //
    public sealed class PinningSessionHandler : NativeMessageHandler
    {
        private const string ApiHost = "jsonplaceholder.typicode.com";
        private const string ExpectedFingerprint = "77a0b67182fbf225b979df0226255925cba0ce89e5a0c4a1ba16bd43d6a75752";
        
        protected override bool ValidateTrustChain(string host, SecTrust serverCertChain)
        {
            if (ApiHost != host)
            {
                // don't want to pin connections to other hosts
                return true;
            }
            
            // check certificate, or public key, or fingerprint matches what we expect
            var bytes = serverCertChain[0].DerData.ToArray();
            var cert = new X509Certificate2(bytes);
            var thumb = cert.ThumbprintSha256();
            var result = ExpectedFingerprint.Equals(thumb);
            if(!result)
            {
                Debug.WriteLine("Pinning failed.");
            }

            return result;
        }
    }
}