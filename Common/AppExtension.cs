using System;
using System.Linq;
using Consul;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Net;

namespace Common.Extension
{
    public static class AppExtensions
    {
        public static IServiceCollection AddConsulConfiguration(this IServiceCollection services)
        {
            IConfiguration configuration;
            using (var serviceProvider = services.BuildServiceProvider())
            {
                configuration = serviceProvider.GetService<IConfiguration>();
            }

            var consulHostURL = configuration.GetSection("Consul").GetSection("HostURL").Value;
            var consulDockerHostURL = configuration.GetSection("Consul").GetSection("DockerHostURL").Value;
            var hostInDocker = configuration.GetSection("HostInDocker").Value;

            services.AddSingleton<IConsulClient, ConsulClient>(p => new ConsulClient(consulConfig =>
                 {
                     if (hostInDocker == "true")
                         consulConfig.Address = new Uri(consulDockerHostURL);
                     else
                         consulConfig.Address = new Uri(consulHostURL);
                 }
            ));

            return services;
        }

        public static IApplicationBuilder UseConsul(this IApplicationBuilder app)
        {
            var configuration = app.ApplicationServices.GetRequiredService<IConfiguration>();
            var serviceName = configuration.GetSection("ServiceName").Value;

            var consulClient = app.ApplicationServices.GetRequiredService<IConsulClient>();
            var logger = app.ApplicationServices.GetRequiredService<ILoggerFactory>();
            var lifeTime = app.ApplicationServices.GetRequiredService<IApplicationLifetime>();

            if (!(app.Properties["server.Features"] is FeatureCollection features)) return app;

            var addresses = features.Get<IServerAddressesFeature>();
            var address = addresses.Addresses.First();

            // foreach (var item in addresses.Addresses)
            // {
            //     Console.WriteLine($"inside loop - address = {item}");    
            // }

            Console.WriteLine($"address = {address}");

            // var uri = new Uri(address);

            // Console.WriteLine($"Host: {uri.Host}");

            //get host name and IP
            var hostName = Dns.GetHostName(); // get container id
            var ip = Dns.GetHostEntry(hostName).AddressList.FirstOrDefault(x => x.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork);

            Console.WriteLine($"Host Name: {hostName}, IP Address: {ip}");

            var registration = new AgentServiceRegistration()
            {
                /*
                ID = $"{serviceName}-{uri.Port}",
                Name = serviceName,
                Address = $"{uri.Host}",
                Port = uri.Port
                */


                ID = $"{serviceName}-{Guid.NewGuid().ToString()}",
                Name = serviceName,
                Address = $"{ip}", // $"{hostName}",
                Port = 80

            };

            //logger.LogInformation("Registering with Consul");

            consulClient.Agent.ServiceDeregister(registration.ID).ConfigureAwait(true);
            consulClient.Agent.ServiceRegister(registration).ConfigureAwait(true);

            lifeTime.ApplicationStopped.Register(() =>
            {
                //logger.LogInformation("Unregistering from Consul");
                consulClient.Agent.ServiceDeregister(registration.ID).ConfigureAwait(true);
            });

            return app;
        }
    }
}
