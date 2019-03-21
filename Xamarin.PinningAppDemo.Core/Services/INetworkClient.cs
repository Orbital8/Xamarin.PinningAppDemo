using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xamarin.PinningAppDemo.Core.Models;

namespace Xamarin.PinningAppDemo.Core.Services
{
    public interface INetworkClient
    {
        Task<IEnumerable<User>> GetUsersAsync();
        Task<IEnumerable<User>> GetUsersAsync(CancellationToken token);
    }
}
