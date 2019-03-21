using Prism.Mvvm;
using Prism.Navigation;

namespace Xamarin.PinningAppDemo.Core.ViewModels
{
    public abstract class ViewModelBase : BindableBase, INavigationAware, IDestructible
    {
        public virtual void OnNavigatedFrom(INavigationParameters parameters)
        {
        }

        public virtual void OnNavigatedTo(INavigationParameters parameters)
        {
        }

        public virtual void OnNavigatingTo(INavigationParameters parameters)
        {
        }

        public virtual void Destroy()
        {
        }
    }
}
