#if WINDOWS
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using WinRT.Interop;


namespace Inventory.Ui.Platforms.Windows.Services
{
    public static class WindowService
    {
        public static void SetFullScreen(Microsoft.UI.Xaml.Window window)
        {
            var hwnd = WindowNative.GetWindowHandle(window);
            var id = Win32Interop.GetWindowIdFromWindow(hwnd);
            var appWindow = AppWindow.GetFromWindowId(id);

            if (appWindow.Presenter is OverlappedPresenter presenter)
            {
                presenter.SetBorderAndTitleBar(false, false);
                presenter.IsResizable = false;
                presenter.IsMaximizable = false;
                presenter.IsMinimizable = false;
            }

            appWindow.SetPresenter(AppWindowPresenterKind.FullScreen);
        }
    }
}
#endif
