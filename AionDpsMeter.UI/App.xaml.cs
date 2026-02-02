using AionDpsMeter.Services.Extensions;
using AionDpsMeter.Services.Models;
using AionDpsMeter.Services.PacketCapture;
using AionDpsMeter.UI.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Serilog;
using System.Windows;
using AionDpsMeter.Core.Models;
using AionDpsMeter.Services.Services;
using AionDpsMeter.Services.Services.Session;

namespace AionDpsMeter.UI
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static IHost AppHost { get; private set; }

        public App()
        {

          

            AppHost = Host.CreateDefaultBuilder()
                .UseSerilog((context, services, loggerConfiguration) =>
                {
                    loggerConfiguration
                        .ReadFrom.Configuration(context.Configuration);
                })
                .ConfigureServices((context, services) =>
                {
                   
                    services.AddSingleton<TcpStreamBuffer>();  
                    
                    //services.AddSingleton<IPacketCaptureDevice, FilePacketCaptureDevice>();
                    services.AddSingleton<IPacketCaptureDevice, LoopbackCaptureDevice>();

                    services.AddSingleton<IPacketService, AionPacketService>();
                    services.AddSingleton<CombatSessionManager>();
                    services.AddSingleton<MainViewModel>();
                    services.AddSingleton<MainWindow>();

                })
                .Build();
        }

        protected override async void OnStartup(StartupEventArgs e)
        {
            await AppHost.StartAsync();

            var mainWindow = AppHost.Services.GetRequiredService<MainWindow>();
            mainWindow.Show();

            base.OnStartup(e);
        }

        protected override async void OnExit(ExitEventArgs e)
        {
            await AppHost.StopAsync();
            await Log.CloseAndFlushAsync();
            base.OnExit(e);
        }
    }

}
