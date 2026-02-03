using Azure.Core;
using Azure.Data.Tables;
using BOTGC.API.Interfaces;
using BOTGC.API.Services;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace BOTGC.API.Extensions;

public static class TableStoreRegistration
{
    public static IServiceCollection AddAzureTableStore<T>(this IServiceCollection services, string tableName)
        where T : class, ITableEntity, new()
    {
        services.TryAddSingleton<TableServiceClient>(sp =>
        {
            var app = sp.GetRequiredService<IOptions<AppSettings>>().Value;

            var options = new TableClientOptions
            {
                Retry =
                {
                    Mode = RetryMode.Exponential,
                    MaxRetries = 6,
                    Delay = TimeSpan.FromMilliseconds(200),
                    MaxDelay = TimeSpan.FromSeconds(5),
                    NetworkTimeout = TimeSpan.FromSeconds(30),
                },
            };

            return new TableServiceClient(app.Storage.ConnectionString, options);
        });

        services.AddSingleton<ITableStore<T>>(sp =>
        {
            var serviceClient = sp.GetRequiredService<TableServiceClient>();
            var client = serviceClient.GetTableClient(tableName);
            var logger = sp.GetRequiredService<ILogger<AzureTableStoreService<T>>>();
            return new AzureTableStoreService<T>(client, logger);
        });

        services.AddHostedService(sp =>
        {
            var serviceClient = sp.GetRequiredService<TableServiceClient>();
            var client = serviceClient.GetTableClient(tableName);
            var logger = sp.GetRequiredService<ILogger<TableClientInitialiser>>();
            return new TableClientInitialiser(client, logger);
        });

        return services;
    }
}
