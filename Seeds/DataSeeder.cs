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
        var mostradorService    = services.GetRequiredService<MostradorService>();
        var ingredienteService  = services.GetRequiredService<IngredienteService>();
        var tipoProductoService = services.GetRequiredService<TipoProductoService>();
        var recetaService       = services.GetRequiredService<RecetaService>();
        var productoService     = services.GetRequiredService<ProductoService>();
        var ventaService        = services.GetRequiredService<VentaService>();

        // Si ya hay datos, no volver a insertar (evita duplicar en cada restart)
        if ((await puntoDeVentaService.GetAllAsync()).Any()) return;

        // 1-4: entidades simples, mapean 1 a 1 con el JSON -> deserialización genérica
        var puntosDeVenta = await SeedEntidadAsync<PuntoDeVenta>("Seeds/puntosDeVenta.json", puntoDeVentaService.CreateAsync);
        var ingredientes   = await SeedEntidadAsync<Ingrediente>("Seeds/ingredientes.json", ingredienteService.CreateAsync);
        var tiposProducto  = await SeedEntidadAsync<TipoProducto>("Seeds/tipoProductos.json", tipoProductoService.CreateAsync);
        var recetas        = await SeedEntidadAsync<Receta>("Seeds/recetas.json", recetaService.CreateAsync);

        // 5+: entidades que resuelven una relación por nombre/índice -> método a medida
        var mostradores = await SeedMostradoresAsync(puntosDeVenta, mostradorService);
        await SeedDetallesRecetaAsync(recetas, ingredientes, recetaService);
        var productos = await SeedProductosAsync(recetas, tiposProducto, productoService);
        var ventas    = await SeedVentasAsync(mostradores, ventaService);
        await SeedDetallesVentaAsync(ventas, productos, ventaService);
    }

    // ------------------------------------------------------------------
    // Genérico: usalo cuando el JSON tiene exactamente las mismas
    // propiedades que el modelo (sin relaciones que resolver).
    // Devuelve la lista ya con los Id asignados por Mongo, para que los
    // métodos siguientes puedan usarla para resolver relaciones por nombre.
    // ------------------------------------------------------------------
    private static async Task<List<T>> SeedEntidadAsync<T>(string rutaArchivo, Func<T, Task> crear)
    {
        var json = await File.ReadAllTextAsync(rutaArchivo);
        var datos = JsonSerializer.Deserialize<List<T>>(json)!;

        foreach (var dato in datos)
            await crear(dato);

        return datos;
    }

    // ------------------------------------------------------------------
    // Mostrador: referencia PuntoDeVentaId -> resolver por "PuntoDeVentaNombre" del JSON
    // ------------------------------------------------------------------
    private static async Task<List<Mostrador>> SeedMostradoresAsync(
        List<PuntoDeVenta> puntosDeVenta, MostradorService service)
    {
        var json = await File.ReadAllTextAsync("Seeds/mostradores.json");
        var datos = JsonSerializer.Deserialize<List<JsonElement>>(json)!;
        var mostradores = new List<Mostrador>();

        foreach (var dato in datos)
        {
            var puntoDeVentaNombre = dato.GetProperty("PuntoDeVentaNombre").GetString();
            var puntoDeVenta = puntosDeVenta.First(p => p.Nombre == puntoDeVentaNombre);

            var mostrador = new Mostrador
            {
                Nombre = dato.GetProperty("Nombre").GetString()!,
                PuntoDeVentaId = puntoDeVenta.Id!
            };

            await service.CreateAsync(mostrador);
            mostradores.Add(mostrador);
        }

        return mostradores;
    }

    // ------------------------------------------------------------------
    // DetalleReceta: vive embebido dentro de Receta.Detalles.
    // Resuelve la Receta por posición (RecetaIndex, igual que en el proyecto EF Core)
    // y el Ingrediente por nombre, y lo agrega con Push (no se reemplaza el documento entero).
    // ------------------------------------------------------------------
    private static async Task SeedDetallesRecetaAsync(
        List<Receta> recetas, List<Ingrediente> ingredientes, RecetaService service)
    {
        var json = await File.ReadAllTextAsync("Seeds/detallesReceta.json");
        var datos = JsonSerializer.Deserialize<List<JsonElement>>(json)!;

        foreach (var dato in datos)
        {
            var receta = recetas[dato.GetProperty("RecetaIndex").GetInt32()];

            var ingredienteNombre = dato.GetProperty("IngredienteNombre").GetString();
            var ingrediente = ingredientes.First(i => i.Nombre == ingredienteNombre);

            var detalle = new DetalleReceta
            {
                Cantidad = dato.GetProperty("Cantidad").GetDecimal(),
                Ingrediente = ingrediente
            };

            await service.AddDetalleAsync(receta.Id!, detalle);
        }
    }

    // ------------------------------------------------------------------
    // Producto: referencia RecetaId, embebe TipoProducto -> resolver ambos por nombre
    // ------------------------------------------------------------------
    private static async Task<List<Producto>> SeedProductosAsync(
        List<Receta> recetas, List<TipoProducto> tiposProducto, ProductoService service)
    {
        var json = await File.ReadAllTextAsync("Seeds/productos.json");
        var datos = JsonSerializer.Deserialize<List<JsonElement>>(json)!;
        var productos = new List<Producto>();

        foreach (var dato in datos)
        {
            var recetaNombre = dato.GetProperty("RecetaNombre").GetString();
            var receta = recetas.First(r => r.Nombre == recetaNombre);

            var tipoProductoNombre = dato.GetProperty("TipoProductoNombre").GetString();
            var tipoProducto = tiposProducto.First(t => t.Nombre == tipoProductoNombre);

            var producto = new Producto
            {
                Nombre = dato.GetProperty("Nombre").GetString()!,
                Descripcion = dato.GetProperty("Descripcion").ValueKind == JsonValueKind.Null
                    ? null
                    : dato.GetProperty("Descripcion").GetString(),
                PorcentajeDeGanancia = dato.GetProperty("PorcentajeDeGanancia").GetDecimal(),
                RecetaId = receta.Id!,
                TipoProducto = tipoProducto
            };

            await service.CreateAsync(producto);
            productos.Add(producto);
        }

        return productos;
    }

    // ------------------------------------------------------------------
    // Venta: referencia MostradorId -> resolver por "MostradorNombre" del JSON
    // ------------------------------------------------------------------
    private static async Task<List<Venta>> SeedVentasAsync(
        List<Mostrador> mostradores, VentaService service)
    {
        var json = await File.ReadAllTextAsync("Seeds/ventas.json");
        var datos = JsonSerializer.Deserialize<List<JsonElement>>(json)!;
        var ventas = new List<Venta>();

        foreach (var dato in datos)
        {
            var mostradorNombre = dato.GetProperty("MostradorNombre").GetString();
            var mostrador = mostradores.First(m => m.Nombre == mostradorNombre);

            var venta = new Venta
            {
                FechaDeVenta = dato.GetProperty("FechaDeVenta").GetDateTime(),
                MostradorId = mostrador.Id!
            };

            await service.CreateAsync(venta);
            ventas.Add(venta);
        }

        return ventas;
    }

    // ------------------------------------------------------------------
    // DetalleVenta: vive embebido dentro de Venta.Detalles.
    // Resuelve la Venta por posición (VentaIndex) y el Producto por nombre,
    // y lo agrega con Push.
    // ------------------------------------------------------------------
    private static async Task SeedDetallesVentaAsync(
        List<Venta> ventas, List<Producto> productos, VentaService service)
    {
        var json = await File.ReadAllTextAsync("Seeds/detallesVenta.json");
        var datos = JsonSerializer.Deserialize<List<JsonElement>>(json)!;

        foreach (var dato in datos)
        {
            var venta = ventas[dato.GetProperty("VentaIndex").GetInt32()];

            var productoNombre = dato.GetProperty("ProductoNombre").GetString();
            var producto = productos.First(p => p.Nombre == productoNombre);

            var detalle = new DetalleVenta
            {
                Cantidad = dato.GetProperty("Cantidad").GetInt32(),
                Producto = producto
            };

            await service.AddDetalleAsync(venta.Id!, detalle);
        }
    }
}