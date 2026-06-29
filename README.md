## PracticoOrmMongo — Guía Completa: ASP.NET Core + MongoDB Driver + Docker

> Mismo objetivo que un proyecto con EF Core + PostgreSQL (modelos, servicios, seeds, Docker), pero
> hablando con una base NoSQL orientada a documentos en vez de una relacional. Cada sección incluye
> una comparación con el proyecto equivalente hecho con EF Core + PostgreSQL.

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
| Migraciones | `dotnet ef migrations add` + `Database.Migrate()` | No existen (esquema flexible) |
| Acceso a datos | `AppDbContext` (un `DbSet<T>` por tabla) | Un `Service` por colección, cada uno con su `IMongoCollection<T>` |
| Ciclo de vida del contexto | `AddDbContext` -> `Scoped` (no es thread-safe) | `AddSingleton<IMongoClient>` (sí es thread-safe) |
| Panel admin | CoreAdmin (paquete NuGet, requiere `AddCoreAdmin()`) | Mongo Express (contenedor aparte en Docker) |
| Id | `int` autoincremental, generado por la BD | `ObjectId` (string de 24 caracteres hex), generado por el driver |
| Datos iniciales | JSONs en `Seeds/` + resolución de relaciones por nombre/índice | JSONs en `Seeds/` + resolución de relaciones por nombre/índice desde listas en memoria |
| Convención de nombres | `EFCore.NamingConventions` (snake_case en la BD) | `[BsonElement("nombre")]` manual por campo |

---

## 1. Crear el proyecto

```bash
dotnet new webapi -n PracticoOrmMongo
cd PracticoOrmMongo
```

> **Comparación:** en el proyecto con EF Core el comando de arranque es idéntico
> (`dotnet new webapi -n PracticoOrm`). La diferencia entre ambos proyectos no está en cómo
> se crea el proyecto, sino en qué se instala y cómo se conecta a la base después.

---

## 2. Instalar paquetes

```bash
dotnet add package MongoDB.Driver
```

Solo se necesita este paquete.

> **Comparación:** el proyecto con EF Core necesitaba 4 paquetes: `Microsoft.EntityFrameworkCore`, `Microsoft.EntityFrameworkCore.Design` (para poder generar migraciones), `Npgsql.EntityFrameworkCore.PostgreSQL` (el driver específico de Postgres) y `EFCore.NamingConventions` (para snake_case). Con Mongo no hay paquete de diseño porque no hay migraciones que generar, y no hace falta un paquete de convención de nombres porque el nombre de cada campo se define a mano con `[BsonElement]`.

---

## 3. Estructura de carpetas

```
PracticoOrmMongo/
├── Models/              ← una clase C# por colección (documento)
├── Services/            ← lógica de acceso a MongoDB (reemplaza al DbContext)
├── Seeds/               ← DataSeeder.cs + un JSON por entidad
│   ├── DataSeeder.cs
│   ├── puntosDeVenta.json
│   ├── ingredientes.json
│   ├── tipoProductos.json
│   ├── recetas.json
│   ├── mostradores.json
│   ├── detallesReceta.json
│   ├── productos.json
│   ├── ventas.json
│   └── detallesVenta.json
├── Settings/            ← MongoDbSettings.cs (configuración de conexión)
├── Controllers/         ← endpoints HTTP
├── Program.cs
├── appsettings.json
├── appsettings.Development.json   ← NO subir al repo
├── Dockerfile
├── docker-compose.yml
├── .env.mongo                     ← NO subir al repo
├── .env.example.mongo             ← SI subir al repo (campos vacíos)
└── .gitignore
```

> **Comparación:** la estructura es prácticamente igual a la del proyecto con EF Core, con dos diferencias: no hay carpeta `Migrations/` (Mongo no migra esquemas).

---

## 4. Configuración de conexión

### Settings/MongoDbSettings.cs

Clase que representa la sección de configuración en `appsettings.json`.

