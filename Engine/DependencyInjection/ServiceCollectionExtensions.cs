using Engine.DAL.Repositories;
using Engine.Database;
using Engine.Hubs;
using Engine.Interfaces;
using Engine.Services;
using Engine.Utils;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace Engine.DependencyInjection
{

        
        public static class ServiceCollectionExtensions
        {
            public static IServiceCollection AddApplicationServicesPreBuild(this IServiceCollection services)
            {
                services.AddSingleton<RepositoryFactory>();
                services.AddSingleton<IEngineIdProviderFactory, EngineIdProviderFactory>();
        
                services.AddScoped<IEngineIdProvider>(sp =>
                {
                    var factory = sp.GetRequiredService<IEngineIdProviderFactory>();
                    return factory.CreateEngineIdProvider();
                });

                services.AddSingleton<IMessageEnricher, MessageEnricher>();
                services.AddSingleton<IHubWorkerEventHandlers, HubWorkerWorkerEventHandlers>();
//                services.AddSingleton<IMessageQueue, MessageQueue>(provider => new MessageQueue(10));
                services.AddSingleton<IMessageQueue>(provider => new MessageQueue(10)); // Sørg for at bruge konstruktøren korrekt

                services.AddScoped<IEngineRepository, EngineRepository>();
                services.AddSingleton<IEngineService, EngineService>();
                services.AddSingleton<StreamerWatchdogFactory>();
                services.AddSingleton<INetworkInterfaceProvider, NetworkInterfaceProvider>();
                services.AddHostedService<MetricsService>();
                services.AddSingleton<ILoggerService, LoggerService>();
                services.AddSingleton<IWorkerManager, WorkerManager>();
                services.AddSingleton<StreamHub>();

                return services;
            }
            
            
            public static void InitializeEngineId(IServiceProvider serviceProvider)
            {
                using var scope = serviceProvider.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var engineEntity = dbContext.EngineEntities.FirstOrDefault();

                if (engineEntity != null)
                {
                    var loggerService = scope.ServiceProvider.GetRequiredService<ILoggerService>();
                    loggerService.SetEngineId(engineEntity.EngineId); // Sæt EngineId i LoggerService
                }
                else
                {
                    Console.WriteLine("Ingen EngineId fundet under initialisering.");
                }
            }
        }

    
}