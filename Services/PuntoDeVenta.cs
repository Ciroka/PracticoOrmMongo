// Services/PuntoDeVentaService.cs
using MongoDB.Driver;
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