using CarpetPC.Core;
using CarpetPC.Core.Models;
using System.Diagnostics;
using System.Windows;
using WpfButton = System.Windows.Controls.Button;
using WpfDock = System.Windows.Controls.Dock;
using WpfDockPanel = System.Windows.Controls.DockPanel;
using WpfGrid = System.Windows.Controls.Grid;
using WpfMessageBox = System.Windows.MessageBox;
using WpfMessageBoxButton = System.Windows.MessageBoxButton;
using WpfMessageBoxImage = System.Windows.MessageBoxImage;
using WpfOrientation = System.Windows.Controls.Orientation;
using WpfProgressBar = System.Windows.Controls.ProgressBar;
using WpfRowDefinition = System.Windows.Controls.RowDefinition;
using WpfScrollViewer = System.Windows.Controls.ScrollViewer;
using WpfStackPanel = System.Windows.Controls.StackPanel;
using WpfTextBlock = System.Windows.Controls.TextBlock;

namespace CarpetPC.App.Models;

public sealed class ModelSetupWindow : Window
{
    private readonly ModelSetupService _modelSetupService;
    private readonly IRuntimeLog _runtimeLog;
    private readonly Dictionary<string, WpfProgressBar> _progressBars = [];
    private readonly Dictionary<string, WpfTextBlock> _statusBlocks = [];
    private CancellationTokenSource? _downloadCancellation;

    public ModelSetupWindow(ModelSetupService modelSetupService, IRuntimeLog runtimeLog)
    {
        _modelSetupService = modelSetupService;
        _runtimeLog = runtimeLog;

        Title = "CarpetPC Model Setup";
        Width = 780;
        Height = 560;
        MinWidth = 640;
        MinHeight = 420;
        Content = BuildContent();
    }

    private UIElement BuildContent()
    {
        var root = new WpfDockPanel { Margin = new Thickness(16) };
        var header = new WpfTextBlock
        {
            Text = "Download only what you choose. CarpetPC will not auto-download models.",
            FontSize = 16,
            Margin = new Thickness(0, 0, 0, 12)
        };
        WpfDockPanel.SetDock(header, WpfDock.Top);
        root.Children.Add(header);

        var toolbar = new WpfStackPanel
        {
            Orientation = WpfOrientation.Horizontal,
            Margin = new Thickness(0, 0, 0, 12)
        };

        var openModels = new WpfButton { Content = "Open Model Folder", Margin = new Thickness(0, 0, 8, 0) };
        openModels.Click += (_, _) => Process.Start(new ProcessStartInfo(_modelSetupService.GetModelDirectory()) { UseShellExecute = true });

        var openRuntimes = new WpfButton { Content = "Open Runtime Folder", Margin = new Thickness(0, 0, 8, 0) };
        openRuntimes.Click += (_, _) => Process.Start(new ProcessStartInfo(_modelSetupService.GetRuntimeDirectory()) { UseShellExecute = true });

        var cancel = new WpfButton { Content = "Cancel Active Download" };
        cancel.Click += (_, _) => _downloadCancellation?.Cancel();

        toolbar.Children.Add(openModels);
        toolbar.Children.Add(openRuntimes);
        toolbar.Children.Add(cancel);
        WpfDockPanel.SetDock(toolbar, WpfDock.Top);
        root.Children.Add(toolbar);

        var list = new WpfStackPanel();
        foreach (var item in _modelSetupService.GetAvailableModels())
        {
            list.Children.Add(BuildModelRow(item));
        }

        root.Children.Add(new WpfScrollViewer { Content = list });
        return root;
    }

    private UIElement BuildModelRow(ModelCatalogItem item)
    {
        var row = new WpfGrid { Margin = new Thickness(0, 0, 0, 14) };
        row.RowDefinitions.Add(new WpfRowDefinition { Height = GridLength.Auto });
        row.RowDefinitions.Add(new WpfRowDefinition { Height = GridLength.Auto });
        row.RowDefinitions.Add(new WpfRowDefinition { Height = GridLength.Auto });
        row.RowDefinitions.Add(new WpfRowDefinition { Height = GridLength.Auto });

        var title = new WpfTextBlock
        {
            Text = $"{item.DisplayName} ({FormatBytes(item.ApproximateBytes)}){(item.Required ? " - Required" : " - Optional")}",
            FontWeight = FontWeights.Bold
        };
        WpfGrid.SetRow(title, 0);
        row.Children.Add(title);

        var description = new WpfTextBlock
        {
            Text = $"{item.Description}\nSource: {item.SourceUri}",
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 2, 0, 4)
        };
        WpfGrid.SetRow(description, 1);
        row.Children.Add(description);

