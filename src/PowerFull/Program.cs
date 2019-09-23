using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace PowerFull
{
    class Program
    {
        private static async Task Main(string[] args)
        {
            var builder = new HostBuilder()
                .ConfigureAppConfiguration(
                    (hostingContext, config) =>
                    {
                        config.AddEnvironmentVariables("PowerFull");
                        if (args != null)
                        {
                            config.AddCommandLine(args);
                        }
                    })
                .ConfigureServices(
                    (hostContext, services) =>
                    {
                        services.AddOptions<Device.Config>().Bind(hostContext.Configuration.GetSection("Device"));
                        services.AddSingleton<Device.IFactory, Device.Factory>();
                        services.AddOptions<Messaging.Config>().Bind(hostContext.Configuration.GetSection("MQTT"));
                        services.AddSingleton<Messaging.Mqtt.IFactory, Messaging.Mqtt.Factory>();
                        services.AddSingleton<Messaging.Facade.IFactory, Messaging.Facade.Factory>();
                        services.AddOptions<Service.Config>().Bind(hostContext.Configuration.GetSection("Service"));
                        services.AddSingleton<Service.State.IMachine, Service.State.Machine>();
                        services.AddSingleton<Service.State.IFactory, Service.State.Factory>();
                        services.AddSingleton<Service.State.Transition.IFactory, Service.State.Transition.Factory>();
                        services.AddSingleton<IHostedService, Service.Implementation>();
                    })
                .ConfigureLogging(
                (hostingContext, logging) => 
                    {
                        logging.AddConsole();
                    });

            try
            {
                await builder
                    .UseConsoleLifetime()
                    .Build()
                    .ValidateConfiguration<Device.Config, Messaging.Config, Service.Config>()
                    .RunAsync();
            }
            catch (ConfigurationValidationException e)
            {
                Console.WriteLine($"One or more configuration errors occured:{Environment.NewLine}{e.Message}");
                Console.ReadLine();
            }
        }
    }
}
