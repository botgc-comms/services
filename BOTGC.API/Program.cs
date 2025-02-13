using BOTGC.API.Services;
using Microsoft.OpenApi.Models;
using Services;
using Services.Common;
using Services.Extensions;
using Services.Interfaces;
using Services.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Trophy API",
        Version = "v1",
        Description = "API for retrieving trophy information, including processed winner images."
    });

    options.OperationFilter<CacheControlHeaderFilter>(); 
});

builder.Services.Configure<AppSettings>(builder.Configuration);
builder.Services.AddSingleton<IConfiguration>(builder.Configuration);

builder.Services.AddSingleton<TrophyFilesDiskStorage>();
builder.Services.AddSingleton<ITrophyDataStore, TrophyDataStore>();
builder.Services.AddSingleton<ITrophyService, TrophyService>();

builder.Services.AddSingleton<ICognitiveServices, AzureCognitiveServices>();
builder.Services.AddSingleton<IImageServices, ImageServices>();

builder.Services.AddHttpContextAccessor();

//builder.Services.AddMemoryCache();
//builder.Services.AddScoped<ICacheService, MemoryCacheService>();

builder.Services.AddScoped<ICacheService, FileCacheService>();

// Add support for interacting with IG
builder.Services.AddIGSupport();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