```csharp
namespace PracticoOrmMongo.Settings;

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

> En Docker los valores se sobrescriben con variables de entorno (ver sección 10).

> **Comparación:** el proyecto con EF Core no tenía una clase `Settings` ni una sección en `appsettings.json`. La connection string se armaba a mano en `Program.cs`, leyendo variables de entorno sueltas (`POSTGRES_HOST`, `POSTGRES_PORT`, `POSTGRES_DB`, etc.) con valores por defecto en código (`?? "localhost"`). El enfoque con `MongoDbSettings` + `appsettings.json` es más prolijo porque ASP.NET Core sabe mapear variables de entorno a la configuración sin necesidad de escribir ese mapeo a mano (ver sección 10).

---

## 5. Modelos (documentos)

En MongoDB cada documento vive en una **colección**. Los atributos del driver le dicen al
serializador cómo mapear las propiedades C# a BSON.

### Atributos más usados

| Atributo | Para qué sirve |
|---|---|
| `[BsonId]` | Marca la propiedad como `_id` del documento |
| `[BsonRepresentation(BsonType.ObjectId)]` | Permite usar `string` en C# pero guardarlo como `ObjectId` en Mongo |
| `[BsonElement("nombre_en_mongo")]` | Renombra el campo en la BD (útil para usar camelCase en Mongo y PascalCase en C#) |
| `[BsonIgnoreIfNull]` | No guarda el campo si es null (evita campos vacíos en el documento) |

> **Comparación: tipos de columna:** en EF Core/Postgres se usaban atributos como `[Column(TypeName = "decimal(10,2)")]` o `[MaxLength(100)]` porque la base de datos tiene tipos estrictos y hay que indicarle exactamente cómo guardar cada campo. En MongoDB no existe eso: el tipo lo define directamente el tipo de C# (`decimal` -> `Decimal128`, `int` -> `Int32`, `string` -> `String`, etc.) y el driver hace la conversión de forma automática. Los únicos atributos necesarios son los de la tabla de arriba.

> **Comparación — el Id:** en EF Core el Id es `int` con `[DatabaseGenerated(DatabaseGeneratedOption.Identity)]`, que le indica a Postgres que autoincremente la columna. En Mongo el Id debe ser `string?` (nunca `int`) con `[BsonId]` + `[BsonRepresentation(BsonType.ObjectId)]`, porque el `ObjectId` nativo de Mongo es un string hexadecimal de 24 caracteres, no un número.

### Ejemplo: entidad simple

```csharp
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace PracticoOrmMongo.Models;

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
Se recomienda cuando los datos del hijo no se consultan de forma independiente y no se comparten entre documentos.

```csharp
public class Mostrador
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    [BsonElement("nombre")]
    public string Nombre { get; set; } = null!;

    [BsonElement("puntoDeVenta")]
    public PuntoDeVenta PuntoDeVenta { get; set; } = null!;
}
```

**Opción B — Referencia por Id** (se guarda solo el Id del padre):
Se recomienda cuando el padre se consulta o modifica por separado, o se referencia desde muchos documentos.

```csharp
public class Mostrador
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    [BsonElement("nombre")]
    public string Nombre { get; set; } = null!;

    [BsonRepresentation(BsonType.ObjectId)]
    [BsonElement("puntoDeVentaId")]
    public string PuntoDeVentaId { get; set; } = null!;
}
```

> **¿Cuál usar?** Si siempre se muestra el mostrador junto a su punto de venta, la opción embebida es preferible. Si el punto de venta cambia con frecuencia o se consulta de forma independiente, conviene la referencia por Id.

> **Comparación:** en EF Core esta decisión no existe: toda relación se modela igual (FK + propiedad de navegación) y es el motor (con el `JOIN`) el que decide cómo traer los datos relacionados en cada consulta. En MongoDB la decisión se toma al diseñar el modelo, y una vez tomada queda fija en la forma del documento.

> **Cuidado con los datos embebidos:** si se embebe un objeto completo (por ejemplo, un `Ingrediente` adentro de un `DetalleReceta`), se guarda una **copia congelada** de ese dato en el momento de la inserción. Si después se actualiza el `Ingrediente` original (cambia su costo, por ejemplo), esa copia embebida **no se actualiza sola**.

---

## 6. Services (reemplaza al DbContext)

En lugar de un `AppDbContext`, cada entidad tiene su propio **Service** que encapsula el
acceso a su colección. Es el equivalente al repositorio en otros patrones.

