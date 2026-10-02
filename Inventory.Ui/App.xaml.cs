using Inventory.Ui.Pages;

#if WINDOWS
using Microsoft.UI.Windowing;
using WinRT.Interop;
#endif

namespace Inventory.Ui;

public partial class App : Application
{
    private Window? _customerDisplayWindow;

    public App()
    {
        InitializeComponent();
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        var mainWindow = new Window(new MainPage()) { Title = "Inventory POS" };

        mainWindow.Created += OnMainWindowCreated;
        mainWindow.Destroying += (_, _) =>
        {
            var window = _customerDisplayWindow;
            _customerDisplayWindow = null;

            if (window is null)
                return;

            try { CloseWindow(window); }
            catch { /* already closing */ }
        };

        return mainWindow;
    }

    private void OnMainWindowCreated(object? sender, EventArgs e)
    {
#if WINDOWS
    if (_customerDisplayWindow is not null || FindSecondaryDisplay() is null)
        return;

    _customerDisplayWindow = new Window(new CustomerDisplayPage())
    {
        Title = "Customer Display"
    };

    _customerDisplayWindow.Created += OnCustomerDisplayCreated;
    _customerDisplayWindow.Destroying += OnCustomerDisplayDestroyed;

    OpenWindow(_customerDisplayWindow);
#endif
    }


#if WINDOWS
private static DisplayArea? FindSecondaryDisplay()
{
    var areas = DisplayArea.FindAll();

    for (var i = 0; i < areas.Count; i++)   // no foreach (WinRT enumerator issue)
    {
        if (!areas[i].IsPrimary)
            return areas[i];
    }

    return null;
}
#endif

    private void OnCustomerDisplayCreated(object? sender, EventArgs e)
    {
#if WINDOWS
    if (sender is not Window mauiWindow ||
        mauiWindow.Handler?.PlatformView is not Microsoft.Maui.MauiWinUIWindow nativeWindow)
        return;

    var hwnd = WindowNative.GetWindowHandle(nativeWindow);
    var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hwnd);
    var appWindow = AppWindow.GetFromWindowId(windowId);

    var second = FindSecondaryDisplay();
    if (appWindow is null || second is null)
        return;

    // 1. Restore (a maximized window cannot be moved between screens)
    if (appWindow.Presenter is OverlappedPresenter p)
        p.Restore();

    // 2. Move to the second display (inside its work area)
    appWindow.Move(new Windows.Graphics.PointInt32(
        second.WorkArea.X + 50,
        second.WorkArea.Y + 50));

    // 3. Full screen on that display (no title bar, no taskbar)
    appWindow.SetPresenter(AppWindowPresenterKind.FullScreen);
#endif
    }

    private void OnCustomerDisplayDestroyed(
        object? sender,
        EventArgs e)
    {
        _customerDisplayWindow = null;
    }
}