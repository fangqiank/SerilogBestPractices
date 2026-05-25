# Serilog Best Practices / Serilog 最佳实践

ASP.NET Core 10 示例项目，演示 Serilog 结构化日志的最佳实践，集成 MediatR CQRS 管道日志和 Seq 日志聚合平台。

![Architecture](serilog-architecture.svg)

## Tech Stack

| 技术 | 版本 | 用途 |
|------|------|------|
| .NET | 10 | Web API 框架 |
| MediatR | 14.1 | CQRS + Pipeline Behavior |
| Serilog | 10.0 | 结构化日志 |
| Seq | latest (Docker) | 日志聚合与查询 |
| Scalar | 2.14 | OpenAPI 文档 UI |

## Architecture / 架构

```
HTTP Client (X-Correlation-Id Header)
    │
    ▼
CorrelationIdMiddleware ──→ LogContext.PushProperty("CorrelationId")
    │
    ▼
SerilogRequestLogging ──→ HTTP {Method} {Path} {Status} {Elapsed}
    │
    ▼
OrdersController ──→ IMediator.Send(command/query)
    │                  ArgumentException → 400 BadRequest
    ▼
LoggingBehavior<,> ──→ Stopwatch (try/finally) 自动记录耗时、异常
    │
    ▼
CQRS Handlers ──→ ILogger<T> 结构化日志
    │
    ▼
Serilog ──→ Console Sink / Seq Sink (Docker)
```

### Module Table

| 模块 | 文件 | 职责 |
|------|------|------|
| CorrelationIdMiddleware | `Middleware/CorrelationIdMiddleware.cs` | 优先读取客户端 X-Correlation-Id 请求头，推入 LogContext，写入响应头 |
| LoggingBehavior | `Behaviors/LoggingBehavior.cs` | MediatR Pipeline Behavior，try/finally + Stopwatch 自动记录请求名称、耗时和异常 |
| OrdersController | `Controllers/OrdersController.cs` | 订单 API 控制器，委托 MediatR 处理，ArgumentException 返回 400 |
| CreateOrderHandler | `Features/Orders/CreateOrder.cs` | 创建订单 Command Handler，ArgumentException 验证 |
| GetOrderHandler | `Features/Orders/GetOrder.cs` | 查询订单 Query Handler |
| Order | `Models/Order.cs` | 订单领域模型，DateTimeOffset 时区安全 |

## Quick Start

### 启动 Seq（日志聚合）

```bash
docker-compose up -d seq
```

Seq Dashboard: http://localhost:8081

### 运行应用

```bash
dotnet run --project SerilogBestPractices/SerilogBestPractices.csproj
```

应用地址: http://localhost:5018

### 构建

```bash
dotnet build SerilogBestPractices/SerilogBestPractices.csproj
```

## API Endpoints

| Method | Route | Description |
|--------|-------|-------------|
| `POST` | `/api/orders` | 创建订单 |
| `GET` | `/api/orders/{id}` | 查询订单 |

### Example Request

```bash
# 创建订单
curl -X POST http://localhost:5018/api/orders \
  -H "Content-Type: application/json" \
  -d '{"customerName":"John Doe","amount":99.9}'

# 响应头包含 X-Correlation-Id
# X-Correlation-Id: 0HNLQ5G9I3UHL:00000001
```

```bash
# 查询订单
curl http://localhost:5018/api/orders/{id}
```

## Best Practices Summary

1. **appsettings.json 配置 Serilog** — 环境分级，Enrich 节自包含，零代码冗余
2. **CorrelationId 全链路追踪** — 支持客户端 X-Correlation-Id 请求头传入
3. **MediatR LoggingBehavior** — try/finally + Stopwatch 自动记录
4. **SerilogRequestLogging** — HTTP 请求自动日志，EnrichDiagnosticContext 注入上下文
5. **结构化日志模板** — `{CustomerName}` 命名占位符，DateTimeOffset 时区安全
6. **验证异常结构化响应** — ArgumentException → 400 BadRequest，非 500
