using MongoDB.Driver;
using PracticoOrmMongo.Services;
using PracticoOrmMongo.Settings;
using PracticoOrmMongo.Seeds;
using Microsoft.Extensions.Options;
using PracticoOrmMongo.Models;

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
builder.Services.AddSingleton<IngredienteService>();
builder.Services.AddSingleton<MostradorService>();
builder.Services.AddSingleton<ProductoService>();
builder.Services.AddSingleton<PuntoDeVentaService>();
builder.Services.AddSingleton<RecetaService>();
builder.Services.AddSingleton<TipoProducto>();
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