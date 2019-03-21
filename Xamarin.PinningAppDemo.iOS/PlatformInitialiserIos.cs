using Prism;
using Prism.Ioc;
using Xamarin.PinningAppDemo.Core.Services;
using Xamarin.PinningAppDemo.iOS.Services;

namespace Xamarin.PinningAppDemo.iOS
{
    public class PlatformInitialiserIos : IPlatformInitializer
    {
        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.Register<IHttpClientFactory, HttpClientFactoryIos>();
        }
    }
}