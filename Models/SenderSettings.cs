using CommunityToolkit.Mvvm.ComponentModel;

namespace ClassIsland.TcpUdpSender.Models;

/// <summary>
/// TCP/UDP 课表发送器设置
/// </summary>
public partial class SenderSettings : ObservableObject
{
    [ObservableProperty]
    private bool _isEnabled = false;

    [ObservableProperty]
    private string _targetAddress = "127.0.0.1";

    [ObservableProperty]
    private int _targetPort = 8080;

    [ObservableProperty]
    private string _protocol = "UDP";

    [ObservableProperty]
    private string _sendFormat = "JSON";

    [ObservableProperty]
    private int _sendInterval = 30;

    [ObservableProperty]
    private string _customTemplate = "{date} {time} {subject} {teacher} {start}-{end}";
}
