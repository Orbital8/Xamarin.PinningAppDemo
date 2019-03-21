using System;
using System.Collections.ObjectModel;
using Prism.Commands;
using Prism.Services;
using Xamarin.PinningAppDemo.Core.Models;
using Xamarin.PinningAppDemo.Core.Services;

namespace Xamarin.PinningAppDemo.Core.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        private readonly Lazy<INetworkClient> _networkService;
        private readonly IPageDialogService _dialogService;

        private DelegateCommand _refreshDataCommand;

        public MainViewModel(Lazy<INetworkClient> networkService, IPageDialogService dialogService)
        {
            _networkService = networkService;
            _dialogService = dialogService;
        }

        public DelegateCommand RefreshData => _refreshDataCommand ?? (_refreshDataCommand = new DelegateCommand(OnRefreshData));

        public ObservableCollection<User> Users { get; } = new ObservableCollection<User>();

        private async void OnRefreshData()
        {
            try
            {
                Users.Clear();

                var data = await _networkService.Value.GetUsersAsync();
                foreach (var user in data)
                {
                    Users.Add(user);
                }
            }
            catch
            {
                await _dialogService.DisplayAlertAsync("Error", "Network request failed.", "Ok");
            }
        }
    }
}
