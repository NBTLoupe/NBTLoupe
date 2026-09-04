using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using NBTLoupe.Core;
using NBTLoupe.ViewModels.Dialogs;
using NBTLoupe.ViewModels.Main;
using NBTLoupe.Views;
using NBTLoupe.Views.Main;
using NBTModel.Interop;
using Serilog;

namespace NBTLoupe;

public class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        var mainViewModel = new MainViewModel();

        switch (ApplicationLifetime)
        {
            case IClassicDesktopStyleApplicationLifetime desktop:
            {
                desktop.MainWindow = new MainWindow
                {
                    DataContext = mainViewModel
                };

                // TODO: Add Clipboard support on other platforms.
                if (desktop.MainWindow.Clipboard is not null)
                    NbtClipboardController.Initialize(new NbtClipboardControllerAvalonia(desktop.MainWindow.Clipboard));
                break;
            }
            case IActivityApplicationLifetime singleViewFactoryApplicationLifetime:
                singleViewFactoryApplicationLifetime.MainViewFactory =
                    () => new MainView { DataContext = mainViewModel };
                break;
            case ISingleViewApplicationLifetime singleViewPlatform:
                singleViewPlatform.MainView = new MainView
                {
                    DataContext = mainViewModel
                };
                break;
        }

        SetupErrorHandling(mainViewModel);

        base.OnFrameworkInitializationCompleted();
    }

    private static void SetupErrorHandling(MainViewModel mainViewModel)
    {
        // Exception handling for non-RelayCommands.
        Dispatcher.UIThread.UnhandledException += async (_, e) =>
        {
            // If something goes wrong, we log it and show a Dialog to the user. :C
            Log.Error(e.Exception, "[NBTLoupe]: Unhandled UI thread exception");
            await mainViewModel.OpenDialogAsync(new ErrorDialogViewModel(mainViewModel, e.Exception));

            e.Handled = true;
        };
        TaskScheduler.UnobservedTaskException += async (_, e) =>
        {
            // If something goes wrong, we log it and show a Dialog to the user. :C
            Log.Error(e.Exception, "[NBTLoupe]: Unobserved task exception");
            await mainViewModel.OpenDialogAsync(new ErrorDialogViewModel(mainViewModel, e.Exception));

            e.SetObserved();
        };
        AppDomain.CurrentDomain.UnhandledException += async (_, e) =>
        {
            // If something goes wrong, we log it and show a Dialog to the user. :C
            var exception = e.ExceptionObject as Exception;

            Log.Error(exception, "[NBTLoupe]: Unhandled domain exception (terminating: {IsTerminating})",
                e.IsTerminating);
            if (exception is not null && !e.IsTerminating)
                await mainViewModel.OpenDialogAsync(new ErrorDialogViewModel(mainViewModel, exception));
        };
    }
}
