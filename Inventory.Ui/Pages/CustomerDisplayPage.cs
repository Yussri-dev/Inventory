using Microsoft.AspNetCore.Components.WebView.Maui;


namespace Inventory.Ui.Pages
{
    public partial class CustomerDisplayPage : ContentPage
    {
        public CustomerDisplayPage()
        {
            Content = new BlazorWebView
            {
                HostPage = "wwwroot/index.html",
                RootComponents =
            {
                new RootComponent
                {
                    Selector = "#app",
                    ComponentType = typeof(App)
                }
            }
            };
        }
    }
}