### Services/PuntoDeVentaService.cs

```csharp
using MongoDB.Driver;
using PracticoOrmMongo.Models;
using PracticoOrmMongo.Settings;
using Microsoft.Extensions.Options;

namespace PracticoOrmMongo.Services;

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

Para una entidad que tiene una lista embebida (como `Receta` con sus `DetalleReceta`), el
service necesita además métodos para empujar o reemplazar esa lista sin tocar el resto del
documento:

```csharp
// Agregar un detalle sin reemplazar todo el documento
public async Task AddDetalleAsync(string recetaId, DetalleReceta detalle)
{
    var update = Builders<Receta>.Update.Push(r => r.Detalles, detalle);
    await _collection.UpdateOneAsync(r => r.Id == recetaId, update);
}

// Reemplazar todos los detalles (útil para edición completa)
public async Task UpdateDetallesAsync(string recetaId, List<DetalleReceta> detalles)
{
    var update = Builders<Receta>.Update.Set(r => r.Detalles, detalles);
    await _collection.UpdateOneAsync(r => r.Id == recetaId, update);
}
```

> **¿Por qué `IMongoClient` y no `IMongoDatabase` directo?**
> `IMongoClient` se registra como singleton en `Program.cs` y es thread-safe. Desde él se obtiene la base de datos, y desde la base de datos la colección.

> **Comparación:** en EF Core un solo `AppDbContext` centraliza el acceso a todas las tablas (`DbSet<T>` por cada una) y las relaciones se navegan con LINQ (`.Include()`, propiedades de navegación). Aquí cada Service es independiente y solo conoce su propia colección. Para modificar una lista embebida (como los detalles de una receta) no alcanza con reemplazar una propiedad en memoria como ocurre con una `ICollection<T>` de EF Core: hay que usar `Builders<T>.Update.Push` o `.Set`, porque se está modificando un campo dentro de un documento ya guardado en la base.

---

## 7. Program.cs

```csharp
using MongoDB.Driver;
using PracticoOrmMongo.Services;
using PracticoOrmMongo.Settings;
using PracticoOrmMongo.Seeds;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddControllers();

// Leer configuración de MongoDB
// En Docker, las variables de entorno sobrescriben appsettings.json automáticamente
builder.Services.Configure<MongoDbSettings>(
    builder.Configuration.GetSection("MongoDbSettings"));

// Registrar el cliente de MongoDB como singleton (una sola conexión compartida)
builder.Services.AddSingleton<IMongoClient>(sp =>
{
    var settings = sp.GetRequiredService<IOptions<MongoDbSettings>>().Value;
    return new MongoClient(settings.ConnectionString);
});

// Registrar los services, uno por colección.
// IMPORTANTE: cada línea registra el SERVICE (termina en "Service"), nunca el Model directo.
builder.Services.AddSingleton<IngredienteService>();
builder.Services.AddSingleton<MostradorService>();
builder.Services.AddSingleton<ProductoService>();
builder.Services.AddSingleton<PuntoDeVentaService>();
builder.Services.AddSingleton<RecetaService>();
builder.Services.AddSingleton<TipoProductoService>();
builder.Services.AddSingleton<VentaService>();

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
> `IMongoClient` es thread-safe y está diseñado para vivir toda la vida de la app. Con EF Core se usaba `AddScoped` porque el `DbContext` no es thread-safe.

> **Comparación:** en EF Core, `Program.cs` registra un solo `AppDbContext` con `AddDbContext<AppDbContext>(...)` (vida `Scoped`, una instancia por request) y aplica las migraciones pendientes con `dbContext.Database.Migrate()` antes de llamar al seeder. En Mongo no hay nada que migrar (las colecciones se crean solas al insertar), por eso ese paso simplemente no existe, y en su lugar se registra un Service `Singleton` por cada colección.

---

## 8. Controllers

Cada Controller es básicamente CRUD estándar que delega todo al Service correspondiente. Por
ejemplo, para una entidad simple sin relaciones:

```csharp
using Microsoft.AspNetCore.Mvc;
using PracticoOrmMongo.Models;
using PracticoOrmMongo.Services;

namespace PracticoOrmMongo.Controllers;

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

Para una entidad que tiene una lista embebida (como `Receta` o `Venta`), se agregan endpoints extra
para manejar esa lista sin tocar el resto del documento:

```csharp
[HttpPost("{id}/detalles")]
public async Task<IActionResult> AddDetalle(string id, DetalleReceta detalle)
{
    var existing = await _service.GetByIdAsync(id);
    if (existing is null) return NotFound();
    await _service.AddDetalleAsync(id, detalle);
    return NoContent();
}

[HttpPut("{id}/detalles")]
public async Task<IActionResult> UpdateDetalles(string id, List<DetalleReceta> detalles)
{
    var existing = await _service.GetByIdAsync(id);
    if (existing is null) return NotFound();
    await _service.UpdateDetallesAsync(id, detalles);
    return NoContent();
}
```

---

## 9. Seeds

Al igual que en EF Core, el seeder lee archivos JSON (uno por entidad) con `JsonSerializer.Deserialize`
y resuelve las relaciones entre entidades por nombre o por índice. La diferencia respecto a EF Core
es que la resolución se realiza sobre listas en memoria de los objetos ya insertados, en lugar de
consultar el contexto de base de datos. MongoDB crea las colecciones automáticamente al insertar el
primer documento, por lo que no se necesitan migraciones previas.

### Orden de inserción

El orden importa porque las entidades que **referencian** a otras (por Id) o las que
**embeben** a otras necesitan que la entidad referenciada/embebida ya exista en memoria:

```
1. PuntoDeVenta      (independiente)
2. Ingrediente       (independiente)
3. TipoProducto      (independiente)
4. Receta            (independiente, se inserta sin detalles)
5. Mostrador         (referencia PuntoDeVentaId → resuelve por nombre)
6. DetalleReceta     (embebido en Receta → resuelve Receta por índice e Ingrediente por nombre, agrega con Push)
7. Producto          (referencia RecetaId, embebe TipoProducto → resuelve ambos por nombre)
8. Venta             (referencia MostradorId → resuelve por nombre, se inserta sin detalles)
9. DetalleVenta      (embebido en Venta → resuelve Venta por índice y Producto por nombre, agrega con Push)
```

### Seeds/DataSeeder.cs — patrón general

```csharp
using System.Text.Json;
using MongoDB.Driver;
using PracticoOrmMongo.Models;
using PracticoOrmMongo.Services;

namespace PracticoOrmMongo.Seeds;

public class DataSeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        var puntoDeVentaService = services.GetRequiredService<PuntoDeVentaService>();
        // ... obtener el resto de los services

        // Cortar todo el seeder si ya hay datos, para no duplicar en cada reinicio
        if ((await puntoDeVentaService.GetAllAsync()).Any()) return;

        // 1. Entidades independientes: se deserializan del JSON y se insertan directamente.
        //    SeedEntidadAsync devuelve la lista con los Id ya asignados por Mongo.
        var puntosDeVenta = await SeedEntidadAsync<PuntoDeVenta>("Seeds/puntosDeVenta.json", puntoDeVentaService.CreateAsync);
        var ingredientes  = await SeedEntidadAsync<Ingrediente>("Seeds/ingredientes.json", ingredienteService.CreateAsync);
        // ... idem para TipoProducto y Receta

        // 2. Entidades con relaciones: se resuelven por nombre o por índice desde las listas en memoria
        var mostradores = await SeedMostradoresAsync(puntosDeVenta, mostradorService);
        //    → lee mostradores.json, busca el PuntoDeVenta por nombre en la lista ya cargada

        // 3. Entidades embebidas: se agregan al documento padre con Push (sin reemplazarlo)
        await SeedDetallesRecetaAsync(recetas, ingredientes, recetaService);
        //    → lee detallesReceta.json, resuelve Receta por índice e Ingrediente por nombre
    }

    // Genérico: deserializa el JSON y llama a CreateAsync por cada elemento.
    // Devuelve la lista con los Id ya asignados por Mongo.
    private static async Task<List<T>> SeedEntidadAsync<T>(string rutaArchivo, Func<T, Task> crear)
    {
        var json = await File.ReadAllTextAsync(rutaArchivo);
        var datos = JsonSerializer.Deserialize<List<T>>(json)!;
        foreach (var dato in datos)
            await crear(dato);
        return datos;
    }
}
```

> **Comparación:** en EF Core, `DataSeeder.SeedAsync` también lee un JSON por entidad (`ingredientes.json`, `recetas.json`, etc.) con `JsonSerializer.Deserialize`, y resuelve relaciones buscando el Id por nombre (`context.Recetas.First(r => r.Nombre == recetaNombre)`) o por posición (`RecetaIndex` apuntando a una lista en memoria). En Mongo el mecanismo es equivalente: la diferencia es que la resolución se hace sobre listas en memoria (los objetos devueltos por `SeedEntidadAsync`) en lugar de consultar el contexto de base de datos, ya que `IMongoCollection<T>` no es un `DbSet<T>` navegable. En ambos proyectos los datos de seed viven en archivos JSON separados del código C#.

---

## 10. Variables de entorno

Con MongoDB el connection string incluye usuario, contraseña y host todo junto.

### .env.mongo — NO subir al repo

```env
MONGO_INITDB_ROOT_USERNAME=admin
MONGO_INITDB_ROOT_PASSWORD=admin123
MONGO_INITDB_DATABASE=practico_mongo
# Para la app (sobrescribe appsettings.json)
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

