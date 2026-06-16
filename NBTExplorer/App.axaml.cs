using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using Serilog;

namespace NBTExplorer;

public class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        var mainWindow = new MainWindow();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop) desktop.MainWindow = mainWindow;

        // Exception handling for non-AppCommands.
        Dispatcher.UIThread.UnhandledException += (_, e) =>
        {
            // If something goes wrong, we log it and show a Dialog to the user. :C
            Log.Error(e.Exception, "[neoNBTExplorer]: Unhandled UI thread exception");
            mainWindow.OpenDialog(new ErrorDialogState(e.Exception));

            e.Handled = true;
        };
        TaskScheduler.UnobservedTaskException += (_, e) =>
        {
            // If something goes wrong, we log it and show a Dialog to the user. :C
            Log.Error(e.Exception, "[neoNBTExplorer]: Unobserved task exception");
            mainWindow.OpenDialog(new ErrorDialogState(e.Exception));

            e.SetObserved();
        };
        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
        {
            // If something goes wrong, we log it and show a Dialog to the user. :C
            var exception = e.ExceptionObject as Exception;

            Log.Error(exception, "[neoNBTExplorer]: Unhandled domain exception (terminating: {IsTerminating})",
                e.IsTerminating);
            if (exception is not null && !e.IsTerminating)
                mainWindow.OpenDialog(new ErrorDialogState(exception));
        };

        base.OnFrameworkInitializationCompleted();
    }
}