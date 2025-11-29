using Ilvi.Asana.Application.Services;
using Ilvi.Asana.Domain.Entities;
using Ilvi.Asana.Domain.Interfaces;
using Ilvi.Asana.Infrastructure.AsanaApi;
using Ilvi.Asana.Infrastructure.Persistence;
using Ilvi.Asana.Infrastructure.Persistence.Repositories;
using Ilvi.Asana.Infrastructure.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Polly;
using Polly.Extensions.Http;

namespace Ilvi.Asana.Infrastructure;

/// <summary>
/// Infrastructure layer dependency injection
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Database
        services.AddDbContext<AsanaDbContext>(options =>
        {
            options.UseSqlServer(
                configuration.GetConnectionString("DefaultConnection"),
                sqlOptions =>
                {
                    sqlOptions.CommandTimeout(300); // 5 dakika timeout
                    sqlOptions.EnableRetryOnFailure(
                        maxRetryCount: 3,
                        maxRetryDelay: TimeSpan.FromSeconds(30),
                        errorNumbersToAdd: null);
                });
        });

        // Repositories
        services.AddScoped(typeof(IRepository<>), typeof(GenericRepository<>));
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // Asana API
        services.AddSingleton<AsanaRateLimiter>(sp =>
        {
            var logger = sp.GetService<Microsoft.Extensions.Logging.ILogger<AsanaRateLimiter>>();
            var requestsPerMinute = configuration.GetValue<int>("Asana:RequestsPerMinute", 1400);
            return new AsanaRateLimiter(requestsPerMinute, 50, logger);
        });

        services.AddHttpClient<IAsanaApiClient, AsanaApiClient>((sp, client) =>
        {
            client.BaseAddress = new Uri("https://app.asana.com/api/1.0/");
            client.DefaultRequestHeaders.Add("Accept", "application/json");
            
            var token = configuration["Asana:PersonalAccessToken"];
            if (!string.IsNullOrEmpty(token))
            {
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
            }
            
            client.Timeout = TimeSpan.FromMinutes(5);
        })
        .AddPolicyHandler(GetRetryPolicy())
        .AddPolicyHandler(GetCircuitBreakerPolicy());

        // Storage
        services.AddHttpClient<IStorageService, LocalStorageService>()
            .AddPolicyHandler(GetRetryPolicy());

        services.AddScoped<IStorageService>(sp =>
        {
            var httpClient = sp.GetRequiredService<IHttpClientFactory>().CreateClient();
            var logger = sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<LocalStorageService>>();
            var basePath = configuration["Storage:BasePath"] ?? "./attachments";
            var maxWidth = configuration.GetValue<int>("Storage:ThumbnailMaxWidth", 400);
            var generateThumbnails = configuration.GetValue<bool>("Storage:GenerateThumbnails", true);
            
            return new LocalStorageService(httpClient, logger, basePath, maxWidth, generateThumbnails);
        });

        // Services
        services.AddScoped<ISyncService, SyncOrchestrator>();

        return services;
    }

    private static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .OrResult(msg => msg.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: (retryAttempt, response, context) =>
                {
                    // Rate limit durumunda Retry-After header'ını kullan
                    if (response.Result?.Headers.RetryAfter?.Delta != null)
                    {
                        return response.Result.Headers.RetryAfter.Delta.Value;
                    }
                    
                    // Exponential backoff
                    return TimeSpan.FromSeconds(Math.Pow(2, retryAttempt));
                },
                onRetryAsync: (outcome, timespan, retryAttempt, context) =>
                {
                    // Log retry attempt
                    return Task.CompletedTask;
                });
    }

    private static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy()
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .CircuitBreakerAsync(
                handledEventsAllowedBeforeBreaking: 5,
                durationOfBreak: TimeSpan.FromSeconds(30));
    }
}
