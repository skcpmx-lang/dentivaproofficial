using System.Windows;

namespace DentivaPro.Desktop;

public partial class MainWindow : Window
{
    private readonly ILogger<MainWindow> _logger;

    public MainWindow(ILogger<MainWindow> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        InitializeComponent();
        _logger.LogInformation(new EventId(100, "FoundationShellShown"), "Phase 1 foundation shell shown.");
    }
}
