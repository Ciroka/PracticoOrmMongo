# PracticoMongo — Guía Completa: ASP.NET Core + MongoDB Driver + Docker

> El mismo concepto que el proyecto con EF Core + PostgreSQL, pero usando MongoDB como base de datos NoSQL.
> La idea central es la misma: modelos C#, servicios, seeds, Docker. Lo que cambia es cómo se habla con la BD.

## Stack utilizado

| Tecnología | Rol |
|---|---|
| .NET 10 / C# | Framework backend |
| MongoDB.Driver | Driver oficial de MongoDB para .NET (reemplaza a EF Core) |
| MongoDB | Base de datos NoSQL orientada a documentos |
| Mongo Express | Panel de administración web para MongoDB |
| Docker + Docker Compose | Contenedores para BD, panel y backend |

## Diferencias clave respecto a EF Core + PostgreSQL

| Concepto | EF Core + PostgreSQL | MongoDB Driver |
|---|---|---|
| Unidad de datos | Fila en una tabla | Documento JSON (BSON) |
| Agrupación | Tabla | Colección |
| Relaciones | FK + JOIN | Embebido o referencia manual |
| Migraciones | `dotnet ef migrations add` | No existen (esquema flexible) |
| Contexto | `AppDbContext` | `IMongoDatabase` / `IMongoCollection<T>` |
| Panel admin | CoreAdmin | Mongo Express |
| Id | Int autoincremental | `ObjectId` (string de 24 hex) |

---

## 1. Crear el proyecto

```bash
dotnet new webapi -n PracticoMongo
cd PracticoMongo
```

---

## 2. Instalar paquetes

```bash
dotnet add package MongoDB.Driver
```

Solo necesitás este. A diferencia de EF Core, no hay paquete de diseño ni de migraciones porque MongoDB no las usa.

---

## 3. Estructura de carpetas

```
PracticoMongo/
├── Models/              ← una clase C# por colección (documento)
├── Services/            ← lógica de acceso a MongoDB (reemplaza al DbContext)
├── Seeds/               ← DataSeeder.cs + JSONs con datos iniciales
├── Settings/            ← MongoDbSettings.cs (configuración de conexión)
├── Controllers/         ← endpoints HTTP
├── Program.cs
├── appsettings.json
├── appsettings.Development.json   ← NO subir al repo
├── Dockerfile
├── docker-compose.yml
├── .env.mongo                     ← NO subir al repo
├── .env.example.mongo             ← SÍ subir al repo (campos vacíos)
└── .gitignore
```

---

## 4. Configuración de conexión

### Settings/MongoDbSettings.cs

Clase que representa la sección de configuración en `appsettings.json`.

```csharp
namespace PracticoMongo.Settings;

public class MongoDbSettings
{
    public string ConnectionString { get; set; } = null!;
    public string DatabaseName     { get; set; } = null!;
}
```

### appsettings.json

```json
{
  "MongoDbSettings": {
    "ConnectionString": "mongodb://localhost:27017",
    "DatabaseName": "practico_mongo"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*"
}
```

> En Docker los valores se sobreescriben con variables de entorno (ver sección 8).

---

## 5. Modelos (documentos)

En MongoDB cada documento vive en una **colección**. Los atributos del driver le dicen al serializador cómo mapear las propiedades C# a BSON.

### Atributos más usados

