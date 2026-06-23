// Services/VentaService.cs
using MongoDB.Driver;
using PracticoOrmMongo.Models;
using PracticoOrmMongo.Settings;
using Microsoft.Extensions.Options;

namespace PracticoOrmMongo.Services;

public class VentaService
{
    private readonly IMongoCollection<Venta> _collection;

    public VentaService(IMongoClient client, IOptions<MongoDbSettings> settings)
    {
        var db = client.GetDatabase(settings.Value.DatabaseName);
        _collection = db.GetCollection<Venta>("ventas");
    }

    public async Task<List<Venta>> GetAllAsync() =>
        await _collection.Find(_ => true).ToListAsync();

    public async Task<Venta?> GetByIdAsync(string id) =>
        await _collection.Find(v => v.Id == id).FirstOrDefaultAsync();

    public async Task<List<Venta>> GetByMostradorAsync(string mostradorId) =>
        await _collection.Find(v => v.MostradorId == mostradorId).ToListAsync();

    public async Task CreateAsync(Venta venta) =>
        await _collection.InsertOneAsync(venta);

    public async Task UpdateAsync(string id, Venta updated) =>
        await _collection.ReplaceOneAsync(v => v.Id == id, updated);

    public async Task DeleteAsync(string id) =>
        await _collection.DeleteOneAsync(v => v.Id == id);

    // Agregar un detalle a la venta
    public async Task AddDetalleAsync(string ventaId, DetalleVenta detalle)
    {
        var update = Builders<Venta>.Update.Push(v => v.Detalles, detalle);
        await _collection.UpdateOneAsync(v => v.Id == ventaId, update);
    }

    // Reemplazar todos los detalles
    public async Task UpdateDetallesAsync(string ventaId, List<DetalleVenta> detalles)
    {
        var update = Builders<Venta>.Update.Set(v => v.Detalles, detalles);
        await _collection.UpdateOneAsync(v => v.Id == ventaId, update);
    }
}