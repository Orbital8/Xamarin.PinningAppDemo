using System.Net.Http;
using Xamarin.PinningAppDemo.Core.Services;

namespace Xamarin.PinningAppDemo.iOS.Services
{
    public class HttpClientFactoryIos : IHttpClientFactory
    {
        public HttpClient GetClient()
        {
            return new HttpClient(new PinningSessionHandler()) { MaxResponseContentBufferSize = 25000 };
        }
    }
}