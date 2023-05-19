using ConsoleApp.Configs;
using ConsoleApp.Events;
using ConsoleApp.Services;
using ConsoleApp.Access;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using System.Data;
using Microsoft.EntityFrameworkCore;

namespace ConsoleApp;

internal class Program
{
    static async Task Main(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json")
            .Build();

        using var logger = new LoggerConfiguration()
            .ReadFrom.Configuration(configuration)
            .CreateLogger();
        try
        {
            logger.Information("Configuration process");
            var services = new ServiceCollection()
                .AddLogging(builder => { builder.AddSerilog(logger); });

            new Startup(configuration).ConfigureServices(services);

            await using var serviceProvider = services.BuildServiceProvider();

            await CreateDbAsync(serviceProvider);

            bool isCompleted = false;
            using var cts = new CancellationTokenSource();
            Console.CancelKeyPress += (s, e) =>
            {
                if (!cts.IsCancellationRequested && !isCompleted) cts.Cancel();
                e.Cancel = true;
            };
            logger.Information("For interrupted proceess press CTRL+C");
            await RunWorkerAsync(serviceProvider, cts.Token);
            isCompleted = true;
        }
        catch (OperationCanceledException)
        {
            logger.Warning("Process Service is interrupted");
        }
        catch (Exception ex)
        {
            logger.Fatal(ex, "Error occurring run application");
        }
        logger.Information("Press any key");
        Console.ReadKey();

        async Task CreateDbAsync(IServiceProvider serviceProvider)
        {
            var factory = serviceProvider.GetRequiredService<IDbContextFactory<StorageContext>>();
            using var ctx = await factory.CreateDbContextAsync();
            await ctx.Database.EnsureCreatedAsync();
        }
    }

    internal static async Task RunWorkerAsync(IServiceProvider serviceProvider, CancellationToken token = default)
    {
        var logger = serviceProvider.GetRequiredService<ILogger<Program>>();
        var service = serviceProvider.GetRequiredService<IDownloader>();
        logger.LogInformation("Start process service");
        var dataSources = serviceProvider.GetRequiredService<DataSourceConfig>();
        var config = serviceProvider.GetRequiredService<DownloaderConfig>();

        try
        {
            service.ProgressChanged += OnProgressChanged;
            service.FileDownloadedAsync += OnFileDownloadedAsync;

            var urls = await DataSourceFilteredAsync(dataSources.Urls, token);
            await service.DownloadAsync(urls, config.DownloadDirecory, config.MaxParallelism, token);
        }
        finally
        {
            service.FileDownloadedAsync -= OnFileDownloadedAsync;
            service.ProgressChanged -= OnProgressChanged;
        }
        logger.LogInformation("Process Service is completed !!!");


        void OnProgressChanged(object seder, ProgressChangedEventArgs arg)
        {
            logger.LogDebug("Downloading. Progress: {1} %", arg.Downloaded * 100 / arg.Total);
        }

        async Task OnFileDownloadedAsync(object sender, FileDownloadedEventArgs arg)
        {
            if (!config.SaveState) return;
            try
            {
                var repository = serviceProvider.GetRequiredService<IStorageRepository>();
                await repository.AddAsync(new Models.Source() { Url = arg.Url }, token);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while saving the downloaded URL");
            }
        }

        async Task<ICollection<string>> DataSourceFilteredAsync(ICollection<string> src, CancellationToken token = default)
        {
            if (!config.SaveState) return src;
            var repository = serviceProvider.GetRequiredService<IStorageRepository>();
            return src.Except((await repository.GetAllAsync(token)).Select(x => x.Url)).ToArray();
        }
    }
}