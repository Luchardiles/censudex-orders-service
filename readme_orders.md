# Censudex Order Service

Microservicio de gestión de pedidos para la plataforma Censudex, desarrollado con .NET 8.

## Información del Proyecto

**Asignatura:** Arquitectura de Sistemas  
**Institución:** Universidad Católica del Norte  
**Taller:** N°2 - Migración a Microservicios  

### Integrante
- [Luis Ardiles] - [20.972.802-8]

## 🏗️ Arquitectura

### Patrón de Diseño
Este microservicio implementa el patrón **Repository Pattern** junto con **Service Layer Pattern** para separar la lógica de negocio del acceso a datos.

**Capas:**
- **Presentation Layer (gRPC):** Servicios gRPC que exponen la funcionalidad
- **Business Logic Layer:** Servicios que contienen la lógica de negocio
- **Data Access Layer:** Repositorios que acceden a la base de datos
- **Messaging Layer:** Integración con RabbitMQ
- **Notification Layer:** Envío de emails con SendGrid

### Stack Tecnológico
- **.NET 8:** Framework principal
- **MySQL:** Base de datos relacional
- **Entity Framework Core:** ORM
- **gRPC:** Comunicación con API Gateway
- **RabbitMQ:** Mensajería asíncrona
- **SendGrid:** Notificaciones por email
- **FluentValidation:** Validación de datos

## 🚀 Requisitos Previos

- .NET 8 SDK
- MySQL Server 8.0+
- RabbitMQ
- Visual Studio 2022 / VS Code
- Cuenta de SendGrid (para notificaciones)

## ⚙️ Configuración

### 1. Clonar el Repositorio

```bash
git clone https://github.com/tu-usuario/censudex-orderservice.git
cd censudex-orderservice
```

## 2: Inicia la Base de Datos y RabbitMQ (con Docker)

Nuestra aplicación necesita una base de datos para guardar cosas. Usaremos Docker para esto.

1.  Abre **Docker Desktop** y asegúrate de que esté corriendo (el icono de la ballena debe estar verde).
2.  Abre tu **Terminal** (CMD, PowerShell, o Terminal en Mac).
3.  Copia y pega el siguiente comando. Puedes cambia la contraseña `tu_password` con la que mas te acomode  .

    ```bash
   docker run --name censudex_orders -e MYSQL_ROOT_PASSWORD=tu_password -e MYSQL_DATABASE=censudex_orders -p 3306:3306 -d mysql:latest
    ```
4.  Presiona **Enter**.

* *(Nota: La primera vez, esto tardará un minuto porque tiene que "descargar" mysql.)*
* *(Nota 2: Si ya lo habías creado y solo está detenido, el comando es: `docker start censudex_orders`)*

¡Listo! Tu base de datos ya está corriendo.

5. Copia y pega el siguiente comando, el usuario y contraseña son guest por default.

    ```bash
   docker run -d --hostname my-rabbit --name rabbitmq -p 5672:5672 -p 15672:15672 rabbitmq:3-management
    ```
* *(Nota: La primera vez, esto tardará un minuto porque tiene que "descargar" RabbitMQ.)*
* *(Nota 2: Si ya lo habías creado y solo está detenido, el comando es: `docker start rabbitmq`)*


---

### 3. Configurar appsettings.json

Editar `appsettings.json` con tus credenciales, recordando los datos que ocupaste al crear las imagenes de docker:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Port=3306;Database=censudex_orders;User=root;Password=tu_password;"
  },
  "RabbitMQ": {
    "HostName": "localhost",
    "Port": 5672,
    "UserName": "guest",
    "Password": "guest"
  },
  "SendGrid": {
    "ApiKey": "TU_SENDGRID_API_KEY",
    "FromEmail": "noreply@censudex.cl",
    "FromName": "Censudex"
  }
}
```

### 4. Aplicar Migraciones

```bash
dotnet ef database update
```

O desde Visual Studio:
```powershell
Update-Database
```

### 5. Ejecutar el Seeder (Opcional)

Descomentar en `Program.cs`:
```csharp
await OrderSeeder.SeedAsync(dbContext);
```

## 🏃 Ejecución

### Modo Desarrollo

```bash
dotnet run
```

O desde Visual Studio: presiona `F5`

El servicio estará disponible en: `http://localhost:5001`

## 📡 Endpoints gRPC

