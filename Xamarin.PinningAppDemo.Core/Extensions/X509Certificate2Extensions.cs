using System;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace Xamarin.PinningAppDemo.Core.Extensions
{
    public static class X509Certificate2Extensions
    {
        public static string ThumbprintSha256(this X509Certificate2 cert) 
        {
            byte[] hashBytes;
            
            using (var hasher = new SHA256Managed()) {
                hashBytes = hasher.ComputeHash(cert.RawData);
            }

            return hashBytes.Aggregate(String.Empty, (str, hashByte) => str + hashByte.ToString("x2"));
        }
    }
}