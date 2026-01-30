using Azure.Data.Tables;
using BOTGC.API.Interfaces;
using BOTGC.API.Services;
using Microsoft.Extensions.Options;

namespace BOTGC.API.Extensions;

public static class TableStoreRegistration
{
    public static IServiceCollection AddAzureTableStore<T>(this IServiceCollection services, string tableName)
        where T : class, ITableEntity, new()
    {
        services.AddSingleton<ITableStore<T>>(sp =>
        {
            var app = sp.GetRequiredService<IOptions<AppSettings>>().Value;

            var client = new TableClient(app.Storage.ConnectionString, tableName);
            client.CreateIfNotExists();
            return new AzureTableStoreService<T>(client);
        });

        return services;
    }
}
