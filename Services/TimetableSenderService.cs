using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Shared;
using ClassIsland.Shared.ComponentModels;
using ClassIsland.Shared.Models.Profile;
using ClassIsland.TcpUdpSender.Models;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ClassIsland.TcpUdpSender.Services;

/// <summary>
/// 课表定时发送后台服务
/// </summary>
public class TimetableSenderService : BackgroundService
{
    private readonly SenderSettings _settings;
    private readonly ILogger<TimetableSenderService> _logger;
    private ILessonsService? _lessonsService;
    private IProfileService? _profileService;

    public TimetableSenderService(SenderSettings settings, ILogger<TimetableSenderService> logger)
    {
        _settings = settings;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // 延迟等待 ClassIsland 服务初始化完成
        await Task.Delay(5000, stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (_settings.IsEnabled)
                {
                    await SendTimetableAsync(stoppingToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "发送课表时出错");
            }

            await Task.Delay(TimeSpan.FromSeconds(_settings.SendInterval), stoppingToken);
        }
    }

    /// <summary>
    /// 发送测试数据包（不依赖课表服务，用于验证网络连通性）
    /// </summary>
    public async Task<string> SendTestPacketAsync()
    {
        var testPayload = JsonSerializer.Serialize(new
        {
            type = "test",
            message = "ClassIsland TcpUdpSender 测试包",
            timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
            settings = new
            {
                address = _settings.TargetAddress,
                port = _settings.TargetPort,
                protocol = _settings.Protocol,
                format = _settings.SendFormat,
                interval = _settings.SendInterval
            }
        });

        try
        {
            await SendRawAsync(testPayload);
            return "✅ 测试包已发送";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "发送测试包失败");
            return $"❌ 发送失败：{ex.Message}";
        }
    }

    /// <summary>
    /// 立即发送课表（供设置页测试按钮调用），返回结果描述
    /// </summary>
    public async Task<string> SendNowAsync()
    {
        // 延迟获取服务
        _lessonsService ??= IAppHost.TryGetService<ILessonsService>();
        _profileService ??= IAppHost.TryGetService<IProfileService>();

        if (_lessonsService is null || _profileService is null)
        {
            return "⚠️ 课表服务尚未初始化，无法发送课表数据";
        }

        var today = DateTime.Now;
        var classPlan = _lessonsService.GetClassPlanByDate(today, out var guid);
        if (classPlan is null)
        {
            return "⚠️ 今日无课表，无法发送课表数据";
        }

        try
        {
            var subjects = _profileService.Profile?.Subjects ?? new ObservableDictionary<Guid, Subject>();
            var payload = _settings.SendFormat switch
            {
                "PlainText" => BuildPlainText(classPlan, subjects),
                _ => BuildJson(classPlan, subjects)
            };

            await SendRawAsync(payload);
            return $"✅ 课表已通过 {_settings.Protocol} 发送至 {_settings.TargetAddress}:{_settings.TargetPort}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "发送课表时出错");
            return $"❌ 发送失败：{ex.Message}";
        }
    }

    /// <summary>
    /// 原始数据发送（TCP/UDP）
    /// </summary>
    private async Task SendRawAsync(string payload)
    {
        var data = Encoding.UTF8.GetBytes(payload);
        var endpoint = new IPEndPoint(
            IPAddress.Parse(_settings.TargetAddress),
            _settings.TargetPort);

        if (_settings.Protocol == "TCP")
        {
            using var client = new TcpClient();
            await client.ConnectAsync(endpoint.Address, endpoint.Port);
            await client.GetStream().WriteAsync(data);
            _logger.LogDebug("已通过 TCP 发送数据到 {Endpoint}", endpoint);
        }
        else
        {
            using var client = new UdpClient();
            await client.SendAsync(data, data.Length, endpoint);
            _logger.LogDebug("已通过 UDP 发送数据到 {Endpoint}", endpoint);
        }
    }

    private async Task SendTimetableAsync(CancellationToken cancellationToken)
    {
        _lessonsService ??= IAppHost.TryGetService<ILessonsService>();
        _profileService ??= IAppHost.TryGetService<IProfileService>();

        if (_lessonsService is null || _profileService is null)
        {
            _logger.LogWarning("课表服务尚未初始化，跳过本次发送");
            return;
        }

        var today = DateTime.Now;
        var classPlan = _lessonsService.GetClassPlanByDate(today, out var guid);
        if (classPlan is null)
        {
            _logger.LogDebug("今日无课表，跳过发送");
            return;
        }

        var subjects = _profileService.Profile?.Subjects ?? new ObservableDictionary<Guid, Subject>();
        var payload = _settings.SendFormat switch
        {
            "PlainText" => BuildPlainText(classPlan, subjects),
            _ => BuildJson(classPlan, subjects)
        };

        var data = Encoding.UTF8.GetBytes(payload);
        var endpoint = new IPEndPoint(
            IPAddress.Parse(_settings.TargetAddress),
            _settings.TargetPort);

        if (_settings.Protocol == "TCP")
        {
            using var client = new TcpClient();
            await client.ConnectAsync(endpoint.Address, endpoint.Port, cancellationToken);
            await client.GetStream().WriteAsync(data, cancellationToken);
            _logger.LogDebug("已通过 TCP 发送课表到 {Endpoint}", endpoint);
        }
        else
        {
            using var client = new UdpClient();
            await client.SendAsync(data, data.Length, endpoint);
            _logger.LogDebug("已通过 UDP 发送课表到 {Endpoint}", endpoint);
        }
    }

    private string BuildJson(ClassPlan classPlan, ObservableDictionary<Guid, Subject> subjects)
    {
        var date = DateTime.Now.ToString("yyyy-MM-dd");
        var time = DateTime.Now.ToString("HH:mm:ss");
        var classes = new List<object>();

        foreach (var cls in classPlan.Classes)
        {
            if (!cls.IsEnabled) continue;

            var subject = subjects.TryGetValue(cls.SubjectId, out var s) ? s : null;
            var layoutItem = cls.CurrentTimeLayoutItem;

            classes.Add(new
            {
                index = cls.Index + 1,
                subject = subject?.Name ?? "???",
                initial = subject?.Initial ?? "?",
                teacher = subject?.TeacherName ?? "",
                start = layoutItem.StartTime.ToString("hh\\:mm"),
                end = layoutItem.EndTime.ToString("hh\\:mm"),
                isChanged = cls.IsChangedClass
            });
        }

        var payload = new
        {
            date,
            time,
            classPlanName = classPlan.Name,
            classes
        };

        return JsonSerializer.Serialize(payload, new JsonSerializerOptions
        {
            WriteIndented = false
        });
    }

    private string BuildPlainText(ClassPlan classPlan, ObservableDictionary<Guid, Subject> subjects)
    {
        var sb = new StringBuilder();
        var date = DateTime.Now.ToString("yyyy-MM-dd");
        var time = DateTime.Now.ToString("HH:mm:ss");

        foreach (var cls in classPlan.Classes)
        {
            if (!cls.IsEnabled) continue;

            var subject = subjects.TryGetValue(cls.SubjectId, out var s) ? s : null;
            var layoutItem = cls.CurrentTimeLayoutItem;
            var line = _settings.CustomTemplate
                .Replace("{date}", date)
                .Replace("{time}", time)
                .Replace("{subject}", subject?.Name ?? "???")
                .Replace("{initial}", subject?.Initial ?? "?")
                .Replace("{teacher}", subject?.TeacherName ?? "")
                .Replace("{start}", layoutItem.StartTime.ToString("hh\\:mm"))
                .Replace("{end}", layoutItem.EndTime.ToString("hh\\:mm"))
                .Replace("{index}", (cls.Index + 1).ToString())
                .Replace("{isChanged}", cls.IsChangedClass ? "[换]" : "");
            sb.AppendLine(line);
        }

        return sb.ToString();
    }
}
