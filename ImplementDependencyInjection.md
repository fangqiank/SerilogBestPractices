# .NET 依赖注入实践指南 / Implementing Dependency Injection in .NET

本文归纳 .NET 中引入 DI 的三种方式，覆盖 Console App、WPF、WinForms，附带完整代码示例。

## 目录

- [核心 NuGet 包](#核心-nuget-包)
- [方式一：手动搭建 ServiceCollection（Console App）](#方式一手动搭建-servicecollectionconsole-app)
- [方式二：Generic Host（WPF / WinForms .NET 6+）](#方式二generic-hostwpf--winforms-net-6)
- [方式三：手动搭建（WPF / WinForms .NET Framework）](#方式三手动搭建wpf--winforms-net-framework)
- [三种方式对比](#三种方式对比)
- [服务生命周期](#服务生命周期)
- [最佳实践](#最佳实践)

---

## 核心 NuGet 包

所有方式都基于同一个 DI 容器包：

```xml
<PackageReference Include="Microsoft.Extensions.DependencyInjection" Version="9.0.0" />
```

使用 Generic Host 时需要额外引用：

```xml
<PackageReference Include="Microsoft.Extensions.Hosting" Version="9.0.0" />
```

---

## 方式一：手动搭建 ServiceCollection（Console App）

适用场景：Console App、Lambda/函数、没有宿主基础架构的项目。

### 完整示例

```csharp
// Program.cs
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

// 1. 注册服务
var services = new ServiceCollection();

services.AddSingleton<IOrderRepository, OrderRepository>();
services.AddSingleton<IEmailService, FakeEmailService>();
services.AddLogging(builder => builder.AddConsole());

// 2. 构建 ServiceProvider
var serviceProvider = services.BuildServiceProvider();

// 3. 解析并使用
var repository = serviceProvider.GetRequiredService<IOrderRepository>();

try
{
    // 业务逻辑
    var order = await repository.GetByIdAsync(Guid.NewGuid());
}
catch (Exception ex)
{
    Console.WriteLine($"Error: {ex.Message}");
}
finally
{
    // 4. 手动释放所有 IDisposable 单例
    serviceProvider.Dispose();
}
```

### 关键点

| 项目 | 说明 |
|------|------|
| 构建 | `services.BuildServiceProvider()` |
| 解析 | `serviceProvider.GetRequiredService<T>()` |
| 释放 | `finally` 中手动调用 `serviceProvider.Dispose()` |
| Scoped | 无 HTTP 请求边界，`Scoped` 退化为 `Transient` |
| 配置 | 需手动创建 `ConfigurationBuilder` |
| 日志 | 需手动调用 `services.AddLogging(...)` |

### 本项目（SerilogBestPractices）的实际注册

```csharp
// Program.cs 中的实际 DI 注册
var services = new ServiceCollection();

// MediatR CQRS + Pipeline Behavior
services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(Program).Assembly);
    cfg.AddOpenBehavior(typeof(LoggingBehavior<,>));
    cfg.LicenseKey = configuration["MediatR:LicenseKey"];
});

// Serilog → Microsoft.Extensions.Logging 桥接
services.AddLogging(loggingBuilder => loggingBuilder.AddSerilog(dispose: false));

// 基础设施
var dbConnection = new SqliteConnection("Data Source=serilogdemo.db");
services.AddSingleton<IDisposable>(dbConnection);  // 确保 Dispose 时关闭连接
services.AddSingleton(dbConnection);
services.AddSingleton<IOrderRepository, OrderRepository>();

// 外部服务抽象
services.AddSingleton<IEmailService, FakeEmailService>();

var serviceProvider = services.BuildServiceProvider();
var mediator = serviceProvider.GetRequiredService<IMediator>();

// ... 业务逻辑 ...

finally
{
    serviceProvider.Dispose();  // 级联释放 SQLite 连接等
    Log.CloseAndFlush();
}
```

---

## 方式二：Generic Host（WPF / WinForms .NET 6+）

适用场景：.NET 6+ 的 WPF / WinForms 桌面应用。推荐方式，提供完整的宿主基础架构。

### WPF 完整示例

```csharp
// App.xaml.cs
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;

public partial class App : Application
{
    private IHost? _host;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        _host = Host.CreateDefaultBuilder()
            .ConfigureServices((context, services) =>
            {
                // 配置（自动加载 appsettings.json）
                // 日志（自动注册 Console / Debug / EventLog 等输出）
                // 以上均由 CreateDefaultBuilder 自动配置

                // 注册应用服务
                services.AddSingleton<IOrderRepository, OrderRepository>();
                services.AddSingleton<IEmailService, FakeEmailService>();

                // 注册主窗口（WPF）
                services.AddTransient<MainWindow>();
                services.AddTransient<OrderViewModel>();
            })
            .Build();

        // 解析并显示主窗口
        var mainWindow = _host.Services.GetRequiredService<MainWindow>();
        mainWindow.Show();

        // 启动 Host（处理后台任务、生命周期事件等）
        await _host.RunAsync();
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        base.OnExit(e);
        if (_host is not null)
        {
            await _host.StopAsync();
            _host.Dispose();
        }
    }
}
```

```csharp
// MainWindow.xaml.cs — 构造函数注入
public partial class MainWindow : Window
{
    public MainWindow(
        IOrderRepository repository,
        IEmailService emailService,
        OrderViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        // repository, emailService 可直接使用
    }
}
```

### WinForms 完整示例

```csharp
// Program.cs (.NET 6+ WinForms)
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;

internal static class Program
{
    [STAThread]
    static async Task Main(string[] args)
    {
        ApplicationConfiguration.Initialize();

        var host = Host.CreateDefaultBuilder(args)
            .ConfigureServices((context, services) =>
            {
                services.AddSingleton<IOrderRepository, OrderRepository>();
                services.AddSingleton<IEmailService, FakeEmailService>();

                // 注册窗体
                services.AddTransient<MainForm>();
            })
            .Build();

        // 解析并运行主窗体
        var mainForm = host.Services.GetRequiredService<MainForm>();
        Application.Run(mainForm);

        await host.RunAsync();
    }
}
```

```csharp
// MainForm.cs — 构造函数注入
public class MainForm : Form
{
    private readonly IOrderRepository _repository;
    private readonly IEmailService _emailService;

    public MainForm(IOrderRepository repository, IEmailService emailService)
    {
        _repository = repository;
        _emailService = emailService;
        InitializeComponent();
    }
}
```

### Generic Host 自动提供的能力

| 能力 | 手动搭建需要 | Generic Host 自动 |
|------|-------------|-------------------|
| `IConfiguration` | 手动 `ConfigurationBuilder` | 自动加载 appsettings.json + 环境变量 |
| `ILogger<T>` | 手动 `services.AddLogging(...)` | 自动注册多种 Logger Provider |
| `IHostApplicationLifetime` | 无 | 自动注册，可监听启动/停止事件 |
| `IHostedService` | 无 | 支持后台任务 |
| 资源释放 | 手动 `Dispose` | `StopAsync()` + `Dispose()` 自动管理 |
| 环境变量 | 手动读取 | 自动加载 `DOTNET_ENVIRONMENT` |

---

## 方式三：手动搭建（WPF / WinForms .NET Framework）

适用场景：.NET Framework 4.x 的旧项目，无法使用 Generic Host。

### WPF (.NET Framework) 示例

```csharp
// App.xaml.cs
using Microsoft.Extensions.DependencyInjection;

public partial class App : Application
{
    private ServiceProvider? _serviceProvider;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var services = new ServiceCollection();

        services.AddSingleton<IOrderRepository, OrderRepository>();
        services.AddSingleton<IEmailService, FakeEmailService>();
        services.AddTransient<MainWindow>();

        _serviceProvider = services.BuildServiceProvider();

        var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
        mainWindow.Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        base.OnExit(e);
        _serviceProvider?.Dispose();
    }
}
```

### WinForms (.NET Framework) 示例

```csharp
// Program.cs
using Microsoft.Extensions.DependencyInjection;

internal static class Program
{
    private static ServiceProvider? _serviceProvider;

    [STAThread]
    static void Main()
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        var services = new ServiceCollection();
        services.AddSingleton<IOrderRepository, OrderRepository>();
        services.AddSingleton<IEmailService, FakeEmailService>();
        services.AddTransient<Form1>();

        _serviceProvider = services.BuildServiceProvider();

        var mainForm = _serviceProvider.GetRequiredService<Form1>();
        Application.ApplicationExit += (s, e) => _serviceProvider?.Dispose();
        Application.Run(mainForm);
    }
}
```

> .NET Framework 需要 NuGet 安装 `Microsoft.Extensions.DependencyInjection`，最低支持 .NET Framework 4.6.1+。

---

## 三种方式对比

| | Console App | WPF/WinForms .NET 6+ | WPF/WinForms .NET Framework |
|---|---|---|---|
| DI 容器 | `ServiceCollection` | `ServiceCollection`（Host 内部） | `ServiceCollection` |
| 构建方式 | 手动 | `Host.CreateDefaultBuilder()` | 手动 |
| 配置 | 手动 `ConfigurationBuilder` | 自动加载 appsettings.json | 手动 `ConfigurationBuilder` |
| 日志 | 手动 `AddLogging(...)` | 自动注册 | 手动 |
| 生命周期管理 | `try/finally` 手动 `Dispose` | `IHost` 自动管理 | `OnExit` / `ApplicationExit` 手动 `Dispose` |
| 后台任务 | 无 | `IHostedService` 支持 | 无 |
| Scoped 语义 | 无意义（无请求边界） | 同左 | 同左 |
| 推荐注册方式 | `AddSingleton` | `AddSingleton` | `AddSingleton` |

---

## 服务生命周期

三种方式中，`ServiceLifetime` 行为完全一致：

```csharp
// Singleton: 全局唯一实例，整个应用生命周期共享
services.AddSingleton<IOrderRepository, OrderRepository>();

// Transient: 每次解析都创建新实例
services.AddTransient<IEmailService, FakeEmailService>();

// Scoped: Console/WPF/WinForms 中无 HTTP 请求边界，退化为 Transient
services.AddScoped<IPaymentService, PaymentService>();
```

### 桌面应用中的生命周期选择

```
Singleton   ← 数据库连接、HttpClient、Repository、配置服务
Transient   ← ViewModel (WPF)、Form (WinForms)、每个窗口独立实例
Scoped      ← 桌面应用中避免使用（无请求边界，行为不可预期）
```

### 陷阱：Captured Dependencies

Singleton 服务不能依赖 Scoped/Transient 服务（会导致被提升为 Singleton，状态不再独立）：

```csharp
// 错误：OrderRepository 是 Singleton，ILogger<OrderRepository> 内部是 Scoped
// 解决：ILogger<T> 实际注入的是代理，不受此限制
services.AddSingleton<IOrderRepository, OrderRepository>();

// 正确：如果 OrderService 需要 DbContext 那样的 Scoped 依赖，应改为 Transient
services.AddTransient<IOrderService, OrderService>();
```

---

## 最佳实践

### 1. 依赖接口，不依赖实现

```csharp
// 正确
services.AddSingleton<IEmailService, FakeEmailService>();

// 错误 — 调用方直接依赖具体类，无法替换
services.AddSingleton<FakeEmailService>();
```

### 2. 构造函数注入（推荐）

```csharp
// 正确：构造函数注入（C# primary constructor 语法）
public class OrderCreatedEmailHandler(
    IEmailService emailService
    ) : INotificationHandler<OrderCreatedEvent>
{
    public async Task Handle(...)
    {
        await emailService.SendOrderConfirmationAsync(...);
    }
}
```

### 3. 避免服务定位器反模式

```csharp
// 错误：在方法内部解析服务（隐藏依赖关系）
public class OrderService
{
    public void Process()
    {
        var repo = _serviceProvider.GetRequiredService<IOrderRepository>();
    }
}

// 正确：构造函数注入，依赖关系显式声明
public class OrderService(IOrderRepository repository)
{
    public void Process()
    {
        var order = repository.GetByIdAsync(id);
    }
}
```

### 4. IDisposable 资源必须注册释放

```csharp
// 确保 SQLite 连接在 ServiceProvider.Dispose 时被释放
services.AddSingleton<IDisposable>(dbConnection);  // 注册为 IDisposable
services.AddSingleton(dbConnection);                // 同时注册为具体类型
```

### 5. 批量注册（Assembly Scanning）

```csharp
// MediatR 自动扫描所有 Handler — 无需逐个注册
services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(Program).Assembly);
});
```
