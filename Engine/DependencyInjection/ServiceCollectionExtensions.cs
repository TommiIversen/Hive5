using Engine.DAL.Repositories;
using Engine.Hubs;
using Engine.Services;
using Engine.Utils;
using Microsoft.Extensions.DependencyInjection;

namespace Engine.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddApplicationServices(this IServiceCollection services)
        {
            services.AddSingleton<IEngineIdProviderFactory, EngineIdProviderFactory>();
            var providerFactory = services.BuildServiceProvider().GetRequiredService<IEngineIdProviderFactory>();
            services.AddSingleton(providerFactory.CreateEngineIdProvider());
            services.AddSingleton<IMessageEnricher, MessageEnricher>();
            services.AddSingleton<IHubWorkerEventHandlers, HubWorkerWorkerEventHandlers>();
            
            services.AddSingleton<MessageQueue>(provider => new MessageQueue(10));
            services.AddScoped<IEngineRepository, EngineRepository>();
            services.AddSingleton<IEngineService, EngineService>();
            
            services.AddSingleton<RepositoryFactory>();
            services.AddSingleton<INetworkInterfaceProvider, NetworkInterfaceProvider>();
            
            services.AddHostedService<MetricsService>();
            services.AddSingleton<ILoggerService, LoggerService>();
            services.AddSingleton<IWorkerManager, WorkerManager>();
            services.AddSingleton<StreamHub>();

            return services;
        }
    }
}