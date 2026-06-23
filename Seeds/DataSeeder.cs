using MongoDB.Driver;
using PracticoOrmMongo.Models;
using PracticoOrmMongo.Services;

namespace PracticoOrmMongo.Seeds;

public class DataSeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        var puntoDeVentaService  = services.GetRequiredService<PuntoDeVentaService>();
        var mostradorService     = services.GetRequiredService<MostradorService>();
        var ingredienteService   = services.GetRequiredService<IngredienteService>();
        var tipoProductoService  = services.GetRequiredService<TipoProductoService>();
        var recetaService        = services.GetRequiredService<RecetaService>();
        var productoService      = services.GetRequiredService<ProductoService>();
        var ventaService         = services.GetRequiredService<VentaService>();
    }
}