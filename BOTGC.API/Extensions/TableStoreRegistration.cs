using BOTGC.API.Services;
using BOTGC.API.Interfaces;
using Azure.Data.Tables;
using Microsoft.Extensions.Options;

namespace BOTGC.API.Extensions
{
    public static class TableStoreRegistration
    {
        public static IServiceCollection AddAzureTableStore<T>(this IServiceCollection services, string? tableNameOverride = null)
            where T : class, ITableEntity, new()
        {
            services.AddSingleton<ITableStore<T>>(sp =>
            {
                var app = sp.GetRequiredService<IOptions<AppSettings>>().Value;
                var tableName = string.IsNullOrWhiteSpace(tableNameOverride)
                    ? app.Storage.LookupDataSource
                    : tableNameOverride;

                var client = new TableClient(app.Storage.ConnectionString, tableName);
                client.CreateIfNotExists();
                return new AzureTableStoreService<T>(client);
            });

            return services;
        }
    }
}
