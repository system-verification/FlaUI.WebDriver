
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using System.IO;
using FlaUI.WebDriver;
using FlaUI.WebDriver.Services;
using Microsoft.OpenApi.Models;
using Serilog;


var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File(
        Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs/webdriver-.log"),
        rollingInterval: RollingInterval.Day,
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}"));


builder.Services.AddSingleton<ISessionRepository, SessionRepository>();
builder.Services.AddScoped<IActionsDispatcher, ActionsDispatcher>();
builder.Services.AddScoped<IWindowsExtensionService, WindowsExtensionService>();
builder.Services.AddScoped<IConditionParser, ConditionParser>();

builder.Services.Configure<RouteOptions>(options => options.LowercaseUrls = true);
builder.Services.AddControllers(options =>
    options.Filters.Add(new WebDriverResponseExceptionFilter()));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "FlaUI.WebDriver", Version = "v1" });
});

builder.Services.Configure<SessionCleanupOptions>(
    builder.Configuration.GetSection(SessionCleanupOptions.OptionsSectionName));
builder.Services.AddHostedService<SessionCleanupService>();

var app = builder.Build();

app.UseMiddleware<NotFoundMiddleware>();

app.UseExceptionHandler("/error");

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "FlaUI.WebDriver v1"));
}

app.UseAuthorization();

app.MapControllers();

app.Run();
