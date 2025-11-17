using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using FluentValidation;
using censudex_orders.Data.Context;
using censudex_orders.Repositories.Interfaces;
using censudex_orders.Repositories.Implementations;
using censudex_orders.Services;
using censudex_orders.Validators;
using censudex_orders.Services.GrpcServices;
using censudex_orders.Data.Seeders; 

var builder = WebApplication.CreateBuilder(args);

// ===== CONFIGURACIÓN DE Kestrel PARA HTTP/2 =====
builder.WebHost.ConfigureKestrel(options =>
{
    // Puerto definido en appsettings o .env, por defecto 5001
    var grpcPort = int.Parse(builder.Configuration["gRPC:Port"] ?? "5001");
    options.ListenAnyIP(grpcPort, listenOptions =>
    {
        listenOptions.Protocols = HttpProtocols.Http2;  // Necesario para gRPC
        // No se configura TLS aquí (plaintext HTTP/2)
    });
});

// ===== CONFIGURACIÓN DE SERVICIOS =====

// Configurar DbContext con MySQL
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<OrderDbContext>(options =>
    options.UseMySql(
        connectionString,
        ServerVersion.AutoDetect(connectionString),
        mySqlOptions => mySqlOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(10),
            errorNumbersToAdd: null
        )
    )
);

// Configurar gRPC
builder.Services.AddGrpc(options =>
{
    options.EnableDetailedErrors = true;
});

// Registrar HttpClient para comunicación con otros servicios
builder.Services.AddHttpClient();

// Registrar Repositorios
builder.Services.AddScoped<IOrderRepository, OrderRepository>();

// Registrar Servicios de Negocio
builder.Services.AddScoped<IOrderBusinessService, OrderBusinessService>();
builder.Services.AddScoped<IEmailService, SendGridService>();

// Registrar RabbitMQ como Singleton
builder.Services.AddSingleton<IRabbitMQService, RabbitMQService>();

// Registrar Validadores
builder.Services.AddValidatorsFromAssemblyContaining<CreateOrderDtoValidator>();

// Configurar CORS para desarrollo
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader()
              .WithExposedHeaders("Grpc-Status", "Grpc-Message");
    });
});

// Configurar Logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

var app = builder.Build();

// ===== CONFIGURACIÓN DE MIDDLEWARE =====

app.UseCors("AllowAll");
app.MapGrpcService<OrderGrpcService>();

// Endpoint de health check
app.MapGet("/health", () => Results.Ok(new
{
    status = "healthy",
    service = "OrderService",
    timestamp = DateTime.UtcNow
}));

// ===== INICIALIZACIÓN =====

if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<OrderDbContext>();
    try
    {
        dbContext.Database.Migrate();
        await OrderSeeder.SeedAsync(dbContext);
        app.Logger.LogInformation("Migraciones aplicadas y datos sembrados");
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "Error al aplicar migraciones");
    }
}

// Escuchar eventos de RabbitMQ
var rabbitMQService = app.Services.GetRequiredService<IRabbitMQService>();
var configuration = app.Services.GetRequiredService<IConfiguration>();
var stockFailedQueue = configuration["RabbitMQ:Queues:StockFailed"];
if (!string.IsNullOrEmpty(stockFailedQueue))
{
    rabbitMQService.SubscribeToQueue(stockFailedQueue, async (message) =>
    {
        app.Logger.LogWarning($"Stock fallido recibido: {message}");
    });
}

var grpcPortValue = builder.Configuration["gRPC:Port"] ?? "5001";
app.Logger.LogInformation($"OrderService iniciado en HTTP/2 (sin TLS) en puerto {grpcPortValue}");
app.Logger.LogInformation($"Servicio gRPC disponible en grpc://localhost:{grpcPortValue}");

app.Run();

