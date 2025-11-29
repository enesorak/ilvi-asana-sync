using Hangfire;
using Hangfire.SqlServer;
using Ilvi.Asana.Infrastructure;
using Ilvi.Asana.Infrastructure.Persistence;
using Ilvi.Asana.Web.Jobs;
using Microsoft.EntityFrameworkCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Serilog yapılandırması
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/asana-sync-.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

// Services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "Asana Sync API", Version = "v1" });
});

// Infrastructure services
builder.Services.AddInfrastructure(builder.Configuration);

// Hangfire
builder.Services.AddHangfire(config =>
{
    config.SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
        .UseSimpleAssemblyNameTypeSerializer()
        .UseRecommendedSerializerSettings()
        .UseSqlServerStorage(builder.Configuration.GetConnectionString("HangfireConnection"), new SqlServerStorageOptions
        {
            CommandBatchMaxTimeout = TimeSpan.FromMinutes(5),
            SlidingInvisibilityTimeout = TimeSpan.FromMinutes(5),
            QueuePollInterval = TimeSpan.Zero,
            UseRecommendedIsolationLevel = true,
            DisableGlobalLocks = true,
            PrepareSchemaIfNecessary = true
        });
});

builder.Services.AddHangfireServer(options =>
{
    options.WorkerCount = 1; // Tek worker - sync paralel çalışmasın
    options.Queues = new[] { "sync", "default" };
});

// CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});

var app = builder.Build();

// Database migration
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AsanaDbContext>();
    try
    {
        await context.Database.MigrateAsync();
        Log.Information("✅ Database migration başarılı");
    }
    catch (Exception ex)
    {
        Log.Error(ex, "❌ Database migration hatası");
    }

    // Varsayılan config oluştur
    if (!await context.SyncConfigurations.AnyAsync())
    {
        context.SyncConfigurations.Add(new Ilvi.Asana.Domain.Entities.SyncConfiguration());
        await context.SaveChangesAsync();
        Log.Information("✅ Varsayılan sync configuration oluşturuldu");
    }
}

// Middleware pipeline
app.UseSerilogRequestLogging();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Asana Sync API v1");
    c.RoutePrefix = "swagger";
});

// Static files (UI)
app.UseDefaultFiles();
app.UseStaticFiles();

app.UseCors();
app.UseAuthorization();

// Hangfire Dashboard
app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    // Production'da auth ekle
    // Authorization = new[] { new HangfireAuthorizationFilter() }
});

app.MapControllers();

// Fallback to index.html for SPA
app.MapFallbackToFile("index.html");

// Recurring job ayarla
RecurringJob.AddOrUpdate<SyncJob>(
    "full-sync",
    job => job.ExecuteAsync(CancellationToken.None),
    Cron.Never); // Başlangıçta devre dışı, config'den aktive edilecek

Log.Information("🚀 Asana Sync uygulaması başlatıldı");
Log.Information("📊 Dashboard: /hangfire");
Log.Information("📚 API Docs: /swagger");

app.Run();
