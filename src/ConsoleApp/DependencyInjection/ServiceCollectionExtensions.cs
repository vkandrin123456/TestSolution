using ConsoleApp;
using ConsoleApp.Services;
using ConsoleApp.Access;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Polly;

namespace ConsoleApp.DependencyInjection;

public static class ServiceCollectionExtensions
{

    public static IServiceCollection AddServices(this IServiceCollection services)
    {
        services.AddTransient<IDownloader, Downloader>();
        return services;
    }

    public static IServiceCollection AddHttpClients(this IServiceCollection services) 
    {
        services.AddHttpClient(Consts.HttpClientName)
            .AddTransientHttpErrorPolicy(p => p.WaitAndRetryAsync(3, _ => TimeSpan.FromMilliseconds(500)));

        return services;
    }

    public static IServiceCollection AddDataAccess(this IServiceCollection services)
    {
        services.AddPooledDbContextFactory<StorageContext>(
            (sp, opts) =>
                opts.UseSqlite(sp.GetRequiredService<IConfiguration>()["DataSourcesDb:ConnectionString"]));

        services.AddTransient<IStorageRepository, StorageRepository>();
        return services;
    }
}
