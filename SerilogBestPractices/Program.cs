using MediatR;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Serilog.Context;
using Serilog.Settings.Configuration;
using Serilog.Sinks.SystemConsole.Themes;
using SerilogBestPractices.Behaviors;
using SerilogBestPractices.Data;
using SerilogBestPractices.Features.Orders;
using SerilogBestPractices.Services;

// ===== 最佳实践 #1：使用 appsettings.json 配置 Serilog =====
var environment = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "Development";

var configuration = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{environment}.json", optional: true, reloadOnChange: true)
    .Build();

// 直接通过 LoggerConfiguration 创建 Serilog，ReadFrom.Configuration 读取 appsettings 中的 Serilog 节
// 显式指定 sink/enricher 所在 assemblies，避免运行时自动扫描依赖 Serilog.AspNetCore
var readerOptions = new ConfigurationReaderOptions(
    typeof(Serilog.ConsoleLoggerConfigurationExtensions).Assembly,  // Serilog.Sinks.Console
    System.Reflection.Assembly.Load("Serilog.Sinks.Seq"),
    System.Reflection.Assembly.Load("Serilog.Enrichers.Environment"),
    System.Reflection.Assembly.Load("Serilog.Enrichers.Thread")
);
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console(
        theme: AnsiConsoleTheme.Code,
        outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] [{CorrelationId}] {Message:lj}{NewLine}{Exception}")
    .ReadFrom.Configuration(configuration, readerOptions)
    .CreateLogger();

// ===== 手动构建 DI 容器（替代 ASP.NET Core 自动注入） =====
var services = new ServiceCollection();

// 最佳实践 #6：注册 MediatR + Logging Pipeline Behavior
// RegisterServicesFromAssembly 自动扫描所有 IRequestHandler 和 INotificationHandler
services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(Program).Assembly);
    cfg.AddOpenBehavior(typeof(LoggingBehavior<,>));
    cfg.LicenseKey = configuration["MediatR:LicenseKey"];
});

// 将 Serilog 桥接到 Microsoft.Extensions.Logging，使 ILogger<T> 注入使用 Serilog 输出
services.AddLogging(loggingBuilder => loggingBuilder.AddSerilog(dispose: false));

// 注册 SQLite 数据库连接 + Repository
var dbConnection = new SqliteConnection("Data Source=serilogdemo.db");
DbInitializer.Initialize(dbConnection);
services.AddSingleton<IDisposable>(dbConnection);
services.AddSingleton(dbConnection);
services.AddSingleton<IOrderRepository, OrderRepository>();
services.AddSingleton<IEmailService, FakeEmailService>();

var serviceProvider = services.BuildServiceProvider();
var mediator = serviceProvider.GetRequiredService<IMediator>();

try
{
    Log.Information("Starting Serilog Demo Console Application");

    // ===== 最佳实践 #2：CorrelationId 全链路追踪 =====
    // 通过 LogContext.PushProperty 手动设置 CorrelationId（替代原 ASP.NET Core 中间件）
    // 每个场景有独立的 CorrelationId，using 结束后自动弹出

    // 场景 1：创建有效订单 -> 触发 OrderCreatedEvent（2 个 Handler）
    using (LogContext.PushProperty("CorrelationId", Guid.NewGuid().ToString()))
    {
        Log.Information("=== 场景 1：创建有效订单 ===");
        var order = await mediator.Send(new CreateOrderCommand("Alice Smith", 150.00m));
        Log.Information("场景 1 完成：订单 {OrderId} 已创建", order.Id);
    }

    // 场景 2：无效金额 -> ArgumentException 验证异常
    using (LogContext.PushProperty("CorrelationId", Guid.NewGuid().ToString()))
    {
        Log.Information("=== 场景 2：无效订单（负数金额） ===");
        try
        {
            await mediator.Send(new CreateOrderCommand("Bob Jones", -50.00m));
        }
        catch (ArgumentException ex)
        {
            // 最佳实践 #7：验证异常结构化响应，非致命错误用 Warning 级别
            // LoggingBehavior 已记录完整异常堆栈，此处仅记录业务摘要
            Log.Warning("场景 2 已处理：{ErrorMessage}", ex.Message);
        }
    }

    // 场景 3：重复客户检测
    using (LogContext.PushProperty("CorrelationId", Guid.NewGuid().ToString()))
    {
        Log.Information("=== 场景 3：重复客户检测 ===");
        try
        {
            await mediator.Send(new CreateOrderCommand("duplicate_user", 75.00m));
        }
        catch (ArgumentException ex)
        {
            Log.Warning("场景 3 已处理：{ErrorMessage}", ex.Message);
        }
    }

    // 场景 4：查询不存在的订单 -> 返回 null
    using (LogContext.PushProperty("CorrelationId", Guid.NewGuid().ToString()))
    {
        Log.Information("=== 场景 4：查询不存在的订单 ===");
        var orderId = Guid.NewGuid();
        var queriedOrder = await mediator.Send(new GetOrderQuery(orderId));
        Log.Information("场景 4 完成：查询订单 {OrderId}，结果：{Found}", orderId, queriedOrder is not null ? "已找到" : "未找到");
    }

    // 场景 5：先创建再支付 -> 触发 OrderPaidEvent（2 个 Handler）
    using (LogContext.PushProperty("CorrelationId", Guid.NewGuid().ToString()))
    {
        Log.Information("=== 场景 5：创建并支付订单 ===");
        var order5 = await mediator.Send(new CreateOrderCommand("Dave Brown", 299.99m));
        Log.Information("场景 5 订单已创建：{OrderId}", order5.Id);
        var paidOrder = await mediator.Send(new PayOrderCommand(order5.Id, order5.Amount));
        Log.Information("场景 5 完成：订单 {OrderId} 已支付", paidOrder.Id);
    }

    // 场景 6：完整发货流程（创建 -> 支付 -> 发货） -> 触发 OrderShippedEvent（2 个 Handler）
    using (LogContext.PushProperty("CorrelationId", Guid.NewGuid().ToString()))
    {
        Log.Information("=== 场景 6：创建、支付并发货订单 ===");
        var order6 = await mediator.Send(new CreateOrderCommand("Eve Johnson", 188.88m));
        await mediator.Send(new PayOrderCommand(order6.Id, order6.Amount));
        var shippedOrder = await mediator.Send(new ShipOrderCommand(order6.Id, "123 Main St, Springfield, IL 62704"));
        Log.Information("场景 6 完成：订单 {OrderId} 已发货", shippedOrder.Id);
    }

    // 场景 7：完整订单生命周期（创建 -> 支付 -> 发货，同一 CorrelationId）
    using (LogContext.PushProperty("CorrelationId", Guid.NewGuid().ToString()))
    {
        Log.Information("=== 场景 7：完整订单生命周期 ===");
        var created = await mediator.Send(new CreateOrderCommand("Carol White", 499.99m));
        Log.Information("生命周期步骤 1：订单 {OrderId} 已创建", created.Id);

        var paid = await mediator.Send(new PayOrderCommand(created.Id, created.Amount));
        Log.Information("生命周期步骤 2：订单 {OrderId} 已支付", paid.Id);

        var shipped = await mediator.Send(new ShipOrderCommand(paid.Id, "456 Oak Ave, Portland, OR 97201"));
        Log.Information("生命周期步骤 3：订单 {OrderId} 已发货", shipped.Id);

        Log.Information("场景 7 完成：订单 {OrderId} 完整生命周期结束", created.Id);
    }

    Log.Information("所有模拟场景已完成");
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    serviceProvider.Dispose();
    Log.CloseAndFlush();
}