| Atributo | Para qué sirve |
|---|---|
| `[BsonId]` | Marca la propiedad como `_id` del documento |
| `[BsonRepresentation(BsonType.ObjectId)]` | Permite usar `string` en C# pero guardarlo como `ObjectId` en Mongo |
| `[BsonElement("nombre_en_mongo")]` | Renombra el campo en la BD (útil para usar camelCase en Mongo y PascalCase en C#) |
| `[BsonIgnoreIfNull]` | No guarda el campo si es null (evita campos vacíos en el documento) |

### Ejemplo: entidad simple

```csharp
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace PracticoMongo.Models;

public class PuntoDeVenta
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    [BsonElement("nombre")]
    public string Nombre { get; set; } = null!;
}
```

### Ejemplo: entidad con referencia a otro documento

En MongoDB no hay FK automáticas. Hay dos estrategias:

**Opción A — Embebido** (el subdocumento vive dentro del documento padre):
Usalo cuando los datos del hijo no se consultan solos y no se comparten entre documentos.

```csharp
public class Mostrador
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    [BsonElement("nombre")]
    public string Nombre { get; set; } = null!;

    // El subdocumento se guarda adentro del Mostrador
    [BsonElement("puntoDeVenta")]
    public PuntoDeVenta PuntoDeVenta { get; set; } = null!;
}
```

**Opción B — Referencia por Id** (se guarda solo el Id del padre):
Usalo cuando el padre se consulta/modifica por separado o se referencia desde muchos documentos.

```csharp
public class Mostrador
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    [BsonElement("nombre")]
    public string Nombre { get; set; } = null!;

    // Solo guardamos el Id del PuntoDeVenta
    [BsonRepresentation(BsonType.ObjectId)]
    [BsonElement("puntoDeVentaId")]
    public string PuntoDeVentaId { get; set; } = null!;
}
```

> **¿Cuál usar?** Si siempre mostrás el mostrador con su punto de venta → embebido.
> Si el punto de venta cambia seguido o lo consultás solo → referencia.

---

## 6. Services (reemplaza al DbContext)

En lugar de un `AppDbContext`, acá cada entidad tiene su propio **Service** que encapsula el acceso a su colección. Es el equivalente al repositorio en otros patrones.

### Services/PuntoDeVentaService.cs

```csharp
using MongoDB.Driver;
using PracticoMongo.Models;
using PracticoMongo.Settings;
using Microsoft.Extensions.Options;

namespace PracticoMongo.Services;

public class PuntoDeVentaService
{
    private readonly IMongoCollection<PuntoDeVenta> _collection;

    public PuntoDeVentaService(IMongoClient client, IOptions<MongoDbSettings> settings)
    {
        var db = client.GetDatabase(settings.Value.DatabaseName);
        _collection = db.GetCollection<PuntoDeVenta>("puntosDeVenta");
    }

    public async Task<List<PuntoDeVenta>> GetAllAsync() =>
        await _collection.Find(_ => true).ToListAsync();

    public async Task<PuntoDeVenta?> GetByIdAsync(string id) =>
        await _collection.Find(p => p.Id == id).FirstOrDefaultAsync();

    public async Task CreateAsync(PuntoDeVenta puntoDeVenta) =>
        await _collection.InsertOneAsync(puntoDeVenta);

    public async Task UpdateAsync(string id, PuntoDeVenta updated) =>
        await _collection.ReplaceOneAsync(p => p.Id == id, updated);

    public async Task DeleteAsync(string id) =>
        await _collection.DeleteOneAsync(p => p.Id == id);
}
```

> **¿Por qué `IMongoClient` y no `IMongoDatabase` directo?**
> `IMongoClient` se registra como singleton en `Program.cs` y es thread-safe.
> Desde él obtenés la base de datos, y desde la base de datos la colección.

---

## 7. Program.cs

```csharp
using MongoDB.Driver;
using PracticoMongo.Services;
using PracticoMongo.Settings;
using PracticoMongo.Seeds;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddControllers();

// Leer configuración de MongoDB
// En Docker, las variables de entorno sobreescriben appsettings.json automáticamente
builder.Services.Configure<MongoDbSettings>(
    builder.Configuration.GetSection("MongoDbSettings"));

// Registrar el cliente de MongoDB como singleton (una sola conexión compartida)
builder.Services.AddSingleton<IMongoClient>(sp =>
{
    var settings = sp.GetRequiredService<IOptions<MongoDbSettings>>().Value;
    // También se puede leer directo de env vars si preferís:
    // var connStr = Environment.GetEnvironmentVariable("MONGO_CONNECTION_STRING") ?? settings.ConnectionString;
    return new MongoClient(settings.ConnectionString);
});

// Registrar los services (uno por colección)
builder.Services.AddSingleton<PuntoDeVentaService>();
builder.Services.AddSingleton<MostradorService>();
// ... resto de services

var app = builder.Build();

if (app.Environment.IsDevelopment())
    app.MapOpenApi();

app.UseHttpsRedirection();
app.MapControllers();

// Seeds: insertar datos iniciales si las colecciones están vacías
using (var scope = app.Services.CreateScope())
{
    await DataSeeder.SeedAsync(scope.ServiceProvider);
}

await app.RunAsync();
```

> **¿Por qué `AddSingleton` y no `AddScoped`?**
> `IMongoClient` es thread-safe y está diseñado para vivir toda la vida de la app.
> Con EF Core se usaba `AddScoped` porque el `DbContext` no es thread-safe.

---

## 8. Controllers

```csharp
using Microsoft.AspNetCore.Mvc;
using PracticoMongo.Models;
using PracticoMongo.Services;

namespace PracticoMongo.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PuntosDeVentaController : ControllerBase
{
    private readonly PuntoDeVentaService _service;

    public PuntosDeVentaController(PuntoDeVentaService service) =>
        _service = service;

    [HttpGet]
    public async Task<ActionResult<List<PuntoDeVenta>>> GetAll() =>
        Ok(await _service.GetAllAsync());

    [HttpGet("{id}")]
    public async Task<ActionResult<PuntoDeVenta>> GetById(string id)
    {
        var item = await _service.GetByIdAsync(id);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpPost]
    public async Task<IActionResult> Create(PuntoDeVenta puntoDeVenta)
    {
        await _service.CreateAsync(puntoDeVenta);
        return CreatedAtAction(nameof(GetById), new { id = puntoDeVenta.Id }, puntoDeVenta);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, PuntoDeVenta updated)
    {
        var existing = await _service.GetByIdAsync(id);
        if (existing is null) return NotFound();
        await _service.UpdateAsync(id, updated);
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        var existing = await _service.GetByIdAsync(id);
        if (existing is null) return NotFound();
        await _service.DeleteAsync(id);
        return NoContent();
    }
}
```

---

## 9. Seeds

A diferencia de EF Core, no hay migraciones que creen las colecciones: MongoDB las crea automáticamente al insertar el primer documento.

### Seeds/DataSeeder.cs

```csharp
using MongoDB.Driver;
using PracticoMongo.Models;
using PracticoMongo.Services;

namespace PracticoMongo.Seeds;

public class DataSeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        var puntosDeVentaService = services.GetRequiredService<PuntoDeVentaService>();
        var mostradorService     = services.GetRequiredService<MostradorService>();

        // Solo insertar si la colección está vacía
        var puntosExistentes = await puntosDeVentaService.GetAllAsync();
        if (!puntosExistentes.Any())
        {
            var sucursalCentro = new PuntoDeVenta { Nombre = "Sucursal Centro" };
            var sucursalNorte  = new PuntoDeVenta { Nombre = "Sucursal Norte" };

            await puntosDeVentaService.CreateAsync(sucursalCentro);
            await puntosDeVentaService.CreateAsync(sucursalNorte);

            // Después de crear, ya tienen Id asignado por MongoDB
            await mostradorService.CreateAsync(new Mostrador
            {
                Nombre          = "Mostrador A",
                PuntoDeVentaId  = sucursalCentro.Id!
            });
            await mostradorService.CreateAsync(new Mostrador
            {
                Nombre          = "Mostrador B",
                PuntoDeVentaId  = sucursalCentro.Id!
            });
        }
    }
}
```

> **Diferencia con EF Core:** no hay JSONs intermedios con `RecetaIndex` ni resolución
> de FKs por nombre. Directamente usás el `Id` que MongoDB asigna al insertar
> (`InsertOneAsync` lo llena en el objeto C# automáticamente).

---

## 10. Variables de entorno

Con MongoDB el connection string incluye usuario, contraseña y host todo junto.

### .env.mongo — NO subir al repo

```env
MONGO_INITDB_ROOT_USERNAME=admin
MONGO_INITDB_ROOT_PASSWORD=admin123
MONGO_INITDB_DATABASE=practico_mongo
# Para la app (sobreescribe appsettings.json)
MongoDbSettings__ConnectionString=mongodb://admin:admin123@db:27017
MongoDbSettings__DatabaseName=practico_mongo
# Para Mongo Express
ME_CONFIG_MONGODB_ADMINUSERNAME=admin
ME_CONFIG_MONGODB_ADMINPASSWORD=admin123
ME_CONFIG_MONGODB_URL=mongodb://admin:admin123@db:27017/
ME_CONFIG_BASICAUTH=false
```

### .env.example.mongo — SÍ subir al repo

```env
MONGO_INITDB_ROOT_USERNAME=
MONGO_INITDB_ROOT_PASSWORD=
MONGO_INITDB_DATABASE=
MongoDbSettings__ConnectionString=mongodb://<user>:<password>@db:27017
MongoDbSettings__DatabaseName=
ME_CONFIG_MONGODB_ADMINUSERNAME=
ME_CONFIG_MONGODB_ADMINPASSWORD=
ME_CONFIG_MONGODB_URL=mongodb://<user>:<password>@db:27017/
ME_CONFIG_BASICAUTH=false
```

> **¿Cómo sobreescribe el env var a `appsettings.json`?**
> ASP.NET Core convierte automáticamente `MongoDbSettings__ConnectionString`
> (doble guión bajo) en la clave anidada `MongoDbSettings:ConnectionString`.
> No hace falta tocar nada en el código.

---

## 11. Dockerfile

```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY PracticoMongo.csproj .
RUN dotnet restore
COPY . .
RUN dotnet publish -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "PracticoMongo.dll"]
```

---

## 12. docker-compose.yml

```yaml
services:
  db:
    image: mongo:7
    env_file:
      - .env.mongo
    volumes:
      - mongo-data:/data/db
    healthcheck:
      test: ["CMD", "mongosh", "--eval", "db.adminCommand('ping')"]
      interval: 10s
      timeout: 5s
      retries: 5
    networks:
      - net

  mongo-express:
    image: mongo-express:latest
    env_file:
      - .env.mongo
    ports:
      - "8081:8081"
    depends_on:
      db:
        condition: service_healthy
    networks:
      - net

  backend:
    build: ./
    env_file:
      - .env.mongo
    environment:
      - ASPNETCORE_ENVIRONMENT=Development
    ports:
      - "8000:8080"
    depends_on:
      db:
        condition: service_healthy
    networks:
      - net

networks:
  net:

volumes:
  mongo-data:
```

> **¿Por qué `mongo:7` y no `mongo:latest`?**
> `latest` puede cambiar entre builds y romper cosas. Mejor fijar la versión mayor.

---

## 13. .gitignore

```
*.env
!.env.example.mongo
bin/
obj/
appsettings.Development.json
```

---

## 14. Levantar el proyecto

```bash
# Primera vez o después de cambios en código
docker compose up --build

# Sin rebuild
docker compose up -d

# Bajar y borrar los datos (para empezar de cero)
docker compose down -v
```

Una vez levantado:

| URL | Qué es |
|---|---|
| `http://localhost:8000/openapi/v1.json` | Swagger / OpenAPI |
| `http://localhost:8081` | Mongo Express (panel visual) |

---

## 15. Conectarse a MongoDB manualmente

```bash
# Entrar al contenedor de MongoDB con mongosh
docker exec -it practicomongo-db-1 mongosh -u admin -p admin123

# Ver bases de datos
show dbs

# Usar la BD del proyecto
use practico_mongo

# Ver colecciones
show collections

# Consultar documentos
db.puntosDeVenta.find().pretty()
db.mostradores.find({ puntoDeVentaId: "<id>" })
```

---

## 16. Problemas frecuentes

### Mongo Express no conecta / queda en restart loop
La imagen de Mongo Express a veces arranca antes de que MongoDB esté listo aunque `depends_on` diga lo contrario. Si pasa, esperá unos segundos y hacé:
```bash
docker compose restart mongo-express
```

### El backend no conecta a MongoDB
Verificá que el connection string en `.env.mongo` use `db` como host (el nombre del servicio en Docker), no `localhost`.
```
# BIEN (dentro de Docker)
MongoDbSettings__ConnectionString=mongodb://admin:admin123@db:27017

# MAL (solo funciona desde la máquina host)
MongoDbSettings__ConnectionString=mongodb://admin:admin123@localhost:27017
```

### El Id llega null en el POST
Si mandás un documento sin `Id`, MongoDB lo genera solo. Pero si el modelo tiene `[BsonId]` y `[BsonRepresentation(BsonType.ObjectId)]`, el driver lo rellena en el objeto C# **después** del `InsertOneAsync`. Por eso en los seeds podés usar `sucursalCentro.Id` luego de insertarlo.

### `mongosh` no encontrado en el healthcheck
Usá la imagen `mongo:7` o superior. Las versiones viejas usan `mongo` en lugar de `mongosh`. En ese caso cambiá el healthcheck a:
```yaml
test: ["CMD", "mongo", "--eval", "db.adminCommand('ping')"]
```

---

## Resumen del flujo completo

```
1. Crear proyecto         →  dotnet new webapi
2. Instalar paquete       →  dotnet add package MongoDB.Driver
3. Settings/              →  MongoDbSettings.cs
4. Models/                →  una clase por colección, con [BsonId] etc.
5. Services/              →  uno por colección, accede a IMongoCollection<T>
6. Controllers/           →  uno por entidad, delega todo al Service
7. Program.cs             →  Configure<MongoDbSettings> + AddSingleton<IMongoClient>
8. Seeds/                 →  DataSeeder.cs, usa los services directamente
9. .env.mongo             →  credenciales (no subir)
10. Dockerfile            →  multi-stage build
11. docker-compose.yml    →  servicios db + mongo-express + backend
12. Levantar              →  docker compose up --build
13. Panel                 →  http://localhost:8081
```