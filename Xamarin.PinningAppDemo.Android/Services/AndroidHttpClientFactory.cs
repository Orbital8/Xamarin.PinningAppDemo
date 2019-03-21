using System.Net.Http;
using Xamarin.PinningAppDemo.Core.Services;

namespace Xamarin.PinningAppDemo.Droid.Services
{
    public class AndroidHttpClientFactory : IHttpClientFactory
    {
        public HttpClient GetClient()
        {
            var client = new HttpClient(new PinningClientHandler()) { MaxResponseContentBufferSize = 25000 };
            return client;
        }
    }
}