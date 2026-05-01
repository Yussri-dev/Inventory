using Inventory.Ui.Pages;

namespace Inventory.Ui.Services
{
    public static class DisplayHandler
    {
        public static void OpenCustomerDisplay()
        {
            var window = new Window(new CustomerDisplayPage());

            Application.Current.OpenWindow(window);
        }
    }
}
