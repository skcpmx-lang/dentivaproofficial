using System.Diagnostics.CodeAnalysis;
using System.Windows;
using System.Windows.Threading;

namespace DentivaPro.Desktop;

public partial class App : Application
{
    private IHost? _host;
    private ILogger<App>? _logger;

    [SuppressMessage("Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "The WPF process boundary must report startup failure generically and stop safely.")]
    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        try
        {
            _host = AppHostBuilder.Create(e.Args);
            await _host.StartAsync();
            _logger = _host.Services.GetRequiredService<ILogger<App>>();
            DispatcherUnhandledException += OnDispatcherUnhandledException;
            AppDomain.CurrentDomain.UnhandledException += OnDomainUnhandledException;

            var mainWindow = _host.Services.GetRequiredService<MainWindow>();
            MainWindow = mainWindow;
            mainWindow.Show();
            _logger.LogInformation(new EventId(101, "ApplicationStarted"), "Dentiva Pro foundation application started.");
        }
        catch (Exception exception)
        {
            _logger?.LogCritical(new EventId(901, "StartupFailed"), exception, "Application startup failed.");
            MessageBox.Show(
                "Dentiva Pro could not start. No clinic workflows are available in this foundation build. Check the local application log or contact support.",
                "Dentiva Pro",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            Shutdown(-1);
        }
    }

    [SuppressMessage("Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "Shutdown is a top-level process boundary; failures are logged without preventing disposal.")]
    protected override void OnExit(ExitEventArgs e)
    {
        try
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            _host?.StopAsync(timeout.Token).GetAwaiter().GetResult();
        }
        catch (Exception exception)
        {
            _logger?.LogError(new EventId(902, "ShutdownFailed"), exception, "The application host did not stop cleanly.");
        }
        finally
        {
            _host?.Dispose();
            _host = null;
        }

        base.OnExit(e);
    }

    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        _logger?.LogCritical(new EventId(903, "UnhandledUiException"), e.Exception, "Unhandled UI exception; allowing the process to terminate.");
        MessageBox.Show(
            "An unexpected error occurred. Dentiva Pro will close. Check the local application log before trying again.",
            "Dentiva Pro",
            MessageBoxButton.OK,
            MessageBoxImage.Error);
        e.Handled = false;
    }

    private void OnDomainUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception exception)
        {
            _logger?.LogCritical(new EventId(904, "UnhandledDomainException"), exception, "An unhandled background exception occurred.");
        }
        else
        {
            _logger?.LogCritical(new EventId(904, "UnhandledDomainException"), "An unhandled non-exception object was raised.");
        }
    }
}
