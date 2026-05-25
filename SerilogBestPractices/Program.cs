using Scalar.AspNetCore;
using Serilog;
using SerilogBestPractices.Behaviors;
using SerilogBestPractices.Middleware;

var builder = WebApplication.CreateBuilder(args);

// ===== 最佳实践 #1：使用 appsettings.json 配置 Serilog =====
builder.Host.UseSerilog((context, services, configuration) =>
{
    configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()      
        .Enrich.WithMachineName()     
        .Enrich.WithThreadId();       
});

// 添加 MediatR
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(Program).Assembly);

    // 最佳实践 #6：注册 Logging Pipeline Behavior
    cfg.AddOpenBehavior(typeof(LoggingBehavior<,>));
});

builder.Services.AddControllers();

builder.Services.AddOpenApi();

var app = builder.Build();

// ===== 最佳实践 #5：注册 CorrelationId 中间件（必须在 UseSerilogRequestLogging 之前） =====
app.UseCorrelationId();

// ===== 最佳实践 #4：启用 Serilog 请求日志中间件 =====
app.UseSerilogRequestLogging(options =>
{
    options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";
    options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
    {
        diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value);
        diagnosticContext.Set("RequestScheme", httpContext.Request.Scheme);
        diagnosticContext.Set("RemoteIp", httpContext.Connection.RemoteIpAddress?.ToString());
    };
});


// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    app.MapScalarApiReference();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

try
{
    Log.Information("Starting web application");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