> **¿Cómo sobrescribe el env var a `appsettings.json`?**
> ASP.NET Core convierte automáticamente `MongoDbSettings__ConnectionString` (doble guión bajo) en la clave anidada `MongoDbSettings:ConnectionString`. No es necesario modificar el código.

> **Comparación:** el proyecto con EF Core usa variables sueltas (`POSTGRES_HOST`, `POSTGRES_PORT`, `POSTGRES_DB`, `POSTGRES_USER`, `POSTGRES_PASSWORD`) que se leen una por una en `Program.cs` con `Environment.GetEnvironmentVariable(...)` y se concatenan a mano en un connection string. El truco del doble guión bajo (`MongoDbSettings__ConnectionString`) evita ese paso manual: ASP.NET Core mapea la variable directo a la sección de configuración ya leída con `Configure<MongoDbSettings>` en `Program.cs`, sin necesidad de ningún `GetEnvironmentVariable` adicional.

---

## 11. Dockerfile

```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY PracticoOrmMongo.csproj .
RUN dotnet restore
COPY . .
RUN dotnet publish -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "PracticoOrmMongo.dll"]
```

> **Comparación:** la estructura multi-stage es idéntica a la del proyecto con EF Core (build con el SDK completo, runtime con la imagen liviana de ASP.NET). La única diferencia es que el proyecto con Postgres agrega una línea extra, `ENV POSTGRES_DISABLE_GSS=true`, para evitar un problema conocido de autenticación de Npgsql en contenedores Alpine. Mongo no necesita ese ajuste.

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
> `latest` puede cambiar entre builds y romper cosas. Es preferible fijar la versión mayor.

> **Comparación:** la única diferencia estructural con el `docker-compose.yml` del proyecto con Postgres es que Mongo suma un tercer servicio, `mongo-express` (el panel admin), porque corre en un contenedor aparte. En el proyecto con EF Core, el panel (`CoreAdmin`) es un paquete NuGet que corre dentro del mismo contenedor `backend`, por lo que no requiere un servicio de Docker adicional. El healthcheck también cambia de herramienta: `pg_isready` en Postgres, `mongosh --eval ...` en Mongo.

---

## 13. .gitignore

```
.env.mongo
!.env.example.mongo
bin/
obj/
appsettings.Development.json
```

> **Comparación:** el proyecto con EF Core usa exactamente este mismo patrón (nombre exacto del archivo, `.env.db`, más la negación `!.env.example.db`), por lo que conviene replicar esa misma lógica en lugar de generalizar con `*.env`.

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

> **Comparación:** los comandos de Docker son idénticos en ambos proyectos. Lo único que cambia es qué URL de panel admin se abre: en el proyecto con EF Core el panel de CoreAdmin vive en una ruta servida por el propio backend (no en un puerto separado), mientras que Mongo Express tiene su propio puerto (`8081`) porque es otro contenedor.

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