El servicio expone los siguientes métodos gRPC (consumidos por API Gateway)

### Configuración Rápida de Postman

1.  Abre Postman y crea una **nueva solicitud gRPC**.
2.  En la URL, escribe: `localhost:5001`
3.  Ve a `Settings` ⚙️ > `General` y **apaga** (`OFF`) la opción `SSL certificate verification`.
4.  Haz clic en **"Import a .proto"** e importa el archivo `censudex-orders/Protos/orders.proto`.
5.  En el menú de la izquierda, selecciona el método que quieres probar.

---

### CreateOrder
Crea un nuevo pedido.

**Request:**
```protobuf
message CreateOrderRequest {
  string client_id = 1;
  string shipping_address = 2;
  repeated OrderItemRequest items = 3;
}

ejemplo:
{
    "client_id": "050d95e8-07ac-406c-aca8-23a705146390",
    "items": [
        {
            "product_id": "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
            "quantity": 2
        }
    ],
    "shipping_address": "velit"
}

```

**Response:** `OrderResponse`

### GetOrders
Obtiene todos los pedidos con filtros opcionales.

**Request:**
```protobuf
message GetOrdersRequest {
  optional string order_id = 1;
  optional string client_id = 2;
  optional string client_name = 3;
  optional string start_date = 4;
  optional string end_date = 5;
}
ejemplo:

```

**Response:** `OrderListResponse`

### GetOrderById
Obtiene un pedido específico por ID.

**Request:**
```protobuf
message GetOrderByIdRequest {
  string order_id = 1;
}
ejemplo:
{
    "order_id": "f4b9d2fd-ca16-4c0e-b14d-858a8b563cfb"
}
```

**Response:** `OrderResponse`

### UpdateOrderStatus
Actualiza el estado de un pedido.

**Request:**
```protobuf
message UpdateOrderStatusRequest {
  string order_id = 1;
  string status = 2;
  optional string tracking_number = 3;
}

ejemplo:
{
    "order_id": "f4b9d2fd-ca16-4c0e-b14d-858a8b563cfb",
    "status": "Enviado",
    "tracking_number": "TRACK-451723"
}
```

**Response:** `OrderResponse`

### CancelOrder
Cancela un pedido.

**Request:**
```protobuf
message CancelOrderRequest {
  string order_id = 1;
  string cancellation_reason = 2;
}
ejemplo:

```

**Response:** `OrderResponse`

## 🔄 Integración con RabbitMQ

### Eventos Publicados

#### order.created
Publicado cuando se crea un nuevo pedido.

```json
{
  "OrderId": "guid",
  "ClientId": "guid",
  "Items": [
    {
      "ProductId": "guid",
      "Quantity": 2
    }
  ],
  "CreatedAt": "2025-11-14T10:30:00Z"
}
```

#### order.status.updated
Publicado cuando cambia el estado de un pedido.

```json
{
  "OrderId": "guid",
  "OldStatus": "Pendiente",
  "NewStatus": "EnProcesamiento",
  "TrackingNumber": "TRACK-123456",
  "UpdatedAt": "2025-11-14T11:00:00Z"
}
```

#### order.cancelled
Publicado cuando se cancela un pedido.

```json
{
  "OrderId": "guid",
  "CancellationReason": "Cliente solicitó cancelación",
  "CancelledAt": "2025-11-14T12:00:00Z"
}
```

### Eventos Consumidos

#### order.failed.stock
Consumido cuando el Inventory Service indica falta de stock.

## 📧 Notificaciones por Email

El servicio envía automáticamente emails en los siguientes eventos:

- **Pedido Creado:** Confirmación de pedido
- **En Procesamiento:** Pedido siendo preparado
- **Enviado:** Pedido despachado con número de tracking
- **Entregado:** Confirmación de entrega
- **Cancelado:** Notificación de cancelación con motivo

## 📚 Recursos Adicionales

- [Documentación .NET 8](https://learn.microsoft.com/es-es/dotnet/core/whats-new/dotnet-8)
- [gRPC en .NET](https://learn.microsoft.com/es-es/aspnet/core/grpc/)
- [Entity Framework Core](https://learn.microsoft.com/es-es/ef/core/)
- [RabbitMQ .NET Client](https://www.rabbitmq.com/dotnet-api-guide.html)
- [SendGrid .NET Library](https://github.com/sendgrid/sendgrid-csharp)

