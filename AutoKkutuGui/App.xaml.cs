using System;
using System.Globalization;
using System.IO;
using System.Windows;
using Serilog;
using Serilog.Sinks.SystemConsole.Themes;

namespace AutoKkutuGui;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
	private const int MaxSizeBytes = 8388608; // 64 MB
	private const string LoggingTemplate = "{Timestamp:yyyy-MM-dd HH:mm:ss.ffff} [{Level:u3}] <Thread#{ThreadId}> [{Module:l}] {Message:lj}{NewLine}{Exception}";
	private readonly TimeSpan FlushPeriod = TimeSpan.FromSeconds(1);

	public App()
	{
		try
		{
			// Initialize console output
			ConsoleManager.Show();

			// Initialize logger
			Log.Logger = new LoggerConfiguration()
				.MinimumLevel.Verbose()
				.WriteTo.Async(c => c.Console(outputTemplate: LoggingTemplate, theme: AnsiConsoleTheme.Code, applyThemeToRedirectedOutput: true, restrictedToMinimumLevel: Serilog.Events.LogEventLevel.Information))
				.WriteTo.Async(c => c.File(path: "AutoKkutu.log", outputTemplate: LoggingTemplate, fileSizeLimitBytes: MaxSizeBytes, rollOnFileSizeLimit: true, buffered: true, flushToDiskInterval: FlushPeriod))
				.Enrich.WithThreadId()
				.CreateLogger();

			AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;

			AppDomain.CurrentDomain.ProcessExit += OnProcessExit;
		}
		catch (Exception e)
		{
			MessageBox.Show("Failed to initialize logging system:\r\n" + e.ToString(), "Logger initialization failure", MessageBoxButton.OK, MessageBoxImage.Error);
			Shutdown(); // Can't continue execution
		}
	}

	private void OnProcessExit(object? sender, EventArgs e) => Log.CloseAndFlush(); // Ensure all logs to be logged

	private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
	{
		Log.Fatal((Exception)e.ExceptionObject, "Unhandled exception!");
		new CrashReportWriter("CrashReport_" + DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss_fff", CultureInfo.InvariantCulture)).Write();
		MessageBox.Show(e.ExceptionObject?.ToString(), "Unhandled Exception!", MessageBoxButton.OK, MessageBoxImage.Error);
		Log.CloseAndFlush(); // Ensure all logs to be logged
	}
}
