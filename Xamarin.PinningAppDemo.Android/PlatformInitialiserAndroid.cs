using Prism;
using Prism.Ioc;
using Xamarin.PinningAppDemo.Core.Services;
using Xamarin.PinningAppDemo.Droid.Services;

namespace Xamarin.PinningAppDemo.Droid
{
    public class PlatformInitialiserAndroid : IPlatformInitializer
    {
        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.Register<IHttpClientFactory, AndroidHttpClientFactory>();
        }
    }
}