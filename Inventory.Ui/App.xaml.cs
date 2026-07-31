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

    protected override Window CreateWindow(
        IActivationState? activationState)
    {
        var mainWindow =
            new Window(
                new MainPage())
            {
                Title = "Inventory POS"
            };

        mainWindow.Created +=
            OnMainWindowCreated;

        return mainWindow;
    }

    private void OnMainWindowCreated(
        object? sender,
        EventArgs e)
    {
        if (_customerDisplayWindow is not null)
        {
            return;
        }

        _customerDisplayWindow =
            new Window(
                new CustomerDisplayPage())
            {
                Title = "Customer Display"
            };

        // Exécuté lorsque la fenêtre native Windows est disponible.
        _customerDisplayWindow.Created +=
            OnCustomerDisplayCreated;

        _customerDisplayWindow.Destroying +=
            OnCustomerDisplayDestroyed;

        OpenWindow(
            _customerDisplayWindow);
    }

    private void OnCustomerDisplayCreated(
     object? sender,
     EventArgs e)
    {
#if WINDOWS
    if (
        sender is not Window mauiWindow ||
        mauiWindow.Handler?.PlatformView
            is not Microsoft.Maui.MauiWinUIWindow nativeWindow)
    {
        return;
    }

    var windowHandle =
        WindowNative.GetWindowHandle(
            nativeWindow);

    var windowId =
        Microsoft.UI.Win32Interop
            .GetWindowIdFromWindow(
                windowHandle);

    var appWindow =
        AppWindow.GetFromWindowId(
            windowId);

    if (appWindow is null)
    {
        return;
    }

    var displayAreas =
        DisplayArea.FindAll();

    DisplayArea? secondDisplay =
        null;

    // Ne pas utiliser foreach ici.
    // L'énumérateur WinRT peut provoquer InvalidCastException.
    for (
        var index = 0;
        index < displayAreas.Count;
        index++)
    {
        var display =
            displayAreas[index];

        if (!display.IsPrimary)
        {
            secondDisplay =
                display;

            break;
        }
    }

    // Aucun écran secondaire connecté.
    if (secondDisplay is null)
    {
        return;
    }

    // Restaurer avant le déplacement.
    if (
        appWindow.Presenter
            is OverlappedPresenter presenter)
    {
        presenter.Restore();
    }

    // Déplacer vers le deuxième écran.
    appWindow.MoveAndResize(
        secondDisplay.WorkArea,
        secondDisplay);

    // Maximiser sur le deuxième écran.
    if (
        appWindow.Presenter
            is OverlappedPresenter maximizedPresenter)
    {
        maximizedPresenter.Maximize();
    }
#endif
    }

    private void OnCustomerDisplayDestroyed(
        object? sender,
        EventArgs e)
    {
        _customerDisplayWindow = null;
    }
}