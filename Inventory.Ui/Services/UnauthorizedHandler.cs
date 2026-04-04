using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Inventory.Ui.Services
{
    public class UnauthorizedHandler : DelegatingHandler
    {
        private readonly NavigationManager _nav;

        public UnauthorizedHandler(NavigationManager nav)
        {
            _nav = nav;
        }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var response = await base.SendAsync(request, cancellationToken);
            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                _nav.NavigateTo("/login", forceLoad: true);
            }
            return response;
        }
    }
}
