using MongoDB.Driver;
using PracticoOrmMongo.Models;
using PracticoOrmMongo.Services;

namespace PracticoOrmMongo.Seeds;

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