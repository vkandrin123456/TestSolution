using ConsoleApp.Configs;
using ConsoleApp.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Polly;
using Polly.Extensions.Http;

namespace ConsoleApp;


public class Startup
{
    public Startup(IConfiguration configuration)
    {
        Configuration = configuration;
    }

    public IConfiguration Configuration { get; }

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<IConfiguration>(Configuration);

        services.AddSingleton(Configuration.GetSection("WorkFlow").Get<DownloaderConfig>() ?? new());
        services.AddSingleton(Configuration.GetSection("DataSources").Get<DataSourceConfig>() ?? new());

        services.AddDataAccess();
        services.AddHttpClients();

        services.AddServices();
    }
}
