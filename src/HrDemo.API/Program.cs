using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;
using HrDemo.API.Middleware;
using HrDemo.Application;
using HrDemo.Infrastructure;
using HrDemo.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services
    .AddApplicationServices()
    .AddInfrastructureServices(builder.Configuration)
    .AddMediator();

builder.Services.AddResponseCompression();
builder.Services.AddControllers();

// Health checks configuration
builder.Services.AddHealthChecks()
    .AddDbContextCheck<ApplicationDbContext>("DatabaseReady");

// Rate limiting setup
builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("AuthPolicy", opt =>
    {
        opt.PermitLimit = 5;
        opt.Window = TimeSpan.FromSeconds(10);
        opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        opt.QueueLimit = 2;
    });
});

builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((document, context, cancellationToken) => 
    {
        return Task.CompletedTask;
    });
});

var app = builder.Build();

app.UseMiddleware<ExceptionHandlingMiddleware>();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}
else
{
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseResponseCompression();
app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Map Health Checks
app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = _ => false // Liveness check does not run any registered checks
});

app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready") || check.Name == "DatabaseReady"
});

app.Run();

public partial class Program { }