        var progress = new WpfProgressBar
        {
            Minimum = 0,
            Maximum = 100,
            Height = 18,
            Value = _modelSetupService.IsModelPresent(item) ? 100 : 0
        };
        _progressBars[item.Id] = progress;
        WpfGrid.SetRow(progress, 2);
        row.Children.Add(progress);

        var controls = new WpfStackPanel { Orientation = WpfOrientation.Horizontal, Margin = new Thickness(0, 6, 0, 0) };
        var status = new WpfTextBlock
        {
            Text = _modelSetupService.IsModelPresent(item) ? "Installed" : "Missing",
            Width = 140,
            VerticalAlignment = VerticalAlignment.Center
        };
        _statusBlocks[item.Id] = status;

        var download = new WpfButton
        {
            Content = item.DirectDownloadUri is null ? "Manual/Train Required" : "Download",
            IsEnabled = item.DirectDownloadUri is not null,
            Margin = new Thickness(0, 0, 8, 0)
        };
        download.Click += async (_, _) => await DownloadAsync(item);

        var openSource = new WpfButton { Content = "Open Source" };
        openSource.Click += (_, _) => Process.Start(new ProcessStartInfo(item.SourceUri.ToString()) { UseShellExecute = true });

        controls.Children.Add(status);
        controls.Children.Add(download);
        controls.Children.Add(openSource);
        WpfGrid.SetRow(controls, 3);
        row.Children.Add(controls);

        return row;
    }

    private async Task DownloadAsync(ModelCatalogItem item)
    {
        var confirm = WpfMessageBox.Show(
            this,
            $"Download {item.DisplayName} from:\n{item.DirectDownloadUri}\n\nDestination:\n{_modelSetupService.GetAssetPath(item)}",
            "Confirm Model Download",
            WpfMessageBoxButton.YesNo,
            WpfMessageBoxImage.Question);

        if (confirm != MessageBoxResult.Yes)
        {
            return;
        }

        _downloadCancellation?.Cancel();
        _downloadCancellation = new CancellationTokenSource();
        var plan = await _modelSetupService.CreateDownloadPlanAsync(item, _downloadCancellation.Token);
        var progress = new Progress<ModelDownloadProgress>(value =>
        {
            _progressBars[item.Id].Value = value.Fraction * 100;
            _statusBlocks[item.Id].Text = $"{FormatBytes(value.DownloadedBytes)} / {FormatBytes(value.TotalBytes ?? item.ApproximateBytes)}";
        });

        try
        {
            _runtimeLog.Info($"Downloading {item.DisplayName}.");
            await _modelSetupService.DownloadAsync(plan, progress, _downloadCancellation.Token);
            _statusBlocks[item.Id].Text = "Installed";
            _runtimeLog.Info($"Installed {item.DisplayName}.");
        }
        catch (OperationCanceledException)
        {
            _statusBlocks[item.Id].Text = "Cancelled";
            _runtimeLog.Warn($"Cancelled download: {item.DisplayName}.");
        }
        catch (Exception ex)
        {
            _statusBlocks[item.Id].Text = "Failed";
            _runtimeLog.Error($"Download failed for {item.DisplayName}: {ex.Message}");
            WpfMessageBox.Show(this, ex.Message, "Download Failed", WpfMessageBoxButton.OK, WpfMessageBoxImage.Error);
        }
    }

    private static string FormatBytes(long bytes)
    {
        string[] units = ["B", "KB", "MB", "GB"];
        var value = (double)bytes;
        var unit = 0;
        while (value >= 1024 && unit < units.Length - 1)
        {
            value /= 1024;
            unit++;
        }

        return $"{value:0.##} {units[unit]}";
    }
}
