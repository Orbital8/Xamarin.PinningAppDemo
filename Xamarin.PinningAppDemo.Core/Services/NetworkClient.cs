using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Xamarin.PinningAppDemo.Core.Models;

namespace Xamarin.PinningAppDemo.Core.Services
{
    internal class NetworkClient : INetworkClient
    {
        private readonly HttpClient _client;

        public NetworkClient(IHttpClientFactory factory)
        {
            //_client = new HttpClient();
            _client = factory.GetClient();
        }

        public Task<IEnumerable<User>> GetUsersAsync()
        {
            return GetUsersAsync(CancellationToken.None);
        }

        public async Task<IEnumerable<User>> GetUsersAsync(CancellationToken token)
        {
            try
            {
                var uri = new Uri("https://jsonplaceholder.typicode.com/users");

                var response = await _client.GetAsync(uri);
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                var users = JsonConvert.DeserializeObject<List<User>>(content);
                return users;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                throw;
            }
        }
    }
}
