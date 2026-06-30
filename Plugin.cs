using ClassIsland.Core.Abstractions;
using ClassIsland.Core.Attributes;
using ClassIsland.Core.Extensions.Registry;
using ClassIsland.TcpUdpSender.Models;
using ClassIsland.TcpUdpSender.Services;
using ClassIsland.TcpUdpSender.Views.SettingsPages;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace ClassIsland.TcpUdpSender;

[PluginEntrance]
public class Plugin : PluginBase
{
    public override void Initialize(HostBuilderContext context, IServiceCollection services)
    {
        services.AddSingleton<SenderSettings>();
        services.AddSingleton<TimetableSenderService>();
        services.AddHostedService<TimetableSenderService>(provider => provider.GetRequiredService<TimetableSenderService>());
        services.AddSettingsPage<TcpUdpSettingsPage>();
    }
}

