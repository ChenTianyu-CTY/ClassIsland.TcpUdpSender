using System.ComponentModel;
using System.Runtime.CompilerServices;
using Avalonia.Controls;
using Avalonia.Interactivity;
using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Core.Attributes;
using ClassIsland.Shared;
using ClassIsland.TcpUdpSender.Models;
using ClassIsland.TcpUdpSender.Services;

namespace ClassIsland.TcpUdpSender.Views.SettingsPages;

[SettingsPageInfo("tcpudpsender.settings", "TCP/UDP 发送器")]
public partial class TcpUdpSettingsPage : SettingsPageBase, INotifyPropertyChanged
{
    private SenderSettings? _settings;
    private PropertyChangedEventHandler? _propertyChanged;

    public TcpUdpSettingsPage()
    {
        InitializeComponent();
        DataContext = this;
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        _settings = IAppHost.TryGetService<SenderSettings>();
        if (_settings != null)
        {
            _settings.PropertyChanged += OnSettingsPropertyChanged;
            OnSettingsPropertyChanged(this, new PropertyChangedEventArgs(nameof(SenderSettings.SendFormat)));
            OnPropertyChanged(string.Empty);  // 刷新所有绑定
        }
    }

    private void OnUnloaded(object? sender, RoutedEventArgs e)
    {
        if (_settings != null)
        {
            _settings.PropertyChanged -= OnSettingsPropertyChanged;
        }
    }

    private void OnSettingsPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(SenderSettings.SendFormat) || e.PropertyName == string.Empty)
        {
            UpdateVisibility();
        }
    }

    private void UpdateVisibility()
    {
        if (CustomTemplateBorder != null)
        {
            CustomTemplateBorder.IsVisible = Settings?.SendFormat == "PlainText";
        }
    }

    public SenderSettings? Settings => _settings ?? IAppHost.TryGetService<SenderSettings>();

    public List<string> ProtocolOptions { get; } = new() { "UDP", "TCP" };

    public List<string> FormatOptions { get; } = new() { "JSON", "PlainText" };

    private async void OnTestSendClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var service = IAppHost.TryGetService<TimetableSenderService>();
        if (service == null)
        {
            await ShowMessageAsync("❌ 发送服务未找到");
            return;
        }

        // 先发送固定测试包验证网络连通性
        var testResult = await service.SendTestPacketAsync();
        await ShowMessageAsync(testResult);

        // 再尝试发送课表数据
        var timetableResult = await service.SendNowAsync();
        await ShowMessageAsync(timetableResult);
    }

    private async Task ShowMessageAsync(string message)
    {
        // 使用 Avalonia MessageBox 或简单的窗口提示
        var window = new Window
        {
            Title = "发送结果",
            Width = 400,
            Height = 120,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Content = new TextBlock
            {
                Text = message,
                Margin = new Avalonia.Thickness(20),
                TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                FontSize = 14
            }
        };
        var owner = TopLevel.GetTopLevel(this) as Window;
        if (owner != null)
            window.Show(owner);
        else
            window.Show();
        await Task.Delay(3000);
        window.Close();
    }

    event PropertyChangedEventHandler? INotifyPropertyChanged.PropertyChanged
    {
        add => _propertyChanged += value;
        remove => _propertyChanged -= value;
    }

    private void OnPropertyChanged([CallerMemberName] string propertyName = "")
    {
        _propertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
