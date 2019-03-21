using System.Net.Http;

namespace Xamarin.PinningAppDemo.Core.Services
{
    public interface IHttpClientFactory
    {
        HttpClient GetClient();
    }
}
