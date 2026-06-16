using System;
using System.Collections.Generic;
using System.IO;
using Avalonia;
using Avalonia.Logging;
using Serilog;

namespace NBTExplorer;

internal static class Program
{
    // We set our app's full name.
    internal static string FullName
    {
        get
        {
            var version = typeof(Program).Assembly.GetName().Version?.ToString(3);
#if DEBUG
            return $"neoNBTExplorer v{version} (DEBUG)";
#else
            return $"neoNBTExplorer v{version}";
#endif
        }
    }

    // We get our app's LocalAppData Directory Path...
    private static string LocalAppDataPath =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "neoNBTExplorer");

    // ...and use it set the Path for our RecentItems data file, as we'll use it a lot.
    internal static string RecentItemsFile => Path.Combine(LocalAppDataPath, "recent_items.json");

    // We get the platform-dependent Minecraft Save Folder, to allow the user to easily open it from the app.
    internal static string MinecraftSaveFolder => OperatingSystem.IsWindows()
        ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), ".minecraft", "saves")
        : OperatingSystem.IsMacOS()
            ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "minecraft",
                "saves")
            : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".minecraft", "saves");

    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
        // And we create it if it doesn't exist yet. 
        if (!Directory.Exists(LocalAppDataPath)) Directory.CreateDirectory(LocalAppDataPath);

        Log.Logger = new LoggerConfiguration().WriteTo.Console()
            .WriteTo.File(Path.Combine(LocalAppDataPath, "log.txt"),
                rollingInterval: RollingInterval.Day,
                rollOnFileSizeLimit: true)
            .CreateLogger();

        try
        {
            Log.Information("[neoNBTExplorer]: Starting up...");
            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        }
        catch (Exception e)
        {
            Log.Fatal(e, "[neoNBTExplorer]: Application terminated unexpectedly");
        }
        finally
        {
            Log.Information("[neoNBTExplorer]: Shutting down...");
            Log.CloseAndFlush();
        }
    }

    private static AppBuilder LogToSerilog(this AppBuilder builder, LogEventLevel level = LogEventLevel.Warning,
        params string[] areas)
    {
        Logger.Sink = new SerilogSink(level, areas);
        return builder;
    }

// Avalonia configuration, don't remove; also used by visual designer.
    private static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
#if DEBUG
            .WithDeveloperTools()
#endif
            .WithInterFont()
            .LogToSerilog();

    // We need a way to "connect" Avalonia's logging with our Serilog. This is how. 
    private class SerilogSink(LogEventLevel minLevel, IList<string> areas) : ILogSink
    {
        public bool IsEnabled(LogEventLevel level, string area)
        {
            return level >= minLevel && (areas.Count < 1 || areas.Contains(area));
        }

        public void Log(LogEventLevel level, string area, object? source, string messageTemplate)
        {
            Serilog.Log.Write((Serilog.Events.LogEventLevel)level, $"[{area} {source}]: {messageTemplate}");
        }

        public void Log(LogEventLevel level, string area, object? source, string messageTemplate,
            params object?[] propertyValues)
        {
            Serilog.Log.Write((Serilog.Events.LogEventLevel)level, $"[{area} {source}]: {messageTemplate}",
                propertyValues);
        }
    }
}