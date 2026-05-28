# Serilog Best Practices / Serilog 最佳实践

.NET 10 Console Application 示例项目，演示 Serilog 结构化日志的 8 个最佳实践，集成 MediatR CQRS 管道日志、领域事件、SQLite 持久化和 Seq 日志聚合平台。

![Architecture](serilog-architecture.svg)

## Tech Stack

| 技术 | 版本 | 用途 |
|------|------|------|
| .NET | 10 | Console Application 运行时 |
| MediatR | 14.1 | CQRS + Pipeline Behavior + Domain Events |
| Serilog | 10.0 | 结构化日志 |
| SQLite + Dapper | 9.0 / 2.1 | 轻量级持久化 |
| Seq | latest (Docker) | 日志聚合与查询 |

## Architecture / 架构

```
Program.cs (Demo Scenarios + LogContext CorrelationId)
    │
    ├─→ LoggerConfiguration ──→ Console (AnsiConsoleTheme.Code) / Seq Sink
    │
    └─→ IMediator.Send(Command/Query)
            │
            ▼
        LoggingBehavior<,> ──→ Stopwatch + Exception Grading
        (ArgumentException → Warning, Others → Error)
            │
            ▼
        CQRS Handlers ──→ ILogger<T> Structured Logging
            │               {OrderId} {CustomerName} {Amount}
            │
            ├─→ IOrderRepository (Dapper) ──→ SQLite (WAL mode)
            │
            └─→ mediator.Publish(DomainEvent) ──→ INotificationHandlers
                   OrderCreatedEvent → EmailHandler + LogHandler
                   OrderPaidEvent   → InventoryHandler + ReceiptHandler
                   OrderShippedEvent→ NotificationHandler + TrackingHandler
```

### Module Table

| 模块 | 文件 | 职责 |
|------|------|------|
| Program.cs | `Program.cs` | Console 入口：Serilog 配置、DI 构建、7 个演示场景 |
| LoggingBehavior | `Behaviors/LoggingBehavior.cs` | MediatR Pipeline Behavior，Stopwatch 计时 + 异常分级 (Warning/Error) |
| CreateOrderHandler | `Features/Orders/CreateOrder.cs` | 创建订单 Command Handler + 发布 OrderCreatedEvent |
| PayOrderHandler | `Features/Orders/PayOrder.cs` | 支付订单 Command Handler + 发布 OrderPaidEvent |
| ShipOrderHandler | `Features/Orders/ShipOrder.cs` | 发货 Command Handler + 发布 OrderShippedEvent |
| GetOrderHandler | `Features/Orders/GetOrder.cs` | 查询订单 Query Handler |
| OrderRepository | `Data/OrderRepository.cs` | Dapper + SQLite 实现，CreateAsync/GetByIdAsync/UpdateStatusAsync |
| Domain Events | `Events/*.cs` | 3 个事件 x 2 个 Handler，演示 Pub-Sub 解耦 |
| Order / OrderStatus | `Models/Order.cs`, `Models/OrderStatus.cs` | 领域模型 + 状态常量 (Created/Paid/Shipped) |

## Quick Start

### 启动 Seq（可选，日志聚合）

```bash
docker-compose up -d seq
```

Seq Dashboard: http://localhost:8081

### 运行应用

```bash
dotnet run --project SerilogBestPractices/SerilogBestPractices.csproj
```

默认以 Development 环境运行（Debug 级别 + Console 彩色输出 + Seq sink）。设置 `DOTNET_ENVIRONMENT=Production` 切换为 Warning 级别。

### 构建

```bash
dotnet build SerilogBestPractices/SerilogBestPractices.csproj
```

## Demo Scenarios

应用运行 7 个场景，覆盖订单完整生命周期：

| 场景 | 操作 | 日志要点 |
|------|------|---------|
| 1 | 创建有效订单 | Information 日志 + OrderCreatedEvent 触发 2 个 Handler |
| 2 | 无效金额（负数） | ArgumentException → LoggingBehavior Warning 级别 |
| 3 | 重复客户检测 | ArgumentException → Warning 级别 |
| 4 | 查询不存在订单 | 返回 null，仅记录查询日志 |
| 5 | 创建 + 支付 | PayOrderCommand + OrderPaidEvent 触发 2 个 Handler |
| 6 | 创建 + 支付 + 发货 | 完整三步流程 + OrderShippedEvent |
| 7 | 完整生命周期（同 CorrelationId） | 同一 CorrelationId 贯穿 Create → Pay → Ship |

## Best Practices Summary

1. **appsettings.json 配置 Serilog** — 环境分级 (Development/Production)，ReadFrom.Configuration 零代码注册
2. **CorrelationId 全链路追踪** — LogContext.PushProperty 手动管理，每个场景独立追踪
3. **结构化日志模板** — `{CustomerName}` `{OrderId}` 命名占位符，禁止 string interpolation
4. **环境分级日志** — Development: Debug 级别 + Seq sink；Production: Warning 级别
5. **Enrichers** — FromLogContext / WithMachineName / WithThreadId 自动注入上下文
6. **MediatR LoggingBehavior** — Stopwatch 计时 + 异常分级，cross-cutting 零侵入
7. **异常分级** — ArgumentException → Warning（业务验证）；Unexpected → Error + 完整堆栈
8. **领域事件 Pub-Sub** — Handler 成功后发布 INotification，解耦业务副作用