> **Comparación:** el equivalente en el proyecto con Postgres es entrar con `psql` (`docker exec -it <contenedor>-db-1 psql -U postgres -d fabrica_pastas`) y usar SQL (`SELECT * FROM mostradores WHERE punto_de_venta_id = '<id>';`). La lógica es la misma: acceder al contenedor de la base y consultar directamente, pero el lenguaje de consulta cambia completamente: SQL vs. la sintaxis de queries de Mongo (`find`, filtros como objetos JSON).

---

## 16. Problemas frecuentes

### Mongo Express no conecta / queda en restart loop
La imagen de Mongo Express a veces arranca antes de que MongoDB esté listo aunque `depends_on`
diga lo contrario. Si esto ocurre, esperar unos segundos y ejecutar:
```bash
docker compose restart mongo-express
```

### El backend no conecta a MongoDB
Verificar que el connection string en `.env.mongo` use `db` como host (el nombre del servicio en
Docker), no `localhost`.
```
# BIEN (dentro de Docker)
MongoDbSettings__ConnectionString=mongodb://admin:admin123@db:27017

# MAL (solo funciona desde la máquina host)
MongoDbSettings__ConnectionString=mongodb://admin:admin123@localhost:27017
```

### El Id llega null en el POST
Si se envía un documento sin `Id`, MongoDB lo genera solo. Cuando el modelo tiene `[BsonId]` y
`[BsonRepresentation(BsonType.ObjectId)]`, el driver rellena el Id en el objeto C# **después** del
`InsertOneAsync`. Por eso en los seeds se puede usar `sucursalCentro.Id` luego de insertarlo, nunca
antes.

### `mongosh` no encontrado en el healthcheck
Usar la imagen `mongo:7` o superior. Las versiones anteriores usan `mongo` en lugar de `mongosh`. En
ese caso cambiar el healthcheck a:
```yaml
test: ["CMD", "mongo", "--eval", "db.adminCommand('ping')"]
```

### Falla la inyección de dependencias al arrancar (`Unable to resolve service for type ...`)
Significa que un Controller solicita un Service en su constructor que nunca se registró en
`Program.cs`, o que se registró el tipo equivocado (por ejemplo, el Model en vez del Service —
ver el aviso de la sección 7). Revisar que la lista de `AddSingleton<...Service>()` tenga
exactamente un renglón por cada Service que algún Controller utilice.

### El build de Docker falla con "file not found" en el `.csproj`
El nombre del archivo en `COPY <nombre>.csproj .` dentro del `Dockerfile` no coincide con el
nombre real del `.csproj`. Ejecutar `ls *.csproj` en la raíz del proyecto y copiar el nombre exacto
(con mayúsculas/minúsculas iguales) tanto en esa línea como en el `ENTRYPOINT` (sección 11).

> **Comparación:** los dos últimos problemas (DI y nombre de `.csproj`/`.dll`) no son específicos de Mongo — son errores típicos de cualquier proyecto ASP.NET Core en Docker, incluido el de EF Core. Conviene revisarlos primero cuando algo no levanta, antes de sospechar de la base de datos en sí.

---

## Resumen del flujo completo

```
1. Crear proyecto         ->  dotnet new webapi
2. Instalar paquete       ->  dotnet add package MongoDB.Driver
3. Settings/              ->  MongoDbSettings.cs
4. Models/                ->  una clase por colección, con [BsonId] etc. y namespace correcto
5. Services/              ->  uno por colección, accede a IMongoCollection<T>
6. Controllers/           ->  uno por entidad, delega todo al Service
7. Program.cs             ->  Configure<MongoDbSettings> + AddSingleton<IMongoClient> + un AddSingleton<XService> por cada Service
8. Seeds/                 ->  DataSeeder.cs + un JSON por entidad, resuelve relaciones por nombre/índice desde listas en memoria
9. .env.mongo             ->  credenciales (nombre exacto en .gitignore, no *.env)
10. Dockerfile            ->  multi-stage build, nombre de .csproj/.dll exacto
11. docker-compose.yml    ->  servicios db + mongo-express + backend
12. Levantar              ->  docker compose up --build
13. Panel                 ->  http://localhost:8081
```
