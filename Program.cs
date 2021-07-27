using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ec2whitelist
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var host = new HostBuilder()
            .ConfigureAppConfiguration((hostingContext, config) => 
            {
                config.AddJsonFile("appsettings.json",optional: false, reloadOnChange: true);
                config.AddJsonFile($"appsettings.{hostingContext.HostingEnvironment.EnvironmentName}.json", true, true);
                config.AddEnvironmentVariables();
                if(args != null)
                {
                    config.AddCommandLine(args);
                }
                config.Build();
                
            })
            .ConfigureServices((hostContext, services) => 
            {
                services.AddOptions();
                
                services.Configure<AWSOptions>(hostContext.Configuration.GetSection("AWSOptions"));
                
                services.AddSingleton<IHostedService, EC2CheckerService>();
            })
            .ConfigureLogging((hostingContext, logging) => 
            {
                logging.AddConfiguration(hostingContext.Configuration.GetSection("Logging"));
                logging.AddConsole();
            })
            .Build();

            await host.RunAsync();
        }
    }
}
